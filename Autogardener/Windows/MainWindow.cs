using Autogardener.Model;
using Autogardener.Model.Plots;
using Autogardener.Modules;
using Dalamud.Plugin.Services;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;

using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Autogardener.Windows;

public class MainWindow : PluginWindowBase, IDisposable
{
    private UtilOld oldUtil;
    private PlotWatcher plotWatcher;
    private GlobalData globalData;
    private Commands commands;
    private PlayerActions playerActions;
    private ISaveManager<CharacterSaveState> saveManager;
    private ITextureProvider textureProvider;
    private static readonly Vector4 LightGreen = new Vector4(0.769f, 0.9f, 0.6f, 1);
    private static readonly Vector4 MidLightGreen = new Vector4(0.58f, 0.75f, 0.37f, 1);
    private static readonly Vector4 NeutralGreen = new Vector4(0.42f, 0.6f, 0.2f, 1);
    private static readonly Vector4 MidDarkGreen = new Vector4(0.278f, 0.455f, 0.075f, 1);
    private static readonly Vector4 DarkGreen = new Vector4(0.161f, 0.302f, 0, 1);
    private static readonly Vector4 NeutralBrown = new Vector4(0.651f, 0.49f, 0.196f, 1);
    private static readonly Vector4 MidDarkBrown = new Vector4(0.494f, 0.341f, 0.067f, 1);
    private readonly string scarecrowPicturePath;

    public MainWindow(ILogService logService, IServiceProvider serviceProvider, string scarecrowPicturePath)
        : base(logService, "Autogardener", ImGuiWindowFlags.AlwaysAutoResize)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        oldUtil = serviceProvider.GetRequiredService<UtilOld>();
        plotWatcher = serviceProvider.GetRequiredService<PlotWatcher>();
        globalData = serviceProvider.GetRequiredService<GlobalData>();
        commands = serviceProvider.GetRequiredService<Commands>();
        playerActions = serviceProvider.GetRequiredService<PlayerActions>();
        saveManager = serviceProvider.GetRequiredService<ISaveManager<CharacterSaveState>>();
        textureProvider = serviceProvider.GetRequiredService<ITextureProvider>();
        this.scarecrowPicturePath = scarecrowPicturePath;
    }

    public void Dispose()
    { }
    private string plotName;
    private int currentPlot = 0;

    protected override unsafe void SafeDraw()
    {        
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            var save = saveManager.GetCharacterSaveInMemory();
            if (ImGui.BeginTabItem("Plots"))
            {
                
                var nearestPlot = playerActions.GetNearestPlot();
                if (nearestPlot != null)
                {
                    ImGui.TextUnformatted("Nearest plot:");
                    ImGui.TextColored(NeutralGreen, nearestPlot.Alias);
                    if (ImGui.Button("Scan"))
                    {
                        playerActions.RegisterNearestPlot();
                        currentPlot = Math.Max(0, save.Plots.IndexOf(p => p.Id == playerActions.GetNearestPlot()?.Id));
                    }
                }

                if (save.Plots.Any())
                {
                    ImGui.Combo("Plot", ref currentPlot, save.Plots.Select(p => p.Alias).ToArray(), save.Plots.Count);
                    var plot = save.Plots[currentPlot];
                    plotName = plot.Alias;
                    if(ImGui.InputText("Rename", ref plotName, 40))
                    {
                        plot.Alias = plotName; saveManager.WriteSave(save);
                    }

                    int[][] displayLayout = [
                        [7, 6, 5],
                        [0, 9 ,4],
                        [1, 2, 3]];
                    foreach (int[] row in displayLayout)
                    {
                        foreach (int index in row){
                            if (index == 9) {
                                DrawCenterHole();
                            }
                            else
                            {
                                DrawPlotHoleStatus(plot.PlantingHoles[index], (uint)index);
                            }
                                
                            ImGui.SameLine();
                        }
                        ImGui.NewLine();                        
                    }
    
                    plotWatcher.HighlightPlots();
                    
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Designs"))
            {
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Other"))
            {
                DrawAssortedActions();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawPlotHoleStatus(PlotHole hole, uint index)
    {
        //ImGui.PushItemWidth(200);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.BeginChildFrame(index, new Vector2(200, 200));
        ImGui.TextColored(LightGreen, globalData.GetSeedStringName(hole.CurrentSeed));        
        ImGui.TextColored(NeutralBrown, globalData.GetSoilStringName(hole.CurrentSoil));
        ImGui.TextColored(NeutralGreen, $"Last tended: {GetHumanizedTimeElapsed(hole.LastTendedUtc)}");
        ImGui.TextColored(MidDarkGreen, $"Last fertilized: {GetHumanizedTimeElapsed(hole.LastFertilizedUtc)}");
        
        if (hole.Design != null)
        {
            ImGui.Separator();
            ImGui.TextUnformatted("Plan:");
            ImGui.TextColored(MidLightGreen, globalData.GetSeedStringName(hole.Design.DesignatedSeed));
            ImGui.TextColored(MidDarkBrown, globalData.GetSoilStringName(hole.Design.DesignatedSoil));
            ImGui.TextColored(NeutralGreen, $"Harvest: {(hole.Design.DoNotHarvest ? "Keep grown" : "Yes")}");
        }
        ImGui.PopStyleVar();
        ImGui.EndChildFrame();
        //ImGui.PopItemWidth();
    }

    private void DrawCenterHole()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.BeginChildFrame(9, new Vector2(200, 200));
        var scarecrowPic = textureProvider.GetFromFile(scarecrowPicturePath).GetWrapOrDefault();
        if (scarecrowPic != null)
        {
            ImGui.Image(scarecrowPic.ImGuiHandle, new Vector2(scarecrowPic.Width, scarecrowPic.Height));
        }
        ImGui.PopStyleVar();
        ImGui.EndChildFrame();
    }

    private string GetHumanizedTimeElapsed(DateTime? dateTime)
    {        
        if (dateTime == null || dateTime == DateTime.MinValue){
            return "Never";
        }

        TimeSpan timeSpan = (dateTime ?? DateTime.UtcNow) - DateTime.UtcNow;
        return timeSpan.Humanize();
    }
    private unsafe void DrawAssortedActions()
    {
        DrawActionButton(() => oldUtil.DescribeTarget(), "Describe target");
        DrawActionButton(() => commands.InteractWithTargetPlot(), "Interact with plot");
        DrawActionButton(() => oldUtil.ListCurrentMenuOptions(), "List current menu options");
        DrawActionButton(() => oldUtil.SelectEntry("Quit"), "Select Quit");
        DrawActionButton(() => oldUtil.SelectEntry("Harvest Crop"), "Select Harvest Crop");
        DrawActionButton(() => oldUtil.SelectEntry("Plant Seeds"), "Select Plant Seeds");
        DrawActionButton(() => oldUtil.TryDetectGardeningWindow(out var _), "Detect gardening window");
        DrawActionButton(() => oldUtil.GetTextButtonText(), "Click cancel in gardening window");
        DrawActionButton(() => oldUtil.GetSoilDragAndDropEntries(), "Get soil entries");
        //DrawActionButton(() => commands.UseFishmeal(), "Fertilize");
        DrawActionButton(() => oldUtil.UseItem(15865), "Use Firelight Seeds");
        DrawActionButton(() => oldUtil.EnumerateInventory(), "Enumerate inventory");
        DrawActionButton(() => oldUtil.ClickFertilizer(), "Click fertilizer");
        ImGui.Separator();
        DrawActionButton(() => commands.FullPlantSeedsInteraction(), "Execute interaction");
        DrawActionButton(() => commands.SkipDialogueIfNeeded(), "SkipDialogue");
        DrawActionButton(() => commands.SelectActionString("Plant seeds"), "Select plant seeds");
        DrawActionButton(() => commands.SeedPlot(), "Fill seeds and soil");
        DrawActionButton(() => commands.ClickConfirmOnHousingGardening(), "Confirm plant seeds");
        DrawActionButton(() => commands.ConfirmYes(), "Click yes on dialog");
        ImGui.Separator();
        DrawActionButton(() => commands.Fertilize(), "Fertilize");
        DrawActionButton(() => plotWatcher.ListNearbyPlots(), "List nearby plots");
        DrawActionButton(() => plotWatcher.UpdatePlotList(), "Scan for plots");
        DrawActionButton(() => plotWatcher.ToggleDrawHighlights(), "Toggle display highlights");
        DrawActionButton(() => logService.Info(globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Purple)), "GetLocalizedString");
        plotWatcher.HighlightPlots();
    }
}

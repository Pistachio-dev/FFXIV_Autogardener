using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Autogardener.Modules;
using Dalamud.Interface;
using Dalamud.Interface.Components;
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
    private IFramework framework;

    private uint[] seedIds;
    private string[] seedNames;
    private uint[] soilIds;
    private string[] soilNames;

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
        GenerateOrderedCollection(globalData.Seeds, out seedIds, out seedNames);
        GenerateOrderedCollection(globalData.Soils, out soilIds, out soilNames);
        framework = serviceProvider.GetRequiredService<IFramework>();
        framework.RunOnFrameworkThread(() =>
        {
            saveManager.GetCharacterSaveInMemory();
        });
        
    }
    private void GenerateOrderedCollection(Dictionary<uint, Lumina.Excel.Sheets.Item> original, out uint[] idArray, out string[] nameArray)
    {
        IEnumerable<(uint id, string name)> orderedEnum = original.Select(e => (e.Key, e.Value.Name.ToString())).OrderBy(t => t.Item2);
        idArray = new uint[original.Count];
        nameArray = new string[original.Count];
        int i = 0;
        foreach (var tuple in orderedEnum)
        {
            idArray[i] = tuple.id;
            nameArray[i] = tuple.name;
            i++;
        }
    }
    public void Dispose()
    { }
    private string plotName;
    private int currentPlot = 0;
    private int currentDesign = 0;

    private bool toggleRenamePlot;
    private bool toggleRenameDesign;
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
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
                    {
                        toggleRenamePlot = !toggleRenamePlot;
                    }
                    DrawTooltip("Rename");
                    if (toggleRenamePlot)
                    {
                        if (ImGui.InputText("New name", ref plotName, 40))
                        {
                            plot.Alias = plotName; saveManager.WriteSave(save);
                        }
                    }

                    if (plot.PlantingHoles.Count == 1)
                    {
                        DrawPlotHoleStatus(plot.PlantingHoles[0], 0);
                    }
                    else
                    {
                        int[][] displayLayout = [
                            [7, 6, 5],
                            [0, 9 ,4],
                            [1, 2, 3]];
                        foreach (int[] row in displayLayout)
                        {
                            foreach (int index in row)
                            {
                                if (index == 9)
                                {
                                    DrawCenterHole();
                                }
                                else
                                {
                                    if (index >= plot.PlantingHoles.Count)
                                    {
                                        logService.Warning($"Planting hole index {index} is out of bounds");
                                    }
                                    else
                                    {
                                        DrawPlotHoleStatus(plot.PlantingHoles[index], (uint)index);
                                    }
                                }

                                ImGui.SameLine();
                            }
                            ImGui.NewLine();
                        }
                    }

                    plotWatcher.HighlightPlots();

                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Designs"))
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PaintBrush, "Create new design for nearest plot")) {
                    var newIndex = playerActions.CreateNewDesign();
                }
                //logService.Info(save.Designs.Count.ToString());
                if (save.Designs.Count > 0)
                {
                    ImGui.Combo("Design", ref currentDesign, save.Designs.Select(d => d.PlanName).ToArray(), save.Designs.Count);
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
                    {
                        toggleRenameDesign = !toggleRenameDesign;
                    }
                    DrawTooltip("Rename");
                    string designName = save.Designs[currentDesign].PlanName;
                    if (toggleRenameDesign) {
                        if (ImGui.InputText("New name", ref designName, 40))
                        {
                            save.Designs[currentDesign].PlanName = designName;
                            saveManager.WriteCharacterSave();
                        }
                    }
                    if (save.Designs[currentDesign].PlotHolePlans.Count == 1)
                    {
                        DrawPlotHoleDesign(save.Designs[currentDesign].PlotHolePlans[0], 0);
                    }
                    else
                    {
                        int[][] displayLayout = [
                                                [7, 6, 5],
                                                [0, 9 ,4],
                                                [1, 2, 3]];
                        foreach (int[] row in displayLayout)
                        {
                            foreach (int index in row)
                            {
                                if (index == 9)
                                {
                                    DrawCenterHole();
                                }
                                else
                                {
                                    DrawPlotHoleDesign(save.Designs[currentDesign].PlotHolePlans[index], (uint)index);
                                }

                                ImGui.SameLine();
                            }
                            ImGui.NewLine();
                        }
                    }


                }


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
        ImGui.PushItemWidth(200);
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
        ImGui.EndChildFrame();
        ImGui.PopStyleVar();        
        ImGui.PopItemWidth();
    }

    private void DrawPlotHoleDesign(PlotHolePlan design, uint index)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
        ImGui.BeginChildFrame(100 + index, new Vector2(200, 200));
        var seedComboIndex = design.DesignatedSeed == 0 ? 0 : seedIds.IndexOf(design.DesignatedSeed);
        if (ImGui.Combo($"Seed##design{index}", ref seedComboIndex, seedNames, seedNames.Length))
        {
            design.DesignatedSeed = seedIds[seedComboIndex];
            saveManager.WriteCharacterSave();
        }

        var soilComboIndex = design.DesignatedSoil == 0 ? 0 : soilIds.IndexOf(design.DesignatedSoil);
        if (ImGui.Combo($"Soil##design{index}", ref soilComboIndex, soilNames, soilNames.Length))
        {
            design.DesignatedSoil = soilIds[soilComboIndex];
            saveManager.WriteCharacterSave();
        }

        bool keepWhenGrown = design.DoNotHarvest;
        if (ImGui.Checkbox($"Keep when grown##{index}", ref keepWhenGrown))
        {
            design.DoNotHarvest = keepWhenGrown;
            saveManager.WriteCharacterSave();
        }
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("For many interbreeding setups, you need a seed to be present,\n" +
            "but gain nothing from harvesting it,\nso it's easier to let it stay fully grown");

        ImGui.EndChildFrame();
        ImGui.PopStyleVar();
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
        
        ImGui.EndChildFrame();
        ImGui.PopStyleVar();
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

using Autogardener.Modules;
using DalamudBasics.GUI.Windows;
using DalamudBasics.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Autogardener.Windows;

public class MainWindow : PluginWindowBase, IDisposable
{
    private UtilOld oldUtil;
    private PlotWatcher plotWatcher;
    private GlobalData globalData;
    private Commands commands;
    private PlayerActions playerActions;

    public MainWindow(ILogService logService, IServiceProvider serviceProvider)
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
    }

    public void Dispose()
    { }

    protected override unsafe void SafeDraw()
    {
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            if (ImGui.BeginTabItem("Plots"))
            {
                if (ImGui.Button("Register nearest plot"))
                {
                    playerActions.RegisterNearestPlot();
                }
                plotWatcher.HighlightPlots();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Designs"))
            {
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
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

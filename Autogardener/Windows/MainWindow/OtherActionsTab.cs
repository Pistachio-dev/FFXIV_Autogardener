using Autogardener.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
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
            DrawActionButton(() => commands.SkipDialogueIfNeeded(), "SkipDialogue");
            DrawActionButton(() => commands.SelectActionString("Plant seeds"), "Select plant seeds");
            DrawActionButton(() => commands.ClickConfirmOnHousingGardeningAddon(), "Confirm plant seeds");
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
}

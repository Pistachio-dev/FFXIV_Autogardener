using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Humanizer;
using System.Linq;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private string plotName;
        private int currentPlot = 0;
        private bool toggleRenamePlot;


        private void DrawPlotsTab(CharacterSaveState save)
        {
            var nearestPlot = storedDataActions.GetNearestTrackedPlot(false);
            if (nearestPlot != null)
            {
                ImGui.TextUnformatted("Nearest plot:");
                ImGui.SameLine();
                ImGui.TextColored(NeutralGreen, nearestPlot.Alias);
                DrawTendButtonAndParameters(nearestPlot);
            }
            else
            {
                ImGui.TextUnformatted("Too far away from any registered plot");
            }

            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Search, "Scan nearest plot", Blue))
            {
                storedDataActions.RegisterNearestPlot();
                currentPlot = Math.Max(0, save.Plots.IndexOf(p => p.Id == storedDataActions.GetNearestTrackedPlot(false)?.Id));
                DrawTooltip("Will check and save the plants of a plot. You need to be on it. It can't read every type of plant, stuff is weird sometimes.");
            }

            if (save.Plots.Any())
            {
                ImGui.Combo("Plot", ref currentPlot, save.Plots.Select(p => p.Alias).ToArray(), save.Plots.Count);
                var plot = save.Plots[currentPlot];
                DrawPlotRenameButton(plot);
                if (DrawForgetPlotButton(save, plot))
                {
                    return;
                }

                DrawDesignSelector(plot, save);
                if (plot.AppliedDesign?.Plan != null)
                {
                    DrawDesignItems(plot.AppliedDesign.Plan);
                }
                
                DrawCurrentPlot(save.Plots[currentPlot]);

                plotWatcher.HighlightPlots();
            }
        }
        private void DrawTendButtonAndParameters(Plot nearestPlot)
        {
            var config = configService.GetConfiguration();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Leaf, "Tend to plot", MidDarkGreen, DarkGreen, MidDarkGreen))
            {
                inGameActions.PlotPatchCare(nearestPlot, config.UseFertilizer, config.Replant);
                //inGameActions.TargetPlantingHoleCare(nearestPlot, config.UseFertilizer, config.Replant);
            }
            DrawTooltip("Will fertilize, tend, harvest and also plant based on the plan selected.\nIt will never remove a crop that is not harvestable.");
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Gun, "Abort", Red))
            {
                gtm.Abort();
            }
            DrawTooltip("Cancel any actions running or scheduled");

            bool useFertilizer = config.UseFertilizer;
            if (ImGui.Checkbox("Use fertilizer", ref useFertilizer))
            {
                config.UseFertilizer = useFertilizer;
                configService.SaveConfiguration();
            }
            DrawTooltip("Will use fertilizer if available before tending to de crop");
            bool rePlant = config.Replant;
            if (ImGui.Checkbox("Replant after harvest", ref rePlant))
            {
                config.Replant = rePlant;
                configService.SaveConfiguration();
            }
            DrawTooltip("After harvesting a plant, will plant the seeds given in the design.");

        }

        private void DrawDesignItems(PlotPlan plotPlan)
        {
            var checkResult = storedDataActions.CheckResourceAvailability(plotPlan, true);
            foreach (var item in checkResult.Entries)
            {
                var color = item.ActualAmount >= item.ExpectedAmount ? MidDarkGreen : Red;
                ImGui.TextColored(color, $"{item.ItemName} {item.ActualAmount}/{item.ExpectedAmount}");
                ImGui.SameLine();
            }

            ImGui.NewLine();
        }

        private void DrawDesignSelector(Plot plot, CharacterSaveState save)
        {
            var designForCurrentPlot = GetCurrentDesignNumber(plot, save);
            var designNames = save.Designs.Select(p => p.PlanName).ToArray();
            if (ImGui.Combo("Plan", ref designForCurrentPlot, designNames, designNames.Length))
            {
                if (!storedDataActions.ApplyDesign(ref plot, save.Designs[designForCurrentPlot]))
                {
                    return;
                }
                plot.AppliedDesign = new AppliedPlotPlan(save.Designs[designForCurrentPlot]);
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
            }
        }

        private void DrawPlotRenameButton(Plot plot)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
            {
                toggleRenamePlot = !toggleRenamePlot;
            }
            DrawTooltip("Rename");
            if (toggleRenamePlot)
            {
                var plotName = plot.Alias;
                if (ImGui.InputText("New name", ref plotName, 40))
                {
                    plot.Alias = plotName; saveManager.WriteCharacterSave();
                }
            }
        }
        private bool DrawForgetPlotButton(CharacterSaveState save, Plot plot)
        {
            ImGui.SameLine();
            bool buttonsPressed = ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl;

            if (ImGuiComponents.IconButton(FontAwesomeIcon.SquareXmark, Red) && buttonsPressed)
            {
                save.Plots.Remove(plot);
                currentPlot = 0;
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
                return true;
            }
            
            DrawTooltip("Ctrl+Shift to forget this plot\n(The plugin will stop tracking it)");

            return false;

        }

        private void DrawCurrentPlot(Plot plot)
        {
            if (plot.PlantingHoles.Count == 0)
            {
                ImGui.TextUnformatted("This plot has no planting slots, somehow. This is strange.");
                return;
            }
            int[][] displayLayout = GetPlotLayout(plot.PlantingHoles.Count);
            foreach (var row in displayLayout)
            {
                foreach (var index in row)
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

        private int GetCurrentDesignNumber(Plot plot, CharacterSaveState state)
        {
            if (plot.AppliedDesign == null)
            {
                return 0; // None
            }

            var designId = plot.AppliedDesign.Plan.Id;
            int index = state.Designs.IndexOf(p => p.Id == designId);
            if (index == -1) { return 0; }
            return index;
        }

        private string GetHumanizedTimeElapsed(DateTime? dateTime)
        {
            if (dateTime == null || dateTime == DateTime.MinValue)
            {
                return "Never";
            }

            var timeSpan = (dateTime ?? DateTime.UtcNow) - DateTime.UtcNow;
            return timeSpan.Humanize();
        }
    }
}

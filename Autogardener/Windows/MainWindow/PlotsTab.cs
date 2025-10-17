using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Autogardener.Modules.Schedulers;
using Dalamud.Bindings.ImGui;
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
            var nearestPlot = GetNearestPlotThrottled();
            if (nearestPlot != null)
            {
                ImGui.TextUnformatted("Nearest plot:");
                ImGui.SameLine();
                ImGui.TextColored(NeutralGreen, nearestPlot.Name);
                DrawTendButtonAndParameters(nearestPlot);
            }
            else
            {
                ImGui.TextUnformatted("Too far away from any registered plot");
            }
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Search, "Scan nearby plots", Blue))
            {
                storedDataActions.RegisterNearestPlotPatch();
                currentPlot = Math.Max(0, save.Plots.IndexOf(p => p.Id == storedDataActions.GetNearestTrackedPlotPatch(false)?.Id));
                DrawTooltip("Will check and save the plants of a plot. You need to be on it. It can't read every type of plant, stuff is weird sometimes.");
            }

            if (save.Plots.Any())
            {
                if (!toggleRenamePlot)
                {
                    ImGui.Combo("Plot", ref currentPlot, save.Plots.Select(p => p.Name).ToArray(), save.Plots.Count);
                }
                
                var plot = save.Plots[currentPlot];
                DrawPlotRenameButton(plot);
                if (DrawForgetPlotButton(save, plot))
                {
                    return;
                }
                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Flag, MidLightGreen))
                {
                    plotWatcher.FlagPlot(plot);
                }
                DrawTooltip("Mark the position of the plot in the map, if it's here." +
                    "If you moved the plot, forget it (red button) and scan again");
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("If \"Tend to plot\" says the plot is too far away, but you're on it, try removing it and scanning it again. " +
                    "Don't forget to reselect the design. Apologies for the jank, this one bug is tricky.", FontAwesomeIcon.ClipboardQuestion);

                DrawDesignSelector(plot, save);
                ImGuiComponents.HelpMarker("If you made changes to the design and they don't show, try reselecting it.");
                if (plot.AppliedDesign?.Design != null)
                {
                    DrawDesignItems(plot.AppliedDesign.Design);
                }                
                
                DrawCurrentPlot(save.Plots[currentPlot]);

                plotWatcher.HighlightPlots();
            }
        }
        private void DrawTendButtonAndParameters(PlotPatch nearestPlot)
        {
            var config = configService.GetConfiguration();

            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Leaf, "Tend to plot", MidDarkGreen, DarkGreen, MidDarkGreen))
            {
                hlScheduler.SchedulePatchTend(nearestPlot);
            }
            DrawTooltip("Will fertilize, tend, harvest and also plant based on the plan selected.\nIt will never remove a crop that is not harvestable.");

            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Gun, "Abort", Red))
            {
                hlScheduler.Abort(AbortReason.UserRequest);
            }
            DrawTooltip("Cancel any actions running or scheduled");

            bool harvest = config.Harvest;
            if (ImGui.Checkbox("Harvest unless marked", ref harvest))
            {
                config.Harvest = harvest;
                configService.SaveConfiguration();
            }
            DrawTooltip("Will harvest grown plants unless marked as \"Do not harvest\" on the design.");
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

        private void DrawDesignItems(PlotPatchDesign plotPlan)
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

        private void DrawDesignSelector(PlotPatch plot, CharacterSaveState save)
        {
            var designForCurrentPlot = GetCurrentDesignNumber(plot, save);
            var designNames = save.Designs.Select(p => p.Name).ToArray();
            if (ImGui.Combo("Design", ref designForCurrentPlot, designNames, designNames.Length))
            {
                if (!storedDataActions.ApplyDesign(ref plot, save.Designs[designForCurrentPlot]))
                {
                    return;
                }
                plot.AppliedDesign = new AppliedPlotPatchDesign(save.Designs[designForCurrentPlot]);
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
            }
        }

        private void DrawPlotRenameButton(PlotPatch plot)
        {
            if (!toggleRenamePlot) { ImGui.SameLine(); }
            else
            {
                var plotName = plot.Name;
                if (ImGui.InputText("New name", ref plotName, 40))
                {
                    plot.Name = plotName; saveManager.WriteCharacterSave();
                }
                ImGui.SameLine();
            }
            if (ImGuiComponents.IconButton(toggleRenameDesign ? FontAwesomeIcon.Save : FontAwesomeIcon.Pen, Blue))
            {
                toggleRenamePlot = !toggleRenamePlot;
            }
            DrawTooltip(toggleRenamePlot ? "Save" : "Rename");

        }
        private bool DrawForgetPlotButton(CharacterSaveState save, PlotPatch plot)
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

        private void DrawCurrentPlot(PlotPatch patch)
        {
            if (patch.Plots.Count == 0)
            {
                ImGui.TextUnformatted("This plot has no planting slots, somehow. This is strange.");
                return;
            }
            int[][] displayLayout = GetPlotLayout(patch.Plots.Count);
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
                        if (index > patch.Plots.Count)
                        {
                            logService.Warning($"Planting hole index {index} is out of bounds");
                            return;
                        }
                        else
                        {
                            DrawPlotStatus(patch, patch.Plots[index], (uint)index);
                        }
                    }

                    ImGui.SameLine();
                }
                ImGui.NewLine();
            }           
        }

        private void DrawPlotStatus(PlotPatch patch, Plot plot, uint index)
        {
            ImGui.PushItemWidth(200);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1f);
            ImGui.BeginChildFrame(index, new Vector2(200, 200));
            ImGui.TextColored(LightGreen, globalData.GetSeedStringName(plot.CurrentSeed));
            ImGui.TextColored(NeutralBrown, globalData.GetSoilStringName(plot.CurrentSoil));
            ImGui.TextColored(NeutralGreen, $"Last tended: {GetHumanizedTimeElapsed(plot.LastTendedUtc)}");
            ImGui.TextColored(MidDarkGreen, $"Last fertilized: {GetHumanizedTimeElapsed(plot.LastFertilizedUtc)}");

            PlotDesign? design = patch.Design(plot);
            if (design != null)
            {
                ImGui.Separator();
                ImGui.TextUnformatted("Plan:");
                ImGui.TextColored(MidLightGreen, globalData.GetSeedStringName(design.DesignatedSeed));
                ImGui.TextColored(MidDarkBrown, globalData.GetSoilStringName(design.DesignatedSoil));
                ImGui.TextColored(NeutralGreen, $"Harvest: {(design.DoNotHarvest ? "Keep grown" : "Yes")}");
            }
            ImGui.EndChildFrame();
            ImGui.PopStyleVar();
            ImGui.PopItemWidth();
        }

        private int GetCurrentDesignNumber(PlotPatch plot, CharacterSaveState state)
        {
            if (plot.AppliedDesign == null)
            {
                return 0; // None
            }

            var designId = plot.AppliedDesign.Design.Id;
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

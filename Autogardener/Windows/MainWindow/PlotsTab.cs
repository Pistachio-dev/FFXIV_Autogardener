using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private string plotName;
        private int currentPlot = 0;
        private bool toggleRenamePlot;

        private void DrawPlotsTab(CharacterSaveState save)
        {
            if (save.BlackList.Any() && !save.Plots.Any())
            {
                DrawClearBlackListButton(save.BlackList);
                return;
            }

            var nearestPlot = playerActions.GetNearestPlot();
            if (nearestPlot != null)
            {
                ImGui.TextUnformatted("Nearest plot:");
                ImGui.TextColored(NeutralGreen, nearestPlot.Alias);
                if (ImGui.Button("Scan the plot you're on"))
                {
                    playerActions.RegisterNearestPlot();
                    currentPlot = Math.Max(0, save.Plots.IndexOf(p => p.Id == playerActions.GetNearestPlot()?.Id));
                }
                DrawTooltip("Will check and save the plants of a plot. You need to be on it.");
            }

            if (save.Plots.Any())
            {
                ImGui.Combo("Plot", ref currentPlot, save.Plots.Select(p => p.Alias).ToArray(), save.Plots.Count);
                var plot = save.Plots[currentPlot];
                DrawPlotRenameButton(plot);
                if(DrawForgetPlotButton(save, plot))
                {
                    return;
                }
                ImGui.SameLine();
                DrawClearBlackListButton(save.BlackList);

                DrawDesignSelector(plot, save);

                DrawCurrentPlot(save.Plots[currentPlot]);            

                plotWatcher.HighlightPlots();
            }
        }

        private void DrawDesignSelector(Plot plot, CharacterSaveState save)
        {
            var designForCurrentPlot = GetCurrentDesignNumber(plot, save);
            var designNames = save.Designs.Select(p => p.PlanName).ToArray();
            if (ImGui.Combo("Plan", ref designForCurrentPlot, designNames, designNames.Length))
            {
                if (!playerActions.ApplyDesign(ref plot, save.Designs[designForCurrentPlot]))
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
                if (plot.PlantingHoles.Count > 0)
                {
                    save.BlackList.Add(plot.PlantingHoles[0].GameObjectId);
                }
                currentPlot = 0;
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
                return true;
            }
            
            DrawTooltip("Ctrl+Shift to blacklist this plot\n(The plugin will stop tracking it)");

            return false;

        }

        private void DrawClearBlackListButton(List<ulong> blacklist)
        {
            bool buttonsPressed = ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl;

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Binoculars, DarkGreen) && buttonsPressed)
            {
                blacklist.Clear();
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
            }

            DrawTooltip("Ctrl+Shift to clear the blacklist");
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
            return state.Designs.IndexOf(p => p.Id == designId);
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

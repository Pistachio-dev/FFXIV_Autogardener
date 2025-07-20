using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private bool toggleRenameDesign;
        private int currentDesign = 0;

        private void DrawDesignsTab(CharacterSaveState save)
        {
            var nearestPlot = playerActions.GetNearestTrackedPlot(false);
            if (nearestPlot == null)
            {
                ImGui.BeginDisabled();
            }
            string newDesignButtonText = nearestPlot == null
                ? "Create new design for nearest plot"
                : $"Create new design for {nearestPlot.Alias}";
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PaintBrush, newDesignButtonText))
            {
                currentDesign = playerActions.CreateNewDesign();
                return;
            }
            if (nearestPlot == null)
            {
                ImGui.EndDisabled();
            }
            //logService.Info(save.Designs.Count.ToString());
            if (save.Designs.Count > 1)
            {
                ImGui.Combo("Design", ref currentDesign, save.Designs.Select(d => d.PlanName).ToArray(), save.Designs.Count);
                DrawDesignRenameButton(save.Designs);
                if (DrawRemoveDesignButton(save.Designs, save.Designs[currentDesign]))
                {
                    return;
                }

                if (save.Designs[currentDesign].PlotHolePlans.Count == 0)
                {
                    ImGui.TextUnformatted("This design has no plots defined");
                    return;
                }

                int[][] displayLayout = GetPlotLayout(save.Designs[currentDesign].PlotHolePlans.Count);
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
                            DrawPlotHoleDesign(save.Designs[currentDesign].PlotHolePlans[index], (uint)index);
                        }

                        ImGui.SameLine();
                    }
                    ImGui.NewLine();
                }
            }
        }

        private void DrawDesignRenameButton(List<PlotPlan> designs)
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Pen))
            {
                toggleRenameDesign = !toggleRenameDesign;
            }
            DrawTooltip("Rename");
            var designName = designs[currentDesign].PlanName;
            if (toggleRenameDesign)
            {
                if (ImGui.InputText("New name", ref designName, 40))
                {
                    designs[currentDesign].PlanName = designName;
                    saveManager.WriteCharacterSave();
                }
            }
        }

        private bool DrawRemoveDesignButton(List<PlotPlan> designs, PlotPlan design)
        {
            ImGui.SameLine();
            bool buttonsPressed = ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl;

            if (ImGuiComponents.IconButton(FontAwesomeIcon.SquareXmark, Red) && buttonsPressed)
            {
                if (design == designs[0])
                {
                    chatGui.PrintError("Can't delete the default design.");
                    return false;
                }
                designs.Remove(design);
                currentDesign = 0;
                taskManager.Enqueue(() => saveManager.WriteCharacterSave());
                return true;
            }

            DrawTooltip("Ctrl+Shift to delete this design");
            return false;

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

            var keepWhenGrown = design.DoNotHarvest;
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
    }
}

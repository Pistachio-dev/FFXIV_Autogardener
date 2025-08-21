using Autogardener.Model;
using Autogardener.Model.Designs;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using System.Linq;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private bool toggleRenameDesign;
        private int currentDesign = 0;
        private uint[] seedIds;
        private string[] seedNames;
        private uint[] soilIds;
        private string[] soilNames;

        private void DrawDesignsTab(CharacterSaveState save)
        {
            UpdateSelectorData(configService.GetConfiguration().ShowOnlyItemsInInventory);            
            var nearestPlot = storedDataActions.GetNearestTrackedPlotPatch(false);
            if (nearestPlot == null)
            {
                ImGui.BeginDisabled();
            }
            string newDesignButtonText = nearestPlot == null
                ? "Create new design for nearest plot"
                : $"Create new design for {nearestPlot.Name}";
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.PaintBrush, newDesignButtonText, NeutralGreen))
            {
                currentDesign = storedDataActions.CreateNewDesign();
                return;
            }
            if (nearestPlot == null)
            {
                ImGui.EndDisabled();
            }
            //logService.Info(save.Designs.Count.ToString());
            if (save.Designs.Count > 1)
            {
                if (!toggleRenameDesign)
                {
                    ImGui.Combo("Design", ref currentDesign, save.Designs.Select(d => d.Name).ToArray(), save.Designs.Count);
                }
                
                DrawDesignRenameButton(save.Designs);
                var selectedDesign = save.Designs[currentDesign];
                if (DrawRemoveDesignButton(save.Designs, selectedDesign))
                {
                    return;
                }

                if (selectedDesign.PlotDesigns.Count == 0)
                {
                    ImGui.TextUnformatted("This design has no plots defined");
                    return;
                }

                bool showOnlyInInventory = configService.GetConfiguration().ShowOnlyItemsInInventory;
                if (ImGui.Checkbox("Show only items in inventory", ref showOnlyInInventory))
                {
                    var config = configService.GetConfiguration();
                    config.ShowOnlyItemsInInventory = showOnlyInInventory;
                    configService.SaveConfiguration();
                }

                int[][] displayLayout = GetPlotLayout(selectedDesign.PlotDesigns.Count);
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
                            DrawPlotDesign(selectedDesign.PlotDesigns[index], (uint)index, selectedDesign);
                        }

                        ImGui.SameLine();
                    }
                    ImGui.NewLine();
                }
            }
        }

        private void DrawDesignRenameButton(List<PlotPatchDesign> designs)
        {
            if (!toggleRenameDesign)
            {
                ImGui.SameLine();
            }
            else
            {
                var designName = designs[currentDesign].Name;
                if (toggleRenameDesign)
                {
                    if (ImGui.InputText("New name", ref designName, 40))
                    {
                        designs[currentDesign].Name = designName;
                        saveManager.WriteCharacterSave();
                    }
                    ImGui.SameLine();
                }
            }
            if (ImGuiComponents.IconButton(toggleRenameDesign ? FontAwesomeIcon.Save : FontAwesomeIcon.Pen, Blue))
            {
                toggleRenameDesign = !toggleRenameDesign;
            }
            DrawTooltip(toggleRenameDesign ? "Save" : "Rename");

        }

        private bool DrawRemoveDesignButton(List<PlotPatchDesign> designs, PlotPatchDesign design)
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
        private void DrawPlotDesign(PlotDesign design, uint index, PlotPatchDesign parentPatchDesign)
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

            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, "Copy to all plots"))
            {
                storedDataActions.PropagateDesign(design, parentPatchDesign);
            }
            ImGuiComponents.HelpMarker("Copy this setup for the other plots inside this patch");

            ImGui.EndChildFrame();
            ImGui.PopStyleVar();
        }

        private void UpdateSelectorData(bool showOnlyInPossession)
        {
            HashSet<uint>? filter = null;
            if (showOnlyInPossession)
            {
                filter = storedDataActions.GetItemIdsOfSoilsAndSeedsInInventory();
            }

            GenerateOrderedCollection(globalData.Seeds, out seedIds, out seedNames, filter);
            GenerateOrderedCollection(globalData.Soils, out soilIds, out soilNames, filter);
        }

        private void GenerateOrderedCollection(Dictionary<uint, Lumina.Excel.Sheets.Item> original,
                                                out uint[] idArray,
                                                out string[] nameArray,
                                                HashSet<uint>? filter = null)
        {
            Dictionary<uint, Lumina.Excel.Sheets.Item> filtered = filter == null
                ? original
                : original.Where(entry => filter.Contains(entry.Key)).ToDictionary();

            List<(uint id, string name)> orderedEnum = filtered.Select(e => (e.Key, e.Value.Name.ToString())).OrderBy(t => t.Item2).ToList();
            idArray = new uint[filtered.Count + 1];
            nameArray = new string[filtered.Count + 1];
            idArray[0] = 0;
            nameArray[0] = "None";
            for(int i = 1; i < idArray.Length; i++)                
            {
                idArray[i] = orderedEnum[i - 1].id;
                nameArray[i] = orderedEnum[i - 1].name;
            }
        }
    }
}

using Autogardener.Model;
using Autogardener.Model.ActionChains;
using Autogardener.Model.Plots;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using System.Linq;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private void DrawChainedActionsTab()
        {
            var save = saveManager.GetCharacterSaveInMemory();
            DrawChainAddButtons(save);
            DrawChainList(save);
            ImGui.EndTabItem();
        }

        public void DrawChainAddButtons(CharacterSaveState save)
        {
            if (save.Plots.Count == 0)
            {
                return;
            }
            if (ImGui.Button("Add plot destination"))
            {
                save.Actions.Add(new ChainedAction { Type = ChainedActionType.GoToPlot, PatchId = save.Plots.First().Id });
            }
            ImGuiComponents.HelpMarker("You will run to the plot and tend to it automatically");
            ImGui.SameLine();
            if (ImGui.Button("Add chat command"))
            {
                save.Actions.Add(new ChainedAction { Type = ChainedActionType.ExecuteCommand });
            }
            ImGuiComponents.HelpMarker("You can use plugins like Lifestream to move to other locations. This plugin will only move in a straight line to the plot");
        }

        public void DrawChainList(CharacterSaveState save)
        {
            for (int i = 0; i < save.Actions.Count; i++)
            {
                var action = save.Actions[i];
                if (action.Type == ChainedActionType.GoToPlot)
                {
                    DrawPlotsComboBox(save.Plots, action, i);
                }
                if (action.Type == ChainedActionType.ExecuteCommand)
                {
                    DrawCommandTexbox(action, i);
                }
                ImGui.SameLine();
                WriteDeleteActionButton(action, i);
            }
        }

        private void DrawPlotsComboBox(List<PlotPatch> patches, ChainedAction action, int index)
        {
            int currentPlot = patches.IndexOf(patch => patch.Id == action.PatchId);
            if (currentPlot == -1) { return; }
            if (ImGui.Combo($"Go to patch##{index}", ref currentPlot, patches.Select(p => p.Name).ToArray(), patches.Count))
            {
                action.PatchId = patches[currentPlot].Id;
                saveManager.WriteCharacterSave();
            }
        }

        private void DrawCommandTexbox(ChainedAction action, int index)
        {
            string command = action.Command;
            if (ImGui.InputText($"Chat command##{index}", ref command))
            {
                action.Command = command;
                saveManager.WriteCharacterSave();
            }
        }
        private void WriteDeleteActionButton(ChainedAction action, int index)
        {
            if (!ImGui.GetIO().KeyShift)
            {
                ImGui.BeginDisabled();
            }

            if (ImGuiComponents.IconButtonWithText(Dalamud.Interface.FontAwesomeIcon.Trash, $"##{index}", Red))
            {
                storedDataActions.RemoveAction(action.Id);
            }
            
            if (!ImGui.GetIO().KeyShift)
            {
                ImGui.EndDisabled();
            }
            DrawTooltip("Shift + Click to delete this action");
        }
    }
}

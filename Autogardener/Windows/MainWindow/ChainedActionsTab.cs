using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Windows.MainWindow
{
    public partial class MainWindow
    {
        private void DrawChainedActionsTab()
        {
            if (ImGui.Button("Move to plot"))
            {
                var location = saveManager.GetCharacterSaveInMemory().Plots.FirstOrDefault()?.Location ?? Vector3.Zero;
                if (location == Vector3.Zero)
                {
                    chatGui.PrintError("No plot selected");
                }
                movementController.MoveToPoint(location);
            }
            ImGui.EndTabItem();
        }
    }
}

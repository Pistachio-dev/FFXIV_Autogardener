using Autogardener.Model.ActionChains;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;

namespace Autogardener.Model
{
    public class CharacterSaveState
    {
        public List<PlotPatch> Plots { get; set; } = new();

        public List<PlotPatchDesign> Designs { get; set; } = new()
        {
            PlotPatchDesign.CreateEmptyWithSlots(8, "Default")
        };

        public List<ChainedAction> Actions { get; set; } = new();
    }
}

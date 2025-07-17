using Autogardener.Model.Designs;
using Autogardener.Model.Plots;

namespace Autogardener.Model
{
    public class CharacterSaveState
    {
        public List<Plot> Plots { get; set; } = new();

        public List<PlotPlan> Designs { get; set; } = new();
    }
}

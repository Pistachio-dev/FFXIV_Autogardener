using Autogardener.Model.Designs;
using System.Linq;

namespace Autogardener.Model.Plots
{
    public class Plot
    {
        public Plot(string alias)
        {
            Alias = alias;
        }

        public string DesignName => AppliedDesign?.PlanName ?? "Unassigned plan";
        public PlotPlan AppliedDesign { get; set; } = null;

        public string Alias { get; set; }

        public List<PlotHole> PlantingHoles { get; set; } = new();

        public Vector3 Location => PlantingHoles.FirstOrDefault()?.Location ?? Vector3.Zero;
    }
}

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

        public Guid Id { get; set; } = Guid.NewGuid();

        public string DesignName => AppliedDesign?.Plan.PlanName ?? "Unassigned plan";
        public AppliedPlotPlan? AppliedDesign { get; set; } = null;

        public string Alias { get; set; }

        public List<PlotHole> PlantingHoles { get; set; } = new();

        public Vector3 Location => PlantingHoles.FirstOrDefault()?.Location ?? Vector3.Zero;

        public override bool Equals(object? otherPlotObject)
        {
            if (otherPlotObject == null || !(otherPlotObject is Plot otherPlot))
            {
                return false; ;
            }
            if (!(otherPlot.PlantingHoles.Count == PlantingHoles.Count))
            {
                return false;
            }

            ulong lowerGameObjectId = PlantingHoles.Select(p => p.GameObjectId).OrderBy(goid => goid).First();
            ulong otherPlotLowerGameObjectId = otherPlot.PlantingHoles.Select(p => p.GameObjectId).OrderBy(goid => goid).First();
            return lowerGameObjectId == otherPlotLowerGameObjectId;
        }
    }
}

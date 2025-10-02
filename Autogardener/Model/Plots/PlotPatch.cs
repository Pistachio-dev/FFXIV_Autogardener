using Autogardener.Model.Designs;
using System.Linq;

namespace Autogardener.Model.Plots
{
    public class PlotPatch
    {
        public PlotPatch(string name, string territoryPrefix)
        {
            Name = name;
            TerritoryPrefix = territoryPrefix;
        }

        public string TerritoryPrefix { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();

        public bool IsFlowerpot => Plots.Count == 1;
        public string DesignName => AppliedDesign?.Design.Name ?? "Unassigned plan";
        public AppliedPlotPatchDesign? AppliedDesign { get; set; } = null;

        public string Name { get; set; }

        public List<Plot> Plots { get; set; } = new();

        public Vector3 Location => Plots.FirstOrDefault()?.Location.AsVector3() ?? Vector3.Zero;

        // Only plots have game object ids, not the patches. So we use the lowest
        public ulong GameObjectId => Plots.OrderBy(plot => plot.GameObjectId).First().GameObjectId;

        public PlotDesign? Design(Plot plot)
        {
            if (AppliedDesign == null)
            {
                return null;
            }

            int index = Plots.IndexOf(plot);
            if (index == -1)
            {
                return null;
            }

            if (index >= AppliedDesign.Design.PlotDesigns.Count)
            {
                return null;
            }

            return AppliedDesign.Design.PlotDesigns[index];
        }

        public override bool Equals(object? otherPlotPatchObject)
        {
            if (otherPlotPatchObject == null || !(otherPlotPatchObject is PlotPatch otherPlotPatch))
            {
                return false;
            }
            if (!(otherPlotPatch.Plots.Count == Plots.Count))
            {
                return false;
            }

            ulong lowerGameObjectId = GameObjectId;
            ulong otherPlotLowerGameObjectId = otherPlotPatch.GameObjectId;
            return lowerGameObjectId == otherPlotLowerGameObjectId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

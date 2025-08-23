using Autogardener.Model.Designs;
using System.Linq;

namespace Autogardener.Model.Plots
{
    public class PlotPatch
    {        
        public PlotPatch(string name)
        {
            Name = name;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public string DesignName => AppliedDesign?.Design.Name ?? "Unassigned plan";
        public AppliedPlotPatchDesign? AppliedDesign { get; set; } = null;

        public string Name { get; set; }

        public List<Plot> Plots { get; set; } = new();

        public Vector3 Location => Plots.FirstOrDefault()?.Location.AsVector3() ?? Vector3.Zero;

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
                return false; ;
            }
            if (!(otherPlotPatch.Plots.Count == Plots.Count))
            {
                return false;
            }

            ulong lowerGameObjectId = Plots.Select(p => p.GameObjectId).OrderBy(goid => goid).First();
            ulong otherPlotLowerGameObjectId = otherPlotPatch.Plots.Select(p => p.GameObjectId).OrderBy(goid => goid).First();
            return lowerGameObjectId == otherPlotLowerGameObjectId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

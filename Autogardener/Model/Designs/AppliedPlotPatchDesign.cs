namespace Autogardener.Model.Designs
{
    public class AppliedPlotPatchDesign
    {
        public PlotPatchDesign Design { get; set; }

        public int Rotations { get; set; }// Each rotation means a 90º clockwise rotation
        public AppliedPlotPatchDesign(PlotPatchDesign design)
        {
            Design = design;
        }
    }
}

namespace Autogardener.Model.Designs
{
    public class AppliedPlotPlan
    {
        public PlotPlan Plan { get; set; }

        public int Rotations { get; set; }// Each rotation means a 90º clockwise rotation
        public AppliedPlotPlan(PlotPlan plan)
        {
            Plan = plan;
        }
    }
}

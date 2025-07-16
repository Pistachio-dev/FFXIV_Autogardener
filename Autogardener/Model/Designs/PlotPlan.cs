namespace Autogardener.Model.Designs
{
    public class PlotPlan
    {
        public string PlanName { get; set; } = "Unnamed plan";

        public string PlanDescription { get; set; } = string.Empty;

        public List<PlotHolePlan> PlotHolePlans { get; set; } = new();

        public int Rotations = 0; // Each rotatio means a 90º clockwise rotation
    }
}

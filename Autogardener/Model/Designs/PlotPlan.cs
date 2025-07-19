namespace Autogardener.Model.Designs
{
    public class PlotPlan
    {
        public static PlotPlan CreateEmptyWithSlots(int slots)
        {
            var plotPlan = new PlotPlan();
            for (int i = 0; i < slots; i++)
            {
                plotPlan.PlotHolePlans.Add(new PlotHolePlan(i));
            }

            return plotPlan;
        }
        public Guid Id { get; set; } = Guid.NewGuid();

        public string PlanName { get; set; } = "New plan";

        public string PlanDescription { get; set; } = string.Empty;

        public List<PlotHolePlan> PlotHolePlans { get; set; } = new();
    }
}

namespace Autogardener.Model.Designs
{
    public class PlotPatchDesign
    {
        public static PlotPatchDesign CreateEmptyWithSlots(int slots, string name)
        {
            var plotPlan = new PlotPatchDesign();
            for (int i = 0; i < slots; i++)
            {
                plotPlan.PlotDesigns.Add(new PlotDesign(i));
            }

            plotPlan.Name = name;
            return plotPlan;
        }

        public Guid Id { get; set; } = Guid.NewGuid();

        public bool IsFlowerpot => PlotDesigns.Count == 1;
        public string Name { get; set; } = "New design";

        public string Description { get; set; } = string.Empty;

        public List<PlotDesign> PlotDesigns { get; set; } = new();
    }
}

namespace Autogardener.Model.Plots
{
    public class PlotHighlightData
    {
        public PlotHighlightData(Vector2 position, string plotName, string designName)
        {
            Position = position;
            PlotName = plotName;
            DesignName = designName;
        }

        public Vector2 Position = Vector2.Zero;

        public string PlotName = "Untracked plot";

        public string DesignName = "No design applied";
    }
}

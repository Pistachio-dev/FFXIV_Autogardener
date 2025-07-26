namespace Autogardener.Model.Designs
{
    public class PlotDesign
    {
        public PlotDesign(int relativeIndex)
        {
            RelativeIndex = relativeIndex;
        }
        public uint DesignatedSeed { get; set; } //ItemId
        public uint DesignatedSoil { get; set; } //ItemId
        public bool DoNotHarvest { get; set; } // For those plants that you leave up, for interbreeding
        public int RelativeIndex { get; set; } // From zero to 7, the number is added to the actual plot id.
    }
}

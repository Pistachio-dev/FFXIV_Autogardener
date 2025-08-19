namespace Autogardener.Model.Plots
{
    public enum PlotStatus
    {
        Unknown, // We haven't checked yet
        Harvestable, // "Harvest" available
        Growing, // "Fertilize" and "Tend" available
        Empty, // "Plant seeds" available
        BeyondHope // Only "Remove Crop" and "Quit"
    }
}

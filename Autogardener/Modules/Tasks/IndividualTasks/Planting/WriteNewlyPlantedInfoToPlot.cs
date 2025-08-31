using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;

namespace Autogardener.Modules.Tasks.IndividualTasks.Planting
{
    internal class WriteNewlyPlantedInfoToPlot : GardeningTaskBase
    {
        private readonly PlotPatch patch;

        public WriteNewlyPlantedInfoToPlot(string name, PlotPatch patch, GameActions op, bool softBailout = false) : base(name, op, softBailout)
        {
            this.patch = patch;
        }

        public override bool Confirmation(Plot plot)
        {
            return true;
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            plot.CurrentSeed = patch.Design(plot)?.DesignatedSeed ?? 0;
            plot.CurrentSoil = patch.Design(plot)?.DesignatedSoil ?? 0;
            return true;
        }
    }
}

using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;

namespace Autogardener.Modules.Tasks.IndividualTasks.Planting
{
    internal class PickSeedsTask : GardeningTaskBase
    {
        private readonly PlotPatch patch;
        private readonly bool isFlowerPot;

        public PickSeedsTask(string name, PlotPatch patch, GameActions op) : base(name, op)
        {
            this.patch = patch;
            this.isFlowerPot = patch.IsFlowerpot;
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
            return op.AddonManagement.PickSeeds(patch.Design(plot)?.DesignatedSeed ?? throw new MissingDesignException(), isFlowerPot);
        }
    }
}

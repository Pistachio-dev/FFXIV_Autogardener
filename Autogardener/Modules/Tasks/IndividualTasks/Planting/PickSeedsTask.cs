using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks.Planting
{
    internal class PickSeedsTask : GardeningTaskBase
    {
        private readonly PlotPatch patch;

        public PickSeedsTask(string name, PlotPatch patch, GameActions op) : base(name, op)
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
            return op.AddonManagement.PickSeeds(patch.Design(plot)?.DesignatedSeed ?? throw new MissingDesignException());
        }
    }
}

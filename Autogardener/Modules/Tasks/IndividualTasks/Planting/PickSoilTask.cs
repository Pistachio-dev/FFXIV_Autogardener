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
    internal class PickSoilTask : GardeningTaskBase
    {
        public PickSoilTask(string name, GameActions op) : base(name, op)
        {
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
            return op.AddonManagement.PickSoil(plot.Design?.DesignatedSoil ?? throw new MissingDesignException());
        }
    }
}

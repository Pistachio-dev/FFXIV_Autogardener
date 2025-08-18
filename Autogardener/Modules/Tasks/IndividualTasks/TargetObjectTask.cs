using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class TargetObjectTask : GardeningTaskBase
    {
        public TargetObjectTask(string name, GameActions op):base(name, op)
        {

        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            return op.Targeting.TargetObject(plot.GameObjectId);
        }
        public override bool Confirmation(Plot plot)
        {
            return op.Targeting.VerifyTarget(plot.GameObjectId);
        }
    }
}

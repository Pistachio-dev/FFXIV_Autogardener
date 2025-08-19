using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class TargetObjectAndInteractTask : GardeningTaskBase
    {
        public TargetObjectAndInteractTask(string name, GameActions op):base(name, op)
        {

        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            if (!op.Targeting.TargetObject(plot.GameObjectId))
            {
                return false;
            }
            if (!op.Targeting.VerifyTarget(plot.GameObjectId))
            {
                return false;
            }

            return op.GoInteractions.InteractWithTargetedPlot();
        }
        public override bool Confirmation(Plot plot)
        {
            bool isTalkAddonVisible = op.AddonManagement.IsTalkAddonVisible();
            bool isTargetTheExpected = op.Targeting.VerifyTarget(plot.GameObjectId);

            return isTalkAddonVisible && isTargetTheExpected;
        }
    }
}

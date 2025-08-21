using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks.Planting
{
    public class ConfirmGardeningAddonTask : GardeningTaskBase
    {
        public ConfirmGardeningAddonTask(string name, GameActions op) : base(name, op)
        {
        }

        public override bool Confirmation(Plot plot)
        {
            return op.AddonManagement.VerifyGardeningConfirmationYesNoPopupAppears();
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            return op.AddonManagement.ClickConfirmOnHousingGardeningAddon();
        }
    }
}

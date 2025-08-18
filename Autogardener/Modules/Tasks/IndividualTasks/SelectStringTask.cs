using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    /// <summary>
    /// </summary>
    /// <exception cref="SelectStringNotPresentInAddon">There is a ready SelectString addon, but the option does not exist.</exception>
    internal class SelectStringTask : GardeningTaskBase
    {
        private readonly string stringToSelect;
        private List<string>? optionsFound;

        public SelectStringTask(string name, string stringToSelect, GameActions op): base(name, op)
        {
            this.stringToSelect = stringToSelect;
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            return op.AddonManagement.TrySelectActionString(stringToSelect, out optionsFound);
        }

        public override bool Confirmation(Plot plot)
        {
            return optionsFound != null && op.AddonManagement.VerifySelectStringAddonIsGone(optionsFound);
        }
    }
}

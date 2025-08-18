using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal class InteractWithObjectTask : GardeningTaskBase
    {
        public InteractWithObjectTask(string name, GameActions op): base(name, op)
        {

        }

        public override bool Confirmation(Plot plot)
        {
            // TODO: Use having a visible talk addon as confirmation
            return true;
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            return op.GoInteractions.InteractWithTargetedPlot();
        }
    }
}

using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal class SelectSeedsAndSoilTask : GardeningTaskBase
    {
        public SelectSeedsAndSoilTask(string name, GameActions op) : base(name, op)
        {
        }

        public override bool Confirmation(Plot plot)
        {
            throw new NotImplementedException();
        }

        public override bool PreRun(Plot plot)
        {
            throw new NotImplementedException();
        }

        public override bool Task(Plot plot)
        {
            throw new NotImplementedException();
        }
    }
}

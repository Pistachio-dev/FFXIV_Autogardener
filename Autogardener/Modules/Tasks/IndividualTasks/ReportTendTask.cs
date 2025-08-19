using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class ReportTendTask : GardeningTaskBase
    {
        public ReportTendTask(string name, GameActions op) : base(name, op)
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
            plot.LastTendedUtc = DateTime.UtcNow;
            return true;
        }
    }
}

using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks.Harvest
{
    internal class VerifyHarvestAndContinueTask : GardeningTaskBase
    {
        private readonly PlotTendScheduler scheduler;
        private readonly GlobalData gData;
        private readonly ErrorMessageMonitor errorMonitor;

        public VerifyHarvestAndContinueTask(string name, PlotTendScheduler scheduler, GlobalData gData, ErrorMessageMonitor errorMonitor, GameActions op) : base(name, op)
        {

            this.scheduler = scheduler;
            this.gData = gData;
            this.errorMonitor = errorMonitor;
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
            if (errorMonitor.WasThereARecentError(gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.NotEnoughInventorySpace)))
            {
                // Out of inventory space, most likely
                op.Log.Warning("No inventory space to harvest. Skipping.");
                plot.CurrentSoil = 0;
                plot.CurrentSeed = 0;
                scheduler.AddFinishTask();
                return true;
            }

            scheduler.AddTasksToGetBackToOptionsMenu();
            scheduler.AddTasksConditionally();
            return true;
        }
    }
}

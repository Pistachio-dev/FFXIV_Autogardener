using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Autogardener.Modules.GlobalData;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class ScheduleBasedOnPlotStatusTask : GardeningTaskBase
    {
        private readonly ErrorMessageMonitor errorMonitor;
        private readonly PlotTendScheduler scheduler;
        private readonly GlobalData gData;

        public ScheduleBasedOnPlotStatusTask(ErrorMessageMonitor errorMonitor, PlotTendScheduler scheduler, GlobalData gData, 
            string name, GameActions op) : base(name, op)
        {
            this.errorMonitor = errorMonitor;
            this.scheduler = scheduler;
            this.gData = gData;
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
            if (scheduler.PlotStatus == PlotStatus.Harvestable)
            {
                // Harvest
                scheduler.PlotStatus = PlotStatus.Empty;
            }
            if (scheduler.PlotStatus == PlotStatus.Empty)
            {
                // PlantSeeds
                scheduler.PlotStatus = PlotStatus.Growing;
            }
            if (scheduler.PlotStatus == PlotStatus.Growing)
            {
                // Fertilize and tend
                //if (DateTime.UtcNow - plot.LastFertilizedUtc < TimeSpan.FromHours(1)
                //    && !errorMonitor.WasThereARecentError())
                //{
                //    // Fertilize
                //}
            }

            return true;
        }

    }
}

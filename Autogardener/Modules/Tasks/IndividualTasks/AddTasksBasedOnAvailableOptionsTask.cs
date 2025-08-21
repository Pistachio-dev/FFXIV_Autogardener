using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal class AddTasksBasedOnAvailableOptionsTask : GardeningTaskBase
    {
        private readonly PlotTendScheduler scheduler;

        public AddTasksBasedOnAvailableOptionsTask(string name, PlotTendScheduler scheduler, GameActions op) : base(name, op)
        {
            this.scheduler = scheduler;
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
            switch (scheduler.PlotStatus)
            {
                case PlotStatus.Harvestable:
                    scheduler.AddHarvestingTasks(); break;
                case PlotStatus.Empty:
                    scheduler.AddPlantOrQuitTask(); break;
                case PlotStatus.Growing:
                    scheduler.AddFertilizeAndTendTasks(); break;
                case PlotStatus.BeyondHope:
                default:
                    scheduler.AddQuitTask();
                    scheduler.AddFinishTask();
                    break;
            }

            return true;
        }
    }
}

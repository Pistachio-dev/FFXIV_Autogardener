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
    public class QueryPlotStatusTask : GardeningTaskBase
    {
        private readonly PlotTendScheduler scheduler;
        private readonly GlobalData gData;

        public QueryPlotStatusTask(PlotTendScheduler scheduler, GlobalData gData, string name, GameActions op) : base(name, op)
        {
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
            if(!op.AddonManagement.TryGetOptionsAsStrings(out List<string> options))
            {
                return false;
            }

            if (OptionAvailable(OptionLocalized(GardeningStrings.HarvestCrop), options)){
                scheduler.PlotStatus = PlotStatus.Harvestable;
                return true;
            }
            if (OptionAvailable(OptionLocalized(GardeningStrings.TendCrop), options))
            {
                scheduler.PlotStatus = PlotStatus.Growing;
                return true;
            }
            if (OptionAvailable(OptionLocalized(GardeningStrings.PlantSeeds), options))
            {
                scheduler.PlotStatus = PlotStatus.Empty;
                return true;
            }

            scheduler.PlotStatus = PlotStatus.Unknown;
            return true;            
        }

        private bool OptionAvailable(string option, List<string> options)
        {
            return options.Any(o => o.Contains(option, StringComparison.OrdinalIgnoreCase));
        }

        private string OptionLocalized(GlobalData.GardeningStrings option)
        {
            return gData.GetGardeningOptionStringLocalized(option);
        }
    }
}

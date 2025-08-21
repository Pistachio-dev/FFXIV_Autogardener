using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks.Planting
{
    public class VerifyCanPlantTask : GardeningTaskBase
    {
        private readonly PlotTendScheduler scheduler;
        private readonly GlobalData gData;

        public VerifyCanPlantTask(PlotTendScheduler scheduler, GlobalData gData, string name, GameActions op) : base(name, op)
        {
            this.scheduler = scheduler;
            this.gData = gData;
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
            if (plot.Design?.DesignatedSeed == 0 || plot.Design?.DesignatedSoil == 0)
            {
                op.ChatGui.Print("Missing seeds or soil. Not replanting.");
                scheduler.PlantingStatus = PlantingStatus.ImpossibleOrUnwanted;
                return true;
            }

            var seed = op.Inventory.TryGetItemInInventory(plot.Design!.DesignatedSeed);
            
            if (seed == null)
            {
                op.ChatGui.Print($"Missing seed {gData.GetSeedStringName(plot.Design.DesignatedSeed)}. Can't replant.");
                scheduler.PlantingStatus = PlantingStatus.ImpossibleOrUnwanted;
                return true;
            }
            var soil = op.Inventory.TryGetItemInInventory(plot.Design.DesignatedSoil);
            if (soil == null)
            {
                op.ChatGui.Print($"Missing soil {gData.GetSeedStringName(plot.Design.DesignatedSeed)}. Can't replant.");
                scheduler.PlantingStatus = PlantingStatus.ImpossibleOrUnwanted;
                return true;
            }

            scheduler.PlantingStatus = PlantingStatus.Possible;

            return true;
        }
    }
}

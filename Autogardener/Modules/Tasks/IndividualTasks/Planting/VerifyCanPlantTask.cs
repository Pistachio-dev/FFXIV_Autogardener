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
        private readonly PlotPatch patch;
        private readonly GlobalData gData;

        public VerifyCanPlantTask(PlotTendScheduler scheduler, PlotPatch patch, GlobalData gData, string name, GameActions op) : base(name, op)
        {
            this.scheduler = scheduler;
            this.patch = patch;
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
            var plantingStatus = GetPlantingStatus(plot);
            if (plantingStatus == PlantingStatus.Possible)
            {
                scheduler.AddPlantSeedsTasks();
            }
            else
            {
                scheduler.AddQuitTask();
            }

            return true;
        }

        private PlantingStatus GetPlantingStatus(Plot plot)
        {
            var design = patch.Design(plot);
            if (design == null)
            {
                op.ChatGui.Print("No design for plot. Skipping replanting.");
                return PlantingStatus.ImpossibleOrUnwanted;
            }

            if (design.DesignatedSeed == 0 || design.DesignatedSoil == 0)
            {
                op.ChatGui.Print("Missing seeds or soil. Not replanting.");
                return PlantingStatus.ImpossibleOrUnwanted;
            }

            var seed = op.Inventory.TryGetItemInInventory(design.DesignatedSeed);

            if (seed == null)
            {
                op.ChatGui.Print($"Missing seed {gData.GetSeedStringName(design.DesignatedSeed)}. Can't replant.");
                return PlantingStatus.ImpossibleOrUnwanted;
            }
            var soil = op.Inventory.TryGetItemInInventory(design.DesignatedSoil);
            if (soil == null)
            {
                op.ChatGui.Print($"Missing soil {gData.GetSeedStringName(design.DesignatedSoil)}. Can't replant.");
                return PlantingStatus.ImpossibleOrUnwanted;
            }

            return PlantingStatus.Possible;
        }
    }
}

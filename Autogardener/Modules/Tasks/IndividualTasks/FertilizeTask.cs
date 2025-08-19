using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class FertilizeTask : GardeningTaskBase
    {
        public FertilizeTask(string name, ErrorMessageMonitor errorMonitor, GameActions op) : base(name, op)
        {
            this.errorMonitor = errorMonitor;

        }

        private const uint FertilizerId = GlobalData.FishmealId;
        private bool couldNotUseFertilizer = false; // Does this if you get an error or don't have enough.
        private int fertilizerCount = 0;
        private readonly ErrorMessageMonitor errorMonitor;



        public override bool PreRun(Plot plot)
        {
            var itemInfo = op.Inventory.TryGetItemInInventory(FertilizerId); // There's only one fertilizer in the game, Fish Meal.
            fertilizerCount = itemInfo?.Quantity ?? 0;
            return true;
        }

        public override bool Task(Plot plot)
        {
            if (fertilizerCount == 0)
            {
                op.ChatGui.PrintError("Out of fertilizer");
                couldNotUseFertilizer = true;
                return true;
            }

            if (errorMonitor.WasThereARecentError()) //TODO: VERIFY THE MESSAGE
            {
                // Most likely, plot already fertilized
                couldNotUseFertilizer = true;
                return true;
            }

            var itemInfo = op.Inventory.TryGetItemInInventory(FertilizerId); // There's only one fertilizer in the game, Fish Meal.
            if (itemInfo == null)
            {
                // This would never happen anyway but eh.
                return true;
            }

            return op.GoInteractions.Fertilize(itemInfo);
        }

        public override bool Confirmation(Plot plot)
        {
            int currentQuantity = op.Inventory.TryGetItemInInventory(FertilizerId)?.Quantity ?? 0;
            if (currentQuantity < fertilizerCount || !couldNotUseFertilizer)
            {
                plot.LastFertilizedUtc = DateTime.UtcNow;
            }

            return couldNotUseFertilizer || currentQuantity < fertilizerCount;
        }
    }
}

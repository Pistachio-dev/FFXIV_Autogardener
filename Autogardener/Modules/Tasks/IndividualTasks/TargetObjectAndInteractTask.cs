using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public class TargetObjectAndInteractTask : GardeningTaskBase
    {
        private readonly PlotTendScheduler scheduler;

        public TargetObjectAndInteractTask(PlotTendScheduler scheduler, string name, GameActions op):base(name, op)
        {
            this.scheduler = scheduler;
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            if (op.Targeting.IsTooFarToInteractWith(plot.GameObjectId))
            {
                scheduler.Abort(AbortReason.MovedTooFarAway);
            }

            if (!op.Targeting.TargetPlotObject(plot.GameObjectId))
            {
                return false;
            }
            if (!op.Targeting.VerifyTarget(plot.GameObjectId))
            {
                return false;
            }

            return op.GoInteractions.InteractWithTargetedPlot();
        }
        public override bool Confirmation(Plot plot)
        {
            bool isTalkAddonVisible = op.AddonManagement.IsTalkAddonVisible();
            bool isTargetTheExpected = op.Targeting.VerifyTarget(plot.GameObjectId);

            return isTalkAddonVisible && isTargetTheExpected;
        }
    }
}

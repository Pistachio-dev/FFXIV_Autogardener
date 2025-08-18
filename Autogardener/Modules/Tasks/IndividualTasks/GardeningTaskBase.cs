using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Schedulers;
using ECommons.Throttlers;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    public abstract class GardeningTaskBase
    {
        public string TaskName { get; }
        protected readonly GameActions op;
        private const int MaxTaskAttempts = 50;
        private const int ConfirmationAttemptsBeforeRetry = 5;
        private int taskAttempts = 0;
        private int confirmationAttempts = 0;
        private bool preRunDone = false;
        
        private int throttleTime = 10;
        private bool waitingConfirmation = false;
        internal GardeningTaskBase(string name, GameActions op)
        {
            TaskName = name;
            this.op = op;
        }

        public abstract bool PreRun(Plot plot);
        public abstract bool Task(Plot plot);
        public abstract bool Confirmation(Plot plot);

        // Run PreRun
        internal GardeningTaskResult Run(PlotTendScheduler scheduler)
        {
            var plot = scheduler.Plot;
            if (!EzThrottler.Throttle(TaskName, throttleTime))
            {
                return GardeningTaskResult.Incomplete;
            }

            if (taskAttempts > MaxTaskAttempts)
            {
                op.Log.Warning($"Task \"{TaskName}\" reached the max try amount. Bailing out. Attempts: {taskAttempts}. Confirmation attempts: {confirmationAttempts}");
                return GardeningTaskResult.Bailout;
            }
            if (!preRunDone)
            {
                op.Log.Info($"Task \"{TaskName}\": Prerun");
                PreRun(plot);
                preRunDone = true;
            }

            if (!waitingConfirmation)
            {
                taskAttempts++;
                if (Task(plot))
                {
                    op.Log.Debug($"Work successful for task {TaskName}");
                    waitingConfirmation = true;
                }

                return GardeningTaskResult.Incomplete;
            }

            confirmationAttempts++;
            if (Confirmation(plot))
            {
                op.Log.Debug($"Confirmation successful for task {TaskName}");
                return GardeningTaskResult.Complete;
            }


            if (confirmationAttempts >= ConfirmationAttemptsBeforeRetry)
            {
                // Retry the action
                waitingConfirmation = false;
                confirmationAttempts = 0;
            }

            return GardeningTaskResult.Incomplete;
            
        }
    }
}

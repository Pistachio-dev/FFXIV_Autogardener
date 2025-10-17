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
        private readonly bool softBailout; // On a bailout, skip this task and not the whole patch
        private int MaxTaskAttempts => op.ConfigService.GetConfiguration().TaskAttemptsBeforeFailure;
        private int ConfirmationAttemptsBeforeRetry => op.ConfigService.GetConfiguration().ConfirmationAttemptsBeforeFailure;
        private int throttleTime => op.ConfigService.GetConfiguration().StepDelayInMs;

        private int taskAttempts = 0;
        private int confirmationAttempts = 0;
        private bool preRunDone = false;
        
        
        private bool waitingConfirmation = false;
        internal GardeningTaskBase(string name, GameActions op, bool softBailout = false)
        {
            TaskName = name;
            this.op = op;
            this.softBailout = softBailout;
        }

        public abstract bool PreRun(Plot plot);
        public abstract bool Task(Plot plot);
        public abstract bool Confirmation(Plot plot);

        protected virtual void TriggerBailout(Plot plot)
        {
            op.Log.Warning($"Soft bailout on task \"{TaskName}\"");
            taskAttempts = int.MaxValue;
        }

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
                if (softBailout)
                {
                    TriggerBailout(plot);
                    return GardeningTaskResult.Bailout_Softbailout;
                }
                op.Log.Warning($"Task \"{TaskName}\" reached the max try amount. Bailing out. Attempts: {taskAttempts}. Confirmation attempts: {confirmationAttempts}");
                return GardeningTaskResult.Bailout_RetriesExceeded;
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

            op.Log.Debug($"Running confirmation for task {TaskName}");
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

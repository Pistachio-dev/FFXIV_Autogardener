using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;
using Autogardener.Modules.Tasks.IndividualTasks;
using DalamudBasics.Logging;
using Humanizer;
using System.Linq;

namespace Autogardener.Modules.Schedulers
{
    public class PlotTendScheduler
    {
        public bool Complete => taskQueue.Count <= currentTaskIndex;

        private readonly GardenPatchScheduler parentScheduler;
        public readonly Plot Plot;
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gData;
        private readonly LinkedList<GardeningTaskBase> taskQueue = new();
        private int currentTaskIndex = 0;

        public PlotTendScheduler(GardenPatchScheduler parentScheduler, Plot plot, ILogService logService, GameActions op, GlobalData gData)
        {
            this.parentScheduler = parentScheduler;
            this.Plot = plot;
            this.logService = logService;
            this.op = op;
            this.gData = gData;
            SetupStartingTasks();
        }

        public void Append(GardeningTaskBase task)
        {
            taskQueue.AddLast(task);
        }

        public void Insert(List<GardeningTaskBase> newTasks)
        {
            var currentTaskNode = taskQueue.Find(taskQueue.ElementAt(currentTaskIndex)); // Inefficient as fuck, but it's a short list anyway
            if (currentTaskNode == null)
            {
                taskQueue.AddRange(newTasks);
                return;
            }

            for (int i = newTasks.Count - 1; i >= 0; i++)
            {
                taskQueue.AddAfter(currentTaskNode, newTasks[i]);
            }
        }
        private void SetupStartingTasks()
        {
            taskQueue.AddLast(new TargetObjectTask("Target object", op));
            taskQueue.AddLast(new InteractWithObjectTask("Interact with object", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
            string quitOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit);
            taskQueue.AddLast(new SelectStringTask("Select Quit", quitOption, op));
        }

        public void Tick()
        {
            if (Complete)
            {
                return;
            }

            GardeningTaskBase currentTask = taskQueue.ElementAt(currentTaskIndex);
            GardeningTaskResult result = currentTask.Run(this);
            try
            {
                switch (result)
                {
                    case GardeningTaskResult.Incomplete:
                        return;
                    case GardeningTaskResult.Complete:
                        currentTaskIndex++;
                        return;
                    case GardeningTaskResult.Bailout:
                        logService.Warning($"Bailout on task \"{currentTask.TaskName}\" at {DateTime.Now.Humanize()}");
                        taskQueue.Clear();
                        parentScheduler.Abort();
                        return;
                }
            }
            catch (SelectStringNotPresentInAddon ex)
            {
                logService.Warning($"SelectString addon did not include option \"{ex.ExpectedString}\". " +
                    $"Available options were \"{ex.PresentStrings.Humanize()}\"");
            }
        }
    }
}

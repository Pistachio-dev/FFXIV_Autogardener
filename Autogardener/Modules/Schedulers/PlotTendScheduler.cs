using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;
using Autogardener.Modules.Tasks;
using Autogardener.Modules.Tasks.IndividualTasks;
using DalamudBasics.Configuration;
using DalamudBasics.Logging;
using Humanizer;
using System.Linq;

namespace Autogardener.Modules.Schedulers
{
    public class PlotTendScheduler
    {
        public bool Complete => taskQueue.Count <= currentTaskIndex;
        internal PlotStatus PlotStatus = PlotStatus.Unknown;
        internal PlantingStatus PlantingStatus = PlantingStatus.NotChecked;
        private readonly GardenPatchScheduler parentScheduler;
        public readonly Plot Plot;
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gData;
        private readonly IConfigurationService<Configuration> confService;
        private readonly ErrorMessageMonitor errorMessageMonitor;
        private readonly LinkedList<GardeningTaskBase> taskQueue = new();
        private int currentTaskIndex = 0;
        

        public PlotTendScheduler(GardenPatchScheduler parentScheduler, Plot plot, ILogService logService,
            GameActions op, GlobalData gData, IConfigurationService<Configuration> confService, ErrorMessageMonitor errorMessageMonitor)
        {
            this.parentScheduler = parentScheduler;
            this.Plot = plot;
            this.logService = logService;
            this.op = op;
            this.gData = gData;
            this.confService = confService;
            this.errorMessageMonitor = errorMessageMonitor;
            SetupStartingTasks();
        }

        public void Abort(AbortReason reason)
        {
            parentScheduler.Abort(reason);
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
            taskQueue.AddLast(new TargetObjectAndInteractTask(this, "Target and interact with object", op));
            taskQueue.AddLast(new ExtractSeedTypeTask("Extract seed type", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
            string quitOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit);
            string plantOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.PlantSeeds);


            taskQueue.AddLast(new QueryPlotStatusTask(this, gData, "Get plot status", op));
            // THIS WILL NOT WORK! PLOT STATUS IS NULL UNTIL THIS TASK RUNS
            if (PlotStatus == PlotStatus.Harvestable && !(Plot.Design?.DoNotHarvest ?? false))
            {

            }
            if (PlotStatus == PlotStatus.BeyondHope)
            {
                taskQueue.AddLast(new SelectStringTask("Select Quit", quitOption, op));
            }

        }

        private void AddTasksToGetBackToOptionsMenu()
        {
            taskQueue.AddLast(new TargetObjectAndInteractTask(this, "Target object again and interact", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
            taskQueue.AddLast(new ExtractSeedTypeTask("Extract seed type", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
            taskQueue.AddLast(new QueryPlotStatusTask(this, gData, "Get plot status", op));
            // Add the "conditionally add more tasks" task here
        }

        private void AddHarvestingTasks()
        {
            string harvestOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.HarvestCrop);

            taskQueue.AddLast(new SelectStringTask("Select \"Harvest\"", harvestOption, op));
            taskQueue.AddLast(new ReportHarvested("Update seed and soil present", op));
        }

        private void AddFertilizeAndTendTasks()
        {
            string fertilizeOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Fertilize);
            string tendOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.TendCrop);

            if (confService.GetConfiguration().UseFertilizer) // TODO: Stop trying when you run out of fertilizer
            {
                taskQueue.AddLast(new SelectStringTask("Select Fertilize", fertilizeOption, op));
                taskQueue.AddLast(new FertilizeTask("Use Fertilizer", errorMessageMonitor, op));
                AddTasksToGetBackToOptionsMenu();
            }
            taskQueue.AddLast(new SelectStringTask("Select Tend", tendOption, op));
            taskQueue.AddLast(new ReportTendTask("Set \"last tended\"", op));
        }

        private void AddPlantSeedsTasks()
        {

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
                    case GardeningTaskResult.Bailout_RetriesExceeded:
                        logService.Warning($"Bailout on task \"{currentTask.TaskName}\" at {DateTime.Now.Humanize()}");
                        taskQueue.Clear();
                        parentScheduler.Abort(AbortReason.RetriesExceeded);
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

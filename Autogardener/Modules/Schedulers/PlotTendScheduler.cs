using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Exceptions;
using Autogardener.Modules.Tasks;
using Autogardener.Modules.Tasks.IndividualTasks;
using Autogardener.Modules.Tasks.IndividualTasks.Harvest;
using Autogardener.Modules.Tasks.IndividualTasks.Planting;
using DalamudBasics.Configuration;
using DalamudBasics.Logging;
using Humanizer;
using System.Linq;

namespace Autogardener.Modules.Schedulers
{
    public class PlotTendScheduler : IScheduler
    {
        public bool Complete => taskQueue.Count <= currentTaskIndex;
        internal PlotStatus PlotStatus = PlotStatus.Unknown;
        internal PlantingStatus PlantingStatus = PlantingStatus.NotChecked;
        private readonly GardenPatchScheduler parentScheduler;
        private readonly PlotPatch patch;
        public readonly Plot Plot;
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gData;
        private readonly IConfigurationService<Configuration> confService;
        private readonly ErrorMessageMonitor errorMessageMonitor;
        private readonly LinkedList<GardeningTaskBase> taskQueue = new();
        private int currentTaskIndex = 0;
        public bool Done() => taskQueue.Count == 0;

        public PlotTendScheduler(GardenPatchScheduler parentScheduler, PlotPatch patch, Plot plot, ILogService logService,
            GameActions op, GlobalData gData, IConfigurationService<Configuration> confService, ErrorMessageMonitor errorMessageMonitor)
        {
            this.parentScheduler = parentScheduler;
            this.patch = patch;
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

        public void SetupStartingTasks()
        {
            taskQueue.AddLast(new TargetObjectAndInteractTask(this, "Target and interact with object", op));
            taskQueue.AddLast(new ExtractSeedTypeTask("Extract seed type", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
            AddTasksConditionally();
        }

        public void AddTasksConditionally()
        {
            // Run his when the options SelectString is showing
            taskQueue.AddLast(new QueryPlotStatusTask(this, gData, "Get plot status", op));
            taskQueue.AddLast(new AddTasksBasedOnAvailableOptionsTask("Schedule next steps", this, op));
        }

        public void AddTasksToGetBackToOptionsMenu()
        {
            taskQueue.AddLast(new TargetObjectAndInteractTask(this, "Target object again and interact", op));
            taskQueue.AddLast(new SkipChatterTask("Skip plant description talk", op));
        }

        public void AddHarvestingTasks()
        {
            if (!confService.GetConfiguration().Harvest || (patch.Design(Plot)?.DoNotHarvest ?? false))
            {
                AddQuitTask();
                AddFinishTask();
                return;
            }

            string harvestOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.HarvestCrop);

            taskQueue.AddLast(new SelectStringTask("Select \"Harvest\"", harvestOption, op));
            taskQueue.AddLast(new VerifyHarvestAndContinueTask("Verify harvest and continue", this, gData, errorMessageMonitor, op));
        }

        public void AddFertilizeAndTendTasks()
        {
            string fertilizeOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Fertilize);
            string tendOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.TendCrop);

            if (confService.GetConfiguration().UseFertilizer) // TODO: Stop trying when you run out of fertilizer
            {
                taskQueue.AddLast(new SelectStringTask("Select Fertilize", fertilizeOption, op));
                taskQueue.AddLast(new FertilizeTask("Use Fertilizer", errorMessageMonitor, gData, op));
                AddTasksToGetBackToOptionsMenu();
            }
            taskQueue.AddLast(new SelectStringTask("Select Tend", tendOption, op));
            taskQueue.AddLast(new ReportTendTask("Set \"last tended\"", op));
            AddFinishTask();
        }

        public void AddPlantOrQuitTask()
        {
            if (confService.GetConfiguration().Replant)
            {
                taskQueue.AddLast(new VerifyCanPlantTask(this, patch, gData, "Verify that we have the resources to plant", op));
            }
            else
            {
                AddQuitTask();
                AddFinishTask();
            }
        }

        public void AddPlantSeedsTasks()
        {
            string plantOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.PlantSeeds);
            taskQueue.AddLast(new SelectStringTask("Select \"Plant Seeds\"", plantOption, op));
            taskQueue.AddLast(new PickSeedsTask("Pick seeds", patch, op));
            taskQueue.AddLast(new PickSoilTask("Pick soil", patch,op));
            taskQueue.AddLast(new ConfirmGardeningAddonTask("Click confirm on gardening addon", op));
            taskQueue.AddLast(new AcceptGardeningAddonYesNoTask("Click yes on gardening addon confirmation", op));
            taskQueue.AddLast(new WriteNewlyPlantedInfoToPlot("Save info of what was planted", patch, op));
            AddTasksToGetBackToOptionsMenu();
            AddTasksConditionally();
        }

        public void AddQuitTask()
        {
            string quitOption = gData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit);
            taskQueue.AddLast(new SelectStringTask("Select Quit", quitOption, op));
        }

        public void AddFinishTask()
        {
            taskQueue.Add(new FinishingTask("Finishing", op));
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
                    case GardeningTaskResult.Bailout_Softbailout:
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

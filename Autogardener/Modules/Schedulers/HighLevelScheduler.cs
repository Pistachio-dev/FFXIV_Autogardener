using Autogardener.Model;
using Autogardener.Model.ActionChains;
using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Autogardener.Modules.Movement;
using Autogardener.Modules.Tasks;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.Output;
using DalamudBasics.Configuration;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Schedulers
{
    // This is a skeleton until I get things working well for one plot
    public class HighLevelScheduler
    {
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gData;
        private readonly IFramework framework;
        private readonly IChatGui chatGui;
        private readonly IConfigurationService<Configuration> confService;
        private readonly ErrorMessageMonitor errorMessageMonitor;
        private readonly ISaveManager<CharacterSaveState> saveManager;
        private readonly MovementController movementController;
        private readonly IChatOutput chatOutput;
        private GardenPatchScheduler? plotPatchScheduler; // Only one for now
        private Queue<IScheduler> schedulerQueue = new();


        public HighLevelScheduler(ILogService logService, GameActions op, GlobalData gData, IFramework framework,            
            IChatGui chatGui, IConfigurationService<Configuration> confService, ErrorMessageMonitor errorMessageMonitor, ISaveManager<CharacterSaveState> saveManager,
            MovementController movementController, IChatOutput chatOutput)
        {            
            this.logService = logService;
            this.op = op;
            this.gData = gData;
            this.framework = framework;
            this.chatGui = chatGui;
            this.confService = confService;
            this.errorMessageMonitor = errorMessageMonitor;
            this.saveManager = saveManager;
            this.movementController = movementController;
            this.chatOutput = chatOutput;
        }
        
        public void ScheduleActionChain(List<ChainedAction> actions)
        {
            var save = saveManager.GetCharacterSaveInMemory();
            schedulerQueue.Clear();
            foreach (ChainedAction action in actions)
            {
                if (action.Type == ChainedActionType.GoToPlot)
                {
                    var patch = save.Plots.FirstOrDefault(patch => patch.Id == action.PatchId);
                    if (patch == null)
                    {
                        continue;
                    }

                    schedulerQueue.Enqueue(new MovementScheduler(this, patch.GetLocation(), logService, chatGui, movementController));
                    schedulerQueue.Enqueue(new GardenPatchScheduler(this, patch, logService, op, gData, confService, errorMessageMonitor));
                    continue;
                }

                if (action.Type == ChainedActionType.ExecuteCommand)
                {
                    schedulerQueue.Enqueue(new ChatCommandScheduler(action, chatOutput));
                    continue;
                }
            }

            framework.Update += Tick;
        }

        public void SchedulePatchTend(PlotPatch patch)
        {
            schedulerQueue.Enqueue(new GardenPatchScheduler(this, patch, logService, op, gData, confService, errorMessageMonitor));
            framework.Update += Tick;
        }

        internal void Abort(AbortReason reason)
        {
            framework.Update -= Tick;
            if (reason == AbortReason.RetriesExceeded)
            {
                chatGui.PrintError("Autogardener got stuck on something. Aborting. Please panic.");
            }
            if (reason == AbortReason.MovedTooFarAway)
            {
                chatGui.PrintError("You moved too far away from the plot. Aborting.");
            }
        }

        private void Tick(IFramework framework)
        {
            if (schedulerQueue.Count == 0)
            {
                framework.Update -= Tick;
                return;
            }

            if (schedulerQueue.Peek().Done())
            {
                schedulerQueue.Dequeue();
                Tick(framework);
                return;
            }

            schedulerQueue.Peek().Tick();
        }
    }
}

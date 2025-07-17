using Autogardener.Model;
using Autogardener.Model.Plots;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Extensions;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
using System.Linq;

namespace Autogardener.Modules
{
    public class PlayerActions
    {
        private readonly ILogService logService;
        private readonly IChatGui chatGui;
        private readonly ISaveManager<CharacterSaveState> saveManager;
        private readonly GlobalData globalData;
        private readonly PlotWatcher plotWatcher;
        private readonly Commands commands;
        private readonly Utils utils;
        private readonly IClientState clientState;
        private readonly IObjectTable objectTable;
        private readonly ITargetManager targetManager;
        private readonly TaskManager taskManager;
        private readonly CharacterSaveState state;

        public PlayerActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
            GlobalData globalData, PlotWatcher plotWatcher, Commands commands, Utils utils, IClientState clientState,
            IObjectTable objectTable, ITargetManager targetManager, TaskManager taskManager, CharacterSaveState state)
        {
            this.logService = logService;
            this.chatGui = chatGui;
            this.saveManager = saveManager;
            this.globalData = globalData;
            this.plotWatcher = plotWatcher;
            this.commands = commands;
            this.utils = utils;
            this.clientState = clientState;
            this.objectTable = objectTable;
            this.targetManager = targetManager;
            this.taskManager = taskManager;
            this.state = state;
        }

        public void RegisterNearestPlot()
        {
            
            var player = clientState.LocalPlayer;
            if (player == null)
            {
                logService.Warning("Attempted to register nearest plot, but local player is null.");
                return;
            }

            var charState = saveManager.GetCharacterSaveInMemory();

            Plot? plot = GetNearestPlot();
            if (plot == null)
            {
                logService.Warning("No plot is near");
                chatGui.PrintError("No plot is near");
                return;
            }
            Plot? alreadySeenPlot = charState.Plots.FirstOrDefault(p => p.Equals(plot));
            if (alreadySeenPlot != null)
            {
                plot = alreadySeenPlot;
            }
            else
            {
                charState.Plots.Add(plot);
            }

            if (plot == null)
            {
                logService.Warning("Could not get the nearest plot");
                return;
            }

            foreach (PlotHole plotHole in plot.PlantingHoles)
            {
                var plotOb = objectTable.SearchById(plotHole.GameObjectId);
                if (plotOb == null)
                {
                    throw new Exception($"Plot with objectdId {plotHole.GameObjectId} was not found in the object table");
                }
                plotHole.Initialize(plotOb);
                taskManager.Enqueue(() => TargetObject(plotOb));
                taskManager.Enqueue(() => commands.InteractWithTargetPlot(), "InteractWithPlot", DefConfig);
                taskManager.Enqueue(() => commands.SetPlantTypeFromDialogue(plotHole), "Extract plant type", DefConfig);
                taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialogue", DefConfig);
                taskManager.Enqueue(() => commands.SelectActionString(globalData
                    .GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit)), "Select Quit", DefConfig);
                taskManager.EnqueueDelay(new Random().Next(200, 300));
            }

            taskManager.Enqueue(() => {saveManager.WriteCharacterSave(charState);});
        }

        private bool TargetObject(IGameObject ob)
        {
            logService.Debug($"Targeting {ob.GameObjectId}");
            targetManager.Target = ob;
            return true;
        }
        public Plot? GetNearestPlot()
        {
            plotWatcher.UpdatePlotList();
            Vector3 playerLocation = clientState.LocalPlayer?.Position ?? Vector3.Zero;
            if (playerLocation == Vector3.Zero)
            {
                logService.Error("Player location is null. Can't register nearest plot.");
                return null;
            }

            IEnumerable<(Plot plot, float distance)> plotsWithDistances
                = state.Plots.Select(x => (x, Vector3.Distance(x.Location, playerLocation)));

            try
            {
                Plot nearestPlot = plotsWithDistances.OrderBy(t => t.distance).First().plot;
                logService.Debug($"Nearest plot found with {nearestPlot.PlantingHoles.Count} slots");
                return nearestPlot;
            }
            catch (InvalidOperationException)
            {
                logService.Error("Could not register nearest plot: no plots in the area");
                return null;
            }
        }

        private static TaskManagerConfiguration DefConfig = new TaskManagerConfiguration()
        {
            ShowDebug = true,
            ShowError = true,
            TimeLimitMS = 10000,
            AbortOnTimeout = true
        };
    }
}

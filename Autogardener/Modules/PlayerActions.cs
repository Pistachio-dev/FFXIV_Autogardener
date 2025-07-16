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
        private readonly ISaveManager<SaveState> saveManager;
        private readonly GlobalData globalData;
        private readonly PlotWatcher plotWatcher;
        private readonly Commands commands;
        private readonly Utils utils;
        private readonly IClientState clientState;
        private readonly IObjectTable objectTable;
        private readonly ITargetManager targetManager;
        private readonly TaskManager taskManager;

        public PlayerActions(ILogService logService, IChatGui chatGui, ISaveManager<SaveState> saveManager,
            GlobalData globalData, PlotWatcher plotWatcher, Commands commands, Utils utils, IClientState clientState,
            IObjectTable objectTable, ITargetManager targetManager, TaskManager taskManager)
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
        }

        public void RegisterNearestPlot()
        {
            
            var player = clientState.LocalPlayer;
            if (player == null)
            {
                logService.Warning("Attempted to register nearest plot, but local player is null.");
                return;
            }

            var state = saveManager.LoadSave()!;
            var charState = state.GetCharacterSaveState(player.GetFullName());
            logService.Info(System.Text.Json.JsonSerializer.Serialize(charState));

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
                    .GetGardeningOptionStringLocalized(GlobalData.GardeningOption.Quit)), "Select Quit", DefConfig);
                taskManager.EnqueueDelay(new Random().Next(200, 300));
            }

            taskManager.Enqueue(() => {saveManager.WriteSave(state);});
        }

        private bool TargetObject(IGameObject ob)
        {
            targetManager.Target = ob;
            return true;
        }
        private Plot? GetNearestPlot()
        {
            plotWatcher.UpdatePlotList();
            Vector3 playerLocation = clientState.LocalPlayer?.Position ?? Vector3.Zero;
            if (playerLocation == Vector3.Zero)
            {
                logService.Error("Player location is null. Can't register nearest plot.");
                return null;
            }

            IEnumerable<(Plot plot, float distance)> plotsWithDistances
                = plotWatcher.Plots.Select(x => (x, Vector3.Distance(x.Location, playerLocation)));

            try
            {
                Plot nearestPlot = plotsWithDistances.OrderBy(t => t.distance).First().plot;
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

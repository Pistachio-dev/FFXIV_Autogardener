using Autogardener.Model;
using Autogardener.Model.Designs;
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

        public PlayerActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
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

            var charState = saveManager.GetCharacterSaveInMemory();

            Plot? plot = GetNearestTrackedPlot(true);
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
                taskManager.Enqueue(() => chatGui.Print("Scan complete! o7"));
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

        public int CreateNewDesign()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            var index = state.Designs.Count;
            var slots = GetNearestTrackedPlot(false)?.PlantingHoles.Count ?? 9;
            state.Designs.Add(PlotPlan.CreateEmptyWithSlots(slots));
            logService.Info($"Current design count: {state.Designs.Count}");
            saveManager.WriteCharacterSave(state);
            return index;
        }
        public Plot? GetNearestTrackedPlot(bool addNewPlots)
        {
            var state = saveManager.GetCharacterSaveInMemory();
            if (addNewPlots)
            {
                plotWatcher.UpdatePlotList();
            }

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
                logService.Debug("Could not register nearest plot: no plots in the area");
                return null;
            }
        }

        public bool ApplyDesign(ref Plot plot, PlotPlan design)
        {
            if (plot.PlantingHoles.Count != design.PlotHolePlans.Count)
            {
                chatGui.PrintError("Plot and design do not match!");
                return false;
            }
            for (var i = 0; i < plot.PlantingHoles.Count; i++){
                var plotHole = plot.PlantingHoles[i];
                var plotHolePlan = design.PlotHolePlans[i];
                plotHole.Design = plotHolePlan;
            }

            return true;
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

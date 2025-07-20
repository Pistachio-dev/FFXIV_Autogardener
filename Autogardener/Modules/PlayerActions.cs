using Autogardener.Model;
using Autogardener.Model.Designs;
using Autogardener.Model.Plots;
using Autogardener.Model.ResourcesCheck;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
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
        private readonly IGameInventory gameInventory;

        public PlayerActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
            GlobalData globalData, PlotWatcher plotWatcher, Commands commands, Utils utils, IClientState clientState,
            IObjectTable objectTable, ITargetManager targetManager, TaskManager taskManager, IGameInventory gameInventory)
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
            this.gameInventory = gameInventory;
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
                taskManager.Enqueue(() => chatGui.Print("Starting scan"));
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
            var slots = GetNearestTrackedPlot(false)?.PlantingHoles.Count ?? 8;
            state.Designs.Add(PlotPlan.CreateEmptyWithSlots(slots, "New plan"));
            logService.Info($"Current design count: {state.Designs.Count}");
            saveManager.WriteCharacterSave(state);
            return index;
        }

        private Dictionary<uint, int> GetExpectedItemAmounts(PlotPlan design, bool fertilize)
        {
            Dictionary<uint, int> expectedItems = new(); // ItemId, quantity expected
            if (fertilize)
            {
                expectedItems.Add(GlobalData.FishmealId, design.PlotHolePlans.Count);

            }
            foreach (var plan in design.PlotHolePlans)
            {
                expectedItems.IncrementOrSet(plan.DesignatedSeed);
                expectedItems.IncrementOrSet(plan.DesignatedSoil);
            }
            expectedItems.Remove(0);

            return expectedItems;
        }
        public ResourcesCheckResult CheckResourceAvailability(PlotPlan design, bool fertilize)
        {
            var expectedItems = GetExpectedItemAmounts(design, fertilize);

            Dictionary<uint, int> presentItems = new Dictionary<uint, int>();

            GameInventoryType[] inventories = [GameInventoryType.Inventory1, GameInventoryType.Inventory2,
                                                GameInventoryType.Inventory3, GameInventoryType.Inventory4];

            foreach (GameInventoryType type in inventories)
            {
                var subInv = gameInventory.GetInventoryItems(type);
                for (int i = 0; i < subInv.Length; i++)
                {
                    GameInventoryItem slot = subInv[i];
                    if (expectedItems.ContainsKey(slot.ItemId))
                    {
                        presentItems.Add(slot.ItemId, slot.Quantity);
                    }
                }
            }

            var result = new ResourcesCheckResult();
            foreach (var keyvaluepair in expectedItems)
            {
                result.Entries.Add(new ResourcesCheckEntry()
                {
                    ItemId = keyvaluepair.Key,
                    ItemName = globalData.GetGardeningItemName(keyvaluepair.Key),
                    ExpectedAmount = keyvaluepair.Value,
                    ActualAmount = presentItems.ContainsKey(keyvaluepair.Key) ? presentItems[keyvaluepair.Key] : 0
                });
            }

            return result;
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
                = state.Plots.Select(x => (x, Math.Abs(Vector3.Distance(x.Location, playerLocation))))
                .Where(tuple => tuple.Item2 < GlobalData.MaxScanDistance);

            try
            {
                (Plot nearestPlot, float distance) = plotsWithDistances.OrderBy(t => t.distance).First();
                logService.Debug($"Nearest plot found with {nearestPlot.PlantingHoles.Count} slots at distance {distance}");
                return nearestPlot;
            }
            catch (InvalidOperationException)
            {
                logService.Debug("Could not register nearest plot: no plots in the immediate area");
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

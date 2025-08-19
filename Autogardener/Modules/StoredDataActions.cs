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
    // Actions that do not directly interact witht he plots.
    public class StoredDataActions
    {
        private readonly ILogService logService;
        private readonly IChatGui chatGui;
        private readonly ISaveManager<CharacterSaveState> saveManager;
        private readonly GlobalData globalData;
        private readonly PlotWatcher plotWatcher;
        private readonly IClientState clientState;
        private readonly IGameInventory gameInventory;
        private readonly InGameActions inGameActions;

        
        public StoredDataActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
            GlobalData globalData, PlotWatcher plotWatcher, IClientState clientState,
            IGameInventory gameInventory, InGameActions ingameActions)
        {
            this.logService = logService;
            this.chatGui = chatGui;
            this.saveManager = saveManager;
            this.globalData = globalData;
            this.plotWatcher = plotWatcher;
            this.clientState = clientState;
            this.gameInventory = gameInventory;
            this.inGameActions = ingameActions;
        }

        public void RegisterNearestPlotPatch()
        {
            
            var player = clientState.LocalPlayer;
            if (player == null)
            {
                logService.Warning("Attempted to register nearest plot patch, but local player is null.");
                return;
            }

            var charState = saveManager.GetCharacterSaveInMemory();

            PlotPatch? plotPatch = GetNearestTrackedPlotPatch(true);
            if (plotPatch == null)
            {
                logService.Warning("No plot patch is near");
                chatGui.PrintError("No plot patch is near");
                return;
            }
            PlotPatch? alreadySeenPlot = charState.Plots.FirstOrDefault(p => p.Equals(plotPatch));
            if (alreadySeenPlot != null)
            {
                plotPatch = alreadySeenPlot;
            }
            else
            {
                charState.Plots.Add(plotPatch);
            }

            if (plotPatch == null)
            {
                logService.Warning("Could not get the nearest plot patch");
                return;
            }

            inGameActions.ScanPlot(plotPatch);
        }

        public int CreateNewDesign()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            var index = state.Designs.Count;
            var slots = GetNearestTrackedPlotPatch(false)?.Plots.Count ?? 8;
            state.Designs.Add(PlotPatchDesign.CreateEmptyWithSlots(slots, "New design"));
            logService.Info($"Current design count: {state.Designs.Count}");
            saveManager.WriteCharacterSave(state);
            return index;
        }

        public ResourcesCheckResult CheckResourceAvailability(PlotPatchDesign design, bool fertilize)
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

        private Dictionary<uint, int> GetExpectedItemAmounts(PlotPatchDesign design, bool fertilize)
        {
            Dictionary<uint, int> expectedItems = new(); // ItemId, quantity expected
            if (fertilize)
            {
                expectedItems.Add(GlobalData.FishmealId, design.PlotDesigns.Count);

            }
            foreach (var plan in design.PlotDesigns)
            {
                expectedItems.IncrementOrSet(plan.DesignatedSeed);
                expectedItems.IncrementOrSet(plan.DesignatedSoil);
            }
            expectedItems.Remove(0);

            return expectedItems;
        }
               
        public PlotPatch? GetNearestTrackedPlotPatch(bool addNewPlots)
        {
            var state = saveManager.GetCharacterSaveInMemory();            
            if (addNewPlots)
            {
                plotWatcher.UpdatePlotPatchList();
            }

            Vector3 playerLocation = clientState.LocalPlayer?.Position ?? Vector3.Zero;
            if (playerLocation == Vector3.Zero)
            {
                logService.Debug("Player location is null. Can't register nearest plot.");
                return null;
            }

            IEnumerable<(PlotPatch plotPatch, float distance)> plotsPatchesWithDistances
                = state.Plots.Select(x => (x, Math.Abs(Vector3.Distance(x.Location, playerLocation))))
                .Where(tuple => tuple.Item2 < GlobalData.MaxInteractDistance);

            try
            {
                (PlotPatch nearestPlotPatch, float distance) = plotsPatchesWithDistances.OrderBy(t => t.distance).First();
                //logService.Debug($"Nearest plot found with {nearestPlotPatch.Plots.Count} slots at distance {distance}");
                return nearestPlotPatch;
            }
            catch (InvalidOperationException)
            {
                logService.Debug("Could not register nearest plot: no plots in the immediate area");
                return null;
            }
        }

        public bool ApplyDesign(ref PlotPatch plotPatch, PlotPatchDesign design)
        {
            if (plotPatch.Plots.Count != design.PlotDesigns.Count)
            {
                chatGui.PrintError("Plot and design do not match!");
                return false;
            }
            for (var i = 0; i < plotPatch.Plots.Count; i++){
                var plot = plotPatch.Plots[i];
                var plotDesign = design.PlotDesigns[i];
                plot.Design = plotDesign;
            }

            return true;
        }

        public HashSet<uint> GetItemIdsOfSoilsAndSeedsInInventory()
        {
            GameInventoryType[] inventories = [GameInventoryType.Inventory1, GameInventoryType.Inventory2,
                                                GameInventoryType.Inventory3, GameInventoryType.Inventory4];

            HashSet<uint> result = new();
            foreach (GameInventoryType type in inventories)
            {

                var subInv = gameInventory.GetInventoryItems(type);
                for (int i = 0; i < subInv.Length; i++)
                {
                    GameInventoryItem slot = subInv[i];
                    if (globalData.Seeds.ContainsKey(slot.ItemId) || globalData.Soils.ContainsKey(slot.ItemId))
                    {
                        result.Add(slot.ItemId);
                    }
                }
            }

            return result;
        }
    }
}

using Autogardener.Model;
using Autogardener.Model.Plots;
using Autogardener.Modules.Territory;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
using ECommons.Throttlers;
using System.Linq;

namespace Autogardener.Modules
{
    public class PlotWatcher
    {
        private static readonly uint HighlightColor = ImGui.GetColorU32(new Vector4(0, 1, 0, 1));
        private static readonly uint HighlightColor2 = ImGui.GetColorU32(new Vector4(0, 1f, 0.7f, 1));
        private static readonly uint HighlightColor3 = ImGui.GetColorU32(new Vector4(1, 0, 0, 1));
        private readonly ILogService log;
        private readonly IObjectTable objectTable;
        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly IGameGui gameGui;
        private readonly TaskManager taskManager;
        private readonly ISaveManager<CharacterSaveState> saveManager;
        private readonly TerritoryWatcher territoryWatcher;
        private readonly IChatGui chatGui;
        private readonly IDataManager dataManager;
        private bool drawHighlights = true;

        public PlotWatcher(ILogService log, IObjectTable objectTable, IClientState clientState,
            IFramework framework, IGameGui gameGui, TaskManager taskManager, ISaveManager<CharacterSaveState> saveManager,
            TerritoryWatcher territoryWatcher, IChatGui chatGui, IDataManager dataManager)
        {        
            this.log = log;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            this.gameGui = gameGui;
            this.taskManager = taskManager;
            this.saveManager = saveManager;
            this.territoryWatcher = territoryWatcher;
            this.chatGui = chatGui;
            this.dataManager = dataManager;
            //this.framework.RunOnFrameworkThread(UpdatePlotList);
            // Add an "scan" button.
        }

        public void ToggleDrawHighlights()
        {
            drawHighlights = !drawHighlights;
        }

        public void HighlightPlots()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            List<PlotHighlightData> points = new();
            foreach (var plot in FilterByTerritory(state.Plots))
            {
                if (gameGui.WorldToScreen(plot.GetLocation(), out var screenPos))
                {
                    points.Add(new PlotHighlightData(screenPos, plot.Name, plot.DesignName));
                }
            }

            DrawHighlights(points);
        }

        private void DrawHighlights(List<PlotHighlightData> data)
        {
            if (!drawHighlights)
            {
                return;
            }
            ImGui.GetBackgroundDrawList().PushClipRect(ImGuiHelpers.MainViewport.Pos, ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size, false);

            foreach (var dataPoint in data)
            {
                ImGui.GetBackgroundDrawList()
                    .AddText(ImGui.GetIO().FontDefault, 40, new Vector2(dataPoint.Position.X + 10, dataPoint.Position.Y - 40), HighlightColor, dataPoint.PlotName);
                ImGui.GetBackgroundDrawList()
                    .AddText(ImGui.GetIO().FontDefault, 40, new Vector2(dataPoint.Position.X + 10, dataPoint.Position.Y - 40), HighlightColor, dataPoint.PlotName);

                ImGui.GetBackgroundDrawList()
                    .AddText(ImGui.GetIO().FontDefault, 35, new Vector2(dataPoint.Position.X + 10, dataPoint.Position.Y - 10), HighlightColor2, dataPoint.DesignName);

                ImGui.GetBackgroundDrawList()
                    .AddText(ImGui.GetIO().FontDefault, 35, new Vector2(dataPoint.Position.X + 10, dataPoint.Position.Y - 10), HighlightColor2, dataPoint.DesignName);

                ImGui.GetBackgroundDrawList().AddCircleFilled(dataPoint.Position, 5, HighlightColor3);
                ImGui.GetBackgroundDrawList().AddCircleFilled(dataPoint.Position, 5, HighlightColor3);
            }

            ImGui.GetBackgroundDrawList().PopClipRect();
        }

        public void ListNearbyPlots()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            foreach (var plot in state.Plots)
            {
                log.Info($"{plot.Name} =========================");
                foreach (var hole in plot.Plots)
                {
                    log.Info($"ObjId: {hole.GameObjectId} X: {hole.Location.X} Y: {hole.Location.Y} Z: {hole.Location.Z}");
                }
            }

            if (clientState.LocalPlayer != null)
            {
                log.Info($"Player pos: X:{clientState.LocalPlayer.Position.X} " +
                    $"Y: {clientState.LocalPlayer.Position.Y} " +
                    $"Z: {clientState.LocalPlayer.Position.Z}");
            }
        }

        public void CheckForGoneOrMovedPlotsThrottled(List<PlotPatch> patches)
        {
            if (EzThrottler.Throttle("Check for gone or moved plots", 5000))
            {
                bool warnOfMissing = EzThrottler.Throttle("Print missing plot warning", 15000);
                
                foreach (PlotPatch patch in FilterByTerritory(patches))
                {
                    var gameObject = objectTable.EventObjects.FirstOrDefault(o => o.GameObjectId == patch.GameObjectId);
                    if (gameObject == null)
                    {
                        gameObject = objectTable.FirstOrDefault(o => o.GameObjectId == patch.Plots.FirstOrDefault()?.GameObjectId);
                    }
                    if (gameObject == null)
                    {
                        if (warnOfMissing)
                        {
                            log.Warning($"Name {patch.Name} Terr:{patch.TerritoryPrefix} GoId: {patch.Plots.FirstOrDefault()?.GameObjectId}");
                            var warningString = $"Plot {patch.Name} should be here, but Autogardener can't find it. Try removing it and rescanning.";
                            log.Warning(warningString);
                            chatGui.PrintError(warningString);
                        }

                        continue;
                    }
                }
            }
        }

        public void UpdatePlotPatchList()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            var discoveredPlots = DiscoverPlots();
            discoveredPlots = FilterOutFlowerPots(discoveredPlots);
            UpdateLocationOfMovedPlots(state.Plots, discoveredPlots);
            discoveredPlots = FilterByDistance(discoveredPlots, GlobalData.MaxInteractDistance);            
            List<PlotPatch> combinedPlots = MergePlotLists(state.Plots, discoveredPlots);
            bool saveToFile = HavePlotsChanged(state.Plots, combinedPlots);
            state.Plots = combinedPlots;
            if (saveToFile)
            {
                saveManager.WriteCharacterSave();
            }
        }

        public List<PlotPatch> FilterOutFlowerPots(List<PlotPatch> plotPatches)
        {
            int flowerPotsCount = plotPatches.Where(p => p.Plots.Count == 1).Count();
            if (flowerPotsCount > 0)
            {
                chatGui.Print($"{flowerPotsCount} flowerpots detected. Those are not supported, sorry.");
            }
            return plotPatches.Where(p => p.Plots.Count > 1).ToList();
        }

        private void UpdateLocationOfMovedPlots(List<PlotPatch> old, List<PlotPatch> scanned)
        {
            foreach (var oldPlot in old)
            {
                var matching = scanned.FirstOrDefault(p => p.Equals(oldPlot));
                if (matching != null)
                {
                    if (Vector3.Distance(oldPlot.GetLocation(), matching.GetLocation()) > 0.1)
                    {
                        log.Warning($"Old: {oldPlot.GetLocation()}");
                        log.Warning($"New: {matching.GetLocation()}");
                        //Plot moved
                        oldPlot.SetLocation(matching.GetLocation());
                        log.Info($"Updated location for patch {oldPlot.Name}");
                        chatGui.Print($"Updated location for patch {oldPlot.Name}");
                        saveManager.WriteCharacterSave();
                    }
                }
            }
        }

        public void FlagPlot(PlotPatch plot)
        {
            var plotsInArea = DiscoverPlots();
            var existing = plotsInArea.FirstOrDefault(p => p.Equals(plot));
            if (existing != null)
            {
                if (Vector3.Distance(plot.GetLocation(), existing.GetLocation()) > 1)
                {
                    chatGui.PrintError("Coordinates have changed. The plot was moved.");
                }
                var map = dataManager.GetExcelSheet<Lumina.Excel.Sheets.Map>().GetRow(clientState.MapId);
                var gameObject = objectTable.FirstOrDefault(o => o.GameObjectId == existing.GameObjectId);
                if (gameObject == null) return;                
                Vector3 mapCoords = Dalamud.Utility.MapUtil.GetMapCoordinates(gameObject, true);
                var mapPayload = new MapLinkPayload(clientState.TerritoryType, clientState.MapId,
                    mapCoords.X, mapCoords.Y);
                
                gameGui.OpenMapWithMapLink(mapPayload);
                log.Info($"X: {existing.GetLocation().X}, Y: {existing.GetLocation().Y}, Z: {existing.GetLocation().Z}");
                return;
            }

            chatGui.PrintError($"Plot \"{plot.Name}\" is not in this area. " +
                $"Maybe it was deleted, maybe it was in other ward, maybe you're in the wrong map.");
        }

        private bool HavePlotsChanged(List<PlotPatch> original, List<PlotPatch> newCombined)
        {
            if (original.Count != newCombined.Count) return true;
            for (int i = 0; i < original.Count; i++)
            {
                if (!original[i].Equals(newCombined[i])) return true;
            }

            return false;
        }
        private List<PlotPatch> MergePlotLists(List<PlotPatch> known, List<PlotPatch> scanned)
        {
            List<PlotPatch> combinedPlots = new List<PlotPatch>(known);
            foreach (var plot in scanned)
            {
                if (!known.Any(p => p.Equals(plot)))
                {
                    combinedPlots.Add(plot);
                }
            }

            return combinedPlots;
        }

        private List<PlotPatch> FilterByTerritory(List<PlotPatch> plots)
        {
            var territoryPrefix = territoryWatcher.GetTerritoryPrefix();
            if (territoryPrefix.IsNullOrEmpty())
            {
                return new List<PlotPatch>();
            }

            return plots.Where(p => p.TerritoryPrefix == territoryPrefix).ToList();
        }

        private List<PlotPatch> FilterByDistance(List<PlotPatch> plots, float maxDistance)
        {
            var playerPos = clientState.LocalPlayer?.Position;
            if (playerPos == null)
            {
                return plots;
            }

            var result = plots.Where(p => Vector3.Distance(p.GetLocation(), playerPos ?? Vector3.Zero) < maxDistance).ToList();
            return result;
        }

        private List<PlotPatch> DiscoverPlots()
        {
            List<PlotPatch> foundPlotPatches = new();
            PlotPatch? plotPatchInConstruction = null;
            var plotObjects = objectTable
                .Where(o => o != null && GlobalData.GardenPlotDataIds.Contains(o.BaseId)).OrderBy(o => o.GameObjectId).ToList();
            var patchNumber = 1;
            int plotCounter = 0;
            IGameObject? lastPlotObject = null;
            foreach (var plotObject in plotObjects)
            {
                log.Debug($"Building plant hole {plotObject.GameObjectId}");
                if (plotPatchInConstruction == null
                    || Math.Abs((decimal)(plotPatchInConstruction?.Plots.Last().GameObjectId ?? 0) - plotObject.GameObjectId) != 1 //Discontiguous id
                    || plotCounter == 8 //Max amount of plots in a patch
                    || lastPlotObject != null && lastPlotObject.Position != plotObject.Position) // All plots in a patch have the same position. Flowerpots do not.
                {
                    log.Debug("New Plot object created");
                    // Discontiguous, or first hole. Create new plot.
                    if (plotPatchInConstruction != null)
                    {
                        FinalizeName(plotPatchInConstruction, patchNumber);
                        foundPlotPatches.Add(plotPatchInConstruction);
                        plotCounter = 0;
                    }

                    plotPatchInConstruction = new PlotPatch($"temporary name", territoryWatcher.GetTerritoryPrefix());
                    patchNumber++;
                }

                var newHole = new Plot(plotObject.GameObjectId, plotObject.EntityId,
                                                    plotObject.ObjectIndex, plotObject.BaseId, new SerializableVector3(plotObject.Position));
                plotPatchInConstruction?.Plots.Add(newHole);
                plotCounter++;
                lastPlotObject = plotObject;
            }

            if (plotPatchInConstruction != null)
            {
                FinalizeName(plotPatchInConstruction, patchNumber);

                foundPlotPatches.Add(plotPatchInConstruction);
            }

            foundPlotPatches = foundPlotPatches.Where(p => p.Plots.Count != 0).ToList();
            return foundPlotPatches;
        }

        private PlotPatch FinalizeName(PlotPatch patch, int patchNumber)
        {
            var territoryPrefix = territoryWatcher.GetTerritoryPrefix();
            string plotTypeName = patch.Plots.Count == 1 ? "Flowerpot" : $"x{patch.Plots.Count}";
            patch.Name = $"{territoryPrefix}{plotTypeName} {patchNumber}";

            return patch;
        }
    }
}

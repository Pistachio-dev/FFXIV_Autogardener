using Autogardener.Model;
using Autogardener.Model.Plots;
using Autogardener.Modules.Territory;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
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
        private bool drawHighlights = true;

        public PlotWatcher(ILogService log, IObjectTable objectTable, IClientState clientState,
            IFramework framework, IGameGui gameGui, TaskManager taskManager, ISaveManager<CharacterSaveState> saveManager,
            TerritoryWatcher territoryWatcher)
        {        
            this.log = log;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            this.gameGui = gameGui;
            this.taskManager = taskManager;
            this.saveManager = saveManager;
            this.territoryWatcher = territoryWatcher;
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
            foreach (var plot in state.Plots)
            {
                if (gameGui.WorldToScreen(plot.Location, out var screenPos))
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

        public void UpdatePlotPatchList()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            var discoveredPlots = DiscoverPlots();
            discoveredPlots = FilterByDistance(discoveredPlots, GlobalData.MaxInteractDistance);
            List<PlotPatch> combinedPlots = MergePlotLists(state.Plots, discoveredPlots);
            bool saveToFile = HavePlotsChanged(state.Plots, combinedPlots);
            state.Plots = combinedPlots;
            if (saveToFile)
            {
                saveManager.WriteCharacterSave();
            }
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

        private List<PlotPatch> FilterByDistance(List<PlotPatch> plots, float maxDistance)
        {
            var playerPos = clientState.LocalPlayer?.Position;
            if (playerPos == null)
            {
                return plots;
            }

            var result = plots.Where(p => Vector3.Distance(p.Location, playerPos ?? Vector3.Zero) < maxDistance).ToList();
            return result;
        }

        private List<PlotPatch> DiscoverPlots()
        {
            List<PlotPatch> foundPlotPatches = new();
            PlotPatch? plotPatchInConstruction = null;
            var plotObjects = objectTable
                .Where(o => o != null && GlobalData.GardenPlotDataIds.Contains(o.DataId)).OrderBy(o => o.GameObjectId).ToList();
            var plotNumber = 1;
            int plotCounter = 0;
            IGameObject? lastPlotObject = null;
            foreach (var plotObject in plotObjects)
            {
                log.Debug($"Building plant hole {plotObject.GameObjectId}");
                if (plotPatchInConstruction == null
                    || Math.Abs((decimal)(plotPatchInConstruction?.Plots.Last().GameObjectId ?? 0) - plotObject.GameObjectId) != 1 //Discontiguous id
                    || plotCounter == 8 //Max amount of plots in a patch
                    || lastPlotObject != null && lastPlotObject.Position != plotObject.Position) // All plots in a patch have the same position
                {
                    log.Debug("New Plot object created");
                    // Discontiguous, or first hole. Create new plot.
                    if (plotPatchInConstruction != null)
                    {
                        var territoryPrefix = territoryWatcher.GetTerritoryPrefix();
                        string plotTypeName = plotPatchInConstruction.Plots.Count == 1 ? "Flowerpot" : $"x{plotPatchInConstruction.Plots.Count}";
                        plotPatchInConstruction.Name = $"{territoryPrefix} {plotTypeName} {plotNumber}";
                        foundPlotPatches.Add(plotPatchInConstruction);
                        plotCounter = 0;
                    }

                    plotPatchInConstruction = new PlotPatch($"temporary name", territoryWatcher.GetTerritoryPrefix());
                    plotNumber++;
                }

                var newHole = new Plot(plotObject.GameObjectId, plotObject.EntityId,
                                                    plotObject.ObjectIndex, plotObject.DataId, new SerializableVector3(plotObject.Position));
                plotPatchInConstruction?.Plots.Add(newHole);
                plotCounter++;
                lastPlotObject = plotObject;
            }

            if (plotPatchInConstruction != null)
            {
                foundPlotPatches.Add(plotPatchInConstruction);
            }

            foundPlotPatches = foundPlotPatches.Where(p => p.Plots.Count != 0).ToList();
            return foundPlotPatches;
        }
    }
}

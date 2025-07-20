using Autogardener.Model;
using Autogardener.Model.Plots;
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
        private readonly ILogService log;
        private readonly IObjectTable objectTable;
        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly IGameGui gameGui;
        private readonly TaskManager taskManager;
        private readonly ISaveManager<CharacterSaveState> saveManager;
        private bool drawHighlights = true;

        public PlotWatcher(ILogService log, IObjectTable objectTable, IClientState clientState,
            IFramework framework, IGameGui gameGui, TaskManager taskManager, ISaveManager<CharacterSaveState> saveManager)
        {        
            this.log = log;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            this.gameGui = gameGui;
            this.taskManager = taskManager;
            this.saveManager = saveManager;
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
                    points.Add(new PlotHighlightData(screenPos, plot.Alias, plot.DesignName));
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
                    .AddText(new Vector2(dataPoint.Position.X, dataPoint.Position.Y - 40), HighlightColor, dataPoint.PlotName);

                ImGui.GetBackgroundDrawList()
                    .AddText(new Vector2(dataPoint.Position.X, dataPoint.Position.Y - 20), HighlightColor, dataPoint.DesignName);

                ImGui.GetBackgroundDrawList().AddCircleFilled(dataPoint.Position, 5, HighlightColor);
            }

            ImGui.GetBackgroundDrawList().PopClipRect();
        }

        public void ListNearbyPlots()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            foreach (var plot in state.Plots)
            {
                log.Info($"{plot.Alias} =========================");
                foreach (var hole in plot.PlantingHoles)
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

        public void UpdatePlotList()
        {
            var state = saveManager.GetCharacterSaveInMemory();
            var discoveredPlots = DiscoverPlots();
            discoveredPlots = FilterByDistance(discoveredPlots, GlobalData.MaxScanDistance);
            List<Plot> combinedPlots = MergePlotLists(state.Plots, discoveredPlots);
            bool saveToFile = HavePlotsChanged(state.Plots, combinedPlots);
            state.Plots = combinedPlots;
            if (saveToFile)
            {
                saveManager.WriteCharacterSave();
            }
        }

        private bool HavePlotsChanged(List<Plot> original, List<Plot> newCombined)
        {
            if (original.Count != newCombined.Count) return true;
            for (int i = 0; i < original.Count; i++)
            {
                if (!original[i].Equals(newCombined[i])) return true;
            }

            return false;
        }
        private List<Plot> MergePlotLists(List<Plot> known, List<Plot> scanned)
        {
            List<Plot> combinedPlots = new List<Plot>(known);
            foreach (var plot in scanned)
            {
                if (!known.Any(p => p.Equals(plot)))
                {
                    combinedPlots.Add(plot);
                }
            }

            return combinedPlots;
        }

        public List<Plot> FilterByDistance(List<Plot> plots, float maxDistance)
        {
            var playerPos = clientState.LocalPlayer?.Position;
            if (playerPos == null)
            {
                return plots;
            }

            var result = plots.Where(p => Vector3.Distance(p.Location, playerPos ?? Vector3.Zero) < maxDistance).ToList();
            return result;
        }

        public List<Plot> DiscoverPlots()
        {
            List<Plot> foundPlots = new();
            Plot? plotInConstruction = null;
            var plotHoleObjects = objectTable
                .Where(o => o != null && GlobalData.GardenPlotDataIds.Contains(o.DataId)).OrderBy(o => o.GameObjectId).ToList();
            var plotNumber = 1;
            int plotHoleCounter = 0;
            foreach (var plotHole in plotHoleObjects)
            {
                log.Debug($"Building plant hole {plotHole.GameObjectId}");
                if (plotInConstruction == null
                    || Math.Abs((decimal)(plotInConstruction?.PlantingHoles.Last().GameObjectId ?? 0) - plotHole.GameObjectId) != 1 //Discontiguous id
                    || plotHoleCounter == 8)
                {
                    log.Debug("New Plot object created");
                    // Discontiguous, or first hole. Create new plot.
                    if (plotInConstruction != null)
                    {
                        foundPlots.Add(plotInConstruction);
                        plotHoleCounter = 0;
                    }

                    plotInConstruction = new Plot($"Plot {plotNumber}");
                    plotNumber++;
                }

                var newHole = new PlotHole(plotHole.GameObjectId, plotHole.EntityId,
                                                    plotHole.ObjectIndex, plotHole.DataId, plotHole.Position);
                plotInConstruction?.PlantingHoles.Add(newHole);
                plotHoleCounter++;
            }

            if (plotInConstruction != null)
            {
                foundPlots.Add(plotInConstruction);
            }

            foundPlots = foundPlots.Where(p => p.PlantingHoles.Count != 0).ToList();
            return foundPlots;
        }
    }
}

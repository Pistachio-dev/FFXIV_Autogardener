using Autogardener.Model.Plots;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
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
        public List<Plot> Plots { get; set; } = new();
        private bool drawHighlights = true;

        public PlotWatcher(ILogService log, IObjectTable objectTable, IClientState clientState, IFramework framework, IGameGui gameGui, TaskManager taskManager)
        {
            this.log = log;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            this.gameGui = gameGui;
            this.taskManager = taskManager;
            this.framework.RunOnFrameworkThread(UpdatePlotList);
            // Add an "scan" button.
        }

        public void ToggleDrawHighlights()
        {
            drawHighlights = !drawHighlights;
        }

        public void HighlightPlots()
        {
            List<PlotHighlightData> points = new();
            foreach (var plot in Plots)
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
            foreach (var plot in Plots)
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
            Plots = DiscoverPlots();
        }

        public List<Plot> DiscoverPlots()
        {
            List<Plot> foundPlots = new();
            Plot? plotInConstruction = null;
            var plotHoleObjects = objectTable
                .Where(o => o != null && GlobalData.GardenPlotDataIds.Contains(o.DataId)).OrderBy(o => o.GameObjectId).ToList();
            log.Info("Total planting holes discovered: " + plotHoleObjects.Count);
            var plotNumber = 1;
            foreach (var plotHole in plotHoleObjects)
            {
                if (plotInConstruction == null
                    || plotInConstruction?.PlantingHoles.Last().ObjectIndex + 1 != plotHole.ObjectIndex)
                {
                    // Discontiguous, or first hole. Create new plot.
                    if (plotInConstruction != null)
                    {
                        foundPlots.Add(plotInConstruction);
                    }

                    plotInConstruction = new Plot($"Plot {plotNumber}");
                    plotNumber++;
                }

                var newHole = new PlotHole(plotHole.GameObjectId, plotHole.EntityId,
                                                    plotHole.ObjectIndex, plotHole.DataId, plotHole.Position);
                plotInConstruction?.PlantingHoles.Add(newHole);
            }

            if (plotInConstruction != null)
            {
                foundPlots.Add(plotInConstruction);
            }

            log.Info("Total plots formed: " + foundPlots.Count);

            return foundPlots;
        }
    }
}

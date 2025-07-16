using Autogardener.Model;
using Autogardener.Model.Plot;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using ECommons.Automation.LegacyTaskManager;
using System.Linq;

namespace Autogardener.Modules
{
    internal class PlotWatcher
    {
        private static readonly uint HighlightColor = ImGui.GetColorU32(new Vector4(0, 1, 0, 1));
        private readonly ILogService log;
        private readonly IObjectTable objectTable;
        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly IGameGui gameGui;
        private readonly TaskManager taskManager;
        private List<Plot> plots = new();
        private bool drawHighlights = false;

        public PlotWatcher(ILogService log, IObjectTable objectTable, IClientState clientState, IFramework framework, IGameGui gameGui)
        {
            this.log = log;
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            this.gameGui = gameGui;
            this.framework.RunOnFrameworkThread(UpdatePlotList);
            // Add an "scan" button. 
        }

        public void ToggleDrawHighlights()
        {
            drawHighlights = !drawHighlights;
        }

        public void HighlightPlots()
        {
            List<(Vector2,string)> points = new();
            foreach (var plot in plots)
            {
                if (gameGui.WorldToScreen(plot.Location, out var screenPos))
                {
                    points.Add((screenPos, plot.Alias));
                }
            }

            DrawHighlights(points);
        }

        private void DrawHighlights(List<(Vector2, string)> pointsWithNames)
        {

            if (!drawHighlights)
            {
                return;
            }
            ImGui.GetBackgroundDrawList().PushClipRect(ImGuiHelpers.MainViewport.Pos, ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size, false);

            foreach ((var point, var alias) in pointsWithNames)
            {
                ImGui.GetBackgroundDrawList().AddText(new Vector2(point.X, point.Y - 20), HighlightColor, alias);
                ImGui.GetBackgroundDrawList().AddCircleFilled(point, 5, HighlightColor);
            }
            
            ImGui.GetBackgroundDrawList().PopClipRect();

        }

        public void ListNearbyPlots()
        {
            foreach (var plot in plots)
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
            plots = DiscoverPlots();
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

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using ECommons.Automation.LegacyTaskManager;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.PlotRecognition
{
    internal class PlotWatcher
    {
        private readonly IObjectTable objectTable;
        private readonly IClientState clientState;
        private readonly IFramework framework;
        private readonly TaskManager taskManager;
        private List<Plot> plots;

        public PlotWatcher(IObjectTable objectTable, IClientState clientState, IFramework framework)
        {
            this.objectTable = objectTable;
            this.clientState = clientState;
            this.framework = framework;
            // Add an "scan" button. 
            this.framework.RunOnFrameworkThread(() => { plots = DiscoverPlots(); });                        
        }

        public void ListNearbyPlots()
        {
            foreach (var plot in plots)
            {
                Log.Information($"{plot.Alias} =========================");
                foreach (var hole in plot.PlantingHoles)
                {
                    Log.Information($"ObjId: {hole.GameObjectId} X: {hole.Location.X} Y: {hole.Location.Y} Z: {hole.Location.Z}");
                }
            }

            if (clientState.LocalPlayer != null)
            {
                Log.Information($"Player pos: X:{clientState.LocalPlayer.Position.X} " +
                    $"Y: {clientState.LocalPlayer.Position.Y} " +
                    $"Z: {clientState.LocalPlayer.Position.Z}");
            }
            
        }

        private void UpdatePlotList(ushort territoryId)
        {
            var plots = DiscoverPlots();
        }

        public List<Plot> DiscoverPlots()
        {
            List<Plot> foundPlots = new();
            Plot? plotInConstruction = null;
            List<IGameObject> plotHoleObjects = objectTable
                .Where(o => o != null && GlobalItemIds.GardenPlotDataIds.Contains(o.DataId)).OrderBy(o => o.GameObjectId).ToList();
            int plotNumber = 1;
            foreach (IGameObject plotHole in plotHoleObjects)
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

                if (plotInConstruction != null)
                {
                    PlotHole newHole = new PlotHole(plotHole.GameObjectId, plotHole.EntityId,
                                                        plotHole.ObjectIndex, plotHole.DataId, plotHole.Position);
                    plotInConstruction?.PlantingHoles.Add(newHole);
                }

            }

            if (plotInConstruction != null)
            {
                foundPlots.Add(plotInConstruction);
            }

            return foundPlots;
        }
    }
}

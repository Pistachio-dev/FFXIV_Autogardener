using Autogardener.Model;
using Autogardener.Model.Plots;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using DalamudBasics.SaveGames;
using ECommons.Automation.NeoTaskManager;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules
{
    // Actions that directly interact with the plots.
    public class InGameActions
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
        
        public InGameActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
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

        public void ScanPlot(Plot plot)
        {
            foreach (PlotHole plotHole in plot.PlantingHoles)
            {
                var plotOb = objectTable.SearchById(plotHole.GameObjectId);
                if (plotOb == null)
                {
                    throw new Exception($"Plot with objectdId {plotHole.GameObjectId} was not found in the object table");
                }
                plotHole.Initialize(plotOb);
                taskManager.Enqueue(() => chatGui.Print("Starting scan"));
                taskManager.Enqueue(() => commands.TargetObject(plotOb));
                taskManager.Enqueue(() => commands.InteractWithTargetPlot(), "InteractWithPlot", DefConfig);
                taskManager.Enqueue(() => commands.SetPlantTypeFromDialogue(plotHole), "Extract plant type", DefConfig);
                taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialogue", DefConfig);
                taskManager.Enqueue(() => commands.SelectActionString(globalData
                    .GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit)), "Select Quit", DefConfig);
                taskManager.EnqueueDelay(new Random().Next(200, 300));
            }

            taskManager.Enqueue(() => chatGui.Print("Scan complete! o7"));
            taskManager.Enqueue(() => { saveManager.WriteCharacterSave(); });
        }

        public void PlotPatchCare(Plot plot, bool fertilize, bool replant)
        {
            foreach (var plotHole in plot.PlantingHoles)
            {
                BeginPlotCare(plotHole, fertilize, replant);
            }
        }

        private void BeginPlotCare(PlotHole plotHole, bool fertilize, bool replant)
        {
            taskManager.Enqueue(() => TargetPlotGameObjectId(plotHole));
            taskManager.Enqueue(commands.SkipDialogueIfNeeded, "Skip dialogue", DefConfig);
            taskManager.Enqueue(() => SelectOption(plotHole, fertilize, replant));
        }

        private bool TargetPlotGameObjectId(PlotHole plotHole)
        {
            var plotOb = objectTable.SearchById(plotHole.GameObjectId);
            if (plotOb == null)
            {
                throw new Exception($"Plot with objectdId {plotHole.GameObjectId} was not found in the object table");
                chatGui.PrintError("Can't access plot. Did you move away?");
            }
            
            commands.TargetObject(plotOb);
            return true;
        }

        public void SeedTargetPlot(PlotHole plotHoleData)
        {
            var d = plotHoleData.Design;
            if (d == null || d.DesignatedSeed == 0 || d.DesignatedSoil == 0)
            {
                logService.Info("No seed or soil designated for this plot. Please make and assign a design.");
                return;
            }

            if (!commands.IsItemPresentInInventory(d.DesignatedSeed))
            {
                logService.Error($"Missing seed: {globalData.GetSeedStringName(d.DesignatedSeed)}. Can't plant.");
                return;
            }
            if (!commands.IsItemPresentInInventory(d.DesignatedSoil))
            {
                logService.Error($"Missing soil: {globalData.GetSoilStringName(d.DesignatedSoil)}");
                return;
            }

            taskManager.Enqueue(() => commands.SelectActionString("plant seeds"), "Select 'Plant Seeds' option", DefConfig);
            taskManager.Enqueue(() => 
                commands.PickSeedsAndSoil(plotHoleData.Design.DesignatedSeed, plotHoleData.Design.DesignatedSoil), DefConfig);
            taskManager.Enqueue(commands.ClickConfirmOnHousingGardeningAddon, "Click 'Confirm'", DefConfig);
            taskManager.Enqueue(commands.ConfirmYes, "Click 'Yes'", DefConfig);
            taskManager.Enqueue(() =>
            {
                plotHoleData.CurrentSeed = d.DesignatedSeed;
                plotHoleData.CurrentSoil = d.DesignatedSoil;
            });
        }

        private unsafe bool SelectOption(PlotHole plotHole, bool fertilize, bool replant)
        {
            if (TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString)
                && IsAddonReady(&addonSelectString->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addonSelectString).Entries.Select(x => x.ToString()).ToList();
                if (entries.Any(e => e?.Contains("Harvest Crop") ?? false))
                {
                    if (!(plotHole.Design?.DoNotHarvest ?? false))
                    {
                        taskManager.Enqueue(() => HarvestTargetPlot(plotHole, replant));
                    }                    
                }
                if (entries.Any(e => e?.Contains("Fertilize Crop") ?? false) && fertilize)
                {
                    taskManager.Enqueue(() => FertilizeTargetPlot(plotHole));
                }
                if (entries.Any(e => e?.Contains("Tend Crop") ?? false))
                {
                    taskManager.Enqueue(() => TendTargetPlot(plotHole));
                }
                if (entries.Any(e => e?.Contains("Plant Seeds") ?? false))
                {
                    taskManager.Enqueue(() => SeedTargetPlot(plotHole));
                }

                return true;
            }

            return false;

        }

        private void FertilizeTargetPlot(PlotHole plotHole)
        {
            if (!commands.IsItemPresentInInventory(GlobalData.FishmealId))
            {
                logService.Error("Out of fertilizer!");
                return;
            }

            taskManager.Enqueue(() => commands.SelectActionString("Fertilize Crop"), "Select 'Fertililze Crop' option", DefConfig);

            taskManager.Enqueue(() => commands.Fertilize());

            taskManager.Enqueue(() =>
            {
                plotHole.LastFertilizedUtc = DateTime.UtcNow;
            });
        }

        private void HarvestTargetPlot(PlotHole plotHole, bool replant)
        {
            taskManager.Enqueue(() => commands.SelectActionString("Harvest Crop"), "Select 'Harvest Crop' option", DefConfig);

            if (replant)
            {
                taskManager.Enqueue(() => commands.InteractWithTargetPlot());
                taskManager.Enqueue(() => commands.SkipDialogueIfNeeded());
                taskManager.Enqueue(() => SeedTargetPlot(plotHole));
            }

        }
        private void TendTargetPlot(PlotHole plot)
        {
            taskManager.Enqueue(() => commands.SelectActionString("Tend Crop"), "Select 'Tend Crop' option", DefConfig);
            taskManager.Enqueue(() =>
            {
                plot.LastTendedUtc = DateTime.UtcNow;
            });
        }

        private void RemoveCrop(bool expectConfirmationDialog)
        {
            // Let the player do this
            throw new NotImplementedException();
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

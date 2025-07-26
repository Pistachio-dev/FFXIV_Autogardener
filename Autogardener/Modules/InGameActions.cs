using Autogardener.Model;
using Autogardener.Model.Plots;
using Autogardener.Modules.Tasks;
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
        private readonly GardeningTaskManager taskManager;
        private readonly IGameInventory gameInventory;
        private readonly ErrorMessageMonitor errorMessageMonitor;

        public InGameActions(ILogService logService, IChatGui chatGui, ISaveManager<CharacterSaveState> saveManager,
            GlobalData globalData, PlotWatcher plotWatcher, Commands commands, Utils utils, IClientState clientState,
            IObjectTable objectTable, ITargetManager targetManager, GardeningTaskManager taskManager, IGameInventory gameInventory,
            ErrorMessageMonitor errorMessageMonitor)
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
            this.errorMessageMonitor = errorMessageMonitor;
        }

        public void ScanPlot(Plot plot)
        {
            int index = 1;
            foreach (PlotHole plotHole in plot.PlantingHoles)
            {
                taskManager.EnqueueSuperTask(() => ScanPlotHole(plotHole), $"Scan plot hole {index}");
                index++;
            }

            taskManager.Enqueue(() => chatGui.Print("Scan complete! o7"), "Scan complete notification");
            taskManager.Enqueue(() => { saveManager.WriteCharacterSave(); }, "Save scan results");
            taskManager.StartProcessingQueuedTasks();
        }

        private bool ScanPlotHole(PlotHole plotHole)
        {
            var plotOb = objectTable.SearchById(plotHole.GameObjectId);
            if (plotOb == null)
            {
                throw new Exception($"Plot with objectdId {plotHole.GameObjectId} was not found in the object table");
            }
            plotHole.Initialize(plotOb);
            taskManager.Enqueue(() => chatGui.Print("Starting scan"), "Scan start");
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plotHole), "InteractWithPlot");
            taskManager.Enqueue(() => commands.SetPlantTypeFromDialogue(plotHole), "Extract plant type");
            taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialogue");
            taskManager.Enqueue(() => commands.SelectActionString(globalData
                .GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit)), "Select Quit");

            return true;
        }
        public void PlotPatchCare(Plot plot, bool fertilize, bool replant)
        {
            int index = 1;

            foreach (var plotHole in plot.PlantingHoles)
            {
                taskManager.EnqueueSuperTask(() => BeginPlotCare(plotHole, fertilize, replant), $"Plot care for index {index}");
                index++;
            }
            taskManager.StartProcessingQueuedTasks();
        }

        public void TargetPlantingHoleCare(Plot plot, bool fertilize, bool replant)
        {
            PlotHole? plotHole = plot.PlantingHoles.FirstOrDefault(p => p.GameObjectId == clientState.LocalPlayer?.TargetObject?.GameObjectId);
            if (plotHole != null)
            {
                taskManager.EnqueueSuperTask(() => BeginPlotCare(plotHole, fertilize, replant), $"Plot care for targeted plot");
                taskManager.StartProcessingQueuedTasks();
            }
            
        }

        private void BeginPlotCare(PlotHole plotHole, bool fertilize, bool replant)
        {
            //taskManager.Enqueue(() => TargetPlotGameObjectId(plotHole), "Target plot");
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plotHole), "Interact with plot");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.Enqueue(commands.SkipDialogueIfNeeded, "Skip dialogue");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.EnqueueSuperTask(() => SelectOption(plotHole, fertilize, replant), "Select branching option");
        }

        private bool TargetAndInteractWithPlot(PlotHole plotHole)
        {
            var targetingResult = TargetPlotGameObjectId(plotHole);
            if (targetingResult == false)
            {
                return false;
            }

            if (targetManager.Target?.GameObjectId != plotHole.GameObjectId){
                return false;
            }

            return commands.InteractWithPlot();
        }
        private bool TargetPlotGameObjectId(PlotHole plotHole)
        {
            var plotOb = objectTable.SearchById(plotHole.GameObjectId);
            if (plotOb == null)
            {
                chatGui.PrintError("Can't access plot. Did you move away?");
                throw new Exception($"Plot with objectdId {plotHole.GameObjectId} was not found in the object table");                
            }
            
            commands.TargetObject(plotOb);
            return true;
        }

        private void PrintErrorAndSelectQuit(string errorMessage)
        {
            logService.Info(errorMessage);
            chatGui.PrintError(errorMessage);
            taskManager.Enqueue(() => commands.SelectActionString("Quit"), "Select 'Quit' option");
        }

        public void SeedTargetPlot(PlotHole plotHoleData)
        {
            var d = plotHoleData.Design;
            if (d == null || d.DesignatedSeed == 0 || d.DesignatedSoil == 0)
            {
                PrintErrorAndSelectQuit("No seed or soil designated for this plot. Please make and assign a design.");
                return;
            }

            if (!commands.IsItemPresentInInventory(d.DesignatedSeed))
            {
                PrintErrorAndSelectQuit($"Missing seed: {globalData.GetSeedStringName(d.DesignatedSeed)}. Can't plant.");
                return;
            }
            if (!commands.IsItemPresentInInventory(d.DesignatedSoil))
            {
                PrintErrorAndSelectQuit($"Missing soil: {globalData.GetSoilStringName(d.DesignatedSoil)}");
                return;
            }

            taskManager.Enqueue(() => commands.SelectActionString("plant seeds"), "Select 'Plant Seeds' option");
            taskManager.EnqueueSuperTask(() =>
                commands.PickSeedsAndSoil(plotHoleData.Design!.DesignatedSeed, plotHoleData.Design.DesignatedSoil), "Pick seeds and soil");
            taskManager.EnqueueDelayMs(300);
            taskManager.Enqueue(commands.ClickConfirmOnHousingGardeningAddon, "Click 'Confirm'");
            taskManager.Enqueue(commands.ConfirmYes, "Click 'Yes'");
            taskManager.Enqueue(() =>
            {
                plotHoleData.CurrentSeed = d.DesignatedSeed;
                plotHoleData.CurrentSoil = d.DesignatedSoil;
            }, "Update designated seed and soil");
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
                        logService.Info("Option chosen: Harvest");
                        HarvestTargetPlot(plotHole, replant);
                        return true;
                    }                    
                }
                if (entries.Any(e => e?.Contains("Fertilize Crop") ?? false) && fertilize)
                {
                    FertilizeTargetPlot(plotHole);
                    logService.Info("Option chosen: Fertilize Crop");
                    return true;
                }
                if (entries.Any(e => e?.Contains("Tend Crop") ?? false))
                {
                    TendTargetPlot(plotHole);
                    logService.Info("Option chosen: Tend Crop");
                    return true;
                }
                if (entries.Any(e => e?.Contains("Plant Seeds") ?? false))
                {
                    SeedTargetPlot(plotHole);
                    logService.Info("Option chosen: Plant seeds");
                    return true;
                }

                return true;
            }

            return false;

        }

        private void FertilizeTargetPlot(PlotHole plotHole)
        {
            if (!commands.IsItemPresentInInventory(GlobalData.FishmealId))
            {
                PrintErrorAndSelectQuit("Out of fertilizer!");
            }
            else
            {
                taskManager.Enqueue(() => commands.SelectActionString(
                        globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Fertilize)), 
                        "Select 'Fertililze Crop' option");
                taskManager.EnqueueDelayMs(new Random().Next(200, 300));

                taskManager.Enqueue(() => commands.Fertilize(), "Fertilize");

                taskManager.Enqueue(() =>
                {
                    string alreadyFertilizedMsg = globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.AlreadyFertilized);
                    if (!errorMessageMonitor.WasThereARecentError(alreadyFertilizedMsg))
                    {
                        plotHole.LastFertilizedUtc = DateTime.UtcNow;
                    }
                }, "Update last fertilized Timer");                                                 
            }

            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            GetToOptionSelectMenu(plotHole);
            TendTargetPlot(plotHole);  
        }

        private void HarvestTargetPlot(PlotHole plotHole, bool replant)
        {
            taskManager.Enqueue(() => commands.SelectActionString(
                globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.HarvestCrop)), "Select 'Harvest Crop' option");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));

            logService.Info("Action enqueued. Select harvest");
            if (replant)
            {
                GetToOptionSelectMenu(plotHole);
                SeedTargetPlot(plotHole);
            }
        }

        private void GetToOptionSelectMenu(PlotHole plotHole)
        {
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plotHole), "Target and interact with plot");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialog");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
        }

        private void TendTargetPlot(PlotHole plot)
        {
            taskManager.Enqueue(() => commands.SelectActionString(
                globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.TendCrop)), "Select 'Tend Crop' option");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.Enqueue(() =>
            {
                plot.LastTendedUtc = DateTime.UtcNow;
            }, "Update Last Tended timer");
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

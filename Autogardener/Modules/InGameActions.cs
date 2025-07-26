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

        public void ScanPlot(PlotPatch plotPatch)
        {
            int index = 1;
            foreach (Plot plot in plotPatch.Plots)
            {
                taskManager.EnqueueSuperTask(() => ScanPlot(plot), $"Scan plot hole {index}");
                index++;
            }

            taskManager.Enqueue(() => chatGui.Print("Scan complete! o7"), "Scan complete notification");
            taskManager.Enqueue(() => { saveManager.WriteCharacterSave(); }, "Save scan results");
            taskManager.StartProcessingQueuedTasks();
        }

        private bool ScanPlot(Plot plot)
        {
            var plotOb = objectTable.SearchById(plot.GameObjectId);
            if (plotOb == null)
            {
                throw new Exception($"Plot with objectdId {plot.GameObjectId} was not found in the object table");
            }
            plot.Initialize(plotOb);
            taskManager.Enqueue(() => chatGui.Print("Starting scan"), "Scan start");
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plot), "InteractWithPlot");
            taskManager.Enqueue(() => commands.SetPlantTypeFromDialogue(plot), "Extract plant type");
            taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialogue");
            taskManager.Enqueue(() => commands.SelectActionString(globalData
                .GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Quit)), "Select Quit");

            return true;
        }
        public void PlotPatchCare(PlotPatch plotPatch, bool fertilize, bool replant)
        {
            int index = 1;

            foreach (var plot in plotPatch.Plots)
            {
                taskManager.EnqueueSuperTask(() => BeginPlotCare(plot, fertilize, replant), $"Plot care for index {index}");
                index++;
            }
            taskManager.StartProcessingQueuedTasks();
        }

        public void TargetPlantingHoleCare(PlotPatch plotPatch, bool fertilize, bool replant)
        {
            Plot? plot = plotPatch.Plots.FirstOrDefault(p => p.GameObjectId == clientState.LocalPlayer?.TargetObject?.GameObjectId);
            if (plot != null)
            {
                taskManager.EnqueueSuperTask(() => BeginPlotCare(plot, fertilize, replant), $"Plot care for targeted plot");
                taskManager.StartProcessingQueuedTasks();
            }            
        }

        private void BeginPlotCare(Plot plot, bool fertilize, bool replant)
        {
            //taskManager.Enqueue(() => TargetPlotGameObjectId(plot), "Target plot");
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plot), "Interact with plot");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.Enqueue(commands.SkipDialogueIfNeeded, "Skip dialogue");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.EnqueueSuperTask(() => SelectOption(plot, fertilize, replant), "Select branching option");
        }

        private bool TargetAndInteractWithPlot(Plot plot)
        {
            var targetingResult = TargetPlotGameObjectId(plot);
            if (targetingResult == false)
            {
                return false;
            }

            if (targetManager.Target?.GameObjectId != plot.GameObjectId){
                return false;
            }

            return commands.InteractWithPlot();
        }
        private bool TargetPlotGameObjectId(Plot plot)
        {
            var plotOb = objectTable.SearchById(plot.GameObjectId);
            if (plotOb == null)
            {
                chatGui.PrintError("Can't access plot. Did you move away?");
                throw new Exception($"Plot with objectdId {plot.GameObjectId} was not found in the object table");                
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

        public void SeedTargetPlot(Plot plot)
        {
            var d = plot.Design;
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
                commands.PickSeedsAndSoil(plot.Design!.DesignatedSeed, plot.Design.DesignatedSoil), "Pick seeds and soil");
            taskManager.EnqueueDelayMs(300);
            taskManager.Enqueue(commands.ClickConfirmOnHousingGardeningAddon, "Click 'Confirm'");
            taskManager.Enqueue(commands.ConfirmYes, "Click 'Yes'");
            taskManager.Enqueue(() =>
            {
                plot.CurrentSeed = d.DesignatedSeed;
                plot.CurrentSoil = d.DesignatedSoil;
            }, "Update designated seed and soil");
        }

        private unsafe bool SelectOption(Plot plot, bool fertilize, bool replant)
        {            
            if (TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString)
                && IsAddonReady(&addonSelectString->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addonSelectString).Entries.Select(x => x.ToString()).ToList();
                if (entries.Any(e => e?.Contains("Harvest Crop") ?? false))
                {
                    if (!(plot.Design?.DoNotHarvest ?? false))
                    {
                        logService.Info("Option chosen: Harvest");
                        HarvestTargetPlot(plot, replant);
                        return true;
                    }                    
                }
                if (entries.Any(e => e?.Contains("Fertilize Crop") ?? false) && fertilize)
                {
                    FertilizeTargetPlot(plot);
                    logService.Info("Option chosen: Fertilize Crop");
                    return true;
                }
                if (entries.Any(e => e?.Contains("Tend Crop") ?? false))
                {
                    TendTargetPlot(plot);
                    logService.Info("Option chosen: Tend Crop");
                    return true;
                }
                if (entries.Any(e => e?.Contains("Plant Seeds") ?? false))
                {
                    SeedTargetPlot(plot);
                    logService.Info("Option chosen: Plant seeds");
                    return true;
                }

                return true;
            }

            return false;

        }

        private void FertilizeTargetPlot(Plot plot)
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
                        plot.LastFertilizedUtc = DateTime.UtcNow;
                    }
                }, "Update last fertilized Timer");                                                 
            }

            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            GetToOptionSelectMenu(plot);
            TendTargetPlot(plot);  
        }

        private void HarvestTargetPlot(Plot plot, bool replant)
        {
            taskManager.Enqueue(() => commands.SelectActionString(
                globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.HarvestCrop)), "Select 'Harvest Crop' option");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));

            logService.Info("Action enqueued. Select harvest");
            if (replant)
            {
                GetToOptionSelectMenu(plot);
                SeedTargetPlot(plot);
            }
        }

        private void GetToOptionSelectMenu(Plot plot)
        {
            taskManager.Enqueue(() => TargetAndInteractWithPlot(plot), "Target and interact with plot");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
            taskManager.Enqueue(() => commands.SkipDialogueIfNeeded(), "Skip dialog");
            taskManager.EnqueueDelayMs(new Random().Next(200, 300));
        }

        private void TendTargetPlot(Plot plot)
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

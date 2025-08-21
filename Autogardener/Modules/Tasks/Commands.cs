using Autogardener.Model.Plots;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.ClientOnlyDisplay;
using DalamudBasics.Logging;
using ECommons.Automation;
using ECommons.Automation.NeoTaskManager;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Excel.Sheets.Experimental;
using System.Linq;
using System.Text.RegularExpressions;

namespace Autogardener.Modules.Tasks
{
    public class Commands
    {
        private readonly ILogService logService;
        private readonly IClientState clientState;
        private readonly GlobalData globalData;
        private readonly Utils utils;
        private readonly IGameGui gameGui;
        private readonly IClientChatGui clientChatGui;
        private readonly TaskManager taskManager;
        private readonly ITargetManager targetManager;
        private readonly ErrorMessageMonitor errorMessageMonitor;
        private readonly MiniTA miniTA;

        public Commands(ILogService logService, IClientState clientState, GlobalData globalData,
            Utils utils, IGameGui gameGui, IClientChatGui clientChatGui, TaskManager taskManager, ITargetManager targetManager,
            ErrorMessageMonitor errorMessageMonitor, MiniTA miniTA)
        {
            this.logService = logService;
            this.clientState = clientState;
            this.globalData = globalData;
            this.utils = utils;
            this.gameGui = gameGui;
            this.clientChatGui = clientChatGui;
            this.taskManager = taskManager;
            this.targetManager = targetManager;
            this.errorMessageMonitor = errorMessageMonitor;
            this.miniTA = miniTA;
        }

        private bool _gardening = false;
        public bool Gardening => _gardening && GlobalData.GardenPlotDataIds.Contains(clientState.LocalPlayer?.TargetObject?.DataId ?? 0);


        public unsafe bool IsItemPresentInInventory(uint itemId)
        {
            return TryGetItemSlotByItemId(itemId, out var _, out var _);
        }

        public unsafe bool Fertilize()
        {
            //var alreadyFertilizedMsg = globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.AlreadyFertilized);
            //if (errorMessageMonitor.WasThereARecentError(alreadyFertilizedMsg))
            //{
            //    return true;
            //}
            //var itemId = GlobalData.FishmealId;
            //if (!TryGetItemSlotByItemId(itemId, out var container, out var itemSlotNumber))
            //{
            //    logService.Warning("Could not find item with id " + itemId);
            //    return true;
            //}

            //logService.Info($"Fertilizer found in slot {container->Type}:{itemSlotNumber}");
            //var ag = AgentInventoryContext.Instance();
            //var addonId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId();
            //ag->OpenForItemSlot(container->Type, itemSlotNumber, addonId);
            //var contextMenu = (AtkUnitBase*)gameGui.GetAddonByName("ContextMenu", 1);
            //if (contextMenu == null) return false;
            //for (var p = 0; p <= contextMenu->AtkValuesCount; p++)
            //{
            //    if (ag->EventIds[p] == 7)
            //    {
            //        Callback.Fire(contextMenu, true, 0, p - 7, 0, 0, 0);
            //        return true;
            //    }
            //}

            return false;
        }

        public unsafe bool SetPlantTypeFromDialogue(Plot plot)
        {
            if (TryGetAddonByName<AddonTalk>("Talk", out var addonTalk)
                && addonTalk->AtkUnitBase.IsVisible)
            {
                var am = new AddonMaster.Talk((nint)addonTalk);
                addonTalk->AtkValues->GetValueAsString();
                var dialogueText = addonTalk->AtkTextNode228->NodeText.ToString();
                logService.Info("Text node 228: " + dialogueText);
                (var id, var seedName) = ExtractPlantNameAndId(dialogueText);
                if (id != 0)
                {                    
                    plot.CurrentSeed = id;
                    logService.Info($"Seed registered: {id}-{seedName}");
                }
                taskManager.InsertDelay(100);
                return true;
            }
            else
            {
                //logService.Warning("No talk addon found");
                return false;
            }
        }

        public unsafe bool SkipDialogueIfNeeded()
        {
            if (TryGetAddonByName<AddonTalk>("Talk", out var addonTalk)
                && addonTalk->AtkUnitBase.IsVisible)
            {
                new AddonMaster.Talk((nint)addonTalk).Click();
                //logService.Warning("Talk addon FOUND");

                return true;
            }
            else
            {
                //logService.Warning("No talk addon found");
                return false;
            }
        }



        public unsafe bool InteractWithPlot()
        {
            if (!Player.Available) return false;
            if (Player.IsAnimationLocked) return false;
            if (!utils.DismountIfNeeded()) return false;
            if (IsOccupied()) return false;
            var plotSelected = clientState.LocalPlayer?.TargetObject;
            if (plotSelected == null)
            {
                //clientChatGui.PrintError("No plot selected.");
                return false;
            }

            if (!GlobalData.GardenPlotDataIds.Contains(plotSelected.DataId))
            {
                clientChatGui.PrintError("That's not a plot");
                return false;
            }

            TargetSystem.Instance()->InteractWithObject(plotSelected.Struct(), true);

            return true;
        }

        public unsafe bool SelectActionString(string actionToSelect)
        {
            throw new NotImplementedException();
            //if (TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString)
            //    && IsAddonReady(&addonSelectString->AtkUnitBase))
            //{
            //    logService.Info("SelectString addon found");
            //    var entries = new AddonMaster.SelectString(addonSelectString).Entries;
            //    if (TryGetMatchingEntry(entries, actionToSelect, out var matchingEntry)){
            //        matchingEntry.Select();
            //        miniTA.RegisterOptionAttemptedToSelect(actionToSelect);
            //    }
            //    else
            //    {
            //        logService.Info($"\"{actionToSelect}\" didn't match any option");
            //        foreach (var entry in new AddonMaster.SelectString(addonSelectString).Entries)
            //        {
            //            logService.Info("Entry: " + entry.SeString.ToString());
            //        }

            //        return false;
            //    }

            //    return true;
            //}
            //else
            //{
            //    //logService.Warning("No SelectString addon found");
            //    return false;
            //}
        }

        public unsafe bool PickSeedsAndSoil(uint seedItemId, uint soilItemId)
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }
            var seedIndex = GetIndexFromCollection(globalData.Seeds.Keys.ToHashSet(), seedItemId);
            var soilIndex = GetIndexFromCollection(globalData.Soils.Keys.ToHashSet(), soilItemId);            
            logService.Info($"Soil: {soilIndex} Seed: {seedIndex}");

            return ChooseGardeningItems(soilIndex, seedIndex, gardeningAddon);
        }

        public unsafe bool ClickConfirmOnHousingGardeningAddon()
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                logService.Warning("HousingGardening addon not found");
                return false;
            }

            Callback.Fire(gardeningAddon, false, 0, 0, 0, 0, 0);

            return true;
        }



        private (uint id, string name) ExtractPlantNameAndId(string dialogueText)
        {
            var matches = new Regex("([\\w ]{4,})").Matches(dialogueText);
            if (matches.Count < 2 || matches[0].Groups.Count == 0)
            {
                logService.Info("Scanned plot was empty");
                return (0, "Empty");
            }
            var plantName = matches[0].Groups[0].Value;

            var result = SearchSeed(plantName);
            if (result.id == 0)
            {
                return ExtractXlightNameAndId(plantName);
            }

            return result;
        }


        // This does not work in french at all, but that's ok
        private (uint id, string name) ExtractXlightNameAndId(string plantName)
        {
            var regex = new Regex($"(\\w+)\\s{globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.Shard)}");
            var match = regex.Match(plantName);
            if (!match.Success || match.Groups.Count == 0)
            {
                return (0, "Empty");
            }
            var shardElement = match.Groups[0];
            var seedName = $"{shardElement}{globalData.GetGardeningOptionStringLocalized(GlobalData.GardeningStrings.xLight)}";
            return SearchSeed(seedName);
        }
        private (uint id, string name) SearchSeed(string seedPartialName)
        {
            try
            {
                var seedDictionaryEntry = globalData.Seeds.First(s => s.Value.Name.ToString().Contains(seedPartialName, StringComparison.OrdinalIgnoreCase));
                return (seedDictionaryEntry.Key, seedDictionaryEntry.Value.Name.ToString());
            }
            catch (InvalidOperationException)
            {
                logService.Warning($"No seed matching the plant {seedPartialName} found");
                return (0, "Empty");
            }
        }

        private unsafe bool ChooseGardeningItems(int soilIndex, int seedIndex, AtkUnitBase* gardeningAddon)
        {
            if (soilIndex != -1)
            {
                taskManager.InsertDelay(100);
                taskManager.Insert(() => TryClickItem(gardeningAddon, 1, soilIndex));
            }

            if (seedIndex != -1)
            {
                taskManager.InsertDelay(100);
                taskManager.Insert(() => TryClickItem(gardeningAddon, 2, seedIndex));
            }

            return true;
        }

        public unsafe bool ConfirmYes()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }

            if (gardeningAddon->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible())
            {
                new AddonMaster.SelectYesno((nint)addon).Yes();
                return true;
            }

            return false;
        }

        private unsafe bool TryGetItemSlotByItemId(uint itemId, out InventoryContainer* container, out int slotNumber)
        {
            var inventories = GetCombinedInventories();
            container = null;
            slotNumber = -1;
            foreach (var inventory in inventories)
            {
                logService.Debug($"Checking inventory {inventory->Type}");
                for (var i = 0; i < inventory->Size; i++)
                {
                    var slot = inventory->GetInventorySlot(i);
                    if (!slot->IsEmpty() && slot->ItemId == itemId)
                    {
                        slotNumber = i;
                        container = inventory;
                        return true;
                    }
                }
            }

            return false;
        }

        private unsafe InventoryContainer*[] GetCombinedInventories()
        {
            var im = InventoryManager.Instance();
            var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
            var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
            var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
            var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);
            InventoryContainer*[] container = { inv1, inv2, inv3, inv4 };
            return container;
        }

        // This returned index is the index within the popup that appears when clicking the request slot, not an inventory index.
        private unsafe int GetIndexFromCollection(HashSet<uint> idCollection, uint targetId)
        {
            var container = GetCombinedInventories();
            var indexOfTargetItem = 0;
            foreach (var cont in container)
            {
                for (var i = 0; i < cont->Size; i++)
                {
                    var item = cont->GetInventorySlot(i);
                    if (idCollection.Contains(item->ItemId)) // It's a member of the collection, like "seeds", or "soils"
                    {
                        if (item->ItemId == targetId)
                        {
                            return indexOfTargetItem;
                        }
                        else
                        {
                            indexOfTargetItem++;
                        }
                    }
                }
            }

            return -1;
        }

        private unsafe bool? TryClickItem(AtkUnitBase* addon, int i, int itemIndex)
        {
            if (!TryGetAddonByName<AtkUnitBase>("ContextIconMenu", out var contextMenu))
            {
                return false;
            }
            if (contextMenu is null || !contextMenu->IsVisible)
            {
                var slot = i - 1;

                Svc.Log.Debug($"{slot}");
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 2
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = (uint)slot
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 1
                };

                addon->FireCallback(5, values);
                CloseItemDetail();
                return false;
            }
            else
            {
                var value = (uint)(i == 1 ? 27405 : 27451);
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = itemIndex
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = value
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    UInt = 0
                };

                contextMenu->FireCallback(5, values, true);
                Svc.Log.Debug($"Filled slot {i}");
                return true;
            }
        }

        private unsafe bool CloseItemDetail()
        {
            TryGetAddonByName<AtkUnitBase>("ItemDetail", out var itemDetail);
            if (itemDetail is null || !itemDetail->IsVisible) return false;

            var values = stackalloc AtkValue[1];
            values[0] = new AtkValue()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = -1
            };

            itemDetail->FireCallback(1, values);
            return true;
        }
    }
}

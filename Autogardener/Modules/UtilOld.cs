using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.ClientOnlyDisplay;
using DalamudBasics.Logging;
using DalamudBasics.Targeting;
using ECommons.Automation;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Automation.UIInput;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.Interop;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using System.Linq;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMJIFarmManagement;

namespace Autogardener.Modules
{
    public partial class UtilOld
    {
        

        private readonly ILogService logService;
        private readonly IClientChatGui clientChatGui;
        private readonly IObjectTable objectTable;
        private readonly ITargetingService targetingService;
        private readonly ITargetManager rawTargeting;
        private readonly IGameGui gameGui;
        private readonly IContextMenu contextMenu;
        private readonly IDataManager dataManager;
        private readonly ICondition condition;
        private readonly IClientState clientState;
        private readonly INotificationManager notificationManager;
        private readonly Utils utils;

        public Dictionary<uint, Item> Seeds { get; set; }
        public Dictionary<uint, Item> Soils { get; set; }
        public Dictionary<uint, Item> Fertilizers { get; set; }

        public UtilOld(ILogService logService, IClientChatGui clientChatGui, IObjectTable objectTable, ITargetingService targetingService,
            ITargetManager rawTargeting,
            IGameGui gameGui,
            IContextMenu contextMenu, IDataManager dataManager, ICondition condition, IClientState clientState,
            INotificationManager notificationManager, Utils utils, IFramework framework, TaskManager taskManager)
        {
            this.logService = logService;
            this.clientChatGui = clientChatGui;
            this.objectTable = objectTable;
            this.targetingService = targetingService;
            this.rawTargeting = rawTargeting;
            this.gameGui = gameGui;
            this.contextMenu = contextMenu;
            this.dataManager = dataManager;
            this.condition = condition;
            this.clientState = clientState;
            this.notificationManager = notificationManager;
            this.utils = utils;

            Seeds = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 20).ToDictionary(x => x.RowId, x => x);
            Soils = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 21).ToDictionary(x => x.RowId, x => x);
            Fertilizers = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 22).ToDictionary(x => x.RowId, x => x);
        }

        public void DescribeTarget()
        {
            EventObject ob;
            IGameObject? target = rawTargeting.Target;
            if (target == null)
            {
                logService.Info("Nothing targeted.");
                return;
            }

            logService.Info($"Name: [{target.Name}] GameObjectId [{target.GameObjectId}] EntityId [{target.EntityId}] DataId [{target.DataId}]");
            logService.Info($" OwnerId [{target.OwnerId}] ObjectIndex [{target.ObjectIndex}] ObjectKind [{target.ObjectKind}] SubKind [{target.SubKind}]");
        }

        public unsafe void ListCurrentMenuOptions()
        {
            if(GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out AddonSelectString* addon)
                && IsAddonReady(&addon->AtkUnitBase))
            {
                foreach (var entry in new AddonMaster.SelectString(addon).Entries)
                {
                    logService.Info($"Entry {entry.Index}: {entry.Text}");
                }
                return;
            }
            logService.Info("No SelectString addon present, or it was not ready.");
            return;
        }

        public unsafe void SelectEntry(string text)
        {
            if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out AddonSelectString* addon)
                && IsAddonReady(&addon->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addon).Entries;
                Func<AddonMaster.SelectString.Entry, bool> equality = (entry) => entry.Text.StartsWith(text, StringComparison.OrdinalIgnoreCase);
                if (!entries.Any(equality))
                {
                    logService.Warning($"No entry with text {text} was found in the list menu.");
                }
                var entry = entries.First(equality);
                entry.Select();
            }

            logService.Info("No SelectString addon present, or it was not ready.");
            return;
        }
        public unsafe bool TryDetectGardeningWindow(out AtkUnitBase* addon)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingGardening", out addon))
            {
                logService.Info("Housing gardening action not detected");
                return false;
            }
            logService.Info("Housing gardening action detected");

            return true;
        }

        public unsafe void GetSoilDragAndDropEntries()
        {
            if (!TryDetectGardeningWindow(out AtkUnitBase* addon))
            {
                logService.Warning("Gardening window not detected or ready");
                return;
            }
            var windowNode = addon->RootNode->ChildNode;
            var soilDragDrop = GetSiblingResNodeById(windowNode, 6);
            var component = soilDragDrop->GetAsAtkComponentDragDrop();

            if (!(TryGetAddonByName<AddonContextIconMenu>("ContextIconMenu", out var subAddon)
                && IsAddonReady(&subAddon->AtkUnitBase)))
            {
                logService.Info("ContextIconMenu not detected");
                return;
            }

            var availableItemTypes = subAddon->AtkValuesSpan[4].UInt;
            for (int i = 13; i < (13 + availableItemTypes * 8); i+= 8)
            {
                var value = subAddon->AtkValuesSpan[i];
                if (value.Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.ManagedString
                    || value.Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String)
                {
                    logService.Info($"Value #{i}: {value.GetValueAsString()}");
                }
            }
        }

        public unsafe void EatACookie()
        {
            uint acornCookieId = 4701;
            int amount = InventoryManager.Instance()->GetInventoryItemCount(acornCookieId);
            clientChatGui.Print($"{amount} cookies left");
            var player = clientState.LocalPlayer;
            ActionManager.Instance()->UseAction(ActionType.Item, acornCookieId, player.GameObjectId, extraParam: 65535);
        }

        public unsafe void UseItem(uint itemId)
        {
            int amount = InventoryManager.Instance()->GetInventoryItemCount(itemId);
            clientChatGui.Print($"{amount} items left");
            var player = clientState.LocalPlayer;
            ActionManager.Instance()->UseAction(ActionType.Item, itemId, player.TargetObjectId, extraParam: 65535);
        }

        public unsafe void UseFishmeal()
        {
            uint fishmealId = 7767;
            int amount = InventoryManager.Instance()->GetInventoryItemCount(fishmealId);
            clientChatGui.Print($"{amount} fertilizer units left");
            var player = clientState.LocalPlayer;
            ActionManager.Instance()->UseAction(ActionType.Unk_18, fishmealId, player.TargetObjectId, extraParam: 65535);
        }

        public unsafe void ClickFertilizer()
        {
            uint fishmealId = 7767;
            if(!TryGetItemSlotAddonByItemId(fishmealId, out var inventory, out var slot)){
                logService.Warning("Could not find the fishmeal");
            }

            //var ev = new AtkEvent();
            //ev.State = new AtkEventState();
            //ev.State.EventType = AtkEventType.Right;
            //ev.Target = &slot->AtkEventTarget;
            //ev.Node = slot;

            //slot->GetAsAtkComponentButton()->
            //slot->GetAsAtkComponentDragDrop()->ClickAddonDragDrop(inventory, &ev);
            //OpenContextMenu(inventory, slot);
            //logService.Warning("Slot should have been rightclicked");

        }
        public unsafe void OpenContextMenu(AtkUnitBase* baseNode, AtkResNode* dragDropNode)
        {
            //int x = (int)(baseNode->RootNode->X + dragDropNode->X + 10);
            //int y = (int)(baseNode->RootNode->Y + dragDropNode->Y + 10);
            //logService.Info($"Moving to X: {x} Y: {y}");
            //WindowsKeypress.SendMouseMove(x, y);
            //WindowsKeypress.SendMousepress(LimitedKeys.RightMouseButton);
        }

        public unsafe void EnumerateInventory()
        {
            InventoryType[] inventories = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4];
            for (int i = 0; i < inventories.Length; i++)
            {
                for (int k = 0; k < 35; k++)
                {
                    InventoryItem* item = InventoryManager.Instance()->GetInventorySlot(inventories[i], k);
                    if (item->GetItemId() != 0)
                    {
                        if(dataManager.GetExcelSheet<Item>().TryGetRow(item->GetItemId(), out Item itemData))
                        {
                            logService.Info($"Slot {k} Id:{item->GetItemId()} Name:{itemData.Name} Action: {itemData.ItemAction}");
                        }
                        else
                        {
                            logService.Info($"Slot {k} Id:{item->GetItemId()} Iten data retrieval failed");
                        }
                    }
                    else
                    {
                        logService.Info($"Slot {k}: emtpy");
                    }

                }
            }
        }

        public unsafe bool TryGetItemSlotAddonByItemId(uint itemId, out AtkUnitBase* inventoryAddon, out AtkResNode* slotAddon)
        {
            slotAddon = null;
            (int inventory, int slot) = GetItemInventorySlot(itemId);
            string inventoryComponentName = $"InventoryGrid{inventory}E";
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(inventoryComponentName, out inventoryAddon))
            {
                logService.Info("Inventory grid not detected");
                return false;
            }

            
            var firstNode = inventoryAddon->RootNode->ChildNode->PrevSiblingNode->ChildNode;
            if (firstNode == null)
            {
                logService.Warning("The first node is null!");
                return false;
            }
            int firstItemNodeId = 3;
            int expectedId = firstItemNodeId + slot;
            logService.Info($"Seeking item in position: {expectedId}");
            AtkResNode* node = GetSiblingResNodeById(firstNode, (uint)(expectedId));
            if (node == null)
            {
                logService.Warning("Found no node with id " + expectedId);
                return false;
            }
            logService.Info($"Arrived at node with id: {node->NodeId}, type: {node->Type}");
            slotAddon = node;
            return true;
        }

        public unsafe (int, int) GetItemInventorySlot(uint itemId)
        {
            InventoryType[] inventories = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4];
            for (int i = 0; i < inventories.Length; i++)
            {
                for (int k = 0; k < 35; k++)
                {
                    InventoryItem* item = InventoryManager.Instance()->GetInventorySlot(inventories[i], k);
                    if (item->ItemId == itemId)
                    {
                        return (i, k);
                    }
                }
            }
            return (-1, -1);
        }

        public unsafe void GetTextButtonText()
        {
            if (TryDetectGardeningWindow(out AtkUnitBase* addon))
            {
                try
                {
                    logService.Warning("Attempting serialization");

                    var node = addon->NameString;
                    logService.Info($"Name: {node}");
                    var root = addon->RootNode->Type;
                    logService.Info($"Root type: {root}");
                    var windowNode = addon->RootNode->ChildNode;
                    var textNode = GetSiblingResNodeById(windowNode, 8);
                    if (textNode == null)
                    {
                        logService.Info("Could not find the text node");
                    }

                    string text = textNode->GetAsAtkComponentButton()->ButtonTextNode->NodeText.GetText();
                    logService.Info($"Text recovered: {text}");

                }
                catch (Exception e) { logService.Error(e, "Error"); }

            }
        }


        private unsafe AtkResNode* GetSiblingResNodeById(AtkResNode* startingPoint, uint id)
        {
            var nodeBefore = GetSiblingResNodeById(startingPoint, id, true);
            if (nodeBefore != null)
            {
                return nodeBefore;
            }

            return GetSiblingResNodeById(startingPoint, id, false);
        }

        private unsafe AtkResNode* GetSiblingResNodeById(AtkResNode* node, uint id, bool goBackwards)
        {
            
            if (node == null )
            {
                return null;
            }
            //logService.Info($"Iterating node: " + node->NodeId);
            if (node->NodeId == id)
            {
                return node;
            }
            var nextNode = goBackwards ? node->PrevSiblingNode : node->NextSiblingNode;

            return GetSiblingResNodeById(nextNode, id, goBackwards);
        }



        public unsafe bool IsCursorOnAddon(int addonAbsolutePosX, int addonAbsolutePosY, int addonHeight)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("CursorAddon", out var cursorAddon))
            {                
                logService.Info("Cursor addon detected");
                return false;
            }
            logService.Info("Cursor addon detected");

            var doesXMatch = cursorAddon->X > addonAbsolutePosX - 70 && cursorAddon->X < addonAbsolutePosX - 40;
            var doesYMatch = cursorAddon->Y > addonAbsolutePosY && cursorAddon->Y < addonAbsolutePosY + addonHeight;
            return true;
        }
    }
}

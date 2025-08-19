using Autogardener.Model;
using DalamudBasics.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkCounterNode.Delegates;

namespace Autogardener.Modules.Actions
{
    public class Inventory
    {
        private readonly ILogService logService;

        public Inventory(ILogService logService)
        {
            this.logService = logService;
        }

        public unsafe ItemInstance? TryGetItemInInventory(uint itemId) {
            var inventories = GetCombinedInventories();
            for (int invSectionIndex = 0; invSectionIndex < inventories.Length; invSectionIndex++)
            {
                var inventory = inventories[invSectionIndex];
                logService.Info($"Checking inventory {inventory->Type}");
                for (var i = 0; i < inventory->Size; i++)
                {
                    var slot = inventory->GetInventorySlot(i);
                    if (!slot->IsEmpty() && slot->ItemId == itemId)
                    {
                        return new ItemInstance
                        {
                            Id = itemId,
                            Quantity = slot->Quantity,
                            InventorySectionIndex = invSectionIndex,
                            InventorySection = inventory,
                            SlotNumber = i,
                            ItemSlot = slot,
                        };
                    }
                }
            }

            return null;
        }

        private unsafe bool TryGetItemSlotByItemId(uint itemId, out InventoryContainer* container, out int slotNumber)
        {
            var inventories = GetCombinedInventories();
            container = null;
            slotNumber = -1;
            foreach (var inventory in inventories)
            {
                logService.Info($"Checking inventory {inventory->Type}");
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
    }
}

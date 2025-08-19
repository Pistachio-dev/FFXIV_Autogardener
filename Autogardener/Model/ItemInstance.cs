using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Model
{
    public unsafe class ItemInstance
    {
        public uint Id { get; set; }
        public int Quantity { get; set; }
        public int InventorySectionIndex { get; set; } // 0 to 3, the 4 divisions of the inventory

        public InventoryContainer* InventorySection { get; set; }

        public int SlotNumber { get; set; } // Relative to the inventory section

        public InventoryItem* ItemSlot { get; set; }
    }
}

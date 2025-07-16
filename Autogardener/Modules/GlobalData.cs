using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using DalamudBasics.Logging;
using Lumina.Excel.Sheets;

namespace Autogardener.Modules
{
    public class GlobalData
    {
        public const uint OutdoorPatchDataId = 2003757;
        public const uint RivieraFlowerpotDataId = 197051;
        public const uint GladeFlowerpotDataId = 197052;
        public const uint OasisFlowerpotDataId = 197053;

        public static readonly List<uint> GardenPlotDataIds
            = new List<uint> { OutdoorPatchDataId, RivieraFlowerpotDataId, GladeFlowerpotDataId, OasisFlowerpotDataId };

        private readonly ILogService logService;
        private readonly IClientState clientState;

        public Dictionary<uint, Item> Seeds { get; set; }
        public Dictionary<uint, Item> Soils { get; set; }
        public Dictionary<uint, Item> Fertilizers { get; set; }

        public Dictionary<uint, Addon> AddonText { get; set; }

        public GlobalData(ILogService logService, IClientState clientState)
        {
            this.logService = logService;
            this.clientState = clientState;
            LoadGlobalData(); // Do a lazy load later.
        }

        public void LoadGlobalData()
        {
            Seeds = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 20).ToDictionary(x => x.RowId, x => x);
            Soils = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 21).ToDictionary(x => x.RowId, x => x);
            Fertilizers = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 22).ToDictionary(x => x.RowId, x => x);
            AddonText = Svc.Data.GetExcelSheet<Addon>().ToDictionary(x => x.RowId, x => x);
        }

        public string GetGardeningOptionStringLocalized(GardeningOption option)
        {
            return clientState.ClientLanguage switch
            {
                Dalamud.Game.ClientLanguage.English => GardeningOptionsTextEnglish[option],
                _ => throw new NotImplementedException()
            };
        }

        private Dictionary<GardeningOption, string> GardeningOptionsTextEnglish = new()
        {
            { GardeningOption.PlantSeeds, "Plant Seeds" },
            { GardeningOption.Fertilize, "Fertilize Crop" },
            { GardeningOption.TendCrop, "Tend Crop" },
            { GardeningOption.RemoveCrop, "Remove Crop" },
            { GardeningOption.HarvestCrop, "Harvest Crop" },
            { GardeningOption.Quit, "Quit" },
            { GardeningOption.Growing, "This crop is doing well." },// Has the plant name in the line above it
            { GardeningOption.Purple, "This crop has seen better days." },
            { GardeningOption.ReadyToHarvest, "This crop is ready to be harvested." }, // Has the plant name in the line above it
        };

        public enum GardeningOption
        {
            PlantSeeds,
            Fertilize,
            TendCrop,
            RemoveCrop,
            HarvestCrop,
            Quit,
            Growing,
            Purple,
            ReadyToHarvest,
        }
    }
}

using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using Lumina.Excel.Sheets;
using System.Linq;

namespace Autogardener.Modules
{
    public class GlobalData
    {
        public const uint OutdoorPatchDataId = 2003757;
        public const uint RivieraFlowerpotDataId = 197051;
        public const uint GladeFlowerpotDataId = 197052;
        public const uint OasisFlowerpotDataId = 197053;

        public const uint FishmealId = 7767;

        public const float MaxInteractDistance = 4;

        public static readonly List<uint> GardenPlotDataIds
            = new List<uint> { OutdoorPatchDataId, RivieraFlowerpotDataId, GladeFlowerpotDataId, OasisFlowerpotDataId };

        private readonly ILogService logService;
        private readonly IClientState clientState;
        
        public string GetSeedStringName(uint id)
        {
            if (Seeds.ContainsKey(id))
            {
                return Seeds[id].Name.ToString();
            }

            return "No/unknown seed";
        }

        public string GetSoilStringName(uint id)
        {
            if (Soils.ContainsKey(id))
            {
                return Soils[id].Name.ToString();
            }

            return "No/unknown soil";
        }

        public string GetGardeningItemName(uint id)
        {
            string errorString = "No/unknown";
            var name = GetSeedStringName(id);
            if (name.StartsWith(errorString))
            {
                name = GetSoilStringName(id);
                if (name.StartsWith(errorString))
                {
                    return "Fishmeal";
                }
            }

            return name;
        }

        public Dictionary<uint, Item> Seeds { get; set; }
        public Dictionary<uint, Item> SeedsIncludingFlowerpots { get; set; }
        public Dictionary<uint, Item> FlowerpotSeeds { get; set; }
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
            SeedsIncludingFlowerpots = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 20).ToDictionary(x => x.RowId, x => x);
            string flowerPotSignString = GetGardeningOptionStringLocalized(GardeningStrings.ForUseInPlanters);
            FlowerpotSeeds = SeedsIncludingFlowerpots.Where(item => item.Value.Description.ToString()
                .Contains(flowerPotSignString, StringComparison.OrdinalIgnoreCase)).ToDictionary();
            Seeds = SeedsIncludingFlowerpots.Except(FlowerpotSeeds).ToDictionary();
            Soils = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 21).ToDictionary(x => x.RowId, x => x);
            Fertilizers = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.RowId == 82 && x.FilterGroup == 22).ToDictionary(x => x.RowId, x => x);
            AddonText = Svc.Data.GetExcelSheet<Addon>().ToDictionary(x => x.RowId, x => x);
        }

        public string GetGardeningOptionStringLocalized(GardeningStrings option)
        {
            return clientState.ClientLanguage switch
            {
                Dalamud.Game.ClientLanguage.English => GardeningStringsEnglish[option],
                Dalamud.Game.ClientLanguage.French => GardeningStringsFrench[option],
                Dalamud.Game.ClientLanguage.German => GardeningStringsGerman[option],
                Dalamud.Game.ClientLanguage.Japanese => GardeningStringsJapanese[option],
                _ => throw new NotImplementedException()
            };
        }

        private Dictionary<GardeningStrings, string> GardeningStringsFrench = new()
        {
            { GardeningStrings.PlantSeeds, "Ensemecer" },
            { GardeningStrings.Fertilize, "Mettre de l'engrais" },
            { GardeningStrings.TendCrop, "Entretenir" },
            { GardeningStrings.RemoveCrop, "Arracher" },
            { GardeningStrings.HarvestCrop, "Récolter" },
            { GardeningStrings.Quit, "Annuler" },

            { GardeningStrings.AlreadyFertilized, "Ce semis a suffisamment été fertilisé.i"},
            { GardeningStrings.NotEnoughInventorySpace, "Impossible d'obtenir l'objet. Votre inventaire est plein."},
            { GardeningStrings.ForUseInPlanters, "Réservé aux pots de fleurs"}
        };

        private Dictionary<GardeningStrings, string> GardeningStringsEnglish = new()
        {
            { GardeningStrings.PlantSeeds, "Plant Seeds" },
            { GardeningStrings.Fertilize, "Fertilize Crop" },
            { GardeningStrings.TendCrop, "Tend Crop" },
            { GardeningStrings.RemoveCrop, "Remove Crop" },
            { GardeningStrings.HarvestCrop, "Harvest Crop" },
            { GardeningStrings.Quit, "Quit" },
            { GardeningStrings.Growing, "This crop is doing well." },// Has the plant name in the line above it
            { GardeningStrings.Purple, "This crop has seen better days." },
            { GardeningStrings.ReadyToHarvest, "This crop is ready to be harvested." }, // Has the plant name in the line above it
            { GardeningStrings.Shard, "Shard" },
            { GardeningStrings.xLight, "light" }, // Has the plant name in the line above it
            { GardeningStrings.AlreadyFertilized, "This crop has already been sufficiently fertilized."},
            { GardeningStrings.NotEnoughInventorySpace, "Unable to obtain item. Insufficient inventory space."},
            { GardeningStrings.ForUseInPlanters, "For use in planters"}
        };

        private Dictionary<GardeningStrings, string> GardeningStringsGerman = new()
        {
            { GardeningStrings.PlantSeeds, "Plant Seeds" },
            { GardeningStrings.Fertilize, "Fertilize Crop" },
            { GardeningStrings.TendCrop, "Tend Crop" },
            { GardeningStrings.RemoveCrop, "Remove Crop" },
            { GardeningStrings.HarvestCrop, "Harvest Crop" },
            { GardeningStrings.Quit, "Quit" },
            { GardeningStrings.Growing, "This crop is doing well." },// Has the plant name in the line above it
            { GardeningStrings.Purple, "This crop has seen better days." },
            { GardeningStrings.ReadyToHarvest, "This crop is ready to be harvested." }, // Has the plant name in the line above it
            { GardeningStrings.Shard, "Shard" },
            { GardeningStrings.xLight, "light" }, // Has the plant name in the line above it
            { GardeningStrings.AlreadyFertilized, "This crop has already been sufficiently fertilized."},
            { GardeningStrings.NotEnoughInventorySpace, "Unable to obtain item. Insufficient inventory space."},
            { GardeningStrings.ForUseInPlanters, "For use in planters"}
        };

        private Dictionary<GardeningStrings, string> GardeningStringsJapanese = new()
        {
            { GardeningStrings.PlantSeeds, "Plant Seeds" },
            { GardeningStrings.Fertilize, "Fertilize Crop" },
            { GardeningStrings.TendCrop, "Tend Crop" },
            { GardeningStrings.RemoveCrop, "Remove Crop" },
            { GardeningStrings.HarvestCrop, "Harvest Crop" },
            { GardeningStrings.Quit, "Quit" },
            { GardeningStrings.Growing, "This crop is doing well." },// Has the plant name in the line above it
            { GardeningStrings.Purple, "This crop has seen better days." },
            { GardeningStrings.ReadyToHarvest, "This crop is ready to be harvested." }, // Has the plant name in the line above it
            { GardeningStrings.Shard, "Shard" },
            { GardeningStrings.xLight, "light" }, // Has the plant name in the line above it
            { GardeningStrings.AlreadyFertilized, "This crop has already been sufficiently fertilized."},
            { GardeningStrings.NotEnoughInventorySpace, "Unable to obtain item. Insufficient inventory space."},
            { GardeningStrings.ForUseInPlanters, "For use in planters"}
        };

        public enum GardeningStrings
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
            Shard,
            xLight,
            AlreadyFertilized,
            NotEnoughInventorySpace,
            ForUseInPlanters
        }
    }
}

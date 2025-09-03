using ECommons.ExcelServices.TerritoryEnumeration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Territory
{
    internal class TerritoryUtils
    {
        public static bool IsInsideHouse(uint territory)
        {
            return territory.EqualsAny<uint>(
                Houses.Private_Cottage_Mist, Houses.Private_House_Mist, Houses.Private_Mansion_Mist,
                Houses.Private_Cottage_Empyreum, Houses.Private_House_Empyreum, Houses.Private_Mansion_Empyreum,
                Houses.Private_Cottage_Shirogane, Houses.Private_House_Shirogane, Houses.Private_Mansion_Shirogane,
                Houses.Private_Cottage_The_Goblet, Houses.Private_House_The_Goblet, Houses.Private_Mansion_The_Goblet,
                Houses.Private_Cottage_The_Lavender_Beds, Houses.Private_House_The_Lavender_Beds, Houses.Private_Mansion_The_Lavender_Beds,
                1249, 1250, 1251
                );
        }

        public static bool IsInsideWorkshop(uint territory)
        {
            return territory.EqualsAny(Houses.Company_Workshop_Empyreum, Houses.Company_Workshop_Mist, Houses.Company_Workshop_Shirogane, Houses.Company_Workshop_The_Goblet, Houses.Company_Workshop_The_Lavender_Beds);
        }

        public static bool IsInsidePrivateChambers(uint territory)
        {
            return territory.EqualsAny(Houses.Private_Chambers_Empyreum, Houses.Private_Chambers_Mist, Houses.Private_Chambers_Shirogane, Houses.Private_Chambers_The_Goblet, Houses.Private_Chambers_The_Lavender_Beds);
        }

        public static bool IsTerritoryResidentialDistrict(ushort obj)
        {
            return obj.EqualsAny(ResidentalAreas.Mist, ResidentalAreas.Shirogane, ResidentalAreas.Empyreum, ResidentalAreas.The_Goblet, ResidentalAreas.The_Lavender_Beds);
        }
    }
}

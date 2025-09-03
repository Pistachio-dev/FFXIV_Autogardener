using Autogardener.Model.Territory;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using ECommons.ExcelServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.GameHelpers;
using ECommons.LazyDataHelpers;
using System.Text;

namespace Autogardener.Modules.Territory;
public class TerritoryWatcher
{
    public uint LastHousingOutdoorTerritory = 0;
    private readonly ILogService logService;
    private readonly IClientState clientState;
    private readonly IDataManager dataService;

    public TerritoryWatcher(ILogService logService, IClientState clientState, IDataManager dataService)
    {
        this.logService = logService;
        this.clientState = clientState;
        this.dataService = dataService;
    }
    public void Initialize()
    {
        Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
        if(Player.Available)
        {
            ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);
            if(TerritoryUtils.IsInsideHouse(LastHousingOutdoorTerritory) 
                || TerritoryUtils.IsInsideWorkshop(LastHousingOutdoorTerritory) 
                || TerritoryUtils.IsInsidePrivateChambers(LastHousingOutdoorTerritory))
            {
                logService.Warning($"Lifestream was loaded or updated while being inside house. Please re-enter house to ensure data reliability.");
            }
        }
        Purgatory.Add(() =>
        {
            Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;
        });
    }

    public bool IsDataReliable() => LastHousingOutdoorTerritory != 0;

    private void ClientState_TerritoryChanged(ushort obj)
    {
        if(TerritoryUtils.IsTerritoryResidentialDistrict(obj))
        {
            LastHousingOutdoorTerritory = obj;
            logService.Debug($"Last residential territory: {ExcelTerritoryHelper.GetName(obj)}");
        }
    }

    private string GetTerritoryShortName(ResidentialArea area, HouseType house)
    {
        if (TerritoryUtils.IsTerritoryResidentialDistrict(clientState.TerritoryType))
        {
            house = HouseType.NotAHouse;
        }

        StringBuilder s = new StringBuilder();
        string areaName = area switch
        {
            ResidentialArea.LavenderBeds => "LB ",
            ResidentialArea.Mist => "Mist ",
            ResidentialArea.Goblet => "Goblet ",
            ResidentialArea.Shirogane => "Shiro ",
            ResidentialArea.Empyreum => "Emp ",
            _ => string.Empty
        };
        string houseName = house switch
        {
            HouseType.Small => "S ",
            HouseType.Medium => "M ",
            HouseType.Large => "L ",
            HouseType.Apartment => "Apt ",
            HouseType.NotAHouse => string.Empty,
            _ => string.Empty
        };

        return $"{areaName}{houseName}";
    }

    public string GetTerritoryPrefix()
    {
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_Cottage_Empyreum, Houses.Private_Cottage_Mist, Houses.Private_Cottage_Shirogane, Houses.Private_Cottage_The_Goblet, Houses.Private_Cottage_The_Lavender_Beds, 1249))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => GetTerritoryShortName(ResidentialArea.Mist, HouseType.Large),
                ResidentalAreas.The_Lavender_Beds => GetTerritoryShortName(ResidentialArea.LavenderBeds, HouseType.Large),
                ResidentalAreas.The_Goblet => GetTerritoryShortName(ResidentialArea.Goblet, HouseType.Large),
                ResidentalAreas.Shirogane => GetTerritoryShortName(ResidentialArea.Shirogane, HouseType.Large),
                ResidentalAreas.Empyreum => GetTerritoryShortName(ResidentialArea.Empyreum, HouseType.Large),
                _ => string.Empty
            };
        }
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_House_Empyreum, Houses.Private_House_Mist, Houses.Private_House_Shirogane, Houses.Private_House_The_Goblet, Houses.Private_House_The_Lavender_Beds, 1250))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => GetTerritoryShortName(ResidentialArea.Mist, HouseType.Medium),
                ResidentalAreas.The_Lavender_Beds => GetTerritoryShortName(ResidentialArea.LavenderBeds, HouseType.Medium),
                ResidentalAreas.The_Goblet => GetTerritoryShortName(ResidentialArea.Goblet, HouseType.Medium),
                ResidentalAreas.Shirogane => GetTerritoryShortName(ResidentialArea.Shirogane, HouseType.Medium),
                ResidentalAreas.Empyreum => GetTerritoryShortName(ResidentialArea.Empyreum, HouseType.Medium),
                _ => string.Empty
            };
        }
        if(Svc.ClientState.TerritoryType.EqualsAny<ushort>(Houses.Private_Mansion_Empyreum, Houses.Private_Mansion_Mist, Houses.Private_Mansion_Shirogane, Houses.Private_Mansion_The_Goblet, Houses.Private_Mansion_The_Lavender_Beds, 1251))
        {
            return LastHousingOutdoorTerritory switch
            {
                ResidentalAreas.Mist => GetTerritoryShortName(ResidentialArea.Mist, HouseType.Large),
                ResidentalAreas.The_Lavender_Beds => GetTerritoryShortName(ResidentialArea.LavenderBeds, HouseType.Large),
                ResidentalAreas.The_Goblet => GetTerritoryShortName(ResidentialArea.Goblet, HouseType.Large),
                ResidentalAreas.Shirogane => GetTerritoryShortName(ResidentialArea.Shirogane, HouseType.Large),
                ResidentalAreas.Empyreum => GetTerritoryShortName(ResidentialArea.Empyreum, HouseType.Large),
                _ => string.Empty
            };
        }
        return string.Empty;
    }
}

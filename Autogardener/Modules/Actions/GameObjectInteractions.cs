using Autogardener.Model;
using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Autogardener.Modules.Actions
{
    public class GameObjectInteractions
    {
        private readonly IGameGui gameGui;
        private readonly IChatGui chatGui;
        private readonly IObjectTable objectTable;
        private readonly ILogService log;
        private readonly Utils utils;

        public GameObjectInteractions(IGameGui gameGui, IChatGui chatGui, IObjectTable objectTable, ILogService log, Utils utils)
        {
            this.gameGui = gameGui;
            this.chatGui = chatGui;
            this.objectTable = objectTable;
            this.log = log;
            this.utils = utils;
        }

        public unsafe bool Fertilize(ItemInstance fertilizerItemData)
        {
            var ag = AgentInventoryContext.Instance();
            var addonId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId();
            ag->OpenForItemSlot(fertilizerItemData.InventorySection->Type, fertilizerItemData.SlotNumber, 0, addonId);
            var contextMenu = gameGui.GetAddonByName("ContextMenu", 1);
            if (contextMenu == null) return false;

            for (var p = 0; p <= contextMenu.AtkValuesCount; p++)
            {
                log.Info($"EventId[{p}] = {ag->EventIds[p]}");
                if (ag->EventIds[p] == 16)
                {
                    ECommons.Automation.Callback.Fire((AtkUnitBase*)contextMenu.Address, true, 0, p - 7, 0, 0, 0); // This may crash, watch out
                    return true;
                }
            }

            return false;
        }

        public unsafe bool InteractWithTargetedPlot()
        {
            if (!IsPlayerReady())
            {
                log.Debug("Interaction failed. Player not ready or occupied.");
                return false;
            }
            var plotSelected = objectTable.LocalPlayer?.TargetObject;
            if (plotSelected == null)
            {
                log.Warning("Attempting to interat with plot object, but no object is selected.");
                return false;
            }

            if (!GlobalData.GardenPlotDataIds.Contains(plotSelected.BaseId))
            {
                log.Warning("Attempting to interact with object that is not a plot");
                return false;
            }

            TargetSystem.Instance()->InteractWithObject(plotSelected.Struct(), true);

            return true;
        }

        private unsafe bool IsPlayerReady()
        {
            if (!Player.Available) return false;
            if (Player.IsAnimationLocked) return false;
            if (!utils.DismountIfNeeded()) return false;
            if (Utils.IsOccupied()) return false;

            return true;
        }
    }
}

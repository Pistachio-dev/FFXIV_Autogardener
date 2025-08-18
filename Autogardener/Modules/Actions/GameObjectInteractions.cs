using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace Autogardener.Modules.Actions
{
    public class GameObjectInteractions
    {
        private readonly IChatGui chatGui;
        private readonly IClientState clientState;
        private readonly ILogService log;
        private readonly Utils utils;

        public GameObjectInteractions(IChatGui chatGui, IClientState clientState, ILogService log, Utils utils)
        {
            this.chatGui = chatGui;
            this.clientState = clientState;
            this.log = log;
            this.utils = utils;
        }

        public unsafe bool InteractWithTargetedPlot()
        {
            if (!Player.Available) return false;
            if (Player.IsAnimationLocked) return false;
            if (!utils.DismountIfNeeded()) return false;
            if (IsOccupied()) return false;
            var plotSelected = clientState.LocalPlayer?.TargetObject;
            if (plotSelected == null)
            {
                log.Warning("Attempting to interat with plot object, but no object is selected.");
                return false;
            }

            if (!GlobalData.GardenPlotDataIds.Contains(plotSelected.DataId))
            {
                log.Warning("Attempting to interact with object that is not a plot");
                return false;
            }

            TargetSystem.Instance()->InteractWithObject(plotSelected.Struct(), true);

            return true;
        }
    }
}

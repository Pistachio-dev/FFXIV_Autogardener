using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using DalamudBasics.Chat.ClientOnlyDisplay;
using DalamudBasics.Logging;
using DalamudBasics.Targeting;
using ECommons;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Autogardener.Modules
{
    public class Commands
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

        public Commands(ILogService logService, IClientChatGui clientChatGui, IObjectTable objectTable, ITargetingService targetingService,
            ITargetManager rawTargeting,
            IGameGui gameGui,
            IContextMenu contextMenu, IDataManager dataManager, ICondition condition, IClientState clientState,
            INotificationManager notificationManager)
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

        public unsafe bool InteractWithTargetPlot()
        {
            if (!Player.Available) return false;
            if (Player.IsAnimationLocked) return false;
            if (!Utils.DismountIfNeeded()) return false;
            if (GenericHelpers.IsOccupied()) return false;
            IGameObject? plotSelected = clientState.LocalPlayer?.TargetObject;
            if (plotSelected == null)
            {
                clientChatGui.PrintError("No plot selected.");
                return false;
            }

            if ( plotSelected.Name.TextValue != "")
            {
                clientChatGui.PrintError("That's not a plot");
                return false;
            }

            // TODO: verify plot is a plot
            TargetSystem.Instance()->InteractWithObject(plotSelected.Struct(), true);

            return true;
        }
    }
}

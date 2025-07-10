using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.ClientOnlyDisplay;
using DalamudBasics.Logging;
using DalamudBasics.Targeting;
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
    }
}

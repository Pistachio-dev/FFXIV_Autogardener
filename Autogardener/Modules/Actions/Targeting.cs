using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Actions
{
    public class Targeting
    {
        private readonly ITargetManager targetManager;
        private readonly IClientState clientState;
        private readonly ILogService logService;
        private readonly IObjectTable objectTable;        

        public Targeting(ITargetManager targetManager, IClientState clientState, ILogService logService, IObjectTable objectTable)
        {
            this.targetManager = targetManager;
            this.clientState = clientState;
            this.logService = logService;
            this.objectTable = objectTable;
        }

        public bool TargetObject(ulong gameObjectId)
        {
            var go = objectTable.SearchById(gameObjectId);
            if (go == null)
            {
                return false;
            }

            logService.Debug($"Targeting {go.GameObjectId}");
            targetManager.Target = go;
            return true;
        }

        public bool VerifyTarget(ulong gameObjectId)
        {
            return targetManager.Target?.GameObjectId == gameObjectId;
        }
    }
}

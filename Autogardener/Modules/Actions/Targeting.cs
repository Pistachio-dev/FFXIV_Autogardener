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

        public ulong? CurrentTarget => targetManager.Target?.GameObjectId;

        public bool TargetPlotObject(ulong gameObjectId)
        {
            var go = objectTable.EventObjects.FirstOrDefault(go => go.GameObjectId == gameObjectId);
            if (go == null)
            {
                return false;
            }

            logService.Debug($"Targeting {go.GameObjectId} Type: {go.ObjectKind}");
            targetManager.Target = go;
            return true;
        }

        public bool VerifyTarget(ulong gameObjectId)
        {
            logService.Debug($"Verifying target is {gameObjectId}");
            return targetManager.Target?.GameObjectId == gameObjectId;
        }

        public unsafe bool IsTooFarToInteractWith(ulong gameObjectId)
        {
            var go = objectTable.EventObjects.FirstOrDefault(go => go.GameObjectId == gameObjectId);
            var player = clientState.LocalPlayer;
            if (go == null || player == null)
            {
                return true; // Can't target what does not exist.
            }

            var distance = Math.Abs(Vector3.Distance(go.Position, player.Position));
            return distance > GlobalData.MaxInteractDistance;
        }
    }
}

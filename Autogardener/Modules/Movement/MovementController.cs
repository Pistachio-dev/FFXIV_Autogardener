using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System.Diagnostics;

namespace Autogardener.Modules.Movement
{
    public class MovementController
    {
        private readonly ILogService logService;
        private readonly IClientState clientState;
        private readonly OverrideMovement movementOverride;

        private const float Tolerance = 0.2f;
        private const int TimeoutInSeconds = 10;
        private Stopwatch currentTimer = new Stopwatch();
        internal bool movementInProgress = false;
        internal bool movementWasCompleted = false;
        internal bool movementTimedOut = false;
        internal bool ActionCompleted => movementWasCompleted || movementTimedOut;

        public MovementController(ILogService logService, IClientState clientState) {
            this.logService = logService;
            this.clientState = clientState;
            movementOverride = new OverrideMovement();
        }

        public void MoveForwards()
        {
            var playerPos = clientState.LocalPlayer?.Position ?? Vector3.Zero;
            var destination = playerPos + new Vector3(4, 0, 9);

            movementOverride.DesiredPosition = destination;

            movementOverride.Enabled = true;
        }

        private Vector3 PlayerPos()
        {
            return clientState.LocalPlayer?.Position ?? Vector3.Zero;
        }

        public void MoveToPoint(Vector3 destination)
        {
            
            if (clientState.LocalPlayer == null)
            {
                logService.Warning("Attempting to do movement, but the local player does not exist.");
            }

            logService.Info($"Starting movement: Player pos: {clientState.LocalPlayer?.Position} Destination: {destination}");
            movementWasCompleted = false;
            movementTimedOut = false;
            movementInProgress = true;
            currentTimer.Restart();
            movementOverride.DesiredPosition = destination;

            movementOverride.Enabled = true;
        }

        public void Attach(IFramework framework)
        {
            framework.Update += Tick;
        }

        public void Tick(IFramework framework)
        {
            var distance = Vector3.Distance(PlayerPos(), movementOverride.DesiredPosition);
            if (distance <= Tolerance)
            {
                StopMovemement();
                movementWasCompleted = true;
                return;
            }
            if (currentTimer.Elapsed > TimeSpan.FromSeconds(TimeoutInSeconds)){
                StopMovemement();
                movementTimedOut = true;
                return;
            }
        }

        private void StopMovemement()
        {
            movementOverride.Enabled = false;
            movementInProgress = false;
            currentTimer.Stop();
        }
    }
}

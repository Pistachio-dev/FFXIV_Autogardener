using Autogardener.Modules.Movement;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Schedulers
{
    public class MovementScheduler : IScheduler
    {
        private readonly HighLevelScheduler parentScheduler;
        private Vector3 destination;
        private readonly ILogService logService;
        private readonly IChatGui chatGui;
        private readonly MovementController controller;
        private bool initialized = false;
        private bool done = false;
        public bool Done() => done;

        public MovementScheduler(HighLevelScheduler parentScheduler, Vector3 destination, ILogService logService, IChatGui chatGui, MovementController controller)
        {
            this.parentScheduler = parentScheduler;
            this.destination = destination;
            this.logService = logService;
            this.chatGui = chatGui;
            this.controller = controller;
        }

        public void Init()
        {
            controller.MoveToPoint(destination);
            initialized = true;
        }
        public void Tick()
        {
            if (!initialized)
            {
                Init();
            }
            if (controller.ActionCompleted)
            {
                if (controller.movementTimedOut)
                {
                    chatGui.PrintError("Could not move to the next plot. Aborting");
                    logService.Warning("Could not move to the next plot. Aborting");
                }
                done = true;
            }
        }
    }
}

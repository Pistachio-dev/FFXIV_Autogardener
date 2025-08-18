using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Schedulers
{
    // This is a skeleton until I get things working well for one plot
    public class HighLevelScheduler
    {
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gData;
        private readonly IFramework framework;
        private readonly IChatGui chatGui;
        private GardenPatchScheduler? plotPatchScheduler; // Only one for now
        public HighLevelScheduler(ILogService logService, GameActions op, GlobalData gData, IFramework framework, IChatGui chatGui)
        {            
            this.logService = logService;
            this.op = op;
            this.gData = gData;
            this.framework = framework;
            this.chatGui = chatGui;
        }
        
        public void SchedulePatchTend(PlotPatch patch)
        {
            this.plotPatchScheduler = new GardenPatchScheduler(this, patch, logService, op, gData);
            framework.Update += Tick;
        }

        internal void Abort()
        {
            framework.Update -= Tick;
            chatGui.PrintError("Autogarder got stuck on something. Aborting. Please panic.");
        }
        private void Tick(IFramework framework)
        {
            plotPatchScheduler?.Tick();
            // TODO: Unregister Tick when done.
        }
    }
}

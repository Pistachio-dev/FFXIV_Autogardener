using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
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
        private readonly GardenPatchScheduler plotPatchScheduler; // Only one for now
        public HighLevelScheduler(PlotPatch plotPatch, ILogService logService, GameActions op, GlobalData gData)
        {
            this.plotPatchScheduler = new GardenPatchScheduler(plotPatch, logService, op, gData);
            this.logService = logService;
            this.op = op;
            this.gData = gData;
        }

        public void Tick()
        {
            plotPatchScheduler.Tick();
        }
    }
}

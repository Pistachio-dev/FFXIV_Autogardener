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
    public class GardenPatchScheduler
    {
        public bool Complete => !plotSchedulerQueue.Any();
        private readonly PlotPatch patch;
        private readonly ILogService logService;
        private readonly GameActions op;
        private readonly GlobalData gd;
        private Queue<PlotTendScheduler> plotSchedulerQueue = new Queue<PlotTendScheduler>();
        public GardenPatchScheduler(PlotPatch patch, ILogService logService, GameActions op, GlobalData gd)
        {
            this.patch = patch;
            this.logService = logService;
            this.op = op;
            this.gd = gd;
            foreach (var plot in patch.Plots)
            {
                PlotTendScheduler plotTendScheduler = new PlotTendScheduler(plot, logService, op, gd);
                plotSchedulerQueue.Enqueue(plotTendScheduler);
                return; // TODO: testing on one plot before on all of them.
            }
        }

        public void Tick()
        {
            if (Complete)
            {
                return;
            }

            var plotScheduler = plotSchedulerQueue.Peek();
            if (plotScheduler.Complete)
            {
                plotSchedulerQueue.Dequeue();
                if (Complete) return;
                plotScheduler = plotSchedulerQueue.Peek();
            }

            plotScheduler.Tick();
        }
    }
}

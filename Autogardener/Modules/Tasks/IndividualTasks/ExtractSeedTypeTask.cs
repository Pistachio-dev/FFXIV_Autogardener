using Autogardener.Model.Plots;
using Autogardener.Modules.Actions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal class ExtractSeedTypeTask : GardeningTaskBase
    {
        public ExtractSeedTypeTask(string name, GameActions op) : base(name, op)
        {
        }

        public override bool Confirmation(Plot plot)
        {
            return true;
        }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            string? text = op.AddonManagement.GetTalkAddonDialogue();
            if (text == null) return true;
            (uint id, string name) = op.DataExtraction.ExtractPlantNameAndId(text);
            if (id != 0)
            {
                op.Log.Info($"Seed {name} identified");
                plot.CurrentSeed = id;
            }

            return true;
        }
    }
}

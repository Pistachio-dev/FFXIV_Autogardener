using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using ECommons.GameHelpers;
using Autogardener.Modules.Actions;
using Autogardener.Model.Plots;


namespace Autogardener.Modules.Tasks.IndividualTasks
{
    internal class SkipChatterTask : GardeningTaskBase
    {
        public SkipChatterTask(string name, GameActions op) : base(name, op) { }

        public override bool PreRun(Plot plot)
        {
            return true;
        }

        public override bool Task(Plot plot)
        {
            return op.AddonManagement.TrySkipTalk();
        }
        public override bool Confirmation(Plot plot)
        {
            return !op.AddonManagement.IsTalkAddonVisible();
        }
    }
}

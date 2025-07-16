using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules
{
    internal class GardeningAddon : AddonMasterBase<AtkUnitBase>
    {
        public GardeningAddon(nint addon) : base(addon)
        {
        }

        public override string AddonDescription => "The pop up window to select seeds and fertilizer for gardening";
    }
}

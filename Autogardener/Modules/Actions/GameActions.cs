using Autogardener.Modules.Tasks.Exceptions;
using DalamudBasics.Logging;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules.Actions
{
    public class GameActions
    {
        public GameActions(AddonManagement management, Targeting targeting, ILogService logService, GameObjectInteractions goInteractions)
        {
            AddonManagement = management;
            Targeting = targeting;
            Log = logService;
            GoInteractions = goInteractions; //GameObject interactions
        }

        public AddonManagement AddonManagement { get; }
        public Targeting Targeting { get; }
        public ILogService Log { get; }
        public GameObjectInteractions GoInteractions { get; }
    }
}

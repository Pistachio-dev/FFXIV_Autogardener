using DalamudBasics.Logging;

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

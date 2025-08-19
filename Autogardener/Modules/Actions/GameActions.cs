using Dalamud.Plugin.Services;
using DalamudBasics.Logging;

namespace Autogardener.Modules.Actions
{
    public class GameActions
    {
        public GameActions(AddonManagement management, Targeting targeting, ILogService logService, GameObjectInteractions goInteractions,
            DataExtraction dataExtraction, IChatGui chatGui, Inventory inventory)
        {
            AddonManagement = management;
            Targeting = targeting;
            Log = logService;
            GoInteractions = goInteractions; //GameObject interactions
            DataExtraction = dataExtraction;
            ChatGui = chatGui;
            Inventory = inventory;
        }

        public AddonManagement AddonManagement { get; }
        public Targeting Targeting { get; }
        public ILogService Log { get; }
        public GameObjectInteractions GoInteractions { get; }
        public DataExtraction DataExtraction { get; }
        public IChatGui ChatGui { get; }
        public Inventory Inventory { get; }
    }
}

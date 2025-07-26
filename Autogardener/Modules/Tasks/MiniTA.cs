using Dalamud.Plugin.Services;
using DalamudBasics.Logging;
using ECommons.Automation;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace Autogardener.Modules.Tasks
{
    public class MiniTA
    {
        private readonly GardeningTaskManager taskManager;
        private readonly IFramework framework;
        private static readonly TimeSpan Recent = TimeSpan.FromMilliseconds(200);

        public MiniTA(GardeningTaskManager taskManager, IFramework framework)
        {
            this.taskManager = taskManager;
            this.framework = framework;
        }

        private string lastSelectedOption;
        private DateTime lastSelectedOptionTimeUtc;

        public void RegisterOptionAttemptedToSelect(string option)
        {
            lastSelectedOption = option;
            lastSelectedOptionTimeUtc = DateTime.UtcNow;
        }

        public void Attach()
        {
            framework.Update += SkipStuckDialogs;
        }


        private unsafe void SkipStuckDialogs(IFramework framework)
        {
            if (taskManager.IsBusy())
            {
                SkipTalkBlockingActions();

                ReselectSelectStringOption();

                ConfirmGardeningHousingAddon();
            }
        }

        private unsafe void ConfirmGardeningHousingAddon()
        {
            if (TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                Callback.Fire(gardeningAddon, false, 0, 0, 0, 0, 0);
            }            
        }
        private unsafe void SkipTalkBlockingActions()
        {
            if (TryGetAddonByName<AddonTalk>("Talk", out var addon) && addon->AtkUnitBase.IsVisible)
            {
                new AddonMaster.Talk((nint)addon).Click();
            }
        }

        private unsafe void ReselectSelectStringOption()
        {
            if (lastSelectedOption != null
                && DateTime.UtcNow - lastSelectedOptionTimeUtc < Recent
                && TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString)
                && IsAddonReady(&addonSelectString->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addonSelectString).Entries;
                if (TryGetMatchingEntry(entries, lastSelectedOption, out var matchingEntry))
                {
                    matchingEntry.Select();
                }
            }
        }

        private bool TryGetMatchingEntry(AddonMaster.SelectString.Entry[] entries, string actionToSelect, out AddonMaster.SelectString.Entry entry)
        {
            Func<AddonMaster.SelectString.Entry, bool> condition = entry => entry.SeString.ToString().Contains(actionToSelect, StringComparison.OrdinalIgnoreCase);
            if (entries.Any(condition))
            {
                entry = entries.First(condition);
                return true;
            }

            entry = default;
            return false;
        }
    }
}

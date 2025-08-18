using Autogardener.Modules.Exceptions;
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
    public class AddonManagement
    {
        private ILogService log { get; }

        public AddonManagement(ILogService logService)
        {
            log = logService;
        }
        
        public unsafe bool TrySkipTalk()
        {
            if (TryGetAddonByName<AddonTalk>("Talk", out var addonTalk)
                && addonTalk->AtkUnitBase.IsVisible)
            {
                new AddonMaster.Talk((nint)addonTalk).Click();

                return true;
            }

            return false;
        }

        public unsafe bool IsTalkAddonVisible()
        {
            return TryGetAddonByName<AddonTalk>("Talk", out var addonTalk)
                && addonTalk->AtkUnitBase.IsVisible;
        }

        public unsafe bool TrySelectActionString(string actionToSelect, out List<string> options)
        {
            options = new List<string>();
            if (TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString)
                && IsAddonReady(&addonSelectString->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addonSelectString).Entries;
                options.AddRange(entries.Select(entry => entry.Text));
                if (TryGetMatchingEntry(entries, actionToSelect, out var matchingEntry))
                {
                    matchingEntry.Select();
                    return true;
                }

                throw new SelectStringNotPresentInAddon(actionToSelect, options);
            }

            return false;
        }

        public unsafe bool VerifySelectStringAddonIsGone(List<string> previousOptions)
        {
            if (!TryGetAddonByName<AddonSelectString>("SelectString", out var addonSelectString) || !addonSelectString->IsVisible)
            {
                return true;
            }
            var entries = new AddonMaster.SelectString(addonSelectString).Entries;
            var options = entries.Select(e => e.Text).ToList();
            if (options.Count >= previousOptions.Count) return true;
            for (var i = 0; i < options.Count; i++)
            {
                if (!options[i].Equals(previousOptions[i])) return true;
            }

            return false;

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

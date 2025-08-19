using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using DalamudBasics.Configuration;
using DalamudBasics.Logging;
using ECommons.Automation;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Autogardener.Modules
{
    public unsafe class Utils
    {
        internal int FrameDelay = 100;

        public Utils(ILogService log, IConfiguration config)
        {
            this.log = log;
        }

        // Snatched from Lifestream
        public bool DismountIfNeeded()
        {
            if (IsMountedEx())
            {
                EzThrottler.Throttle("PlayerMounted", 200, true);
                if (EzThrottler.Throttle("DismountPlayer", 1000))
                {
                    Chat.ExecuteGeneralAction(23);
                }
                return false;
            }
            if (!EzThrottler.Check("PlayerMounted")) return false;
            return true;
        }

        public bool IsMountedEx()
        {
            if (Svc.Condition[ConditionFlag.Mounted]) return true;
            if (IsPlayerFalling()) return true;
            return false;
        }

        public bool IsPlayerFalling()
        {
            var p = Svc.ClientState.LocalPlayer;
            if (p == null)
                return true;

            // 0 if grounded
            // 1 = "jumpsquat"
            // 3 = going up
            // 4 = stopped
            // 5 = going down
            var isJumping = *(byte*)(p.Address + 496 + 208) > 0;
            // 1 iff dismounting and haven't hit the ground yet
            var isAirDismount = **(byte**)(p.Address + 496 + 904) == 1;

            return isJumping || isAirDismount;
        }

        internal const string ThrottleKey = "AutogardenerGenericThrottle";
        private readonly ILogService log;

        internal bool GenericThrottle => FrameThrottler.Throttle(ThrottleKey, FrameDelay);

        internal void RethrottleGeneric(int num)
        {
            FrameThrottler.Throttle(ThrottleKey, num, true);
        }

        internal void RethrottleGeneric()
        {
            FrameThrottler.Throttle(ThrottleKey, FrameDelay, true);
        }

        internal bool TrySelectSpecificEntry(string text, Func<bool> Throttler = null)
        {
            return TrySelectSpecificEntry(new string[] { text }, Throttler);
        }

        internal bool TrySelectSpecificEntry(IEnumerable<string> text, Func<bool> Throttler = null)
        {
            return TrySelectSpecificEntry((x) => x.StartsWithAny(text), Throttler);
        }

        internal bool TrySelectSpecificEntry(Func<string, bool> inputTextTest, Func<bool> Throttler = null)
        {
            if (TryGetAddonByName<AddonSelectString>("SelectString", out var addon) && IsAddonReady(&addon->AtkUnitBase))
            {
                if (new AddonMaster.SelectString(addon).Entries.TryGetFirst(x => inputTextTest(x.Text), out var entry))
                {
                    if ((Throttler?.Invoke() ?? GenericThrottle))
                    {
                        entry.Select();
                        log.Debug($"TrySelectSpecificEntry: selecting {entry}");
                        return true;
                    }
                }
            }
            else
            {
                RethrottleGeneric();
            }
            return false;
        }

        internal List<string> GetEntries(AddonSelectString* addon)
        {
            var list = new List<string>();
            for (var i = 0; i < addon->PopupMenu.PopupMenu.EntryCount; i++)
            {
                list.Add(MemoryHelper.ReadSeStringNullTerminated((nint)addon->PopupMenu.PopupMenu.EntryNames[i].Value).GetText());
            }
            return list;
        }

        public static bool IsOccupied()
        {
            return Svc.Condition[ConditionFlag.Occupied]
                   || Svc.Condition[ConditionFlag.Occupied30]
                   || Svc.Condition[ConditionFlag.Occupied33]
                   || Svc.Condition[ConditionFlag.Occupied38]
                   || Svc.Condition[ConditionFlag.Occupied39]
                   || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                   || Svc.Condition[ConditionFlag.OccupiedInEvent]
                   //|| Svc.Condition[ConditionFlag.OccupiedInQuestEvent] // This one gets triggered while gardening. Why? Who knows. 
                   || Svc.Condition[ConditionFlag.OccupiedSummoningBell]
                   || Svc.Condition[ConditionFlag.WatchingCutscene]
                   || Svc.Condition[ConditionFlag.WatchingCutscene78]
                   || Svc.Condition[ConditionFlag.BetweenAreas]
                   || Svc.Condition[ConditionFlag.BetweenAreas51]
                   || Svc.Condition[ConditionFlag.InThatPosition]
                   || Svc.Condition[ConditionFlag.TradeOpen]
                   || Svc.Condition[ConditionFlag.Crafting]
                   || Svc.Condition[ConditionFlag.ExecutingCraftingAction]
                   || Svc.Condition[ConditionFlag.PreparingToCraft]
                   || Svc.Condition[ConditionFlag.InThatPosition]
                   || Svc.Condition[ConditionFlag.Unconscious]
                   || Svc.Condition[ConditionFlag.MeldingMateria]
                   || Svc.Condition[ConditionFlag.Gathering]
                   || Svc.Condition[ConditionFlag.OperatingSiegeMachine]
                   || Svc.Condition[ConditionFlag.CarryingItem]
                   || Svc.Condition[ConditionFlag.CarryingObject]
                   || Svc.Condition[ConditionFlag.BeingMoved]
                   || Svc.Condition[ConditionFlag.RidingPillion]
                   || Svc.Condition[ConditionFlag.Mounting]
                   || Svc.Condition[ConditionFlag.Mounting71]
                   || Svc.Condition[ConditionFlag.ParticipatingInCustomMatch]
                   || Svc.Condition[ConditionFlag.PlayingLordOfVerminion]
                   || Svc.Condition[ConditionFlag.ChocoboRacing]
                   || Svc.Condition[ConditionFlag.PlayingMiniGame]
                   || Svc.Condition[ConditionFlag.Performing]
                   || Svc.Condition[ConditionFlag.PreparingToCraft]
                   || Svc.Condition[ConditionFlag.Fishing]
                   || Svc.Condition[ConditionFlag.Transformed]
                   || Svc.Condition[ConditionFlag.UsingHousingFunctions]
                   || Svc.ClientState.LocalPlayer?.IsTargetable != true;
        }
    }
}

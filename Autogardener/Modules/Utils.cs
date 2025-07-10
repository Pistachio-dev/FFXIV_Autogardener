using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Autogardener.Modules
{
    internal static unsafe class Utils
    {
        // Snatched from Lifestream
        public static bool DismountIfNeeded()
        {
            if (Utils.IsMountedEx())
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

        public static bool IsMountedEx()
        {
            if (Svc.Condition[ConditionFlag.Mounted]) return true;
            if (IsPlayerFalling()) return true;
            return false;
        }

        public static bool IsPlayerFalling()
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
    }
}

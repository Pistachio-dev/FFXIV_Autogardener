using Dalamud.Hooking;
using DalamudBasics.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.Hooks.SendAction;

namespace Autogardener.Modules
{
    internal static  class ActionWatcher
    {
        private unsafe static ILogService logService;

        static unsafe ActionWatcher()
        {
            SendActionHook ??= Svc.Hook.HookFromSignature<SendActionDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9 41 0F B7 D9", SendActionDetour);
            UseActionHook ??= Svc.Hook.HookFromAddress<UseActionDelegate>(ActionManager.Addresses.UseAction.Value, UseActionDetour);

        }

        public static void SetLogService(ILogService service)
        {
            logService = service;
            SendActionHook.Enable();
            UseActionHook.Enable();
        }

        private delegate void SendActionDelegate(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);
        private static readonly Hook<SendActionDelegate>? SendActionHook;
        private unsafe delegate bool UseActionDelegate(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted);
        private readonly static Hook<UseActionDelegate>? UseActionHook;

        private unsafe static void SendActionDetour(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
        {
            logService.Info($"TargetId: {targetObjectId} ActionType: {(ActionType)actionType} ActionId: {actionId} " +
                $"\nSeq: {sequence} 5: {a5} 6: {a6} 7: {a7} 8: {a8} 9: {a9}");
            SendActionHook!.Original(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);
        }

        private unsafe static bool UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted)
        {
            return UseActionHook.Original(actionManager, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);            
        }
    }
}

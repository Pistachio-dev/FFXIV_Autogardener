using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using DalamudBasics.Chat.ClientOnlyDisplay;
using DalamudBasics.Logging;
using DalamudBasics.Targeting;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace Autogardener.Modules
{
    public class Commands
    {
        private readonly ILogService logService;
        private readonly IClientChatGui clientChatGui;
        private readonly IObjectTable objectTable;
        private readonly ITargetingService targetingService;
        private readonly ITargetManager rawTargeting;
        private readonly IGameGui gameGui;
        private readonly IContextMenu contextMenu;
        private readonly IDataManager dataManager;
        private readonly ICondition condition;
        private readonly IClientState clientState;
        private readonly INotificationManager notificationManager;
        private readonly Utils utils;

        public Commands(ILogService logService, IClientChatGui clientChatGui, IObjectTable objectTable, ITargetingService targetingService,
            ITargetManager rawTargeting,
            IGameGui gameGui,
            IContextMenu contextMenu, IDataManager dataManager, ICondition condition, IClientState clientState,
            INotificationManager notificationManager, Utils utils)
        {
            this.logService = logService;
            this.clientChatGui = clientChatGui;
            this.objectTable = objectTable;
            this.targetingService = targetingService;
            this.rawTargeting = rawTargeting;
            this.gameGui = gameGui;
            this.contextMenu = contextMenu;
            this.dataManager = dataManager;
            this.condition = condition;
            this.clientState = clientState;
            this.notificationManager = notificationManager;
            this.utils = utils;
        }

        public void DescribeTarget()
        {
            EventObject ob;
            IGameObject? target = rawTargeting.Target;
            if (target == null)
            {
                logService.Info("Nothing targeted.");
                return;
            }

            logService.Info($"Name: [{target.Name}] GameObjectId [{target.GameObjectId}] EntityId [{target.EntityId}] DataId [{target.DataId}]");
            logService.Info($" OwnerId [{target.OwnerId}] ObjectIndex [{target.ObjectIndex}] ObjectKind [{target.ObjectKind}] SubKind [{target.SubKind}]");
        }

        public unsafe void ListCurrentMenuOptions()
        {
            if(GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out AddonSelectString* addon)
                && IsAddonReady(&addon->AtkUnitBase))
            {
                foreach (var entry in new AddonMaster.SelectString(addon).Entries)
                {
                    logService.Info($"Entry {entry.Index}: {entry.Text}");
                }
                return;
            }
            logService.Info("No SelectString addon present, or it was not ready.");
            return;
        }

        public unsafe void SelectEntry(string text)
        {
            if (GenericHelpers.TryGetAddonByName<AddonSelectString>("SelectString", out AddonSelectString* addon)
                && IsAddonReady(&addon->AtkUnitBase))
            {
                var entries = new AddonMaster.SelectString(addon).Entries;
                Func<AddonMaster.SelectString.Entry, bool> equality = (entry) => entry.Text.StartsWith(text, StringComparison.OrdinalIgnoreCase);
                if (!entries.Any(equality))
                {
                    logService.Warning($"No entry with text {text} was found in the list menu.");
                }
                var entry = entries.First(equality);
                entry.Select();
            }

            logService.Info("No SelectString addon present, or it was not ready.");
            return;
        }
        public unsafe bool TryDetectGardeningWindow(out AtkUnitBase* addon)
        {
            if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>("HousingGardening", out addon))
            {
                logService.Info("Housing gardening action not detected");
                return false;
            }
            logService.Info("Housing gardening action detected");

            return true;
        }

        public unsafe void ClickCancelOnGardeningWindow()
        {
            if (TryDetectGardeningWindow(out AtkUnitBase* addon))
            {
                try
                {
                    logService.Warning("Attempting serialization");

                    //string serialized = System.Text.Json.JsonSerializer.Serialize(*addon, typeof(AtkComponentBase),
                    //    new System.Text.Json.JsonSerializerOptions()
                    //    {
                    //        IncludeFields = true,
                    //    });
                    //logService.Info($"Node: {addon->GetAtkResNode()->ToString()}"); // This is a crash. Or maybe ToString is...
                    //logService.Info($"Node: {addon->GetAtkResNode()->Type}"); // Nope, Type also crashes...
                    var node = addon->NameString;
                    logService.Info($"Name: {node}");
                    var root = addon->RootNode->Type;
                    logService.Info($"Root type: {root}");
                    //AtkResNode* res = addon->RootNode;
                    //Type button = addon->WindowNode->PrevSiblingNode->GetComponent()->GetType();
                    //var wah = (AtkArrayData*)addon->GetRootNode()->GetComponent()->GetAtkResNode();
                    //logService.Info("Type for button: " + addon->GetRootNode()->GetComponent()->GetAtkResNode()->NodeId);
                    //logService.Info($"Is it null: {*node == default}");
                    var windowNode = addon->RootNode->ChildNode;
                    var textNode = GetSiblingResNodeById(windowNode, 8);
                    if (textNode == null)
                    {
                        logService.Info("Could not find the text node");
                    }

                    string text = textNode->GetAsAtkComponentButton()->ButtonTextNode->NodeText.GetText();
                    logService.Info($"Text recovered: {text}");

                }
                catch (Exception e) { logService.Error(e, "Error"); }

                //try
                //{
                //    logService.Info($"Node: {addon->GetResNodeById(1)->ToString()}");
                //}
                //catch { }
                //var baseNode = addon->GetNodeById(1);
                //if (baseNode == null)
                //{
                //    logService.Warning("We didn't get shit.");
                //}
                //logService.Info($"This should be the base node, id 1, type {baseNode->Type}");

                //var cancelButton = addon->GetNodeById(9);
                //logService.Info($"Hopefully cancel button, id 9, type {cancelButton->Type}");
                //var acceptButton = addon->GetNodeById(8);
                //logService.Info($"Hopefully accept button, id 8, type {acceptButton->Type}");

                //cancelButton->SetChecked(true);
            }
        }


        private unsafe AtkResNode* GetSiblingResNodeById(AtkResNode* startingPoint, uint id)
        {
            var nodeBefore = GetSiblingResNodeById(startingPoint, id, true);
            if (nodeBefore != null)
            {
                return nodeBefore;
            }

            return GetSiblingResNodeById(startingPoint, id, false);
        }

        private unsafe AtkResNode* GetSiblingResNodeById(AtkResNode* node, uint id, bool goBackwards)
        {
            if (node == null || node->NodeId == id)
            {
                return node;
            }
            var nextNode = goBackwards ? node->PrevSiblingNode : node->NextSiblingNode;
            if (nextNode == null)
            {
                return null;
            }

            return GetSiblingResNodeById(nextNode, id, goBackwards);
        }

        public unsafe bool InteractWithTargetPlot()
        {
            if (!Player.Available) return false;
            if (Player.IsAnimationLocked) return false;
            if (!utils.DismountIfNeeded()) return false;
            if (GenericHelpers.IsOccupied()) return false;
            IGameObject? plotSelected = clientState.LocalPlayer?.TargetObject;
            if (plotSelected == null)
            {
                clientChatGui.PrintError("No plot selected.");
                return false;
            }

            if ( plotSelected.Name.TextValue != "")
            {
                clientChatGui.PrintError("That's not a plot");
                return false;
            }

            // TODO: verify plot is a plot
            TargetSystem.Instance()->InteractWithObject(plotSelected.Struct(), true);

            return true;
        }
    }
}

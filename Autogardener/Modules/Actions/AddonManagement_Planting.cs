using ECommons.Automation;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Linq;

namespace Autogardener.Modules.Actions
{
    public partial class AddonManagement
    {
        public unsafe bool ClickConfirmOnHousingGardeningAddon()
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                log.Warning("HousingGardening addon not found");
                return false;
            }

            Callback.Fire(gardeningAddon, false, 0, 0, 0, 0, 0);

            return true;
        }

        public unsafe bool ClickYesGardeningConfirmationPopup()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }

            if (gardeningAddon->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible())
            {
                new AddonMaster.SelectYesno((nint)addon).Yes();
                return true;
            }

            return false;
        }

        public unsafe bool VerifyGardeningConfirmationYesNoPopupAppears()
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }

            return gardeningAddon->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible;
        }

        public unsafe bool VerifyGardeningAddonIsGone()
        {
            return !(TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon) && gardeningAddon->IsVisible);
        }

        public unsafe bool PickSeeds(uint seedItemId)
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }
            var seedIndex = GetIndexFromCollection(gData.Seeds.Keys.ToHashSet(), seedItemId);
            log.Info($"Seed: {seedItemId}");

            return TryClickItem(gardeningAddon, 2, seedIndex);
        }

        public unsafe bool PickSoil(uint soilItemId)
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }
            var soilIndex = GetIndexFromCollection(gData.Soils.Keys.ToHashSet(), soilItemId);
            log.Info($"Soil: {soilItemId}");

            return TryClickItem(gardeningAddon, 1, soilIndex);
        }

        private unsafe int GetIndexFromCollection(HashSet<uint> idCollection, uint targetId)
        {
            var container = Inventory.GetCombinedInventories();
            var indexOfTargetItem = 0;
            foreach (var cont in container)
            {
                for (var i = 0; i < cont->Size; i++)
                {
                    var item = cont->GetInventorySlot(i);
                    if (idCollection.Contains(item->ItemId)) // It's a member of the collection, like "seeds", or "soils"
                    {
                        if (item->ItemId == targetId)
                        {
                            return indexOfTargetItem;
                        }
                        else
                        {
                            indexOfTargetItem++;
                        }
                    }
                }
            }

            return -1;
        }

        private unsafe bool ClickGardeningAddon(int addonIndex, int itemIndex) // Index starts with 1
        {
            if (!TryGetAddonByName<AtkUnitBase>("HousingGardening", out var gardeningAddon))
            {
                return false;
            }

            return TryClickItem(gardeningAddon, addonIndex, itemIndex);
        }

        private unsafe bool TryClickItem(AtkUnitBase* addon, int i, int itemIndex)
        {
            if (!TryGetAddonByName<AtkUnitBase>("ContextIconMenu", out var contextMenu))
            {
                return false;
            }
            if (contextMenu is null || !contextMenu->IsVisible)
            {
                var slot = i - 1;

                Svc.Log.Debug($"{slot}");
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 2
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = (uint)slot
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 1
                };

                addon->FireCallback(5, values);
                CloseItemDetail();
                return false;
            }
            else
            {
                var value = (uint)(i == 1 ? 27405 : 27451);
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = itemIndex
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = value
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    UInt = 0
                };

                contextMenu->FireCallback(5, values, true);
                Svc.Log.Debug($"Filled slot {i}");
                return true;
            }
        }

        private unsafe bool CloseItemDetail()
        {
            TryGetAddonByName<AtkUnitBase>("ItemDetail", out var itemDetail);
            if (itemDetail is null || !itemDetail->IsVisible) return false;

            var values = stackalloc AtkValue[1];
            values[0] = new AtkValue()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = -1
            };

            itemDetail->FireCallback(1, values);
            return true;
        }
    }
}

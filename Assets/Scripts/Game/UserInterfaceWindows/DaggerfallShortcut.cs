using System;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    public static class DaggerfallShortcut
    {
        const string textDatabase = "DialogShortcuts";

        public enum Buttons
        {
            None,

            Accept,
            Reject,
            Cancel,
            Yes,
            No,
            OK,
            Male,
            Female,
            Add,
            Delete,
            Edit,
            Copy,
            Guilty,
            NotGuilty,
            Debate,
            Lie,
            Anchor,
            Teleport,

            // Main menu
            MainMenuLoad,
            MainMenuStart,
            MainMenuExit,

            // Options menu
            OptionsExit,
            OptionsContinue,
            OptionsSave,
            OptionsLoad,
            OptionsControls,
            OptionsFullScreen,
            OptionsHeadBobbing,

            // Rest menu
            RestForAWhile,
            RestUntilHealed,
            RestLoiter,
            RestStop,

            // Transport menu
            TransportFoot,
            TransportHorse,
            TransportCart,
            TransportShip,
            TransportExit,

            // TravelMap
            TravelMapFind,
            TravelMapList,

            // Inventory screen
            InventoryWeapons,
            InventoryMagic,
            InventoryClothing,
            InventoryIngredients,
            InventoryWagon,
            InventoryInfo,
            InventoryEquip,
            InventoryRemove,
            InventoryUse,
            InventoryGold,
            InventoryExit,

            // Trade screen
            TradeWagon,
            TradeInfo,
            TradeSelect,
            TradeSteal,
            TradeBuy,
            TradeIdentify,
            TradeRepair,
            TradeSell,
            TradeClear,
            TradeExit,

            // Automap screen
            AutomapSwitchAutomapGridMode,
            AutomapResetView,
            AutomapResetRotationPivotAxisView,
            AutomapSwitchFocusToNextBeaconObject,
            AutomapSwitchToNextAutomapRenderMode,
            AutomapSwitchToAutomapRenderModeCutout,
            AutomapSwitchToAutomapRenderModeWireframe,
            AutomapSwitchToAutomapRenderModeTransparent,
            AutomapSwitchToAutomapBackgroundOriginal,
            AutomapSwitchToAutomapBackgroundAlternative1,
            AutomapSwitchToAutomapBackgroundAlternative2,
            AutomapSwitchToAutomapBackgroundAlternative3,
            AutomapMoveLeft,
            AutomapMoveRight,
            AutomapMoveForward,
            AutomapMoveBackward,
            AutomapMoveRotationPivotAxisLeft,
            AutomapMoveRotationPivotAxisRight,
            AutomapMoveRotationPivotAxisForward,
            AutomapMoveRotationPivotAxisBackward,
            AutomapRotateLeft,
            AutomapRotateRight,
            AutomapRotateCameraLeft,
            AutomapRotateCameraRight,
            AutomapRotateCameraOnCameraYZplaneAroundObjectUp,
            AutomapRotateCameraOnCameraYZplaneAroundObjectDown,
            AutomapUpstairs,
            AutomapDownstairs,
            AutomapIncreaseSliceLevel,
            AutomapDecreaseSliceLevel,
            AutomapZoomIn,
            AutomapZoomOut,
            AutomapIncreaseCameraFieldOfFiew,
            AutomapDecreaseCameraFieldOfFiew,

            // Guilds
            GuildsJoin,
            GuildsTalk,
            GuildsExit,
            GuildsTraining,
            GuildsGetQuest,
            GuildsRepair,
            GuildsIdentify,
            GuildsDonate,
            GuildsCure,
            GuildsBuyPotions,
            GuildsMakePotions,
            GuildsBuySpells,
            GuildsMakeSpells,
            GuildsBuyMagicItems,
            GuildsMakeMagicItems,
            GuildsSellMagicItems,
            GuildsTeleport,
            GuildsDaedraSummon,
            GuildsSpymaster,
            GuildsBuySoulgems,
            GuildsReceiveArmor,
            GuildsReceiveHouse,

            // Witches Covens
            WitchesTalk,
            WitchesDaedraSummon,
            WitchesQuest,
            WitchesExit,
        }

        public static Dictionary<Buttons, HotkeySequence> keys = null;

        private static void CheckLoaded()
        {
            if (keys == null)
            {
                keys = new Dictionary<Buttons, HotkeySequence>();
                foreach (Buttons button in Enum.GetValues(typeof(Buttons)))
                {
                    if (button == Buttons.None)
                        continue;

                    string buttonName = Enum.GetName(typeof(Buttons), button);
                    if (TextManager.Instance.HasText(textDatabase, buttonName))
                    {
                        keys[button] = HotkeySequence.FromString(TextManager.Instance.GetText(textDatabase, buttonName));
                    }
                    else
                        Debug.Log(string.Format("{0}: no {1} entry", textDatabase, buttonName));
                }
            }
        }

        public static HotkeySequence GetBinding(Buttons button)
        {
            CheckLoaded();
            HotkeySequence key;
            if (keys.TryGetValue(button, out key))
                return key;
            return HotkeySequence.None;
        }
    }
}

// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System.Collections.Generic;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// Potent vs enemy types.
    /// </summary>
    public class PotentVs : BaseEntityEffect
    {
        public static readonly string EffectKey = EnchantmentTypes.PotentVs.ToString();

        public override void SetProperties()
        {
            properties.Key = EffectKey;
            properties.GroupName = TextManager.Instance.GetText(textDatabase, EffectKey);
            properties.ShowSpellIcon = false;
            properties.AllowedCraftingStations = MagicCraftingStations.ItemMaker;
            properties.ItemMakerFlags = ItemMakerFlags.AllowMultiplePrimaryInstances | ItemMakerFlags.WeaponOnly;
            properties.EnchantmentPayloadFlags = EnchantmentPayloadFlags.None; // TEMP: Payload currently handled by PassiveItemSpecialsEffect
        }

        /// <summary>
        /// Outputs all variant settings for this enchantment.
        /// </summary>
        public override EnchantmentSettings[] GetEnchantmentSettings()
        {
            List<EnchantmentSettings> enchantments = new List<EnchantmentSettings>();

            // Enumerate classic params
            for (int i = 0; i < classicParamCosts.Length; i++)
            {
                EnchantmentSettings enchantment = new EnchantmentSettings()
                {
                    Version = 1,
                    EffectKey = EffectKey,
                    ClassicType = EnchantmentTypes.PotentVs,
                    ClassicParam = (short)i,
                    PrimaryDisplayName = properties.GroupName,
                    SecondaryDisplayName = TextManager.Instance.GetText(textDatabase, classicTextKeys[i]),
                    EnchantCost = classicParamCosts[i],
                };

                enchantments.Add(enchantment);
            }

            return enchantments.ToArray();
        }

        #region Payloads

        public override bool IsEnchantmentExclusiveTo(EnchantmentSettings[] settingsToTest, EnchantmentParam? comparerParam = null)
        {
            string lowDamageVsKey = EnchantmentTypes.LowDamageVs.ToString();
            foreach (EnchantmentSettings settings in settingsToTest)
            {
                // Exclusive with opposing LowDamageVs param
                if (settings.EffectKey == lowDamageVsKey && comparerParam != null && settings.ClassicParam == comparerParam.Value.ClassicParam)
                    return true;
            }

            return false;
        }

        #endregion

        #region Classic Support

        static short[] classicParamCosts =
        {
            800,    //Undead
            900,    //Daedra
            1000,   //Humanoid
            1200,   //Animals
        };

        static string[] classicTextKeys =
        {
            "undead",
            "daedra",
            "humanoid",
            "animalsUpper",
        };

        #endregion
    }
}
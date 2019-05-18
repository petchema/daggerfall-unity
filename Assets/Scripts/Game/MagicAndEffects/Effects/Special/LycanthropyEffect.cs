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

using UnityEngine;
using FullSerializer;
using DaggerfallConnect;
using DaggerfallConnect.Utility;
using DaggerfallConnect.FallExe;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;
using Wenzil.Console;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// Stage two curse effect for lycanthropy deployed after stage one infection completed.
    /// Handles buffs and other long-running werebeast effects.
    /// Note: This effect should only be assigned to player entity by stage one disease effect or classic character import.
    /// </summary>
    public class LycanthropyEffect : RacialOverrideEffect
    {
        #region Fields

        public const string LycanthropyCurseKey = "Lycanthropy-Curse";

        const string generalTextDatabase = "GeneralText";
        const int paperDollWidth = 110;
        const int paperDollHeight = 184;

        RaceTemplate compoundRace;
        LycanthropyTypes infectionType = LycanthropyTypes.None;
        uint lastKilledInnocent;
        bool hasStartedInitialLycanthropyQuest;
        bool wearingHircineRing;
        bool isTransformed;
        bool isFullMoon;

        DFSize backgroundFullSize = new DFSize(125, 198);
        Rect backgroundSubRect = new Rect(8, 7, paperDollWidth, paperDollHeight);
        Texture2D backgroundTexture;

        float moveSoundTimer;

        #endregion

        #region Constructors

        public LycanthropyEffect()
        {
            InitMoveSoundTimer();

            // TODO: Register commands
        }

        #endregion

        #region Properties

        public LycanthropyTypes InfectionType
        {
            get { return infectionType; }
            set { infectionType = value; }
        }

        public override RaceTemplate CustomRace
        {
            get { return GetCompoundRace(); }
        }

        public bool IsTransformed
        {
            get { return isTransformed; }
        }

        /// <summary>
        /// Combat Voices option is suppressed while transformed.
        /// Transformed lycanthropes play custom attack voices on enemy hit.
        /// </summary>
        public override bool SuppressOptionalCombatVoices
        {
            get { return isTransformed; }
        }

        /// <summary>
        /// Lycanthropes only display a custom background while transformed.
        /// </summary>
        public override bool SuppressPaperDollBodyAndItems
        {
            get { return isTransformed; }
        }

        #endregion

        #region Overrides

        public override void SetProperties()
        {
            properties.Key = LycanthropyCurseKey;
            properties.ShowSpellIcon = false;
            bypassSavingThrows = true;
        }

        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);

            // Create compound lycanthrope race from birth race
            CreateCompoundRace();

            // Get infection type from stage one disease
            // Note: Classic save import will start this effect and set correct type after load
            LycanthropyInfection infection = (LycanthropyInfection)GameManager.Instance.PlayerEffectManager.FindIncumbentEffect<LycanthropyInfection>();
            if (infection != null)
                infectionType = infection.InfectionType;

            // Considered sated on first start
            UpdateSatiation();

            // Our transformation is complete - cure everything on player (including stage one disease)
            GameManager.Instance.PlayerEffectManager.CureAll();
        }

        public override void ConstantEffect()
        {
            base.ConstantEffect();

            // Get peered entity gameobject
            DaggerfallEntityBehaviour entityBehaviour = GetPeeredEntityBehaviour(manager);
            if (!entityBehaviour)
                return;

            // Assign constant state changes for lycanthropes
            entityBehaviour.Entity.IsImmuneToDisease = true;

            // Assign minimum metal to hit only while transformed
            if (isTransformed)
                entityBehaviour.Entity.MinMetalToHit = WeaponMaterialTypes.Silver;
            else
                entityBehaviour.Entity.MinMetalToHit = WeaponMaterialTypes.Iron;

            // Play move sound while transformed after random amount of time has elapsed
            if (isTransformed)
            {
                moveSoundTimer -= Time.deltaTime;
                if (moveSoundTimer < 0)
                {
                    PlayLycanthropeMoveSound();
                    InitMoveSoundTimer();
                }
            }
        }

        public override void MagicRound()
        {
            base.MagicRound();

            // Check if player is wearing Hircine's Ring at start of each magic round
            // This item will change certain lycanthropy payload behaviours when equipped
            wearingHircineRing = IsWearingHircineRing();

            // Check for full moon in either lunar cycle
            isFullMoon = DaggerfallUnity.Instance.WorldTime.Now.MassarLunarPhase == LunarPhases.Full || DaggerfallUnity.Instance.WorldTime.Now.SecundaLunarPhase == LunarPhases.Full;

            ApplyLycanthropeAdvantages();
            ForceTransformDuringFullMoon();

            // Some temp debug info used during development
            Debug.LogFormat(
                "Lycanthropy MagicRound(). Type={0}, HircineRing={1}, IsTransformed={2}, Massar={3}, Secunda={4}",
                infectionType,
                wearingHircineRing,
                isTransformed,
                DaggerfallUnity.Instance.WorldTime.Now.MassarLunarPhase,
                DaggerfallUnity.Instance.WorldTime.Now.SecundaLunarPhase);
        }

        public override bool GetCustomPaperDollBackgroundTexture(PlayerEntity playerEntity, out Texture2D textureOut)
        {
            const string werewolfBackground = "WOLF00I0.IMG";
            const string wereboarBackground = "BOAR00I0.IMG";

            // Do nothing if not transformed
            textureOut = null;
            if (!isTransformed)
                return false;

            // Get source texture based on lycanthropy type
            string filename;
            switch (infectionType)
            {
                case LycanthropyTypes.Werewolf:
                    filename = werewolfBackground;
                    break;
                case LycanthropyTypes.Wereboar:
                    filename = wereboarBackground;
                    break;
                default:
                    return false;
            }

            // Background is cut into sub-texture and cached on first call
            if (!backgroundTexture)
            {
                Texture2D texture = ImageReader.GetTexture(filename, 0, 0, false);
                backgroundTexture = ImageReader.GetSubTexture(texture, backgroundSubRect, backgroundFullSize);
            }

            textureOut = backgroundTexture;
            return true;
        }

        public override bool SetFPSWeapon(FPSWeapon target)
        {
            if (isTransformed)
            {
                target.WeaponType = WeaponTypes.Werecreature;
                target.MetalType = MetalTypes.None;
                target.DrawWeaponSound = SoundClips.None;
                target.SwingWeaponSound = SoundClips.SwingHighPitch;
                target.Reach = WeaponManager.defaultWeaponReach;
                return true;
            }

            return false;
        }

        public override void OnWeaponHitEnemy(PlayerEntity playerEntity, EnemyEntity enemyEntity)
        {
            const int chanceOfAttackSound = 10;
            const int chanceOfBarkSound = 20;

            // Do nothing if not transformed
            if (!isTransformed)
                return;

            // Lycanthrope characters emit both attack and bark sounds while attacking
            SoundClips customSound = SoundClips.None;
            if (infectionType == LycanthropyTypes.Werewolf)
            {
                if (Dice100.SuccessRoll(chanceOfAttackSound))
                    customSound = SoundClips.EnemyWerewolfAttack;
                else if (Dice100.SuccessRoll(chanceOfBarkSound))
                    customSound = SoundClips.EnemyWerewolfBark;
            }
            else if (infectionType == LycanthropyTypes.Wereboar)
            {
                if (Dice100.SuccessRoll(chanceOfAttackSound))
                    customSound = SoundClips.EnemyWereboarAttack;
                else if (Dice100.SuccessRoll(chanceOfBarkSound))
                    customSound = SoundClips.EnemyWereboarBark;
            }

            // Play sound through weapon
            FPSWeapon screenWeapon = GameManager.Instance.WeaponManager.ScreenWeapon;
            if (screenWeapon && customSound != SoundClips.None)
                screenWeapon.PlayAttackVoice(customSound);
        }

        public override bool GetSuppressInventory(out string suppressInventoryMessage)
        {
            if (isTransformed)
            {
                suppressInventoryMessage = TextManager.Instance.GetText(generalTextDatabase, "inventoryWhileShapechanged");
                return true;
            }
            else
            {
                suppressInventoryMessage = string.Empty;
                return false;
            }
        }

        public override bool GetSuppressTalk(out string suppressTalkMessage)
        {
            if (isTransformed)
            {
                suppressTalkMessage = TextManager.Instance.GetText(generalTextDatabase, "youGetNoResponse");
                return true;
            }
            else
            {
                suppressTalkMessage = string.Empty;
                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets lycanthrope need to kill sated from current point in time.
        /// </summary>
        public void UpdateSatiation()
        {
            lastKilledInnocent = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
        }

        public virtual void MorphSelf()
        {
            // TODO: Implement transformation

            // Simplistic implementation just to bootstrap various payloads
            if (!isTransformed)
            {
                isTransformed = true;

                // Unequip any items held in hands
                GameManager.Instance.PlayerEntity.ItemEquipTable.UnequipItem(EquipSlots.RightHand);
                GameManager.Instance.PlayerEntity.ItemEquipTable.UnequipItem(EquipSlots.LeftHand);

                // Set race name based on infection type
                if (infectionType == LycanthropyTypes.Werewolf)
                    compoundRace.Name = TextManager.Instance.GetText(racesTextDatabase, "werewolf");
                else if (infectionType == LycanthropyTypes.Wereboar)
                    compoundRace.Name = TextManager.Instance.GetText(racesTextDatabase, "wereboar");
                else
                    compoundRace.Name = GameManager.Instance.PlayerEntity.BirthRaceTemplate.Name;

                // Initialise move sound timer
                InitMoveSoundTimer();

                // TODO: Set last transform time for 24-hour cooldown
            }
            else
            {
                isTransformed = false;

                // Restore birth race name
                compoundRace.Name = GameManager.Instance.PlayerEntity.BirthRaceTemplate.Name;
            }
        }

        #endregion

        #region Private Methods

        void CreateCompoundRace()
        {
            // Clone birth race and assign custom settings
            // New compound races will retain almost everything from birth race
            compoundRace = GameManager.Instance.PlayerEntity.BirthRaceTemplate.Clone();

            // Set special lycanthropy flags
            compoundRace.ImmunityFlags |= DFCareer.EffectFlags.Disease;
        }

        RaceTemplate GetCompoundRace()
        {
            // Create compound race if one doesn't already exist
            if (compoundRace == null)
                CreateCompoundRace();

            return compoundRace;
        }

        void ApplyLycanthropeAdvantages()
        {
            // Set stat mods
            const int statModAmount = 40;
            SetStatMod(DFCareer.Stats.Strength, statModAmount);
            SetStatMod(DFCareer.Stats.Agility, statModAmount);
            SetStatMod(DFCareer.Stats.Endurance, statModAmount);
            SetStatMod(DFCareer.Stats.Speed, statModAmount);

            // Set skill mods
            const int skillModAmount = 30;
            SetSkillMod(DFCareer.Skills.Swimming, skillModAmount);
            SetSkillMod(DFCareer.Skills.Running, skillModAmount);
            SetSkillMod(DFCareer.Skills.Stealth, skillModAmount);
            SetSkillMod(DFCareer.Skills.CriticalStrike, skillModAmount);
            SetSkillMod(DFCareer.Skills.Climbing, skillModAmount);
            SetSkillMod(DFCareer.Skills.HandToHand, skillModAmount);
        }

        void InitMoveSoundTimer(float minTime = 4, float maxTime = 20)
        {
            moveSoundTimer = Random.Range(minTime, maxTime);
        }

        void PlayLycanthropeMoveSound()
        {
            // Get sound based on infection type
            SoundClips customSound = SoundClips.None;
            if (infectionType == LycanthropyTypes.Werewolf)
                customSound = SoundClips.EnemyWerewolfMove;
            else if (infectionType == LycanthropyTypes.Wereboar)
                customSound = SoundClips.EnemyWereboarMove;

            // Play sound through weapon
            FPSWeapon screenWeapon = GameManager.Instance.WeaponManager.ScreenWeapon;
            if (screenWeapon && customSound != SoundClips.None)
                screenWeapon.PlayAttackVoice(customSound);
        }

        void ForceTransformDuringFullMoon()
        {
            // Does not happen if Hircine's Ring equipped
            if (wearingHircineRing)
                return;

            // Player is forced into lycanthrope form every magic round for the whole duration of any full moon
            // In classic, player can switch back to humanoid form briefly (remainder of magic round) before being forced to shapechange again (next magic round)
            // Intentionally reproducing this handling here so player can shift back and forth to loot corpses, etc.
            // I'm not sure if this was intentional in classic or not, but it's not much fun to be shut out of the game for a
            // whole 24 hours every 15 days. I *think* this was intentional so lycanthrope players could at least struggle through.
            // Same goes for other weirdness like renting rooms or handing in quests. It's just not fun to shut out of game completely.
            // Ultimately the player has their own choice to do this or not. They can run free in the wilderness for 24 hours if they prefer.
            if (isFullMoon && !isTransformed)
            {
                string youDreamOfTheMoon = TextManager.Instance.GetText(generalTextDatabase, "youDreamOfTheMoon");
                DaggerfallUI.AddHUDText(youDreamOfTheMoon, 2);
                MorphSelf();
            }
        }

        bool IsWearingHircineRing()
        {
            DaggerfallUnityItem[] equipTable = GameManager.Instance.PlayerEntity.ItemEquipTable.EquipTable;
            if (equipTable == null || equipTable.Length == 0)
                return false;

            return IsHircineRingItem(equipTable[(int)EquipSlots.Ring0]) || IsHircineRingItem(equipTable[(int)EquipSlots.Ring1]);
        }

        bool IsHircineRingItem(DaggerfallUnityItem item)
        {
            return
                item != null &&
                item.IsArtifact &&
                item.ContainsEnchantment(EnchantmentTypes.SpecialArtifactEffect, (short)ArtifactsSubTypes.Hircine_Ring);
        }

        #endregion

        #region Serialization

        [fsObject("v1")]
        public struct CustomSaveData_v1
        {
            public RaceTemplate compoundRace;
            public LycanthropyTypes infectionType;
            public uint lastKilledInnocent;
            public bool hasStartedInitialLycanthropyQuest;
            public bool wearingHircineRing;
            public bool isTransformed;
        }

        public override object GetSaveData()
        {
            CustomSaveData_v1 data = new CustomSaveData_v1();
            data.compoundRace = compoundRace;
            data.infectionType = infectionType;
            data.lastKilledInnocent = lastKilledInnocent;
            data.hasStartedInitialLycanthropyQuest = hasStartedInitialLycanthropyQuest;
            data.wearingHircineRing = wearingHircineRing;
            data.isTransformed = isTransformed;

            return data;
        }

        public override void RestoreSaveData(object dataIn)
        {
            if (dataIn == null)
                return;

            CustomSaveData_v1 data = (CustomSaveData_v1)dataIn;
            compoundRace = data.compoundRace;
            infectionType = data.infectionType;
            lastKilledInnocent = data.lastKilledInnocent;
            hasStartedInitialLycanthropyQuest = data.hasStartedInitialLycanthropyQuest;
            wearingHircineRing = data.wearingHircineRing;
            isTransformed = data.isTransformed;
        }

        #endregion

        #region Console Commands
        #endregion
    }
}
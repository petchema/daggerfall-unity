// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System.Collections.Generic;
using DaggerfallConnect;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.Save;
using DaggerfallWorkshop.Game.Banking;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Player;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Utility;
using UnityEngine;
using System;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Guilds;

namespace DaggerfallWorkshop.Game.Entity
{
    /// <summary>
    /// Implements DaggerfallEntity with properties specific to a Player.
    /// </summary>
    public class PlayerEntity : DaggerfallEntity
    {
        #region Fields

        bool godMode = false;
        bool preventEnemySpawns = false;
        bool isResting = false;

        const int testPlayerLevel = 1;
        const string testPlayerName = "Nameless";

        protected RaceTemplate raceTemplate;
        protected int faceIndex;
        protected PlayerReflexes reflexes;
        protected ItemCollection wagonItems = new ItemCollection();
        protected ItemCollection otherItems = new ItemCollection();
        protected int goldPieces = 0;
        protected PersistentFactionData factionData = new PersistentFactionData();
        protected PersistentGlobalVars globalVars = new PersistentGlobalVars();

        protected short[] skillUses;
        protected uint skillsRaisedThisLevel1 = 0;
        protected uint skillsRaisedThisLevel2 = 0;
        protected uint timeOfLastSkillIncreaseCheck = 0;
        protected uint timeOfLastSkillTraining = 0;

        protected uint timeOfLastStealthCheck = 0;

        protected int startingLevelUpSkillSum = 0;
        protected int currentLevelUpSkillSum = 0;
        protected bool readyToLevelUp = false;

        protected short[] sGroupReputations = new short[5];

        protected int biographyResistDiseaseMod = 0;
        protected int biographyResistMagicMod = 0;
        protected int biographyAvoidHitMod = 0;
        protected int biographyResistPoisonMod = 0;
        protected int biographyFatigueMod = 0;
        protected int biographyReactionMod = 0;

        protected uint timeForThievesGuildLetter = 0;
        protected uint timeForDarkBrotherhoodLetter = 0;
        protected int thievesGuildRequirementTally = 0;
        protected int darkBrotherhoodRequirementTally = 0;
        private const int InviteSent = 100;

        protected uint timeToBecomeVampireOrWerebeast = 0;
        protected uint lastTimePlayerAteOrDrankAtTavern = 0;

        protected List<DaggerfallDisease> diseases = new List<DaggerfallDisease>();

        protected RegionDataRecord[] regionData = new RegionDataRecord[62];

        protected Crimes crimeCommitted = 0; // TODO: Save/load
        protected bool haveShownSurrenderToGuardsDialogue = false; // TODO: Save/load
        protected bool arrested = false;
        protected short halfOfLegalRepPlayerLostFromCrime = 0;

        private List<RoomRental_v1> rentedRooms = new List<RoomRental_v1>();

        // Fatigue loss per in-game minute
        public const int DefaultFatigueLoss = 11;
        public const int ClimbingFatigueLoss = 22;
        public const int RunningFatigueLoss = 88;
        public const int SwimmingFatigueLoss = 44;

        private float classicUpdateTimer = 0f;
        public const float ClassicUpdateInterval = 0.0625f; // Update every 1/16 of a second. An approximation of classic's update loop, which varies with framerate.
        private int breathUpdateTally = 0;

        private int JumpingFatigueLoss = 11;        // According to DF Chronicles and verified in classic
        private bool CheckedCurrentJump = false;

        PlayerMotor playerMotor = null;
        ClimbingMotor climbingMotor = null;
        protected uint lastGameMinutes = 0;         // Being tracked in order to perform updates based on changes in the current game minute
        private bool gameStarted = false;

        #endregion

        #region Properties

        public bool GodMode { get { return godMode; } set { godMode = value; } }
        public bool PreventEnemySpawns { get { return preventEnemySpawns; } set { preventEnemySpawns = value; } }
        public bool IsResting { get { return isResting; } set { isResting = value; } }
        public Races Race { get { return (Races)RaceTemplate.ID; } }
        public RaceTemplate RaceTemplate { get { return raceTemplate; } set { raceTemplate = value; } }
        public int FaceIndex { get { return faceIndex; } set { faceIndex = value; } }
        public PlayerReflexes Reflexes { get { return reflexes; } set { reflexes = value; } }
        public ItemCollection WagonItems { get { return wagonItems; } set { wagonItems.ReplaceAll(value); } }
        public ItemCollection OtherItems { get { return otherItems; } set { otherItems.ReplaceAll(value); } }
        public int GoldPieces { get { return goldPieces; } set { goldPieces = value; } }
        public PersistentFactionData FactionData { get { return factionData; } }
        public PersistentGlobalVars GlobalVars { get { return globalVars; } }
        public short[] SkillUses { get { return skillUses; } set { skillUses = value; } }
        public uint TimeOfLastSkillIncreaseCheck { get { return timeOfLastSkillIncreaseCheck; } set { timeOfLastSkillIncreaseCheck = value; } }
        public uint TimeOfLastSkillTraining { get { return timeOfLastSkillTraining; } set { timeOfLastSkillTraining = value; } }
        public uint TimeOfLastStealthCheck { get { return timeOfLastStealthCheck; } set { timeOfLastStealthCheck = value; } }
        public int StartingLevelUpSkillSum { get { return startingLevelUpSkillSum; } set { startingLevelUpSkillSum = value; } }
        public int CurrentLevelUpSkillSum {  get { return currentLevelUpSkillSum; } }
        public bool ReadyToLevelUp { get { return readyToLevelUp; } set { readyToLevelUp = value; } }
        public short[] SGroupReputations { get { return sGroupReputations; } set { sGroupReputations = value; } }
        public int BiographyResistDiseaseMod { get { return biographyResistDiseaseMod; } set { biographyResistDiseaseMod = value; } }
        public int BiographyResistMagicMod { get { return biographyResistMagicMod; } set { biographyResistMagicMod = value; } }
        public int BiographyAvoidHitMod { get { return biographyAvoidHitMod; } set { biographyAvoidHitMod = value; } }
        public int BiographyResistPoisonMod { get { return biographyResistPoisonMod; } set { biographyResistPoisonMod = value; } }
        public int BiographyFatigueMod { get { return biographyFatigueMod; } set { biographyFatigueMod = value; } }
        public int BiographyReactionMod { get { return biographyReactionMod; } set { biographyReactionMod = value; } }
        public uint TimeForThievesGuildLetter { get { return timeForThievesGuildLetter; } set { timeForThievesGuildLetter = value; } }
        public uint TimeForDarkBrotherhoodLetter { get { return timeForDarkBrotherhoodLetter; } set { timeForDarkBrotherhoodLetter = value; } }
        public int ThievesGuildRequirementTally { get { return thievesGuildRequirementTally; } set { thievesGuildRequirementTally = value; } }
        public int DarkBrotherhoodRequirementTally { get { return darkBrotherhoodRequirementTally; } set { darkBrotherhoodRequirementTally = value; } }
        public uint TimeToBecomeVampireOrWerebeast { get { return timeToBecomeVampireOrWerebeast; } set { timeToBecomeVampireOrWerebeast = value; } }
        public uint LastTimePlayerAteOrDrankAtTavern { get { return lastTimePlayerAteOrDrankAtTavern; } set { lastTimePlayerAteOrDrankAtTavern = value; } }
        public float CarriedWeight { get { return Items.GetWeight() + ((float)goldPieces / DaggerfallBankManager.gold1kg); } }
        public float WagonWeight { get { return WagonItems.GetWeight(); } }
        public RegionDataRecord[] RegionData { get { return regionData; } set { regionData = value; } }
        public uint LastGameMinutes { get { return lastGameMinutes; } set { lastGameMinutes = value; } }
        public List<RoomRental_v1> RentedRooms { get { return rentedRooms; } set { rentedRooms = value; } }
        public List<DaggerfallDisease> Diseases { get { return diseases; } set { diseases = value; } }
        public Crimes CrimeCommitted { get { return crimeCommitted; } set { crimeCommitted = value; } }
        public bool HaveShownSurrenderToGuardsDialogue { get { return haveShownSurrenderToGuardsDialogue; } set { haveShownSurrenderToGuardsDialogue = value; } }
        public bool Arrested { get { return arrested; } set { arrested = value; } }

        #endregion

        #region Constructors

        public PlayerEntity()
            :base()
        {
            StartGameBehaviour.OnNewGame += StartGameBehaviour_OnNewGame;
            OnExhausted += PlayerEntity_OnExhausted;
        }

        #endregion

        #region Public Methods

        public RoomRental_v1 GetRentedRoom(int mapId, int buildingKey)
        {
            foreach (RoomRental_v1 room in rentedRooms)
                if (room.mapID == mapId && room.buildingKey == buildingKey)
                    return room;

            return null;
        }

        public List<RoomRental_v1> GetRentedRooms(int mapId)
        {
            return rentedRooms.FindAll(r => r.mapID == mapId);
        }

        public void RemoveExpiredRentedRooms()
        {
            rentedRooms.RemoveAll(r => {
                if (GetRemainingHours(r) < 1) {
                    SaveLoadManager.StateManager.RemovePermanentScene(DaggerfallInterior.GetSceneName(r.mapID, r.buildingKey));
                    return true;
                } else
                    return false;
            });
        }

        public static int GetRemainingHours(RoomRental_v1 room)
        {
            if (room == null)
                return -1;

            double remainingSecs = (double) (room.expiryTime - DaggerfallUnity.Instance.WorldTime.Now.ToSeconds());
            return (int) Math.Ceiling((remainingSecs / DaggerfallDateTime.SecondsPerHour));
        }

        public override void Update(DaggerfallEntityBehaviour sender)
        {
            if (SaveLoadManager.Instance.LoadInProgress)
                return;

            if (CurrentHealth <= 0)
                return;

            bool classicUpdate = false;

            if (classicUpdateTimer < ClassicUpdateInterval)
                classicUpdateTimer += Time.deltaTime;
            else
            {
                classicUpdateTimer = 0;
                classicUpdate = true;
            }

            if (playerMotor == null)
                playerMotor = GameManager.Instance.PlayerMotor;
            if (climbingMotor == null)
                climbingMotor = GameManager.Instance.ClimbingMotor;

            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            // Wait until game has started and the game time has been set.
            // If the game time is taken before then "30" is returned, which causes an initial player fatigue loss
            // after loading or starting a game with a non-30 minute.
            if (!gameStarted && !GameManager.Instance.StateManager.GameInProgress)
                return;
            else if (!gameStarted)
                gameStarted = true;
            if (playerMotor != null)
            {
                // Every game minute, apply fatigue loss to the player
                if (lastGameMinutes != gameMinutes)
                {
                    int amount = DefaultFatigueLoss;
                    if (climbingMotor != null && climbingMotor.IsClimbing)
                        amount = ClimbingFatigueLoss;
                    else if (playerMotor.IsRunning)
                        amount = RunningFatigueLoss;
                    else if (GameManager.Instance.PlayerEnterExit.IsPlayerSwimming)
                    {
                        if (Race != Races.Argonian && UnityEngine.Random.Range(1, 100 + 1) > Skills.GetLiveSkillValue(DFCareer.Skills.Swimming))
                            amount = SwimmingFatigueLoss;
                        TallySkill(DFCareer.Skills.Swimming, 1);
                    }

                    DecreaseFatigue(amount);
                }

                // Handle events that are called by classic's update loop
                if (classicUpdate)
                {
                    // Tally running skill
                    if (playerMotor.IsRunning && !playerMotor.IsRiding)
                        TallySkill(DFCareer.Skills.Running, 1);

                    // Handle breath when underwater
                    if (GameManager.Instance.PlayerEnterExit.IsPlayerSubmerged)
                    {
                        if (currentBreath == 0)
                        {
                            currentBreath = GameManager.Instance.GuildManager.DeepBreath(MaxBreath);
                        }
                        if (breathUpdateTally > 18)
                        {
                            --currentBreath;
                            if (Race == Races.Argonian && (UnityEngine.Random.Range(0, 2) == 1))
                                ++currentBreath;
                            breathUpdateTally = 0;
                        }
                        else
                            ++breathUpdateTally;

                        if (currentBreath <= 0)
                            SetHealth(0);
                    }
                    else
                        currentBreath = 0;
                }

                // Reduce fatigue when jumping and tally jumping skill
                if (!CheckedCurrentJump && playerMotor.IsJumping)
                {
                    DecreaseFatigue(JumpingFatigueLoss);
                    TallySkill(DFCareer.Skills.Jumping, 1);
                    CheckedCurrentJump = true;
                }

                // Reset jump fatigue check when grounded
                if (CheckedCurrentJump && !playerMotor.IsJumping)
                {
                    CheckedCurrentJump = false;
                }
            }

            // Adjust regional prices and update climate weathers and diseases whenever the date has changed.
            uint lastDay = lastGameMinutes / 1440;
            uint currentDay = gameMinutes / 1440;
            int daysPast = (int)(currentDay - lastDay);

            if (daysPast > 0)
            {
                FormulaHelper.UpdateRegionalPrices(ref regionData, daysPast);
                GameManager.Instance.WeatherManager.SetClimateWeathers();
                GameManager.Instance.WeatherManager.UpdateWeatherFromClimateArray = true;
                RemoveExpiredRentedRooms();

                if (Diseases.Count != 0)
                {
                    for (int i = daysPast; i > 0; i--)
                    {
                        foreach (DaggerfallDisease disease in Diseases)
                            disease.ApplyDiseaseEffects(this);
                    }
                }
            }

            // Normalize legal reputation and update faction power and regional conditions every certain number of days
            uint minutesPassed = gameMinutes - lastGameMinutes;
            for (int i = 0; i < minutesPassed; ++i)
            {
                // Normalize legal reputations towards 0
                if (((i + lastGameMinutes) % 161280) == 0) // 112 days
                    NormalizeReputations();

                // Update faction relationships, faction power levels and regional conditions (in progress)
                if (((i + lastGameMinutes) % 40320) == 0) // 28 days
                    RegionPowerAndConditionsUpdate();

                // TODO: Get vampire/werecreature quest
                //if (((i + lastGameMinutes) % 120960) == 0) // 84 days
                //    GetVampireOrWerecreatureQuest();
            }

            // TODO: Right now enemy spawns are only prevented when time has been raised for
            // fast travel. They should later be prevented when time has been raised for
            // 1) Turning into vampire 2) Serving prison sentence
            // Classic also prevents enemy spawns during loitering,
            // but this seems counterintuitive so it's not implemented in DF Unity for now
            if (!preventEnemySpawns)
            {
                for (uint l = 0; l < (gameMinutes - lastGameMinutes); ++l)
                {
                    // Catch up time and break if something spawns
                    if (IntermittentEnemySpawn(l + lastGameMinutes + 1))
                        break;
                }
            }

            lastGameMinutes = gameMinutes;

            // Allow enemy spawns again if they have been disabled
            if (preventEnemySpawns)
                preventEnemySpawns = false;

            // Reset isResting flag. If still resting DaggerfallRestWindow will set it to true again for the next update.
            if (isResting)
                isResting = false;

            HandleStartingCrimeGuildQuests();

            // Reset surrender to guards dialogue if no guards are nearby
            if (haveShownSurrenderToGuardsDialogue == true)
            {
                if (GameManager.Instance.HowManyEnemiesOfType(MobileTypes.Knight_CityWatch, true) == 0)
                    haveShownSurrenderToGuardsDialogue = false;
            }
        }

        public bool IntermittentEnemySpawn(uint Minutes)
        {
            // Define minimum distance from player based on spawn locations
            const int minDungeonDistance = 8;
            const int minLocationDistance = 10;
            const int minWildernessDistance = 10;

            //TODO: if (InOutsideWater)
            //          return;

            // Do not allow spawns if not enough time has passed or spawns are suppressed for any reason
            // Note - should not need the preventEnemySpawns check here as IntermittentEnemySpawn() is only called by Update() !preventEnemySpawns
            bool timeForSpawn = ((Minutes / 12) % 12) == 0;
            if (!timeForSpawn || preventEnemySpawns)
                return false;

            // Spawns when player is outside
            if (!GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                uint timeOfDay = Minutes % 1440; // 1440 minutes in a day
                if (GameManager.Instance.PlayerGPS.IsPlayerInLocationRect)
                {
                    if (timeOfDay < 360 || timeOfDay > 1080)
                    {
                        // In a location area at night
                        if (UnityEngine.Random.Range(0, 24) == 0)
                        {
                            GameObjectHelper.CreateFoeSpawner(true, RandomEncounters.ChooseRandomEnemy(false), 1, minLocationDistance);
                            return true;
                        }
                    }
                }
                else
                {
                    if (timeOfDay >= 360 && timeOfDay <= 1080)
                    {
                        // Wilderness during day
                        if (UnityEngine.Random.Range(0, 36) != 0)
                            return false;
                    }
                    else
                    {
                        // Wilderness at night
                        if (UnityEngine.Random.Range(0, 24) != 0)
                            return false;
                    }

                    GameObjectHelper.CreateFoeSpawner(true, RandomEncounters.ChooseRandomEnemy(false), 1, minWildernessDistance);
                    return true;
                }
            }

            // Spawns when player is inside
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside)
            {
                // Spawns when player is inside a dungeon
                if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
                {
                    if (isResting)
                    {
                        if (UnityEngine.Random.Range(0, 43) == 0) // Normally (0, 36) - making spawns ~20% less for rested dungeons
                        {
                            // TODO: Not sure how enemy type is chosen here.
                            GameObjectHelper.CreateFoeSpawner(false, RandomEncounters.ChooseRandomEnemy(false), 1, minDungeonDistance);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void SpawnCityGuards(bool forceSpawn)
        {
            // Don't spawn if more than 10 guards are already in the area
            if (GameManager.Instance.HowManyEnemiesOfType(MobileTypes.Knight_CityWatch) > 10)
                return;

            if (forceSpawn)
            {
                // TODO: First try to spawn guards from nearby townspeople. If townsperson is a guard spawn, or if is a non-guard 1/3 chance to spawn
                // if out of player view.
                // If none spawned, proceed with below
                int randomNumber = UnityEngine.Random.Range(2, 5 + 1);
                GameObjectHelper.CreateFoeSpawner(true, MobileTypes.Knight_CityWatch, randomNumber, (int)(1032 * MeshReader.GlobalScale), (int)(3096 * MeshReader.GlobalScale));
            }
            // TODO: If !forceSpawn, check for LOS from nearby guard townspeople and spawn from them if they see player.
            // Otherwise, check for LOS from non-guard townspeople. If they see player, random countdown is set, after which guards will spawn.
        }

        /// <summary>
        /// Resets entity to initial state.
        /// </summary>
        public void Reset()
        {
            equipTable.Clear();
            items.Clear();
            wagonItems.Clear();
            otherItems.Clear();
            factionData.Reset();
            globalVars.Reset();
            SetEntityDefaults();
            startingLevelUpSkillSum = 0;
            currentLevelUpSkillSum = 0;
            goldPieces = 0;
            timeOfLastSkillIncreaseCheck = 0;
            timeOfLastSkillTraining = 0;
            rentedRooms.Clear();
            diseases = new List<DaggerfallDisease>();
            if (skillUses != null)
                System.Array.Clear(skillUses, 0, skillUses.Length);
         }

        /// <summary>
        /// Assigns player entity settings from a character document.
        /// </summary>
        public void AssignCharacter(CharacterDocument character, int level = 1, int maxHealth = 0, bool fillVitals = true)
        {
            if (character == null)
            {
                SetEntityDefaults();
                return;
            }

            this.level = level;
            this.gender = character.gender;
            this.raceTemplate = character.raceTemplate;
            this.career = character.career;
            this.name = character.name;
            this.faceIndex = character.faceIndex;
            this.stats = character.workingStats;
            this.skills = character.workingSkills;
            this.reflexes = character.reflexes;
            this.maxHealth = character.maxHealth;
            this.currentHealth = character.currentHealth;
            this.currentMagicka = character.currentSpellPoints;
            this.sGroupReputations[0] = character.reputationCommoners;
            this.sGroupReputations[1] = character.reputationMerchants;
            this.sGroupReputations[2] = character.reputationNobility;
            this.sGroupReputations[3] = character.reputationScholars;
            this.sGroupReputations[4] = character.reputationUnderworld;
            this.currentFatigue = character.currentFatigue;
            this.skillUses = character.skillUses;
            this.skillsRaisedThisLevel1 = character.skillsRaisedThisLevel1;
            this.skillsRaisedThisLevel2 = character.skillsRaisedThisLevel2;
            this.startingLevelUpSkillSum = character.startingLevelUpSkillSum;
            this.minMetalToHit = (WeaponMaterialTypes)character.minMetalToHit;
            this.armorValues = character.armorValues;
            this.timeToBecomeVampireOrWerebeast = character.timeToBecomeVampireOrWerebeast;
            this.lastTimePlayerAteOrDrankAtTavern = character.lastTimePlayerAteOrDrankAtTavern;
            this.timeOfLastSkillTraining = character.lastTimePlayerBoughtTraining;
            this.timeForThievesGuildLetter = character.timeForThievesGuildLetter;
            this.timeForDarkBrotherhoodLetter = character.timeForDarkBrotherhoodLetter;
            this.darkBrotherhoodRequirementTally = character.darkBrotherhoodRequirementTally;
            this.thievesGuildRequirementTally = character.thievesGuildRequirementTally;

            SetCurrentLevelUpSkillSum();

            if (startingLevelUpSkillSum <= 0) // new character
            {
                startingLevelUpSkillSum = currentLevelUpSkillSum;
            }

            if (maxHealth <= 0)
                this.maxHealth = FormulaHelper.RollMaxHealth(level, career.HitPointsPerLevel);
            else
                this.maxHealth = maxHealth;

            if (fillVitals)
                FillVitalSigns();

            timeOfLastSkillIncreaseCheck = DaggerfallUnity.Instance.WorldTime.Now.ToClassicDaggerfallTime();

            DaggerfallUnity.LogMessage("Assigned character " + this.name, true);
        }

        /// <summary>
        /// Assigns character items from classic save tree.
        /// </summary>
        public void AssignItems(SaveTree saveTree)
        {
            // Find character record, should always be a singleton
            CharacterRecord characterRecord = (CharacterRecord)saveTree.FindRecord(RecordTypes.Character);
            if (characterRecord == null)
                return;

            // Find all character-owned items
            List<SaveTreeBaseRecord> itemRecords = saveTree.FindRecords(RecordTypes.Item, characterRecord);

            // Filter for container-based inventory items
            List<SaveTreeBaseRecord> filteredRecords = saveTree.FilterRecordsByParentType(itemRecords, RecordTypes.Container);

            // Add interim Daggerfall Unity items
            foreach (var record in filteredRecords)
            {
                // Get container parent
                ContainerRecord containerRecord = (ContainerRecord)record.Parent;

                // Some (most likely hacked) classic items have 0 or 65535 in image data bitfield
                // Discard these items as they will likely have other bad attributes such as an impossible weight
                // The goal here is just to prevent game from crashing due to bad item data
                if ((record as ItemRecord).ParsedData.image1 == 0 || (record as ItemRecord).ParsedData.image1 == 0xffff)
                    continue;

                // Create item, grabbing trapped soul if needed
                DaggerfallUnityItem newItem = new DaggerfallUnityItem((ItemRecord)record);
                if (newItem.ItemGroup == ItemGroups.MiscItems && newItem.GroupIndex == 1)
                {
                    if (record.Children.Count > 0)
                    {
                        TrappedSoulRecord soulRecord = (TrappedSoulRecord) record.Children[0];
                        newItem.TrappedSoulType = (MobileTypes) soulRecord.RecordRoot.SpriteIndex;
                    }
                    else
                        newItem.TrappedSoulType = MobileTypes.None;
                }
                // Add to local inventory or wagon
                if (containerRecord.IsWagon)
                    wagonItems.AddItem(newItem);
                else
                    items.AddItem(newItem);

                // Equip to player if equipped in save
                for (int i = 0; i < characterRecord.ParsedData.equippedItems.Length; i++)
                {
                    if (characterRecord.ParsedData.equippedItems[i] == record.RecordRoot.RecordID)
                        equipTable.EquipItem(newItem, true, false);
                }
            }
        }

        /// <summary>
        /// Assigns diseases and poisons to player from classic save tree.
        /// </summary>
        public void AssignDiseasesAndPoisons(SaveTree saveTree)
        {
            // Find character record, should always be a singleton
            CharacterRecord characterRecord = (CharacterRecord)saveTree.FindRecord(RecordTypes.Character);
            if (characterRecord == null)
                return;

            // Find all diseases and poisons
            List<SaveTreeBaseRecord> diseaseAndPoisonRecords = saveTree.FindRecords(RecordTypes.DiseaseOrPoison, characterRecord);

            diseases.Clear();

            // Add Daggerfall Unity diseases and poisons
            foreach (var record in diseaseAndPoisonRecords)
            {
                if ((record as DiseaseOrPoisonRecord).ParsedData.ID < 100) // is a disease
                {
                    DaggerfallDisease newDisease = new DaggerfallDisease((DiseaseOrPoisonRecord)record);
                    diseases.Add(newDisease);
                }
                // TODO: Poisons
            }
        }

        /// <summary>
        /// Assigns default entity settings.
        /// </summary>
        public override void SetEntityDefaults()
        {
            // TODO: Add some bonus points to stats
            career = DaggerfallEntity.GetClassCareerTemplate(ClassCareers.Mage);
            if (career != null)
            {
                raceTemplate = CharacterDocument.GetRaceTemplate(Races.Breton);
                faceIndex = 0;
                reflexes = PlayerReflexes.Average;
                gender = Genders.Male;
                stats.SetPermanentFromCareer(career);
                level = testPlayerLevel;
                maxHealth = FormulaHelper.RollMaxHealth(level, career.HitPointsPerLevel);
                name = testPlayerName;
                stats.SetDefaults();
                skills.SetDefaults();
                FillVitalSigns();
                for (int i = 0; i < ArmorValues.Length; i++)
                {
                    ArmorValues[i] = 100;
                }
            }
        }

        /// <summary>
        /// Sets new health value.
        /// Override for godmode support.
        /// </summary>
        public override int SetHealth(int amount)
        {
            if (godMode)
                return currentHealth = MaxHealth;

            currentHealth = Mathf.Clamp(amount, 0, MaxHealth);
            if (currentHealth <= 0)
            {
                // Players can have avoid death benefit from guild memberships
                if (GameManager.Instance.GuildManager.AvoidDeath())
                    return currentHealth = 1;
                else
                    RaiseOnDeathEvent();
            }
            return currentHealth;
        }

        /// <summary>
        /// Sets new fatigue value.
        /// Override for godmode support.
        /// </summary>
        public override int SetFatigue(int amount)
        {
            if (godMode)
                return currentFatigue = MaxFatigue;
            else
                return base.SetFatigue(amount);
        }

        /// <summary>
        /// Sets new magicka value.
        /// Override for godmode support.
        /// </summary>
        public override int SetMagicka(int amount)
        {
            if (godMode)
                return currentMagicka = MaxMagicka;
            else
                return base.SetMagicka(amount);
        }

        /// <summary>
        /// Tally skill usage.
        /// </summary>
        public override void TallySkill(DFCareer.Skills skill, short amount)
        {
            int skillId = (int)skill;

            try
            {
                skillUses[skillId] += amount;
                if (skillUses[skillId] > 20000)
                    skillUses[skillId] = 20000;
                else if (skillUses[skillId] < 0)
                {
                    skillUses[skillId] = 0;
                }
            }
            catch(Exception ex)
            {
                string error = string.Format("Caught exception {0} with skillId {1}.", ex.Message, skillId);

                if (skillUses == null || skillUses.Length == 0)
                    error += " skillUses is null or empty.";

                Debug.Log(error);
            }
        }

        /// <summary>
        /// Tally thefts/break-ins and murders for starting Thieves Guild and Dark Brotherhood quests
        /// </summary>
        public void TallyCrimeGuildRequirements(bool thievingCrime, byte amount)
        {
            if (thievingCrime)
            {
                if (timeForThievesGuildLetter == 0)
                {
                    if (thievesGuildRequirementTally != InviteSent) // Tally is set to 100 when the thieves guild quest line has already started
                    {
                        thievesGuildRequirementTally += amount;
                        if (thievesGuildRequirementTally >= 10)
                        {
                            uint currentMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
                            timeForThievesGuildLetter = currentMinutes + 4320; // 3 days
                        }
                    }
                }
            }
            else // murder
            {
                if (timeForDarkBrotherhoodLetter == 0)
                {
                    if (darkBrotherhoodRequirementTally != InviteSent) // Tally is set to 100 when the thieves guild quest line has already started
                    {
                        darkBrotherhoodRequirementTally += amount;
                        if (darkBrotherhoodRequirementTally >= 15)
                        {
                            uint currentMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
                            timeForDarkBrotherhoodLetter = currentMinutes + 4320; // 3 days
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the amount of gold carried by the player. (gold pieces + letters of credit)
        /// </summary>
        public int GetGoldAmount()
        {
            return goldPieces + items.GetCreditAmount();
        }

        /// <summary>
        /// Deducts the amount of gold specified from the player. (check GoldAmount first)
        /// Gold pieces first if enough, then letters of credit, then gold pieces.
        /// </summary>
        /// <param name="amount">Amount to deduct</param>
        /// <returns>Amount remaining to be paid if not enough funds.</returns>
        public int DeductGoldAmount(int amount)
        {
            if (amount <= goldPieces) {
                goldPieces -= amount;
            } else {
                while (amount > 0)
                {
                    DaggerfallUnityItem loc = items.GetItem(ItemGroups.MiscItems, (int)MiscItems.Letter_of_credit);
                    if (loc == null) {
                        break;
                    } else if (amount <= loc.value) {
                        loc.value -= amount;
                        amount = 0;
                    } else {
                        amount -= loc.value;
                        items.RemoveItem(loc);
                    }
                }

                if (amount > 0) {
                    if (amount <= goldPieces) {
                        goldPieces -= amount;
                    } else {     // Underpaid.
                        amount -= goldPieces;
                        goldPieces = 0;
                        return amount;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Raise skills if conditions are met.
        /// </summary>
        public void RaiseSkills()
        {
            const int youAreNowAMasterOfTextID = 4020;

            DaggerfallDateTime now = DaggerfallUnity.Instance.WorldTime.Now;
            if ((now.ToClassicDaggerfallTime() - timeOfLastSkillIncreaseCheck) <= 360)
                return;

            timeOfLastSkillIncreaseCheck = now.ToClassicDaggerfallTime();

            for (short i = 0; i < skillUses.Length; i++)
            {
                int skillAdvancementMultiplier = DaggerfallSkills.GetAdvancementMultiplier((DFCareer.Skills)i);
                float careerAdvancementMultiplier = Career.AdvancementMultiplier;
                int usesNeededForAdvancement = FormulaHelper.CalculateSkillUsesForAdvancement(skills.GetPermanentSkillValue(i), skillAdvancementMultiplier, careerAdvancementMultiplier, level);
                if (skillUses[i] >= usesNeededForAdvancement)
                {
                    skillUses[i] = 0;

                    if (skills.GetPermanentSkillValue(i) < 100 && (skills.GetPermanentSkillValue(i) < 95 || !AlreadyMasteredASkill()))
                    {
                        skills.SetPermanentSkillValue(i, (short)(skills.GetPermanentSkillValue(i) + 1));
                        SetCurrentLevelUpSkillSum();
                        DaggerfallUI.Instance.PopupMessage(HardStrings.skillImprove.Replace("%s", DaggerfallUnity.Instance.TextProvider.GetSkillName((DFCareer.Skills)i)));
                        if (skills.GetPermanentSkillValue(i) == 100)
                        {
                            List<DFCareer.Skills> primarySkills = GetPrimarySkills();
                            if (primarySkills.Contains((DFCareer.Skills)i))
                            {
                                ITextProvider textProvider = DaggerfallUnity.Instance.TextProvider;
                                TextFile.Token[] tokens;
                                tokens = textProvider.GetRSCTokens(youAreNowAMasterOfTextID);
                                if (tokens != null && tokens.Length > 0)
                                {
                                    DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);
                                    messageBox.SetTextTokens(tokens);
                                    messageBox.ClickAnywhereToClose = true;
                                    messageBox.ParentPanel.BackgroundColor = Color.clear;
                                    messageBox.Show();
                                }
                                DaggerfallUI.Instance.PlayOneShot(SoundClips.ArenaFanfareLevelUp);
                            }
                        }
                    }
                }
            }

            if (CheckForLevelUp())
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenCharacterSheetWindow);
        }

        /// <summary>
        /// Gets whether the player has already become master of a skill.
        /// </summary>
        public bool AlreadyMasteredASkill()
        {
            bool mastered = false;
            List<DFCareer.Skills> primarySkills = GetPrimarySkills();
            foreach (DFCareer.Skills skill in primarySkills)
            {
                if (skills.GetPermanentSkillValue(skill) == 100)
                {
                    mastered = true;
                    break;
                }
            }

            return mastered;
        }

        /// <summary>
        /// Calculate current sum of skills used for determining player level.
        /// </summary>
        public void SetCurrentLevelUpSkillSum()
        {
            short sum = 0;
            short lowestMajorSkillValue = 0;
            short highestMinorSkillValue = 0;
            List<DaggerfallConnect.DFCareer.Skills> primarySkills = GetPrimarySkills();
            List<DaggerfallConnect.DFCareer.Skills> majorSkills = GetMajorSkills();
            List<DaggerfallConnect.DFCareer.Skills> minorSkills = GetMinorSkills();
            for (int i = 0; i < primarySkills.Count; i++)
            {
                sum += skills.GetPermanentSkillValue(primarySkills[i]);
            }

            for (int i = 0; i < majorSkills.Count; i++)
            {
                short value = skills.GetPermanentSkillValue(majorSkills[i]);
                sum += value;
                if (i == 0)
                    lowestMajorSkillValue = value;
                else if (value < lowestMajorSkillValue)
                    lowestMajorSkillValue = value;
            }

            sum -= lowestMajorSkillValue;

            for (int i = 0; i < minorSkills.Count; i++)
            {
                short value = skills.GetPermanentSkillValue(minorSkills[i]);
                if (i == 0)
                    highestMinorSkillValue = value;
                else if (value > highestMinorSkillValue)
                    highestMinorSkillValue = value;
            }

            sum += highestMinorSkillValue;
            currentLevelUpSkillSum = sum;
        }

        /// <summary>
        /// Estimate the starting sum of skills used for determining player level.
        /// </summary>
        public void EstimateStartingLevelUpSkillSum()
        {
            float estimatedLevel = level + 0.5f; // Assume the player is halfway through advancing a level
            int estimatedSum = (int)((estimatedLevel * 15) - (28 + currentLevelUpSkillSum)) * -1;
            if ((currentLevelUpSkillSum > 0) && (startingLevelUpSkillSum > currentLevelUpSkillSum))
                startingLevelUpSkillSum = currentLevelUpSkillSum;
            else
                startingLevelUpSkillSum = estimatedSum;
        }

        /// <summary>
        /// Calculate level and check for level up.
        /// </summary>
        public bool CheckForLevelUp()
        {
            int calculatedLevel = FormulaHelper.CalculatePlayerLevel(startingLevelUpSkillSum, currentLevelUpSkillSum);
            bool levelUp = (level < calculatedLevel);
            if (levelUp)
                readyToLevelUp = true;

            return levelUp;
        }

        void HandleStartingCrimeGuildQuests()
        {
            if (thievesGuildRequirementTally != InviteSent
                && timeForThievesGuildLetter > 0
                && timeForThievesGuildLetter < DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime()
                && !GameManager.Instance.PlayerGPS.GetComponent<PlayerEnterExit>().IsPlayerInside)
            {
                thievesGuildRequirementTally = InviteSent;
                timeForThievesGuildLetter = 0;
                Questing.QuestMachine.Instance.InstantiateQuest(ThievesGuild.InitiationQuestName, ThievesGuild.FactionId);
            }
            if (darkBrotherhoodRequirementTally != InviteSent
                && timeForDarkBrotherhoodLetter > 0
                && timeForDarkBrotherhoodLetter < DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime()
                && !GameManager.Instance.PlayerGPS.GetComponent<PlayerEnterExit>().IsPlayerInside)
            {
                darkBrotherhoodRequirementTally = InviteSent;
                timeForDarkBrotherhoodLetter = 0;
                Questing.QuestMachine.Instance.InstantiateQuest(DarkBrotherhood.InitiationQuestName, DarkBrotherhood.FactionId);
            }
        }

        /// <summary>
        /// Releases a quest item carried by player so it can be assigned back again by quest script.
        /// This ensures item is properly unequipped and optionally makes permanent.
        /// </summary>
        /// <param name="item">Item to release.</param>
        /// <param name="makePermanent">True to make item permanent.</param>
        public void ReleaseQuestItemForReoffer(DaggerfallUnityItem item, bool makePermanent = false)
        {
            if (item == null)
                return;

            // Unequip item if player is wearing it
            if (GameManager.Instance.PlayerEntity.ItemEquipTable.UnequipItem(item))
            {
                // If item was actually unequipped then update armour values
                GameManager.Instance.PlayerEntity.UpdateEquippedArmorValues(item, false);
            }

            // Remove quest from inventory so it can be offered back to player
            GameManager.Instance.PlayerEntity.Items.RemoveItem(item);

            // Optionally make permanent
            if (makePermanent)
                item.MakePermanent();
        }

        #endregion

        #region RegionData
        public struct RegionDataRecord
        {
            public byte[] Values;
            public bool[] Flags;
            public bool[] Flags2;
            public short LegalRep;
            public byte PrecipitationOverride; // Never set by classic. In classic if this is non-0 the screen precipitation effect for classicWeatherTypes[PrecipitationOverride - 1] is forced.
            public byte SeverePunishmentFlags; // 1 = player was banished from region, 2 = player was sent to dungeon for execution by region
            public ushort IDOfPersecutedTemple;
            public ushort PriceAdjustment;
        }

        public enum RegionDataFlags
        {
            Condition0 = 0,
            Condition1 = 1,
            Condition2 = 2,
            Condition3 = 3,
            PlagueBeginning = 4,
            PlagueOngoing = 5,
            PlagueEnding = 6,
            FamineBeginning = 7,
            FamineOngoing = 8,
            FamineEnding = 9,
            WitchBurnings = 10,
            CrimeWave = 11,
            Condition12 = 12,
            Condition13 = 13,
            Condition14 = 14,
            Condition15 = 15,
            Condition16 = 16,
            Condition17 = 17,
            PersecutedTemple = 18,
            PricesHigh = 19,
            PricesLow = 20,
            Condition21 = 21,
            Condition22 = 22,
            Condition23 = 23,
            Condition24 = 24,
            Condition25 = 25,
            Condition26 = 26,
            Condition27 = 27,
            Condition28 = 28,
            Condition29 = 29,
        }

        /// <summary>
        /// Update regional power and conditions. Called every certain number of game days.
        /// </summary>
        public void RegionPowerAndConditionsUpdate()
        {
            int[] TemplesAssociatedWithRegions =    { 106, 82, 0, 0, 0, 98, 0, 0, 0, 92, 0, 106, 0, 0, 0, 84, 36, 8, 84, 88, 82, 88, 98, 92, 0, 0, 82, 0,
                                                        0, 0, 0, 0, 88, 94, 36, 94, 106, 84, 106, 106, 88, 98, 82, 98, 84, 94, 36, 88, 94, 36, 98, 84, 106,
                                                       88, 106, 88, 92, 84, 98, 88, 82, 94};

            foreach (FactionFile.FactionData item in FactionData.FactionDict.Values)
            {
                /*
                if (item.type == (int)FactionFile.FactionTypes.Province ||
                    item.type == (int)FactionFile.FactionTypes.Group ||
                    item.type == (int)FactionFile.FactionTypes.Subgroup)
                {
                    //TODO: Faction power changes
                }*/

                // Handle famine condition
                if (item.region != -1  && item.type == (int)FactionFile.FactionTypes.Province)
                {
                    int alliesPower = 0;
                    FactionFile.FactionData ally;
                    if (item.ally1 != 0)
                    {
                        FactionData.GetFactionData(item.ally1, out ally);
                        alliesPower += ally.power;
                    }
                    if (item.ally2 != 0)
                    {
                        FactionData.GetFactionData(item.ally1, out ally);
                        alliesPower += ally.power;
                    }
                    if (item.ally3 != 0)
                    {
                        FactionData.GetFactionData(item.ally1, out ally);
                        alliesPower += ally.power;
                    }

                    int alliesPowerMod = alliesPower / 10;

                    // Famine
                    if (regionData[item.region - 1].Flags[(int)RegionDataFlags.FamineEnding] == true)
                        TurnOffConditionFlag(item.region - 1, RegionDataFlags.FamineEnding);
                    else if (regionData[item.region - 1].Flags[(int)RegionDataFlags.FamineOngoing] == true)
                    {
                        if (UnityEngine.Random.Range(0, 100 + 1) < item.randomPowerBonus / 5 + alliesPowerMod + item.power / 5)
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.FamineEnding);
                    }
                    else if (regionData[item.region - 1].Flags[(int)RegionDataFlags.FamineBeginning] == true)
                        TurnOnConditionFlag(item.region - 1, RegionDataFlags.FamineOngoing);
                    else if (UnityEngine.Random.Range(1, 100 + 1) <= 2)
                    {
                        if (UnityEngine.Random.Range(0, 100 + 1) > item.randomPowerBonus + alliesPowerMod)
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.FamineBeginning);
                    }

                    // Plague
                    FactionFile.FactionData temple;
                    FactionData.GetFactionData(TemplesAssociatedWithRegions[item.region - 1], out temple);

                    if (regionData[item.region - 1].Flags[(int)RegionDataFlags.PlagueEnding] == true)
                        TurnOffConditionFlag(item.region - 1, RegionDataFlags.PlagueEnding);
                    else if (regionData[item.region - 1].Flags[(int)RegionDataFlags.PlagueOngoing] == true)
                    {
                        //if (temple.id != 0)
                            //--temple.power;
                        //--item.power;
                        if (UnityEngine.Random.Range(0, 100 + 1) < item.power / 5 + item.randomPowerBonus / 5 + alliesPowerMod)
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.PlagueEnding);
                    }
                    else if (regionData[item.region - 1].Flags[(int)RegionDataFlags.PlagueBeginning] == true)
                    {
                        //if (temple.id != 0)
                            //--temple.power;
                        //--item.power;
                        TurnOnConditionFlag(item.region - 1, RegionDataFlags.PlagueOngoing);
                    }
                    else if (UnityEngine.Random.Range(1, 100 + 1) <= 2)
                    {
                        if (UnityEngine.Random.Range(0, 100 + 1) > item.randomPowerBonus + alliesPowerMod)
                        {
                            //if (temple.id != 0)
                                //--temple.power;
                            //--item.power;
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.PlagueBeginning);
                        }
                    }

                    // Persecuted temple
                    if (TemplesAssociatedWithRegions[item.region - 1] != 0)
                    {
                        if (UnityEngine.Random.Range(0, 100 + 1) >= (temple.power - item.power + 5) / 5)
                            TurnOffConditionFlag(item.region - 1, RegionDataFlags.PersecutedTemple);
                        else if (temple.power >= 2 * item.power )
                            TurnOffConditionFlag(item.region - 1, RegionDataFlags.PersecutedTemple);
                        else
                        {
                            regionData[item.region - 1].IDOfPersecutedTemple = (ushort)temple.id;
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.PersecutedTemple);
                            //--temple.power;
                        }
                    }

                    // Crime wave
                    //if (regionData[item.region - 1].Flags[(int)RegionDataFlags.CrimeWave] == true)
                        //--item.power;

                    FactionFile.FactionData thievesGuild;
                    FactionData.GetFactionData(42, out thievesGuild);

                    FactionFile.FactionData darkBrotherhood;
                    FactionData.GetFactionData(108, out darkBrotherhood);

                    if (UnityEngine.Random.Range(0, 101) >= ((thievesGuild.power + darkBrotherhood.power) / 2 - item.power + 5) / 5)
                        TurnOffConditionFlag(item.region - 1, RegionDataFlags.CrimeWave);
                    else
                    {
                        TurnOnConditionFlag(item.region - 1, RegionDataFlags.CrimeWave);
                        //--item.power;
                    }

                    // Witch burnings
                    FactionFile.FactionData witches;
                    FactionData.FindFactionByTypeAndRegion(8, item.region, out witches);
                    //if (regionData[item.region - 1].Flags[(int)RegionDataFlags.WitchBurnings] == true)
                        //--witches.power;
                    if (witches.id != 0)
                    {
                        if (UnityEngine.Random.Range(0, 101) >= (witches.power - item.power + 5) / 5)
                            TurnOffConditionFlag(item.region - 1, RegionDataFlags.WitchBurnings);
                        else
                        {
                            TurnOnConditionFlag(item.region - 1, RegionDataFlags.WitchBurnings);
                            //--witches.power;
                        }
                    }
                }
            }
        }

        public void TurnOffConditionFlag(int regionID, RegionDataFlags flagID)
        {
            regionData[regionID].Flags[(int)flagID] = false;
        }

        public void TurnOnConditionFlag(int regionID, RegionDataFlags flagID)
        {
            regionData[regionID].Flags[(int)flagID] = true;
        }

        /// <summary>
        /// Initialize region data arrays.
        /// </summary>
        public void InitializeRegionData()
        {
            FormulaHelper.RandomizeInitialRegionalPrices(ref regionData);

            // TODO: Randomize values and flags. Their relationships are not fully understood yet so for now just initializing all to 0 and false.
            for (int i = 0; i < regionData.Length; i++)
            {
                regionData[i].Values = new byte[29];

                for (int j = 0; j < 29; j++)
                    regionData[i].Values[j] = 0;

                regionData[i].Flags = new bool[29];
                for (int j = 0; j < 29; j++)
                    regionData[i].Flags[j] = false;

                regionData[i].Flags2 = new bool[14];
                for (int j = 0; j < 14; j++)
                    regionData[i].Flags2[j] = false;
            }
        }

        /// <summary>
        /// Normalize reputations towards 0. Called every certain number of game days.
        /// </summary>
        public void NormalizeReputations()
        {
            for (int i = 0; i < 62; ++i)
            {
                if (regionData[i].LegalRep < 0)
                    ++regionData[i].LegalRep;
                else if (regionData[i].LegalRep > 0)
                    --regionData[i].LegalRep;
            }

            List<int> keys = new List<int>(factionData.FactionDict.Keys);
            foreach (int key in keys)
            {
                if (factionData.FactionDict[key].rep < 0)
                    factionData.ChangeReputation(factionData.FactionDict[key].id, 1);
                else if (factionData.FactionDict[key].rep > 0)
                    factionData.ChangeReputation(factionData.FactionDict[key].id, -1);
            }
        }

        #endregion

        #region Crime

        public enum Crimes
        {
            None = 0,
            Attempted_Breaking_And_Entering = 1,
            Trespassing = 2,
            Breaking_And_Entering = 3,
            Assault = 4,
            Murder = 5,
            Tax_Evasion = 6,
            Criminal_Conspiracy = 7,
            Vagrancy = 8,
            Smuggling = 9,
            Piracy = 10,
            High_Treason = 11,
            Pickpocketing = 12,
            Theft = 13,
            Treason = 14,
        }

        // Values after index 0 are from FALL.EXE. It does not seem to have a valid value for the last crime "Treason," so just using half of "High Treason" value here.
        short[] reputationLossPerCrime = { 0x00, 0x0A, 0x05, 0x0A, 0x08, 0x14, 0x0A, 0x02, 0x01, 0x02, 0x02, 0x4B, 0x02, 0x08, 0x24 };

        public void LowerRepForCrime()
        {
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            regionData[regionIndex].LegalRep -= reputationLossPerCrime[(int)crimeCommitted];

            FactionFile.FactionData peopleFaction;
            FactionData.FindFactionByTypeAndRegion(15, regionIndex + 1, out peopleFaction);

            FactionData.ChangeReputation(peopleFaction.id, -(reputationLossPerCrime[(int)crimeCommitted] / 2));
        }

        public void RaiseReputationForDoingSentence()
        {
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            regionData[regionIndex].LegalRep += (short)(halfOfLegalRepPlayerLostFromCrime - 1);

            FactionFile.FactionData peopleFaction;
            FactionData.FindFactionByTypeAndRegion(15, regionIndex + 1, out peopleFaction);

            // Classic changes reputation here by (1 - halfOfLegalRepPlayerLostFromCrime) / 2). Probably a bug.
            FactionData.ChangeReputation(peopleFaction.id, (halfOfLegalRepPlayerLostFromCrime - 1) / 2);
        }

        public bool SurrenderToCityGuards(bool voluntarySurrender)
        {
            int regionIndex = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            short legalRep = regionData[regionIndex].LegalRep;

            if (CurrentHealth <= 0)
                return false;

            SetHealth(1);
            if (legalRep < -20 && !voluntarySurrender)
                return false;
            else if (legalRep < -20 || legalRep > 0)
                CourtWindow();
            else if ((DFRandom.rand() & 1) != 0 && !voluntarySurrender)
                return false;
            else
                CourtWindow();

            return true;
        }

        public void CourtWindow()
        {
            arrested = true;
            halfOfLegalRepPlayerLostFromCrime = (short)(reputationLossPerCrime[(int)crimeCommitted] / 2);
            DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenCourtWindow);
        }

        #endregion

        #region Event Handlers

        private void StartGameBehaviour_OnNewGame()
        {
            Reset();
        }

        private void PlayerEntity_OnExhausted(DaggerfallEntity entity)
        {
            const int youDropToTheGround1 = 1071;
            const int youDropToTheGround2 = 1072;

            bool enemiesNearby = GameManager.Instance.AreEnemiesNearby();

            ITextProvider textProvider = DaggerfallUnity.Instance.TextProvider;
            TextFile.Token[] tokens;

            GameManager.Instance.PlayerMotor.CancelMovement = true;

            DaggerfallMessageBox messageBox = new DaggerfallMessageBox(DaggerfallUI.UIManager);

            if (GameManager.Instance.PlayerEnterExit.IsPlayerSwimming)
                messageBox.SetText(HardStrings.exhaustedInWater);
            else
            {
                if (!enemiesNearby)
                    tokens = textProvider.GetRSCTokens(youDropToTheGround1);
                else
                    tokens = textProvider.GetRSCTokens(youDropToTheGround2);

                if (tokens != null && tokens.Length > 0)
                {
                    messageBox.SetTextTokens(tokens);
                }
            }
            messageBox.ClickAnywhereToClose = true;
            messageBox.ParentPanel.BackgroundColor = Color.clear;
            messageBox.Show();

            if (!enemiesNearby && !GameManager.Instance.PlayerEnterExit.IsPlayerSwimming)
            {
                // TODO: Duplicates rest code in rest window. Should be unified.
                DaggerfallUnity.Instance.WorldTime.Now.RaiseTime(1 * DaggerfallDateTime.SecondsPerHour);
                int healthRecoveryRate = FormulaHelper.CalculateHealthRecoveryRate(this);
                int fatigueRecoveryRate = FormulaHelper.CalculateFatigueRecoveryRate(MaxFatigue);
                int spellPointRecoveryRate = FormulaHelper.CalculateSpellPointRecoveryRate(MaxMagicka);

                CurrentHealth += healthRecoveryRate;
                CurrentFatigue += fatigueRecoveryRate;
                CurrentMagicka += spellPointRecoveryRate;

                TallySkill(DFCareer.Skills.Medical, 1);
            }
            else
                SetHealth(0);
        }

        #endregion
    }
}
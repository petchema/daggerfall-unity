// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2020 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects;

namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// Implements the large HUD in Daggerfall Unity as just another overlay inside of primary HUD.
    /// This is so it can work uniformly in both widescreen and retro modes.
    /// Other HUD elements can still work alongside large HUD, but some will be disabled as they occupy same screen area.
    /// </summary>
    public class HUDLarge : Panel
    {
        const string mainFilename = "MAIN00I0.IMG";
        const string compass0Filename = "CMPA00I0.BSS";     // Standard compass
        const string compass1Filename = "CMPA01I0.BSS";     // Blue compass (unused)
        const string compass2Filename = "CMPA02I0.BSS";     // Red compass (unused)
        const int compassFrameCount = 32;

        protected Rect mainPanelRect = new Rect(0, 0, 320, 46);
        protected Rect headPanelRect = new Rect(5, 8, 37, 30);
        protected Rect compassPanelRect = new Rect(275, 2, 43, 42);
        protected Rect healthPanelRect = new Rect(49, 7, 4, 32);
        protected Rect fatiguePanelRect = new Rect(57, 7, 4, 32);
        protected Rect magickaPanelRect = new Rect(65, 7, 4, 32);

        Texture2D mainTexture;
        Texture2D[] compassTextures = new Texture2D[compassFrameCount];

        Panel headPanel = new Panel();
        Panel compassPanel = new Panel();
        HUDVitals vitals = new HUDVitals();

        Camera compassCamera;
        float eulerAngle;

        PlayerEntity playerEntity;

        /// <summary>
        /// Gets or sets a compass camera to automatically determine compass heading.
        /// </summary>
        public Camera CompassCamera
        {
            get { return compassCamera; }
            set { compassCamera = value; }
        }

        /// <summary>
        /// Gets or a sets a Euler angle to use for compass heading.
        /// This value is only observed when CompassCamera is null.
        /// </summary>
        public float EulerAngle
        {
            get { return eulerAngle; }
            set { eulerAngle = Mathf.Clamp(value, 0f, 360f); }
        }

        /// <summary>
        /// Gets or sets texture for head image. Set to null if a refresh of head texture is needed.
        /// Head texture can change at runtime, e.g. loading a new game, lycanthrope shapechange, vampire infection.
        /// </summary>
        public Texture2D HeadTexture { get; set; }

        /// <summary>
        /// Gets height of large HUD in screen space pixels (relative to configured resolution).
        /// Always 0 when large HUD disabled
        /// </summary>
        public float ScreenHeight { get; private set; }

        /// <summary>
        /// Gets or sets custom scaling value for all large HUD controls.
        /// </summary>
        public Vector2 CustomScale { get; set; }

        public HUDLarge()
            : base()
        {
            CompassCamera = Camera.main;
            playerEntity = GameManager.Instance.PlayerEntity;
            LoadAssets();
            Setup();

            // Events to refresh head on new game or load
            Serialization.SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;
            Utility.StartGameBehaviour.OnNewGame += StartGameBehaviour_OnNewGame;
        }

        void LoadAssets()
        {
            // Main large HUD background
            mainTexture = ImageReader.GetTexture(mainFilename);

            // Read compass animations
            for (int i = 0; i < compassFrameCount; i++)
            {
                compassTextures[i] = ImageReader.GetTexture(compass0Filename, 0, i, true);
            }
        }

        void Setup()
        {
            // Setup self as main container for all other large HUD controls
            VerticalAlignment = VerticalAlignment.Bottom;
            BackgroundTexture = mainTexture;
            BackgroundColor = Color.gray;

            // Compass
            Components.Add(compassPanel);

            // Head
            headPanel.BackgroundTextureLayout = BackgroundLayout.ScaleToFit;
            Components.Add(headPanel);

            // Vitals
            vitals.HorizontalAlignment = HorizontalAlignment.None;
            vitals.VerticalAlignment = VerticalAlignment.None;
            vitals.AutoSize = AutoSizeModes.None;
            vitals.SetMargins(Margins.All, 0);
            vitals.SetAllAutoSize(AutoSizeModes.None);
            vitals.SetAllHorizontalAlignment(HorizontalAlignment.None);
            vitals.SetAllVerticalAlignment(VerticalAlignment.None);
            Components.Add(vitals);
        }

        void Refresh()
        {
            HeadTexture = null;
        }

        public override void Update()
        {
            if (!Enabled)
            {
                ScreenHeight = 0;
                return;
            }

            // Update screen height
            ScreenHeight = (int)Rectangle.height;

            // Manually update position and scale of controls to match overall large HUD scale
            // Large HUD exists in screen space not in native 320x200 UI space so scale needs to be amended
            Position = mainPanelRect.position * CustomScale;
            Size = mainPanelRect.size * CustomScale;
            compassPanel.Position = compassPanelRect.position * CustomScale;
            compassPanel.Size = compassPanelRect.size * CustomScale;
            headPanel.Position = headPanelRect.position * CustomScale;
            headPanel.Size = headPanelRect.size * CustomScale;
            vitals.CustomHealthBarPosition = healthPanelRect.position * CustomScale;
            vitals.CustomHealthBarSize = healthPanelRect.size * CustomScale;
            vitals.CustomFatigueBarPosition = fatiguePanelRect.position * CustomScale;
            vitals.CustomFatigueBarSize = fatiguePanelRect.size * CustomScale;
            vitals.CustomMagickaBarPosition = magickaPanelRect.position * CustomScale;
            vitals.CustomMagickaBarSize = magickaPanelRect.size * CustomScale;

            // Update head image data when null
            if (HeadTexture == null)
                UpdateHeadTexture();

            // Set head in panel
            headPanel.BackgroundTexture = HeadTexture;

            // Calculate compass rotation percent
            float percent;
            if (compassCamera != null)
                percent = compassCamera.transform.eulerAngles.y / 360f;
            else
                percent = eulerAngle;

            // Update compass pointer
            compassPanel.BackgroundTexture = compassTextures[(int)(compassFrameCount * percent)];

            base.Update();
        }

        void UpdateHeadTexture()
        {
            ImageData head;

            // Check for racial override head
            RacialOverrideEffect racialOverride = GameManager.Instance.PlayerEffectManager.GetRacialOverrideEffect();
            if (racialOverride != null && racialOverride.GetCustomHeadImageData(playerEntity, out head))
            {
                HeadTexture = head.texture;
                return;
            }

            // Otherwise just get standard head based on gender and race
            switch (playerEntity.Gender)
            {
                default:
                case Genders.Male:
                    head = ImageReader.GetImageData(playerEntity.RaceTemplate.PaperDollHeadsMale, playerEntity.FaceIndex, 0, true);
                    break;
                case Genders.Female:
                    head = ImageReader.GetImageData(playerEntity.RaceTemplate.PaperDollHeadsFemale, playerEntity.FaceIndex, 0, true);
                    break;
            }
            HeadTexture = head.texture;
        }

        #region Event Handlers

        private void SaveLoadManager_OnLoad(Serialization.SaveData_v1 saveData)
        {
            Refresh();
        }

        private void StartGameBehaviour_OnNewGame()
        {
            Refresh();
        }

        #endregion
    }
}
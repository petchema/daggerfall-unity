﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors: Justin Steele
//
// Notes:
//

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Utility;


namespace DaggerfallWorkshop.Game.UserInterfaceWindows
{
    /// <summary>
    /// Implements controls window.
    /// </summary>
    public class DaggerfallControlsWindow : DaggerfallPopupWindow
    {
        #region Fields

        Texture2D nativeTexture;
        Texture2D mLookAltTexture;
        Panel controlsPanel = new Panel();
        Panel mLookAltPanel = new Panel();
        List<Button> moveKeysOne = new List<Button>();
        List<Button> moveKeysTwo = new List<Button>();
        List<Button> modeKeys = new List<Button>();
        List<Button> magicKeys = new List<Button>();
        List<Button> weaponKeys = new List<Button>();
        List<Button> statusKeys = new List<Button>();
        List<Button> activateKeys = new List<Button>();
        List<Button> lookKeys = new List<Button>();
        List<Button> uiKeys = new List<Button>();
        List<List<Button>> allKeys = new List<List<Button>>();
        List<string> allLabels = new List<string>();
        List<string> dupeLabels = new List<string>();

        string[] actions = Enum.GetNames(typeof(InputManager.Actions));
        const string nativeTextureName = "CNFG00I0.IMG";
        const string mLookAltTextureName = "CNFG00I1.IMG";
        const string confirmDefaults = "Are you sure you want to set default controls?";
        bool dupeHelper = false;
        bool waitingForInput = false;

        #endregion

        #region Constructors

        public DaggerfallControlsWindow(IUserInterfaceManager uiManager, IUserInterfaceWindow previousWindow = null)
            :base(uiManager, previousWindow)
        {
        }

        #endregion

        #region Unity

        public override void Update()
        {
            base.Update();

            if (!AllowCancel && Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMultipleAssignmentsMessage();
            }
        }

        #endregion

        #region Setup

        protected override void Setup()
        {
            // Load textures
            nativeTexture = DaggerfallUI.GetTextureFromImg(nativeTextureName);
            if (!nativeTexture)
                throw new Exception("DaggerfallControlsWindow: Could not load native texture.");
            mLookAltTexture = DaggerfallUI.GetTextureFromImg(mLookAltTextureName);

            // Always dim background
            ParentPanel.BackgroundColor = ScreenDimColor;

            // Controls panel
            controlsPanel.HorizontalAlignment = HorizontalAlignment.Center;
            controlsPanel.Size = NativePanel.Size;
            controlsPanel.BackgroundTexture = nativeTexture;
            NativePanel.Components.Add(controlsPanel);

            // Mouse Look Alternative Controls Panel
            mLookAltPanel.Position = new Vector2(152, 100);
            mLookAltPanel.Size = new Vector2(168, 45);
            mLookAltPanel.BackgroundTexture = mLookAltTexture;
            controlsPanel.Components.Add(mLookAltPanel);

            #region Tab Buttons

            // Joystick
            Button joystickButton = DaggerfallUI.AddButton(new Rect(0, 190, 80, 10), controlsPanel);
            joystickButton.BackgroundColor = new Color(1, 0, 0, 0.5f);
            joystickButton.OnMouseClick += JoystickButton_OnMouseClick;

            // Mouse
            Button mouseButton = DaggerfallUI.AddButton(new Rect(80, 190, 80, 10), controlsPanel);
            mouseButton.BackgroundColor = new Color(1, 0, 0, 0.5f);
            mouseButton.OnMouseClick += MouseButton_OnMouseClick;

            // Default
            Button defaultButton = DaggerfallUI.AddButton(new Rect(160, 190, 80, 10), controlsPanel);
            defaultButton.OnMouseClick += DefaultButton_OnMouseClick;

            // Continue
            Button continueButton = DaggerfallUI.AddButton(new Rect(240, 190, 80, 10), controlsPanel);
            continueButton.OnMouseClick += ContinueButton_OnMouseClick;

            #endregion

            #region Keybind Buttons

            SetupKeybindButtons(moveKeysOne, 2, 8, 57, 13, true);
            SetupKeybindButtons(moveKeysTwo, 8, 14, 164, 13, true);
            SetupKeybindButtons(modeKeys, 14, 20, 270, 13, true);
            SetupKeybindButtons(magicKeys, 20, 24, 102, 80, true);
            SetupKeybindButtons(weaponKeys, 24, 27, 102, 125, true);
            SetupKeybindButtons(statusKeys, 27, 30, 102, 159, true);
            SetupKeybindButtons(activateKeys, 30, 32, 270, 80, true);
            SetupKeybindButtons(lookKeys, 32, 36, 270, 103, true);
            SetupKeybindButtons(uiKeys, 36, 40, 270, 148, true);

            #endregion
        }

        #endregion

        #region Overrides

        public override void OnPop()
        {
            // Update keybinds only when exiting from a valid configuration
            SaveKeybindValues(moveKeysOne, 2, 8);
            SaveKeybindValues(moveKeysTwo, 8, 14);
            SaveKeybindValues(modeKeys, 14, 20);
            SaveKeybindValues(magicKeys, 20, 24);
            SaveKeybindValues(weaponKeys, 24, 27);
            SaveKeybindValues(statusKeys, 27, 30);
            SaveKeybindValues(activateKeys, 30, 32);
            SaveKeybindValues(lookKeys, 32, 36);
            SaveKeybindValues(uiKeys, 36, 40);
            InputManager.Instance.SaveKeyBinds();
        }

        #endregion

        #region Private Methods

        private void SetupKeybindButtons(List<Button> buttonGroup, int startPoint, int endPoint, int leftOffset, int topOffset, bool firstSetup)
        {
            for (int i = startPoint; i < endPoint; i++)
            {
                InputManager.Actions key = (InputManager.Actions)Enum.Parse(typeof(InputManager.Actions), actions[i]);
                int j = i - startPoint;

                if (firstSetup)
                {
                    buttonGroup.Add(new Button());
                    buttonGroup[j].Label.ShadowPosition = Vector2.zero;
                    buttonGroup[j].Size = new Vector2(47, 7);

                    if (j == 0)
                        buttonGroup[j].Position = new Vector2(leftOffset, topOffset);
                    else
                        buttonGroup[j].Position = new Vector2(leftOffset, buttonGroup[j - 1].Position.y + 11);

                    controlsPanel.Components.Add(buttonGroup[j]);
                    buttonGroup[j].Name = actions[i];
                    buttonGroup[j].OnMouseClick += KeybindButton_OnMouseClick;
                    if (i == endPoint - 1)
                    {
                        allKeys.Add(buttonGroup);
                    }
                }

                buttonGroup[j].Label.Text = InputManager.Instance.GetBinding(key).ToString();
                buttonGroup[j].Label.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
            }
        }

        private void SaveKeybindValues(List<Button> buttonGroup, int startPoint, int endPoint)
        {
            for (int i = startPoint; i < endPoint; i++)
            {
                // Get action and code for this button
                int j = i - startPoint;
                InputManager.Actions action = (InputManager.Actions)Enum.Parse(typeof(InputManager.Actions), actions[i]);
                KeyCode code = (KeyCode)Enum.Parse(typeof(KeyCode), buttonGroup[j].Label.Text);

                // Rebind only if new code is different
                KeyCode curCode = InputManager.Instance.GetBinding(action);
                if (curCode != code)
                {
                    InputManager.Instance.SetBinding(code, action);
                    Debug.LogFormat("Bound Action {0} with Code {1}", action, code.ToString());
                }
            }
        }

        private void CheckDuplicates()
        {
            foreach (List<Button> buttonGroup in allKeys)
            {
                foreach (Button keybindButton in buttonGroup)
                {
                    if (!dupeHelper)
                    {
                        allLabels.Add(keybindButton.Label.Text);
                    }
                    else
                    {
                        if (dupeLabels.Contains(keybindButton.Label.Text))
                        {
                            keybindButton.Label.TextColor = new Color(1, 0, 0);
                        }
                        else
                        {
                            keybindButton.Label.TextColor = DaggerfallUI.DaggerfallDefaultTextColor;
                        }
                    }
                }
            }

            if (!dupeHelper)
            {
                dupeLabels = allLabels.GroupBy(x => x)
                             .Where(g => g.Count() > 1)
                             .Select(g => g.Key)
                             .ToList();
                if (dupeLabels.Count() > 0)
                {
                    AllowCancel = false;
                }
                else
                {
                    AllowCancel = true;
                }
                dupeHelper = true;
                CheckDuplicates();
            }
            else
            {
                allLabels.Clear();
                dupeLabels.Clear();
                dupeHelper = false;
            }
        }

        private void SetDefaults()
        {
            InputManager.Instance.ResetDefaults();
            SetupKeybindButtons(moveKeysOne, 2, 8, 56, 12, false);
            SetupKeybindButtons(moveKeysTwo, 8, 14, 163, 12, false);
            SetupKeybindButtons(modeKeys, 14, 20, 269, 12, false);
            SetupKeybindButtons(magicKeys, 20, 24, 101, 79, false);
            SetupKeybindButtons(weaponKeys, 24, 27, 101, 124, false);
            SetupKeybindButtons(statusKeys, 27, 30, 101, 158, false);
            SetupKeybindButtons(activateKeys, 30, 32, 269, 79, false);
            SetupKeybindButtons(lookKeys, 32, 36, 269, 102, false);
            SetupKeybindButtons(uiKeys, 36, 40, 269, 147, false);
            AllowCancel = true;
        }

        private void ShowMultipleAssignmentsMessage()
        {
            DaggerfallMessageBox multipleAssignmentsBox = new DaggerfallMessageBox(uiManager, this);
            multipleAssignmentsBox.SetText(HardStrings.multipleAssignments);
            multipleAssignmentsBox.ClickAnywhereToClose = true;
            multipleAssignmentsBox.Show();
        }

        #endregion

        #region Tab Button Event Handlers

        private void JoystickButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // uiManager.PostMessage(DaggerfallUIMessages.dfuiOpenJoystickControlsWindow);
        }

        private void MouseButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            // uiManager.PostMessage(DaggerfallUIMessages.dfuiOpenMouseControlsWindow);
        }

        private void DefaultButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            DaggerfallMessageBox confirmDefaultsBox = new DaggerfallMessageBox(uiManager, DaggerfallMessageBox.CommonMessageBoxButtons.YesNo, confirmDefaults, this);
            confirmDefaultsBox.OnButtonClick += ConfirmDefaultsBox_OnButtonClick;
            confirmDefaultsBox.Show();
        }

        private void ConfirmDefaultsBox_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            sender.CloseWindow();
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                SetDefaults();
                InputManager.Instance.SaveKeyBinds();
            }
        }

        private void ContinueButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            if (!AllowCancel)
            {
                ShowMultipleAssignmentsMessage();
            }
            else
            {
                CancelWindow();
            }
        }

        #endregion

        #region Keybind Event Handlers

        private void KeybindButton_OnMouseClick(BaseScreenComponent sender, Vector2 position)
        {
            Button thisKeybindButton = (Button)sender;
            if (!waitingForInput)
                InputManager.Instance.StartCoroutine(WaitForKeyPress(thisKeybindButton));
        }

        IEnumerator WaitForKeyPress(Button button)
        {
            string currentLabel = button.Label.Text;
            //InputManager.Actions buttonAction = (InputManager.Actions)Enum.Parse(typeof(InputManager.Actions), button.Name);

            button.Label.Text = "";
            yield return new WaitForSecondsRealtime(0.05f);

            while(!Input.anyKeyDown)
            {
                waitingForInput = true;
                yield return null;
            }
            waitingForInput = false;

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(code))
                {
                    if (code.ToString() != "Escape")
                    {
                        button.Label.Text = code.ToString();
                        CheckDuplicates();
                    }
                    else
                    {
                        button.Label.Text = currentLabel;
                    }
                }
            }
        }
        
        #endregion
    }
}

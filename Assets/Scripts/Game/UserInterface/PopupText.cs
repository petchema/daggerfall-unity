﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallConnect.Arena2;

namespace DaggerfallWorkshop.Game.UserInterface
{
    /// <summary>
    /// Popup text for notifications, etc.
    /// </summary>
    public class PopupText : Panel
    {
        const float textSpacing = 1;
        const float defaultDisplayTime = 2.5f;
        const int maxRows = 7;
        // Slight throttling
        const float scrollDelay = 0.08f;

        private class Message
        {
            public readonly TextLabel text;
            public readonly float displayTime;
            public float? expirationTimer;

            public Message(TextLabel text, float displayTime)
            {
                this.text = text;
                this.displayTime = displayTime;
                expirationTimer = null;
            }
        }

        LinkedList<Message> textRows = new LinkedList<Message>();
        float timer = 0;
        float nextScrollTimer = 0;

        public PopupText()
            : base()
        {
            //TextLabel label = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, "This is some test text", this);
            //label.HorizontalAlignment = HorizontalAlignment.Center;
        }

        public override void Update()
        {
            base.Update();

            if (textRows.Count > 0)
            {
                timer += Time.deltaTime;
                if (timer >= nextScrollTimer)
                {
                    // Remove items from front of list
                    if (timer >= textRows.First.Value.expirationTimer)
                    {
                        textRows.RemoveFirst();
                    }
                    if (textRows.Count > 0)
                        nextScrollTimer += scrollDelay;
                    else
                    {
                        timer = 0f;
                        nextScrollTimer = 0f;
                    }
                }
            }
        }

        public override void Draw()
        {
            base.Draw();

            // Draw text
            int count = 0;
            float y = 4;
            int maxCount = (textRows.Count > maxRows) ? maxRows : textRows.Count;
            LinkedList<Message>.Enumerator enumerator = textRows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                TextLabel label = enumerator.Current.text;
                if (label != null)
                {
                    label.Position = new Vector2(0, y);
                    label.Draw();
                    y += label.TextHeight + textSpacing;
                }
                if (enumerator.Current.expirationTimer == null)
                {
                    // Start counting when text is first shown
                    enumerator.Current.expirationTimer = timer + enumerator.Current.displayTime;
                }
                if (++count > maxCount)
                    break;
            }
        }

        public void AddText(string text)
        {
            AddText(text, defaultDisplayTime);
        }

        /// <summary>
        /// Adds text with custom delay.
        /// Delay affects this item only. Subsequent text items can override delay.
        /// Delay will return to default after time elapsed.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="delayInSeconds">Time in seconds before removing text.</param>
        public void AddText(string text, float delayInSeconds)
        {
            TextLabel label = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, text);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.Parent = Parent;
            textRows.AddLast(new Message(label, delayInSeconds));
        }

        /// <summary>
        /// Add text from tokens with a custom delay.
        /// </summary>
        /// <param name="tokens">Tokens. One line added per text token.</param>
        /// <param name="delayInSeconds">Delay per line.</param>
        public void AddText(TextFile.Token[] tokens, float delayInSeconds)
        {
            string[] lines = TextFile.GetTokenLines(tokens);
            if (lines != null && lines.Length > 0)
            {
                foreach(string line in lines)
                {
                    AddText(line, delayInSeconds);
                }
            }
        }
    }
}

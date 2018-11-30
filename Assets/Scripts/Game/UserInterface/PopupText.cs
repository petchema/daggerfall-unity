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
        const int maxRows = 7;

        // What delay to apply when some messages are waiting to be displayed
        const float minDisplayTime = 0.5f;

        // Display time when none specified
        private float AutoDisplayTime(string text)
        {
            return Mathf.Max(1f, 0.5f + text.Length / 30f);
        }

        private class Message
        {
            public readonly TextLabel text;
            public readonly float displayTime;

            public Message(TextLabel text, float displayTime)
            {
                this.text = text;
                this.displayTime = displayTime;
            }
        }

        LinkedList<Message> textRows = new LinkedList<Message>();
        float timer = 0;
        // Positive if some scrolling has been scheduled
        float nextScrollTimer = -1f;
        // How many messages must be scrolled quickly
        private int fastScrollMessages = 0;

        public PopupText()
            : base()
        {
            //TextLabel label = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, "This is some test text", this);
            //label.HorizontalAlignment = HorizontalAlignment.Center;
        }

        public override void Update()
        {
            base.Update();

            if (textRows.Count > 0 && nextScrollTimer >= 0f)
            {
                timer += Time.deltaTime;
                // Remove items from front of list
                if (timer >= nextScrollTimer)
                {
                    textRows.RemoveFirst();
                    if (fastScrollMessages > 0)
                        fastScrollMessages--;

                    nextScrollTimer = -1f;
                    if (textRows.Count == 0)
                        timer = 0f;
                }
            }
        }

        private void SetFastScrollMessages(int number)
        {
            if (number > fastScrollMessages)
            {
                fastScrollMessages = number;
                // Quicken current message if needed
                if (fastScrollMessages > 0 && nextScrollTimer > timer + minDisplayTime)
                    nextScrollTimer = timer + minDisplayTime;
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
                if (++count > maxCount)
                    break;
            }
            if (textRows.Count > 0 && nextScrollTimer < 0f)
            {
                // Schedule scrolling when display just happened
                float computedDelay = fastScrollMessages > 0 ? minDisplayTime : textRows.First.Value.displayTime;
                nextScrollTimer = timer + computedDelay;
            }
        }

        public void AddText(string text)
        {
            AddText(text, AutoDisplayTime(text));
        }

        /// <summary>
        /// Adds text with custom delay.
        /// Delay affects this item only.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="delayInSeconds">Time in seconds before removing text.</param>
        public void AddText(string text, float delayInSeconds)
        {
            AddTextInternal(text, delayInSeconds);
            if (textRows.Count > maxRows)
                SetFastScrollMessages(textRows.Count - maxRows);
        }

        private void AddTextInternal(string text, float delayInSeconds)
        {
            // HACK: "override delays, caller doesn't know what it's doing"
            delayInSeconds = AutoDisplayTime(text);
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
                int rowsCountBefore = textRows.Count;
                foreach (string line in lines)
                {
                    AddTextInternal(line, delayInSeconds);
                }
                // Never fast scroll text just added
                if (textRows.Count > maxRows)
                    SetFastScrollMessages(Mathf.Min(rowsCountBefore, textRows.Count - maxRows));
            }
        }
    }
}

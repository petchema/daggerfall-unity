﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2019 Daggerfall Workshop
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

        const float popDelay = 1.0f;
        const int maxRows = 7;
        const float maxUnseenDelay = 1.5f;

        const float speedupPerExtraLine = 1f + 1f / maxUnseenDelay;

        LinkedList<TextLabel> textRows = new LinkedList<TextLabel>();
        // Text scrolls away when timer becomes negative
        float timer;
        float nextPopDelay = popDelay;

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
                if (textRows.Count > maxRows)
                {
                    // Accelerate to keep up
                    float speedup = Mathf.Pow(speedupPerExtraLine, textRows.Count - maxRows);
                    // Smoother, but no time guarantees:
                    // float speedup = 1f + 0.5f * (textRows.Count - maxRows);
                    timer -= Time.deltaTime * speedup;
                }
                else
                    timer -= Time.deltaTime;

                if (timer < -popDelay)
                {
                    timer += nextPopDelay;
                    // Remove item from front of list
                    textRows.RemoveFirst();

                    // Reset pop delay to default
                    nextPopDelay = popDelay;
                }

            }
        }

        public override void Draw()
        {
            base.Draw();

            // Draw text
            int count = 0;
            float y = 4;
            if (textRows.Count > 0 && timer < 0)
                y += (textRows.First.Value.TextHeight + textSpacing) * timer / popDelay;
            int maxCount = (textRows.Count > maxRows) ? maxRows : textRows.Count;
            IEnumerator enumerator = textRows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                TextLabel label = enumerator.Current as TextLabel;
                if (label != null)
                {
                    label.Position = new Vector2(0, y);
                    label.Draw();
                    y += label.TextHeight + textSpacing;
                }
                if (++count > maxCount)
                    break;
            }
        }

        /// <summary>
        /// Adds text with custom delay.
        /// Delay affects this item only. Subsequent text items can override delay.
        /// Delay will return to default after time elapsed.
        /// </summary>
        /// <param name="text">Text to display.</param>
        /// <param name="delayInSeconds">Time in seconds before removing text.</param>
        public void AddText(string text, float delayInSeconds = popDelay)
        {
            if (textRows.Count == 0)
                // set no-scroll delay
                timer = delayInSeconds;
            else if (timer >= 0)
                // retrigger no-scroll delay
                timer = Mathf.Max(timer, delayInSeconds);
            else
                // set next no-scroll delay
                nextPopDelay = Mathf.Max(nextPopDelay, delayInSeconds);
            TextLabel label = DaggerfallUI.AddTextLabel(DaggerfallUI.DefaultFont, Vector2.zero, text);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.Parent = Parent;
            textRows.AddLast(label);
            GameManager.Instance.PlayerEntity.Notebook.AddMessage(text);
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

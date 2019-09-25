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
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Stores enemy settings for serialization and other tasks.
    /// </summary>
    public class DaggerfallEnemy : MonoBehaviour
    {
        ulong loadID = 0;
        bool questSpawn = false;

        public ulong LoadID
        {
            get { return loadID; }
            set { loadID = value; }
        }

        public bool QuestSpawn
        {
            get { return questSpawn; }
            set { questSpawn = value; }
        }

        // UESP describes acute hearing as "allows you to hear sounds from farther away"
        // https://en.uesp.net/wiki/Daggerfall:ClassMaker#Special_Advantages
        // Assuming this means enemy sounds specficially, rather than *all sounds* (which could get annoying)
        // If player has acute hearing advantage then enemy audio source max distance is increased by 25%
        // If player also has improved acute hearing enchantment then enemy audio source max distance is increased by 50%
        // When interrupted, you should have been alerted earlier, so foes should be/spawned further away from you
        // TODO: Learn more about acute hearing and refine how this works
        public static float HearingDistanceMultiplier()
        {
            float multiplier = 1f;
            if (GameManager.Instance.PlayerEntity.Career.AcuteHearing)
            {
                const float acuteHearingMultiplier = 1.25f;
                const float improvedAcuteHearingMultiplier = 1.5f;

                multiplier = (GameManager.Instance.PlayerEntity.ImprovedAcuteHearing) ? improvedAcuteHearingMultiplier : acuteHearingMultiplier;
            }
            return multiplier;
        }

        private void Start()
        {
            // NOTE: This should feel like a fun advantage and not just bombard player with audio!
            float multiplier = HearingDistanceMultiplier();

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource)
                audioSource.maxDistance *= multiplier;
        }
    }
}
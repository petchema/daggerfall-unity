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

using System;
using UnityEngine;

namespace DaggerfallWorkshop
{
    public class DaggerfallGC
    {
        private const float gcThrottleDelay = 180f;
        private static float gcTimer = Time.realtimeSinceStartup;

        private DaggerfallGC()
        {
        }

        public static void ThrottledUnloadUnusedAssets()
        {
            if (Time.realtimeSinceStartup >= gcTimer)
                UnloadUnusedAssets();
        }

        internal static void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
            gcTimer = Time.realtimeSinceStartup + gcThrottleDelay;
        }
    }
}

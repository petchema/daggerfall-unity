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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Special mobile unit behaviours for Daedra Seducer.
    /// </summary>
    public class DaedraSeducerMobileBehaviour : MonoBehaviour
    {
        DaggerfallMobileUnit mobile;

        private void Start()
        {
            mobile = GetComponent<DaggerfallMobileUnit>();
        }
    }
}
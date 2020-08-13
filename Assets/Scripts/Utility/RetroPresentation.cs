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

namespace DaggerfallWorkshop.Utility
{
    /// <summary>
    /// Presents final retro rendering to viewport.
    /// </summary>
    public class RetroPresentation : MonoBehaviour
    {
        public RenderTexture RetroPresentationSource;

        // Set this to your target aspect ratio, eg. (16, 9) or (4, 3).
        public Vector2 targetAspect = new Vector2(4, 3);
        Camera _camera;

        private void Start()
        {
            // Disable self if retro mode not enabled
            if (DaggerfallUnity.Settings.RetroRenderingMode == 0)
                gameObject.SetActive(false);
            else
            {
                _camera = GetComponent<Camera>();
                UpdateCrop();
            }
        }

        // Call this method if your window size or target aspect change.
        public void UpdateCrop() {
            Debug.Log("toto was in UpdateCrop()");
            // Determine ratios of screen/window & target, respectively.
            float screenRatio = Screen.width / (float)Screen.height;
            float targetRatio = targetAspect.x / targetAspect.y;

            if(Mathf.Approximately(screenRatio, targetRatio)) {
                // Screen or window is the target aspect ratio: use the whole area.
                _camera.rect = new Rect(0, 0, 1, 1);
            }
            else if(screenRatio > targetRatio) {
                // Screen or window is wider than the target: pillarbox.
                float normalizedWidth = targetRatio / screenRatio;
                float barThickness = (1f - normalizedWidth)/2f;
                _camera.rect = new Rect(barThickness, 0, normalizedWidth, 1);
            }
            else {
                // Screen or window is narrower than the target: letterbox.
                float normalizedHeight = screenRatio / targetRatio;
                float barThickness = (1f - normalizedHeight) / 2f;
                _camera.rect = new Rect(0, barThickness, 1, normalizedHeight);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (RetroPresentationSource)
            {
                // Present retro render
                Graphics.Blit(RetroPresentationSource, null as RenderTexture);
            }
        }
    }
}
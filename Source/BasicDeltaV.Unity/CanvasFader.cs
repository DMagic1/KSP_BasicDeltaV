#region License
/*
 * Basic DeltaV
 * 
 * CanvasFader - Monobehaviour for making smooth fade in and fade out for UI windows
 * 
 * Copyright (C) 2016 DMagic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version. 
 * 
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details. 
 * 
 * You should have received a copy of the GNU General Public License 
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 * 
 * 
 */
#endregion

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BasicDeltaV.Unity
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class CanvasFader : MonoBehaviour
    {
        [SerializeField]
        private float SlowRate = 0.9f;
        [SerializeField]
        private float FastRate = 0.3f;

        private CanvasGroup canvas;
        private IEnumerator fader;
        private bool allowInterrupt = true;

        protected virtual void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
        }

        protected void Fade(float to, bool fast, Action call = null, bool interrupt = true, bool overrule = false)
        {
            if (canvas == null)
                return;

            Fade(canvas.alpha, to, fast ? FastRate : SlowRate, call, interrupt, overrule);
        }

        protected void Alpha(float to)
        {
            if (canvas == null)
                return;

            to = Mathf.Clamp01(to);
            canvas.alpha = to;
        }

        private void Fade(float from, float to, float duration, Action call, bool interrupt, bool overrule)
        {
            if (!allowInterrupt && !overrule)
                return;

            if (fader != null)
                StopCoroutine(fader);

            fader = FadeRoutine(from, to, duration, call, interrupt);
            StartCoroutine(fader);
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action call, bool interrupt)
        {
            allowInterrupt = interrupt;

            yield return new WaitForEndOfFrame();

            float f = 0;

            while (f <= 1)
            {
                f += Time.deltaTime / duration;
                Alpha(Mathf.Lerp(from, to, f));
                yield return null;
            }

            if (call != null)
                call.Invoke();

            allowInterrupt = true;

            fader = null;
        }

    }
}
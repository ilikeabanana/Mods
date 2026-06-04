using System.Collections;
using UnityEngine;

namespace Ultrarogue.Curses
{
    public class CurseOfDarkness : BaseCurse
    {
        public override string CurseName => "Curse of the Fog";

        private bool originalFogEnabled;
        private Color originalFogColor;
        private float originalFogStart;
        private float originalFogEnd;

        private Coroutine fogCoroutine;

        private const float TargetFogStart = 0f;
        private const float TargetFogEnd = 10f; // Adjust to your scene scale
        private const float LerpDuration = 2f;

        public override void OnApply()
        {
            base.OnApply();

            originalFogEnabled = RenderSettings.fog;
            originalFogColor = RenderSettings.fogColor;
            originalFogStart = RenderSettings.fogStartDistance;
            originalFogEnd = RenderSettings.fogEndDistance;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.black;

            StartFogLerp(originalFogStart, originalFogEnd, TargetFogStart, TargetFogEnd);
        }

        public override void OnRemove()
        {
            base.OnRemove();

            RenderSettings.fogColor = originalFogColor;

            StartFogLerp(
                RenderSettings.fogStartDistance,
                RenderSettings.fogEndDistance,
                originalFogStart,
                originalFogEnd,
                () => RenderSettings.fog = originalFogEnabled
            );
        }

        private void StartFogLerp(float fromStart, float fromEnd, float toStart, float toEnd, System.Action onComplete = null)
        {
            if (fogCoroutine != null)
                Plugin.Instance.StopCoroutine(fogCoroutine);

            fogCoroutine = Plugin.Instance.StartCoroutine(
                LerpFogDistances(fromStart, fromEnd, toStart, toEnd, onComplete)
            );
        }

        private IEnumerator LerpFogDistances(float fromStart, float fromEnd, float toStart, float toEnd, System.Action onComplete)
        {
            float elapsed = 0f;

            while (elapsed < LerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / LerpDuration);

                RenderSettings.fogStartDistance = Mathf.Lerp(fromStart, toStart, t);
                RenderSettings.fogEndDistance = Mathf.Lerp(fromEnd, toEnd, t);

                yield return null;
            }

            RenderSettings.fogStartDistance = toStart;
            RenderSettings.fogEndDistance = toEnd;
            onComplete?.Invoke();
        }
    }
}
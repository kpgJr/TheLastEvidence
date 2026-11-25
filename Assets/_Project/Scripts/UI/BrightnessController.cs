using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastEvidence
{
    /// <summary>
    /// Applique la luminosité : exposition du post-traitement (bureau) ou intensité ambiante (VR,
    /// où le post-traitement est désactivé pour les performances du Quest).
    /// </summary>
    public class BrightnessController : MonoBehaviour
    {
        [SerializeField] private Volume volume;

        private ColorAdjustments colorAdjustments;
        private float baseExposure;
        private Color baseAmbient;

        private void Awake()
        {
            if (volume == null) volume = GetComponent<Volume>();
            if (volume != null && volume.profile != null && volume.profile.TryGet(out colorAdjustments))
                baseExposure = colorAdjustments.postExposure.value;
            baseAmbient = RenderSettings.ambientLight;
        }

        private void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        private void OnDisable() => GameSettings.Changed -= Apply;

        private void Apply()
        {
            float b = GameSettings.Brightness;
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = baseExposure + b;
            }
            if (PlatformMode.IsVR) RenderSettings.ambientLight = baseAmbient * Mathf.Pow(2f, b + 0.6f);
        }
    }
}

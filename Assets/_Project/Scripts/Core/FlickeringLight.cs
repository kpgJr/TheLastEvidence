using UnityEngine;

namespace LastEvidence
{
    /// <summary>Éclairage vacillant occasionnel (ambiance).</summary>
    [RequireComponent(typeof(Light))]
    public class FlickeringLight : MonoBehaviour
    {
        [SerializeField] private float minInterval = 4f;
        [SerializeField] private float maxInterval = 11f;
        [SerializeField] private float flickerDuration = 0.6f;

        private Light targetLight;
        private float baseIntensity;
        private float nextFlicker;
        private float flickerEnd;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseIntensity = targetLight.intensity;
            nextFlicker = Time.time + Random.Range(minInterval, maxInterval);
        }

        private void Update()
        {
            if (Time.time >= nextFlicker)
            {
                flickerEnd = Time.time + flickerDuration;
                nextFlicker = Time.time + Random.Range(minInterval, maxInterval);
            }
            targetLight.intensity = Time.time < flickerEnd
                ? baseIntensity * (Random.value < 0.5f ? 0.15f : Random.Range(0.6f, 1.1f))
                : baseIntensity;
        }
    }
}

using UnityEngine;

namespace LastEvidence
{
    /// <summary>Lampe torche (touche F / gâchette gauche en VR).</summary>
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Light spotLight;
        [SerializeField] private bool startOn;

        public bool IsOn => spotLight != null && spotLight.enabled;

        public void Init(Light light) => spotLight = light;

        private void Start()
        {
            if (spotLight != null) spotLight.enabled = startOn;
        }

        private void Update()
        {
            bool active = GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
            if (active && GameInput.Flashlight.WasPressedThisFrame()) Toggle();
        }

        public void Toggle()
        {
            if (spotLight == null) return;
            spotLight.enabled = !spotLight.enabled;
            AudioManager.Instance?.PlaySfx(SfxType.Switch);
        }
    }
}

using UnityEngine;

namespace LastEvidence
{
    /// <summary>Interrupteur ou lampe : allume/éteint des lumières (peut nécessiter le courant).</summary>
    public class SwitchController : InteractableBase, ISaveable
    {
        [SerializeField] private Light[] lights;
        [SerializeField] private Renderer[] emissiveRenderers;
        [SerializeField] private Color emissionColor = new Color(1f, 0.75f, 0.4f);
        [SerializeField] private bool isOn;

        public bool IsOn => isOn;

        public void Configure(Light[] targetLights, Renderer[] emissive, bool startOn)
        {
            lights = targetLights; emissiveRenderers = emissive; isOn = startOn;
        }

        private void Start() => Apply();

        protected override string GetAvailablePrompt() => isOn ? "Éteindre la lampe" : "Allumer la lampe";

        protected override void OnInteract(PlayerRig player)
        {
            isOn = !isOn;
            AudioManager.Instance?.PlaySfxAt(SfxType.Switch, transform.position);
            Apply();
        }

        private void Apply()
        {
            if (lights != null) foreach (var l in lights) if (l != null) l.enabled = isOn;
            if (emissiveRenderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var r in emissiveRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", isOn ? emissionColor : Color.black);
                r.SetPropertyBlock(block);
            }
        }

        public string CaptureState() => isOn ? "1" : "0";

        public void RestoreState(string state)
        {
            isOn = state == "1";
            Apply();
        }
    }
}

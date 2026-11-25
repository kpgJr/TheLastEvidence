using UnityEngine;

namespace LastEvidence
{
    /// <summary>Retour visuel discret : légère émission ambrée sur l'objet visé.</summary>
    public class InteractableHighlight : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        [SerializeField] private Color highlightColor = new Color(0.32f, 0.22f, 0.1f);

        private Renderer[] renderers;
        private MaterialPropertyBlock block;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
        }

        public void SetHighlighted(bool on)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null || r is LineRenderer) continue;
                r.GetPropertyBlock(block);
                // Hors surbrillance, on restaure l'émission d'origine du matériau (écrans allumés, lampes…).
                Color original = r.sharedMaterial != null && r.sharedMaterial.HasProperty(EmissionColor)
                    ? r.sharedMaterial.GetColor(EmissionColor) : Color.black;
                block.SetColor(EmissionColor, on ? original + highlightColor : original);
                r.SetPropertyBlock(block);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LastEvidence
{
    /// <summary>Trace le rayon de la manette droite (vers l'interface ou l'objet visé) avec un réticule.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class VRPointer : MonoBehaviour
    {
        [SerializeField] private XRRayInteractor rayInteractor;
        [SerializeField] private PlayerInteraction interaction;
        [SerializeField] private Transform reticle;

        private LineRenderer line;

        public void Init(XRRayInteractor ray, PlayerInteraction inter, Transform reticleTransform)
        {
            rayInteractor = ray; interaction = inter; reticle = reticleTransform;
        }

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
        }

        private void LateUpdate()
        {
            Vector3 start = transform.position;
            Vector3 end;
            bool hasTarget = false;
            bool menuOpen = UIManager.Instance != null && UIManager.Instance.AnyPanelOpen
                            || GameManager.Instance == null;

            if (rayInteractor != null && rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult ui) && ui.isValid)
            {
                end = ui.worldPosition;
                hasTarget = true;
            }
            else if (!menuOpen && interaction != null && interaction.LastHitDistance > 0)
            {
                end = start + transform.forward * interaction.LastHitDistance;
                hasTarget = interaction.Current != null;
            }
            else
            {
                end = start + transform.forward * (menuOpen ? 3f : interaction != null ? interaction.MaxDistance : 2.5f);
            }

            line.SetPosition(0, start);
            line.SetPosition(1, end);
            Color c = hasTarget ? UIStyle.Amber : new Color(0.85f, 0.93f, 1f, 0.9f);
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, hasTarget ? 0.9f : 0.35f);
            if (reticle != null)
            {
                reticle.gameObject.SetActive(hasTarget);
                reticle.position = end;
            }
        }
    }
}

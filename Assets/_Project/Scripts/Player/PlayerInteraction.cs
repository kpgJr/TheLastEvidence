using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Système d'interaction générique : Raycast depuis la caméra (bureau) ou la manette droite (VR)
    /// jusqu'à 2,5 m, affichage de l'indication et retour visuel sur l'objet visé.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private PlayerRig rig;
        [SerializeField] private float maxDistance = 2.5f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private Collider currentCollider;
        private IInteractable current;
        private InteractableHighlight currentHighlight;
        private string lastPrompt;

        public IInteractable Current => current;
        public float LastHitDistance { get; private set; } = -1f;
        public float MaxDistance => maxDistance;

        public void Init(PlayerRig owner) => rig = owner;

        private void Awake()
        {
            if (rig == null) rig = GetComponent<PlayerRig>();
        }

        private void OnDisable() => ClearTarget();

        private void Update()
        {
            bool active = GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
            if (!active || rig == null || rig.InteractionOrigin == null)
            {
                ClearTarget();
                return;
            }

            UpdateTarget();

            if (current != null && GameInput.Interact.WasPressedThisFrame())
            {
                if (current.CanInteract()) current.Interact(rig);
                else AudioManager.Instance?.PlaySfx(SfxType.Locked);
                RefreshPrompt(force: true);
            }
            else if (GameInput.Examine.WasPressedThisFrame())
            {
                if (current is IExaminable examinable) examinable.Examine(rig);
                else if (currentCollider != null && currentCollider.GetComponentInParent<IExaminable>() is IExaminable ex)
                    ex.Examine(rig);
            }
        }

        private void UpdateTarget()
        {
            Transform origin = rig.InteractionOrigin;
            var ray = new Ray(origin.position, origin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask, QueryTriggerInteraction.Collide))
            {
                LastHitDistance = hit.distance;
                if (hit.collider != currentCollider)
                {
                    ClearTarget();
                    currentCollider = hit.collider;
                    current = hit.collider.GetComponentInParent<IInteractable>();
                    if (current != null)
                    {
                        currentHighlight = ((Component)current).GetComponent<InteractableHighlight>();
                        currentHighlight?.SetHighlighted(true);
                    }
                }
            }
            else
            {
                LastHitDistance = -1f;
                ClearTarget();
            }
            RefreshPrompt(false);
        }

        private void RefreshPrompt(bool force)
        {
            string prompt = null;
            if (current != null && current is Component c && c != null)
            {
                string key = PlatformMode.IsVR ? "Gâchette" : GameInput.GetKeyboardDisplay(GameInput.Interact);
                prompt = current.CanInteract() || !string.IsNullOrEmpty(current.GetPrompt())
                    ? $"{key} - {current.GetPrompt()}"
                    : null;
                if (current is IExaminable && !(current is InspectableObject))
                {
                    string ek = PlatformMode.IsVR ? "X" : GameInput.GetKeyboardDisplay(GameInput.Examine);
                    prompt += $"\n{ek} - Examiner";
                }
            }
            if (force || prompt != lastPrompt)
            {
                lastPrompt = prompt;
                GameEvents.RaiseInteractionPrompt(prompt);
            }
        }

        private void ClearTarget()
        {
            currentHighlight?.SetHighlighted(false);
            currentHighlight = null;
            current = null;
            currentCollider = null;
            if (lastPrompt != null)
            {
                lastPrompt = null;
                GameEvents.RaiseInteractionPrompt(null);
            }
        }
    }
}

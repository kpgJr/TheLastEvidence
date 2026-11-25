using System.Collections;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Partie mobile générique : tiroir, porte de coffre, compartiment caché d'horloge, panneau secret.
    /// Peut être verrouillée par une condition et s'ouvrir automatiquement quand elle est remplie.
    /// </summary>
    public class DrawerController : InteractableBase, ISaveable
    {
        [SerializeField] private Transform movingPart;
        [SerializeField] private Vector3 openLocalOffset = new Vector3(0, 0, 0.35f);
        [SerializeField] private Vector3 openLocalEuler = Vector3.zero;
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private string openPrompt = "Ouvrir le tiroir";
        [SerializeField] private string closePrompt = "Fermer le tiroir";
        [SerializeField] private bool canClose = true;
        [SerializeField] private bool openAutomaticallyWhenAvailable;
        [SerializeField] private bool interactable = true;
        [SerializeField] private SfxType sound = SfxType.Drawer;
        [SerializeField] private string flagOnOpen;
        [SerializeField] private string messageOnAutoOpen;

        private Vector3 closedPos;
        private Quaternion closedRot;
        private bool isOpen;
        private Coroutine anim;

        public bool IsOpen => isOpen;

        public void Configure(Transform part, Vector3 offset, Vector3 euler, string open, string close,
            bool closable, bool autoOpen, bool isInteractable, SfxType sfx, string flag = null, string autoMessage = null)
        {
            movingPart = part; openLocalOffset = offset; openLocalEuler = euler; openPrompt = open; closePrompt = close;
            canClose = closable; openAutomaticallyWhenAvailable = autoOpen; interactable = isInteractable; sound = sfx;
            flagOnOpen = flag; messageOnAutoOpen = autoMessage;
        }

        private void Awake()
        {
            if (movingPart == null) movingPart = transform;
            closedPos = movingPart.localPosition;
            closedRot = movingPart.localRotation;
        }

        private void OnEnable()
        {
            GameEvents.FlagSet += OnFlag;
            GameEvents.StateRestored += OnRestored;
        }

        private void OnDisable()
        {
            GameEvents.FlagSet -= OnFlag;
            GameEvents.StateRestored -= OnRestored;
        }

        private void OnFlag(string flag)
        {
            if (openAutomaticallyWhenAvailable && !isOpen && IsAvailable)
            {
                SetOpen(true, true);
                if (!string.IsNullOrEmpty(messageOnAutoOpen)) GameEvents.RaiseNotification(messageOnAutoOpen);
            }
        }

        private void OnRestored()
        {
            if (openAutomaticallyWhenAvailable && !isOpen && IsAvailable) SetOpen(true, false);
        }

        public override bool CanInteract() => interactable && (!isOpen || canClose);

        protected override string GetAvailablePrompt()
        {
            if (!interactable) return string.Empty;
            return isOpen ? (canClose ? closePrompt : string.Empty) : openPrompt;
        }

        protected override void OnInteract(PlayerRig player)
        {
            if (!CanInteract()) return;
            SetOpen(!isOpen, true);
        }

        public void SetOpen(bool open, bool animate)
        {
            isOpen = open;
            if (open && !string.IsNullOrEmpty(flagOnOpen)) ProgressionManager.Instance?.SetFlag(flagOnOpen);
            Vector3 targetPos = open ? closedPos + openLocalOffset : closedPos;
            Quaternion targetRot = open ? closedRot * Quaternion.Euler(openLocalEuler) : closedRot;
            if (anim != null) StopCoroutine(anim);
            if (!animate || !gameObject.activeInHierarchy)
            {
                movingPart.localPosition = targetPos;
                movingPart.localRotation = targetRot;
                return;
            }
            AudioManager.Instance?.PlaySfxAt(sound, transform.position);
            anim = StartCoroutine(Animate(targetPos, targetRot));
        }

        private IEnumerator Animate(Vector3 pos, Quaternion rot)
        {
            Vector3 p0 = movingPart.localPosition;
            Quaternion r0 = movingPart.localRotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                movingPart.localPosition = Vector3.Lerp(p0, pos, k);
                movingPart.localRotation = Quaternion.Slerp(r0, rot, k);
                yield return null;
            }
            movingPart.localPosition = pos;
            movingPart.localRotation = rot;
            anim = null;
        }

        public string CaptureState() => isOpen ? "1" : "0";

        public void RestoreState(string state) => SetOpen(state == "1", false);
    }
}

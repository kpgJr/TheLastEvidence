using System.Collections;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Porte pivotante. Peut être verrouillée par une clé (objet d'inventaire) et/ou une condition de
    /// progression. Une porte sans clé requise se déverrouille automatiquement quand sa condition est remplie.
    /// Le collider suit le battant : une porte fermée bloque le joueur.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable, ISaveable
    {
        [SerializeField] private string doorId = "door";
        [SerializeField] private Transform pivot;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float openDuration = 0.8f;
        [SerializeField] private bool startsLocked;
        [SerializeField] private string requiredItemId;
        [SerializeField] private ProgressCondition unlockCondition = new ProgressCondition();
        [SerializeField] private string lockedMessage = "La porte est verrouillée.";
        [SerializeField] private string remoteUnlockMessage;

        private bool isOpen;
        private bool isLocked;
        private Coroutine animation;

        public string DoorId => doorId;
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        public void Configure(string id, Transform hingePivot, bool locked, string keyItemId, string[] flags,
            string lockedMsg, string remoteMsg = null)
        {
            doorId = id; pivot = hingePivot; startsLocked = locked; requiredItemId = keyItemId;
            unlockCondition = new ProgressCondition { requiredFlags = flags ?? new string[0] };
            lockedMessage = lockedMsg; remoteUnlockMessage = remoteMsg;
        }

        private void Awake()
        {
            if (pivot == null) pivot = transform;
            isLocked = startsLocked;
        }

        private void OnEnable()
        {
            GameEvents.FlagSet += OnFlagSet;
            GameEvents.StateRestored += OnStateRestored;
        }

        private void OnDisable()
        {
            GameEvents.FlagSet -= OnFlagSet;
            GameEvents.StateRestored -= OnStateRestored;
        }

        private bool NeedsKey => !string.IsNullOrEmpty(requiredItemId);
        private bool HasKey => !NeedsKey || (InventoryManager.Instance != null && InventoryManager.Instance.Has(requiredItemId));
        private bool ConditionMet => unlockCondition == null || unlockCondition.IsMet();

        private void OnFlagSet(string flag) => TryRemoteUnlock(true);
        private void OnStateRestored() => TryRemoteUnlock(false);

        private void TryRemoteUnlock(bool notify)
        {
            if (!isLocked || NeedsKey || unlockCondition == null || unlockCondition.IsEmpty || !ConditionMet) return;
            Unlock(notify);
            if (notify && !string.IsNullOrEmpty(remoteUnlockMessage)) GameEvents.RaiseNotification(remoteUnlockMessage);
        }

        public string GetPrompt()
        {
            if (isLocked) return HasKey && ConditionMet && NeedsKey ? "Déverrouiller la porte" : "Porte verrouillée";
            return isOpen ? "Fermer la porte" : "Ouvrir la porte";
        }

        public bool CanInteract() => !isLocked || (NeedsKey && HasKey && ConditionMet);

        public void Interact(PlayerRig player)
        {
            if (isLocked)
            {
                if (NeedsKey && HasKey && ConditionMet)
                {
                    Unlock(true);
                    var item = GameDatabase.Instance != null ? GameDatabase.Instance.FindItem(requiredItemId) : null;
                    GameEvents.RaiseNotification(item != null ? $"Porte déverrouillée avec : {item.displayName}" : "Porte déverrouillée");
                    SetOpen(true, true);
                }
                else
                {
                    GameEvents.RaiseNotification(lockedMessage);
                    AudioManager.Instance?.PlaySfxAt(SfxType.Locked, transform.position);
                }
                return;
            }
            SetOpen(!isOpen, true);
        }

        public void Unlock(bool withEffects)
        {
            if (!isLocked) return;
            isLocked = false;
            if (withEffects) AudioManager.Instance?.PlaySfxAt(SfxType.Pickup, transform.position, 0.6f);
            ProgressionManager.Instance?.SetFlag("door." + doorId);
        }

        public void SetOpen(bool open, bool animate)
        {
            isOpen = open;
            Quaternion target = Quaternion.Euler(0, open ? openAngle : 0, 0);
            if (animation != null) StopCoroutine(animation);
            if (!animate || !gameObject.activeInHierarchy)
            {
                pivot.localRotation = target;
                return;
            }
            AudioManager.Instance?.PlaySfxAt(open ? SfxType.DoorOpen : SfxType.DoorClose, transform.position);
            animation = StartCoroutine(Animate(target));
        }

        private IEnumerator Animate(Quaternion target)
        {
            Quaternion start = pivot.localRotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / openDuration)
            {
                pivot.localRotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
            pivot.localRotation = target;
            animation = null;
        }

        public string CaptureState() => (isLocked ? "L" : "U") + (isOpen ? "O" : "C");

        public void RestoreState(string state)
        {
            if (string.IsNullOrEmpty(state) || state.Length < 2) return;
            isLocked = state[0] == 'L';
            SetOpen(state[1] == 'O', false);
        }
    }
}

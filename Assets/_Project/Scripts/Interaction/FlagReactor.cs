using UnityEngine;
using UnityEngine.Events;

namespace LastEvidence
{
    /// <summary>
    /// Active/désactive des objets quand une condition de progression devient vraie
    /// (ex. lumières et écrans quand le courant est rétabli). Fonctionne aussi après un chargement.
    /// </summary>
    public class FlagReactor : MonoBehaviour
    {
        [SerializeField] private ProgressCondition condition = new ProgressCondition();
        [SerializeField] private GameObject[] enableWhenMet;
        [SerializeField] private GameObject[] disableWhenMet;
        [SerializeField] private string notification;
        [SerializeField] private UnityEvent onMet;

        private bool applied;

        public void Configure(string[] flags, GameObject[] enable, GameObject[] disable, string message = null)
        {
            condition = new ProgressCondition { requiredFlags = flags };
            enableWhenMet = enable; disableWhenMet = disable; notification = message;
        }

        private void Awake() => Apply(false);

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

        private void OnFlag(string flag) { if (!applied && condition.IsMet()) Apply(true, true); }
        private void OnRestored() => Apply(condition.IsMet(), false);

        private void Apply(bool met, bool withEffects = false)
        {
            applied = met;
            if (enableWhenMet != null) foreach (var go in enableWhenMet) if (go != null) go.SetActive(met);
            if (disableWhenMet != null) foreach (var go in disableWhenMet) if (go != null) go.SetActive(!met);
            if (met && withEffects)
            {
                if (!string.IsNullOrEmpty(notification)) GameEvents.RaiseNotification(notification);
                onMet?.Invoke();
            }
        }
    }
}

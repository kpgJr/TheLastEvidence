using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Gestionnaire centralisé de progression. Tout changement important (objet ramassé,
    /// preuve découverte, énigme résolue, porte déverrouillée) se traduit par un drapeau :
    ///   item.&lt;id&gt;, evidence.&lt;id&gt;, puzzle.&lt;id&gt;, door.&lt;id&gt;, ou un drapeau libre.
    /// Portes, objectifs et événements dépendent de ces drapeaux via ProgressCondition.
    /// </summary>
    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        public FlagStore Flags { get; } = new FlagStore();

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            GameEvents.ItemAdded += OnItemAdded;
            GameEvents.EvidenceDiscovered += OnEvidence;
            GameEvents.PuzzleSolved += OnPuzzleSolved;
        }

        private void OnDisable()
        {
            GameEvents.ItemAdded -= OnItemAdded;
            GameEvents.EvidenceDiscovered -= OnEvidence;
            GameEvents.PuzzleSolved -= OnPuzzleSolved;
            if (Instance == this) Instance = null;
        }

        private void OnItemAdded(ItemData item) { if (item != null) SetFlag("item." + item.id); }
        private void OnEvidence(EvidenceData e) { if (e != null) SetFlag("evidence." + e.id); }
        private void OnPuzzleSolved(string id) => SetFlag("puzzle." + id);

        public bool HasFlag(string flag) => Flags.Has(flag);

        public void SetFlag(string flag)
        {
            if (SaveManager.IsRestoring) { Flags.Set(flag); return; }
            if (Flags.Set(flag)) GameEvents.RaiseFlagSet(flag);
        }

        public void RestoreFlags(System.Collections.Generic.IEnumerable<string> flags)
        {
            Flags.Clear();
            if (flags == null) return;
            foreach (var f in flags) Flags.Set(f);
        }
    }
}

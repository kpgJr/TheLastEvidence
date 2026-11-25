using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Système d'aide facultatif : jusqu'à 3 niveaux d'indice par énigme, accessibles depuis le journal.</summary>
    public class HintManager : MonoBehaviour
    {
        public static HintManager Instance { get; private set; }
        public const int MaxLevel = 3;

        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

        public int TotalHintsUsed
        {
            get { int n = 0; foreach (var l in levels.Values) n += l; return n; }
        }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public int GetLevel(string puzzleId) => levels.TryGetValue(puzzleId, out int l) ? l : 0;

        /// <summary>Révèle le niveau d'indice suivant et le retourne (0 si plus aucun indice).</summary>
        public int RevealNext(string puzzleId)
        {
            int level = GetLevel(puzzleId);
            if (level >= MaxLevel) return 0;
            level++;
            levels[puzzleId] = level;
            GameEvents.RaiseHintRevealed(puzzleId, level);
            GameEvents.RaiseJournalUpdated();
            return level;
        }

        public List<KeyValueEntry> Capture()
        {
            var list = new List<KeyValueEntry>();
            foreach (var kv in levels) list.Add(new KeyValueEntry(kv.Key, kv.Value.ToString()));
            return list;
        }

        public void Restore(List<KeyValueEntry> entries)
        {
            levels.Clear();
            if (entries == null) return;
            foreach (var e in entries)
                if (int.TryParse(e.value, out int l)) levels[e.key] = Mathf.Clamp(l, 0, MaxLevel);
        }
    }
}

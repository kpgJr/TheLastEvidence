using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Conserve les preuves découvertes et les annonce au reste du jeu (journal, tableau, progression).</summary>
    public class EvidenceManager : MonoBehaviour
    {
        public static EvidenceManager Instance { get; private set; }

        private readonly List<EvidenceData> discovered = new List<EvidenceData>();

        public IReadOnlyList<EvidenceData> Discovered => discovered;
        public int Count => discovered.Count;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Has(string id) => discovered.Exists(e => e.id == id);

        public bool Discover(EvidenceData evidence)
        {
            if (evidence == null || Has(evidence.id)) return false;
            discovered.Add(evidence);
            if (!SaveManager.IsRestoring)
            {
                GameEvents.RaiseNotification($"Nouvelle preuve : {evidence.title}");
                AudioManager.Instance?.PlaySfx(SfxType.Evidence);
            }
            GameEvents.RaiseEvidenceDiscovered(evidence);
            GameEvents.RaiseJournalUpdated();
            return true;
        }

        public List<string> GetIds() => discovered.ConvertAll(e => e.id);

        public void RestoreIds(List<string> ids, GameDatabase db)
        {
            discovered.Clear();
            if (ids == null || db == null) return;
            foreach (var id in ids)
            {
                var e = db.FindEvidence(id);
                if (e != null && !discovered.Contains(e)) discovered.Add(e);
            }
        }
    }
}

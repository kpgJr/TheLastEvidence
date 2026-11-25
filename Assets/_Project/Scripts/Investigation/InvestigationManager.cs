using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Données de l'enquête : affaire courante et associations preuve → suspect faites
    /// par le joueur sur le tableau d'enquête (vérifiées lors de l'accusation).
    /// </summary>
    public class InvestigationManager : MonoBehaviour
    {
        public static InvestigationManager Instance { get; private set; }

        [SerializeField] private CaseData caseData;

        private readonly Dictionary<string, string> evidenceToSuspect = new Dictionary<string, string>();

        public CaseData Case => caseData != null ? caseData : GameDatabase.Instance != null ? GameDatabase.Instance.caseData : null;
        public int AccusationAttempts { get; set; }

        public void SetCase(CaseData data) => caseData = data;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Associe (ou dissocie si suspectId est null) une preuve à un suspect.</summary>
        public void Link(string evidenceId, string suspectId)
        {
            if (string.IsNullOrEmpty(evidenceId)) return;
            if (string.IsNullOrEmpty(suspectId)) evidenceToSuspect.Remove(evidenceId);
            else evidenceToSuspect[evidenceId] = suspectId;
            GameEvents.RaiseBoardChanged(evidenceId);
        }

        public string GetLinkedSuspect(string evidenceId) =>
            evidenceToSuspect.TryGetValue(evidenceId, out var s) ? s : null;

        public List<string> GetEvidenceLinkedTo(string suspectId)
        {
            var list = new List<string>();
            foreach (var kv in evidenceToSuspect) if (kv.Value == suspectId) list.Add(kv.Key);
            return list;
        }

        public List<KeyValueEntry> CaptureLinks()
        {
            var list = new List<KeyValueEntry>();
            foreach (var kv in evidenceToSuspect) list.Add(new KeyValueEntry(kv.Key, kv.Value));
            return list;
        }

        public void RestoreLinks(List<KeyValueEntry> links, int attempts)
        {
            evidenceToSuspect.Clear();
            if (links != null) foreach (var l in links) evidenceToSuspect[l.key] = l.value;
            AccusationAttempts = attempts;
        }
    }
}

using System.Collections.Generic;

namespace LastEvidence
{
    public enum AccusationOutcome { Good, Incomplete, Bad }

    /// <summary>Évalue une accusation (logique pure, testable en Edit Mode).</summary>
    public static class AccusationEvaluator
    {
        /// <param name="linkedEvidenceIds">Preuves associées au suspect accusé sur le tableau d'enquête.</param>
        public static AccusationOutcome Evaluate(CaseData data, string suspectId, int motiveIndex,
            IEnumerable<string> linkedEvidenceIds, out int keyEvidenceCount)
        {
            keyEvidenceCount = 0;
            if (data == null || data.culprit == null || suspectId != data.culprit.id)
                return AccusationOutcome.Bad;

            var keyIds = new HashSet<string>();
            if (data.keyEvidence != null)
                foreach (var e in data.keyEvidence) if (e != null) keyIds.Add(e.id);

            if (linkedEvidenceIds != null)
                foreach (var id in linkedEvidenceIds) if (keyIds.Contains(id)) keyEvidenceCount++;

            bool motiveOk = motiveIndex == data.correctMotiveIndex;
            return motiveOk && keyEvidenceCount >= data.minimumKeyEvidence
                ? AccusationOutcome.Good
                : AccusationOutcome.Incomplete;
        }
    }
}

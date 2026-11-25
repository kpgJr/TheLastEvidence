using UnityEngine;

namespace LastEvidence
{
    /// <summary>Description complète de l'affaire et de sa solution (configurable dans l'Inspector).</summary>
    [CreateAssetMenu(menuName = "The Last Evidence/Case", fileName = "Case_")]
    public class CaseData : ScriptableObject
    {
        public string caseTitle = "L'affaire Lemaire";
        [TextArea(4, 12)] public string caseSummary;
        public SuspectData[] suspects;
        public string[] motives;

        [Header("Solution")]
        public SuspectData culprit;
        public int correctMotiveIndex;
        [Tooltip("Preuves qui incriminent réellement le coupable.")]
        public EvidenceData[] keyEvidence;
        [Tooltip("Nombre minimal de preuves clés associées au coupable pour la bonne fin.")]
        public int minimumKeyEvidence = 4;

        [Header("Textes de fin")]
        [TextArea(4, 12)] public string truthSummary;
        [TextArea(2, 6)] public string goodEndingText;
        [TextArea(2, 6)] public string incompleteEndingText;
        [TextArea(2, 6)] public string badEndingText;
    }
}

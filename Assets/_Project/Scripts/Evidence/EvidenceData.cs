using UnityEngine;

namespace LastEvidence
{
    public enum EvidenceCategory { Physical, Document, Photo, Testimony, Recording }

    /// <summary>Une preuve de l'enquête. Son identifiant sert à vérifier l'accusation finale.</summary>
    [CreateAssetMenu(menuName = "The Last Evidence/Evidence", fileName = "Evidence_")]
    public class EvidenceData : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(3, 10)] public string description;
        public EvidenceCategory category;
        [Tooltip("Lieu de découverte (affiché dans le journal).")]
        public string location;
        [Tooltip("Entrée de chronologie ajoutée au journal (vide si aucune).")]
        public string timelineEntry;
        [Tooltip("Ordre de tri de la chronologie (minutes depuis minuit le 14 octobre).")]
        public int timelineMinutes = -1;
        [Tooltip("Suspects concernés par cette preuve (aide à la lecture, pas la solution).")]
        public SuspectData[] relatedSuspects;
    }
}

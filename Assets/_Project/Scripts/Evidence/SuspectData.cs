using UnityEngine;

namespace LastEvidence
{
    [CreateAssetMenu(menuName = "The Last Evidence/Suspect", fileName = "Suspect_")]
    public class SuspectData : ScriptableObject
    {
        public string id;
        public string fullName;
        public string role;
        [TextArea(2, 6)] public string profile;
        [TextArea(2, 4)] public string potentialMotive;
        [TextArea(2, 4)] public string alibi;
        public Color portraitColor = Color.gray;
    }
}

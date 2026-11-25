using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Base de données centrale chargée depuis Resources : permet de retrouver un objet,
    /// une preuve ou un suspect à partir de son identifiant (indispensable pour la sauvegarde).
    /// </summary>
    [CreateAssetMenu(menuName = "The Last Evidence/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        public const string ResourcePath = "GameDatabase";

        public CaseData caseData;
        public List<ItemData> items = new List<ItemData>();
        public List<EvidenceData> evidence = new List<EvidenceData>();
        public List<SuspectData> suspects = new List<SuspectData>();
        [Tooltip("Matériau utilisé pour le rayon des manettes VR.")]
        public Material pointerMaterial;
        [Tooltip("Matériau des objets temporaires (icônes 3D, surbrillance).")]
        public Material defaultMaterial;
        [Tooltip("Matériau (peau) des mains virtuelles en VR.")]
        public Material handMaterial;

        private static GameDatabase instance;

        public static GameDatabase Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameDatabase>(ResourcePath);
                return instance;
            }
            set => instance = value;
        }

        public ItemData FindItem(string id) => items.Find(i => i != null && i.id == id);
        public EvidenceData FindEvidence(string id) => evidence.Find(e => e != null && e.id == id);
        public SuspectData FindSuspect(string id) => suspects.Find(s => s != null && s.id == id);
    }
}

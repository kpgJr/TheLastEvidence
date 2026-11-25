using UnityEngine;

namespace LastEvidence
{
    public enum ItemCategory { Key, Tool, Document, Evidence, Special }

    /// <summary>Données statiques d'un objet ramassable (ScriptableObject).</summary>
    [CreateAssetMenu(menuName = "The Last Evidence/Item", fileName = "Item_")]
    public class ItemData : ScriptableObject
    {
        [Tooltip("Identifiant unique et persistant (utilisé par la sauvegarde).")]
        public string id;
        public string displayName;
        [TextArea(2, 6)] public string description;
        public Sprite icon;
        public ItemCategory category;
        [Tooltip("Modèle 3D optionnel affiché lors de l'examen depuis l'inventaire.")]
        public GameObject modelPrefab;
        [Tooltip("Un objet important pour l'enquête ne peut pas être retiré de l'inventaire.")]
        public bool isQuestItem = true;
        [Tooltip("Texte lu lorsqu'on utilise l'objet depuis l'inventaire (documents).")]
        [TextArea(3, 10)] public string readableText;
    }
}

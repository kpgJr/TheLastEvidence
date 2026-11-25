using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Donne un identifiant persistant à un GameObject afin de restaurer son état
    /// (porte ouverte, tiroir ouvert, objet ramassé...) après un chargement.
    /// </summary>
    [DisallowMultipleComponent]
    public class PersistentObject : MonoBehaviour
    {
        [SerializeField] private string persistentId;

        public string PersistentId => persistentId;

        public void SetId(string id) => persistentId = id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(persistentId))
                persistentId = System.Guid.NewGuid().ToString("N");
        }
#endif
    }
}

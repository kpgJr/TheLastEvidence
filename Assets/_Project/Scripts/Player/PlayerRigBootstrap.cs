using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Point d'apparition du joueur : instancie le rig bureau ou VR selon le mode détecté.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerRigBootstrap : MonoBehaviour
    {
        [Tooltip("Faux pour les scènes de menu (pas de déplacement ni d'interaction).")]
        [SerializeField] private bool gameplayRig = true;

        public PlayerRig SpawnedRig { get; private set; }

        private void Awake()
        {
            SpawnedRig = RigFactory.Create(transform.position, transform.eulerAngles.y, PlatformMode.IsVR, gameplayRig);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.83f, 0.6f, 0.32f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));
            Gizmos.DrawRay(transform.position + Vector3.up * 1.6f, transform.forward * 0.6f);
        }
    }
}

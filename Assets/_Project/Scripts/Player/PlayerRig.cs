using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Base commune aux deux rigs joueur (bureau et VR). Le reste du jeu ne connaît que cette
    /// abstraction : caméra, origine du rayon d'interaction, point d'examen et position.
    /// </summary>
    public abstract class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Current { get; private set; }

        public abstract Camera ViewCamera { get; }
        /// <summary>Transform depuis lequel part le rayon d'interaction (caméra ou manette droite).</summary>
        public abstract Transform InteractionOrigin { get; }
        /// <summary>Point devant les yeux où l'objet examiné est présenté.</summary>
        public abstract Transform InspectionAnchor { get; }
        /// <summary>Vitesse horizontale actuelle (pour les bruits de pas).</summary>
        public abstract float HorizontalSpeed { get; }
        public abstract bool IsGrounded { get; }

        public abstract void GetPose(out Vector3 position, out float yaw, out float pitch);
        public abstract void SetPose(Vector3 position, float yaw, float pitch);

        protected virtual void Awake()
        {
            Current = this;
        }

        protected virtual void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        protected static bool GameplayActive =>
            GameManager.Instance == null || GameManager.Instance.IsGameplayActive;
    }
}

using UnityEngine;

namespace LastEvidence
{
    /// <summary>Orientation de la caméra à la souris (lacet sur le corps, tangage sur la caméra).</summary>
    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [Tooltip("Facteur appliqué à la sensibilité des paramètres.")]
        [SerializeField] private float baseSensitivity = 0.08f;

        public float Pitch { get; private set; }

        public void Init(Transform playerBody) => body = playerBody;

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameplayActive) return;
            Vector2 delta = GameInput.Look.ReadValue<Vector2>();
            float sensitivity = baseSensitivity * GameSettings.MouseSensitivity;
            Pitch = Mathf.Clamp(Pitch - delta.y * sensitivity, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
            if (body != null) body.Rotate(0, delta.x * sensitivity, 0, Space.Self);
        }

        public void SetPitch(float pitch)
        {
            Pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.localRotation = Quaternion.Euler(Pitch, 0, 0);
        }
    }
}

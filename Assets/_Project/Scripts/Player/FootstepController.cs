using UnityEngine;

namespace LastEvidence
{
    /// <summary>Joue des bruits de pas selon la vitesse et la surface sous le joueur.</summary>
    [RequireComponent(typeof(PlayerRig))]
    public class FootstepController : MonoBehaviour
    {
        [SerializeField] private float stepDistance = 0.7f;

        private PlayerRig rig;
        private AudioSource source;
        private float travelled;

        private void Awake()
        {
            rig = GetComponent<PlayerRig>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
        }

        private void Update()
        {
            if (rig == null || !rig.IsGrounded) return;
            float speed = rig.HorizontalSpeed;
            if (speed < 0.3f) { travelled = stepDistance * 0.6f; return; }
            travelled += speed * Time.deltaTime;
            if (travelled < stepDistance) return;
            travelled = 0f;
            PlayStep();
        }

        private void PlayStep()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            SurfaceType surface = SurfaceType.Wood;
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 0.6f,
                    ~0, QueryTriggerInteraction.Ignore))
            {
                var mat = hit.collider.GetComponent<SurfaceMaterial>();
                if (mat != null) surface = mat.surface;
            }
            source.outputAudioMixerGroup = null;
            source.pitch = surface switch
            {
                SurfaceType.Tile => 1.35f,
                SurfaceType.Carpet => 0.75f,
                SurfaceType.Concrete => 1.1f,
                _ => 1f
            } * Random.Range(0.92f, 1.08f);
            source.volume = (surface == SurfaceType.Carpet ? 0.25f : 0.45f) * audio.SfxVolumeFactor;
            source.PlayOneShot(audio.GetClip(SfxType.Footstep));
        }
    }
}

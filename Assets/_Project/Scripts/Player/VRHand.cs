using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace LastEvidence
{
    /// <summary>
    /// Main virtuelle VR construite uniquement avec des primitives Unity (paume, phalanges, pouce, poignet).
    /// Placée sur la pose de visée (aim) de la manette - +Z = direction du laser, +Y = haut, +X = droite,
    /// identique pour les deux mains - puis reculée/abaissée pour envelopper la poignée.
    /// L'index se plie avec la gâchette, les trois autres doigts avec le bouton de poignée.
    ///
    /// Repère de la main : +Z = direction des doigts, +Y = côté du pouce,
    ///   paume vers -X pour la main droite et vers +X pour la main gauche (vers l'intérieur).
    /// </summary>
    public class VRHand : MonoBehaviour
    {
        [SerializeField] private bool isRight;
        [SerializeField] private Vector3 mountOffset = new Vector3(0.02f, -0.04f, -0.09f);
        [SerializeField] private Vector3 mountEuler = new Vector3(25f, 0f, 0f);
        [SerializeField] private float restCurl = 18f;
        [SerializeField] private float maxCurl = 78f;
        [SerializeField] private float smoothing = 18f;

        private InputAction triggerValue;
        private InputAction gripValue;
        private Transform[][] fingers;   // [doigt][phalange] - 0 = index
        private Transform[] thumb;
        private float palmSide;           // -1 : la paume regarde vers -X (main droite), +1 (main gauche)
        private float indexCurl, otherCurl;

        private static readonly float[] JointWeights = { 1.0f, 1.15f, 0.85f };

        public static VRHand Create(Transform aimPose, bool right, InputAction trigger, InputAction grip, Material material)
        {
            var go = new GameObject(right ? "RightHand" : "LeftHand");
            go.transform.SetParent(aimPose, false);
            var hand = go.AddComponent<VRHand>();
            hand.isRight = right;
            hand.triggerValue = trigger;
            hand.gripValue = grip;
            hand.Build(material);
            return hand;
        }

        private void Build(Material material)
        {
            palmSide = isRight ? -1f : 1f;
            // La main est du côté opposé à sa paume par rapport au manche de la manette.
            transform.localPosition = new Vector3(-palmSide * Mathf.Abs(mountOffset.x), mountOffset.y, mountOffset.z);
            transform.localRotation = Quaternion.Euler(mountEuler);

            var root = transform;
            // Paume et dos de la main.
            Part(root, PrimitiveType.Cube, Vector3.zero, Quaternion.identity, new Vector3(0.026f, 0.078f, 0.085f), material);
            Part(root, PrimitiveType.Sphere, new Vector3(0, 0, 0.036f), Quaternion.identity, new Vector3(0.028f, 0.082f, 0.03f), material);
            // Poignet / début de l'avant-bras.
            Part(root, PrimitiveType.Capsule, new Vector3(0, -0.004f, -0.068f), Quaternion.Euler(90, 0, 0),
                new Vector3(0.03f, 0.036f, 0.046f), material);

            // Quatre doigts : index (en haut, côté pouce) → auriculaire.
            float[] heights = { 0.027f, 0.009f, -0.009f, -0.026f };
            float[][] lengths =
            {
                new[] { 0.040f, 0.025f, 0.021f },
                new[] { 0.045f, 0.028f, 0.023f },
                new[] { 0.042f, 0.026f, 0.021f },
                new[] { 0.033f, 0.020f, 0.018f },
            };
            float[] widths = { 0.0175f, 0.018f, 0.017f, 0.015f };
            fingers = new Transform[4][];
            for (int f = 0; f < 4; f++)
                fingers[f] = Finger(root, $"Finger{f}", new Vector3(0, heights[f], 0.042f), Quaternion.identity,
                    lengths[f], widths[f], material);

            // Pouce : part du côté paume, en haut, orienté vers l'avant et vers le haut.
            thumb = Finger(root, "Thumb", new Vector3(palmSide * 0.01f, 0.03f, -0.018f),
                Quaternion.Euler(-18f, palmSide * 28f, 0f), new[] { 0.034f, 0.027f }, 0.02f, material);

            ApplyPose(restCurl, restCurl);
        }

        private Transform[] Finger(Transform parent, string name, Vector3 pos, Quaternion rot, float[] lengths, float width, Material material)
        {
            var joints = new Transform[lengths.Length];
            Transform current = parent;
            Vector3 p = pos;
            Quaternion r = rot;
            for (int i = 0; i < lengths.Length; i++)
            {
                var joint = new GameObject($"{name}_{i}").transform;
                joint.SetParent(current, false);
                joint.localPosition = p;
                joint.localRotation = r;
                float w = width * (1f - 0.08f * i);
                Part(joint, PrimitiveType.Capsule, new Vector3(0, 0, lengths[i] / 2f), Quaternion.Euler(90, 0, 0),
                    new Vector3(w, Mathf.Max(lengths[i], w) / 2f + w * 0.25f, w), material);
                joints[i] = joint;
                current = joint;
                p = new Vector3(0, 0, lengths[i]);
                r = Quaternion.identity;
            }
            return joints;
        }

        private static void Part(Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.lightProbeUsage = LightProbeUsage.Off;
            r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void Update()
        {
            float trigger = triggerValue != null ? triggerValue.ReadValue<float>() : 0f;
            float grip = gripValue != null ? gripValue.ReadValue<float>() : 0f;
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            indexCurl = Mathf.Lerp(indexCurl, Mathf.Lerp(restCurl, maxCurl, trigger), k);
            otherCurl = Mathf.Lerp(otherCurl, Mathf.Lerp(restCurl, maxCurl, grip), k);
            ApplyPose(indexCurl, otherCurl);
        }

        /// <summary>Force une pose (0 = main ouverte au repos, 1 = poing fermé) - utile pour les captures/tests.</summary>
        public void SetCurl(float index01, float others01)
        {
            indexCurl = Mathf.Lerp(restCurl, maxCurl, index01);
            otherCurl = Mathf.Lerp(restCurl, maxCurl, others01);
            ApplyPose(indexCurl, otherCurl);
        }

        private void ApplyPose(float index, float others)
        {
            if (fingers == null) return;
            for (int f = 0; f < fingers.Length; f++)
            {
                float curl = f == 0 ? index : others;
                for (int j = 0; j < fingers[f].Length; j++)
                {
                    // Rotation autour de +Y : amène +Z (doigt) vers la normale de la paume (±X).
                    fingers[f][j].localRotation = Quaternion.Euler(0, palmSide * curl * JointWeights[j], 0);
                }
            }
            if (thumb != null && thumb.Length > 1)
            {
                // Le pouce se replie légèrement vers le bas (sur les boutons) quand la main se ferme.
                float t = Mathf.InverseLerp(restCurl, maxCurl, others);
                thumb[1].localRotation = Quaternion.Euler(10f + 25f * t, palmSide * 8f * t, 0);
            }
        }
    }
}

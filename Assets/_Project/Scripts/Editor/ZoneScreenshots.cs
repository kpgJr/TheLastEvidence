using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LastEvidence.EditorTools
{
    /// <summary>Génère des captures de chaque zone (vérification visuelle rapide du niveau).</summary>
    public static class ZoneScreenshots
    {
        private static readonly (string name, Vector3 pos, Vector3 euler)[] Shots =
        {
            ("1_hall", new Vector3(0, 1.65f, 1.0f), new Vector3(8, 0, 0)),
            ("1_hall_clock", new Vector3(1.8f, 1.7f, 5.5f), new Vector3(5, 25, 0)),
            ("2_office", new Vector3(-3f, 1.65f, 9.0f), new Vector3(12, 0, 0)),
            ("2_office_desk", new Vector3(-3f, 1.6f, 12.3f), new Vector3(30, 0, 0)),
            ("3_archives", new Vector3(1f, 1.65f, 9.0f), new Vector3(10, 35, 0)),
            ("4_surveillance", new Vector3(6.8f, 1.65f, 12f), new Vector3(8, 90, 0)),
            ("4_surveillance_fuse", new Vector3(7.5f, 1.6f, 9.8f), new Vector3(5, 180, 0)),
            ("5_secret", new Vector3(9f, 1.65f, 16.8f), new Vector3(5, 0, 0)),
        };

        [MenuItem("The Last Evidence/Outils/Captures d'écran des zones")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene($"{ProjectGenerator.ScenesFolder}/Investigation.unity");
            string dir = "Logs/Screenshots";
            Directory.CreateDirectory(dir);
            var go = new GameObject("ShotCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 75;
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            foreach (var (name, pos, euler) in Shots)
            {
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{dir}/{name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Debug.Log($"Captures enregistrées dans {dir}");
        }

        /// <summary>Capture des mains VR dans une scène vide (vérification de la forme et de l'orientation).</summary>
        [MenuItem("The Last Evidence/Outils/Capture des mains VR")]
        public static void CaptureHands()
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var db = GameDatabase.Instance;
            Material mat = db != null ? db.handMaterial : null;
            // Pose de visée d'une manette tenue devant soi : laser vers l'avant, légèrement vers le bas.
            var rot = Quaternion.Euler(15f, 0f, 0f);
            var hands = new System.Collections.Generic.List<VRHand>();
            foreach (bool right in new[] { false, true })
            {
                var grip = new GameObject(right ? "RightAim" : "LeftAim").transform;
                grip.SetPositionAndRotation(new Vector3(right ? 0.18f : -0.18f, 1.3f, 0.45f), rot);
                hands.Add(VRHand.Create(grip, right, null, null, mat));
            }
            string dir = "Logs/Screenshots";
            Directory.CreateDirectory(dir);
            var go = new GameObject("ShotCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.22f, 0.25f);
            go.AddComponent<UniversalAdditionalCameraData>();
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var shots = new (string, Vector3, Vector3, float)[]
            {
                ("hands_firstperson_open", new Vector3(0, 1.6f, 0), new Vector3(35, 0, 0), 0f),
                ("hands_firstperson_fist", new Vector3(0, 1.6f, 0), new Vector3(35, 0, 0), 1f),
                ("hands_side_open", new Vector3(0.75f, 1.3f, 0.35f), new Vector3(5, -90, 0), 0f),
                ("hands_front_open", new Vector3(0, 1.35f, 1.0f), new Vector3(10, 180, 0), 0f),
            };
            foreach (var (name, pos, euler, curl) in shots)
            {
                foreach (var h in hands) h.SetCurl(curl, curl);
                go.transform.SetPositionAndRotation(pos, Quaternion.Euler(euler));
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{dir}/{name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
        }

        public static void CaptureHandsBatch()
        {
            CaptureHands();
            EditorApplication.Exit(0);
        }

        public static void CaptureBatch()
        {
            Capture();
            EditorApplication.Exit(0);
        }
    }
}

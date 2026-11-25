using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LastEvidence.EditorTools
{
    /// <summary>Outils de construction de niveau à partir de primitives Unity.</summary>
    public static class LevelBuilder
    {
        public const float WallHeight = 3f;
        public const float WallThickness = 0.2f;
        public const float DoorGap = 1.2f;
        public const float DoorHeight = 2.25f;
        public const float IntensityScale = 3.2f;

        public static GameObject Group(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material mat,
            bool collider = true, float yaw = 0f, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) { go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); }
            else { go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw, 0); }
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material mat,
            bool collider = true, Vector3? euler = null, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (local) { go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero); }
            else { go.transform.position = position; go.transform.rotation = Quaternion.Euler(euler ?? Vector3.zero); }
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 position, float diameter, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Texte 3D (TextMeshPro) auto-dimensionné dans un rectangle en mètres. Lisible en regardant vers +Z local.</summary>
        public static TextMeshPro Text(Transform parent, string text, Vector3 localPos, float yaw, Vector2 size,
            Color? color = null, float maxFont = 1.2f) =>
            Text(parent, text, localPos, new Vector3(0, yaw, 0), size, color, maxFont);

        public static TextMeshPro Text(Transform parent, string text, Vector3 localPos, Vector3 euler, Vector2 size,
            Color? color = null, float maxFont = 1.2f)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.rectTransform.sizeDelta = size;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.05f;
            tmp.fontSizeMax = maxFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? new Color(0.12f, 0.1f, 0.08f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            // Compense la mise à l'échelle (souvent non uniforme) du parent, en tenant compte de la rotation du texte.
            Vector3 ps = parent.lossyScale;
            Quaternion r = go.transform.localRotation;
            float Axis(Vector3 a) => Mathf.Max(0.0001f, Vector3.Scale(ps, r * a).magnitude);
            go.transform.localScale = new Vector3(1f / Axis(Vector3.right), 1f / Axis(Vector3.up), 1f / Axis(Vector3.forward));
            return tmp;
        }

        public static Light PointLight(string name, Transform parent, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            // Facteur global : ambiance sombre mais indices toujours lisibles.
            light.intensity = intensity * IntensityScale;
            light.range = range * 1.25f;
            light.shadows = LightShadows.None;
            return light;
        }

        /// <summary>Ajoute un identifiant persistant et la surbrillance à un objet interactif.</summary>
        public static T Interactive<T>(GameObject go, string persistentId = null) where T : Component
        {
            if (!string.IsNullOrEmpty(persistentId))
            {
                var p = go.GetComponent<PersistentObject>() ?? go.AddComponent<PersistentObject>();
                p.SetId(persistentId);
            }
            if (go.GetComponent<InteractableHighlight>() == null) go.AddComponent<InteractableHighlight>();
            return go.AddComponent<T>();
        }

        public static void Floor(string name, Transform parent, float x0, float x1, float z0, float z1, Material mat, SurfaceType surface)
        {
            var f = Box(name, parent, new Vector3((x0 + x1) / 2, -0.05f, (z0 + z1) / 2), new Vector3(x1 - x0, 0.1f, z1 - z0), mat);
            f.AddComponent<SurfaceMaterial>().surface = surface;
            f.isStatic = true;
            var c = Box(name + "_Ceiling", parent, new Vector3((x0 + x1) / 2, WallHeight + 0.05f, (z0 + z1) / 2),
                new Vector3(x1 - x0, 0.1f, z1 - z0), MaterialLibrary.Ceiling);
            c.isStatic = true;
        }

        /// <summary>Mur parallèle à l'axe X (à z constant) avec ouvertures de porte centrées sur doorXs.</summary>
        public static void WallX(Transform parent, float z, float x0, float x1, Material mat, params float[] doorXs)
        {
            var cuts = new List<float>(doorXs);
            cuts.Sort();
            float start = x0;
            foreach (float c in cuts)
            {
                float a = c - DoorGap / 2, b = c + DoorGap / 2;
                if (a > start) Segment(parent, new Vector3((start + a) / 2, WallHeight / 2, z), new Vector3(a - start, WallHeight, WallThickness), mat);
                Segment(parent, new Vector3(c, (DoorHeight + WallHeight) / 2, z), new Vector3(DoorGap, WallHeight - DoorHeight, WallThickness), mat);
                start = b;
            }
            if (x1 > start) Segment(parent, new Vector3((start + x1) / 2, WallHeight / 2, z), new Vector3(x1 - start, WallHeight, WallThickness), mat);
        }

        /// <summary>Mur parallèle à l'axe Z (à x constant) avec ouvertures de porte centrées sur doorZs.</summary>
        public static void WallZ(Transform parent, float x, float z0, float z1, Material mat, params float[] doorZs)
        {
            var cuts = new List<float>(doorZs);
            cuts.Sort();
            float start = z0;
            foreach (float c in cuts)
            {
                float a = c - DoorGap / 2, b = c + DoorGap / 2;
                if (a > start) Segment(parent, new Vector3(x, WallHeight / 2, (start + a) / 2), new Vector3(WallThickness, WallHeight, a - start), mat);
                Segment(parent, new Vector3(x, (DoorHeight + WallHeight) / 2, c), new Vector3(WallThickness, WallHeight - DoorHeight, DoorGap), mat);
                start = b;
            }
            if (z1 > start) Segment(parent, new Vector3(x, WallHeight / 2, (start + z1) / 2), new Vector3(WallThickness, WallHeight, z1 - start), mat);
        }

        private static void Segment(Transform parent, Vector3 center, Vector3 size, Material mat)
        {
            var s = Box("Wall", parent, center, size, mat);
            s.isStatic = true;
        }

        /// <summary>
        /// Porte pivotante dans une ouverture. hinge = position de la charnière au sol, yaw = orientation du cadre
        /// (0 : le battant s'étend vers +X ; -90 : vers +Z). openAngle : sens d'ouverture.
        /// </summary>
        public static DoorController Door(Transform parent, string id, Vector3 hinge, float yaw, float openAngle,
            bool locked, string keyItemId, string[] flags, string lockedMessage, string remoteMessage = null)
        {
            var root = Group("Door_" + id, parent);
            root.transform.position = hinge;
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var pivot = Group("Pivot", root.transform);
            var panel = Box("Panel", pivot.transform, new Vector3(0.55f, 1.1f, 0), new Vector3(1.1f, 2.2f, 0.06f), MaterialLibrary.Door, true, 0, true);
            Box("Knob", panel.transform, new Vector3(0.4f, 0f, -0.9f), new Vector3(0.05f, 0.03f, 1.5f), MaterialLibrary.Brass, false, 0, true);
            var door = Interactive<DoorController>(panel, "door." + id);
            SetSerialized(door, "openAngle", openAngle);
            door.Configure(id, pivot.transform, locked, keyItemId, flags, lockedMessage, remoteMessage);
            return door;
        }

        /// <summary>Tiroir coulissant dont le contenu (enfants de « Contents ») se déplace avec lui.</summary>
        public static DrawerController Drawer(Transform parent, string id, Vector3 frontCenter, float yaw, Vector3 size,
            out Transform contents, float pull = 0.4f, Material mat = null)
        {
            var root = Group("Drawer_" + id, parent);
            root.transform.position = frontCenter;
            root.transform.rotation = Quaternion.Euler(0, yaw, 0);
            // Le tiroir s'étend vers +Z local (dans le meuble) ; il s'ouvre vers -Z local.
            var front = Box("Front", root.transform, Vector3.zero, new Vector3(size.x, size.y, 0.03f), mat ?? MaterialLibrary.Wood, true, 0, true);
            Box("Handle", front.transform, new Vector3(0, 0.1f, -1.2f), new Vector3(0.3f, 0.12f, 1f), MaterialLibrary.Brass, false, 0, true);
            Box("Bottom", root.transform, new Vector3(0, -size.y / 2 + 0.01f, size.z / 2), new Vector3(size.x * 0.95f, 0.02f, size.z), mat ?? MaterialLibrary.Wood, false, 0, true);
            contents = Group("Contents", root.transform).transform;
            contents.localPosition = new Vector3(0, -size.y / 2 + 0.03f, size.z / 2);
            var drawer = Interactive<DrawerController>(front, "drawer." + id);
            drawer.Configure(root.transform, new Vector3(0, 0, -pull), Vector3.zero, "Ouvrir le tiroir", "Fermer le tiroir",
                true, false, true, SfxType.Drawer);
            return drawer;
        }

        /// <summary>Feuille de papier (document lisible) posée à plat ou fixée au mur.</summary>
        public static DocumentObject Paper(Transform parent, string id, Vector3 position, float yaw, bool onWall, string label,
            string title, string body, string prompt, EvidenceData evidence, ItemData item = null, Vector2? size = null, Material mat = null)
        {
            Vector2 s = size ?? new Vector2(0.21f, 0.28f);
            GameObject go;
            if (onWall)
            {
                go = Box("Doc_" + id, parent, position, new Vector3(s.x, s.y, 0.01f), mat ?? MaterialLibrary.Paper, true, yaw);
                Text(go.transform, label, new Vector3(0, 0, -0.6f), 0, new Vector2(s.x * 0.9f, s.y * 0.9f), null, 0.35f);
            }
            else
            {
                go = Box("Doc_" + id, parent, position, new Vector3(s.x, 0.006f, s.y), mat ?? MaterialLibrary.Paper, true, yaw);
                Text(go.transform, label, new Vector3(0, 0.6f, 0), new Vector3(90, 0, 0), new Vector2(s.x * 0.9f, s.y * 0.9f), null, 0.35f);
                // Collider plus épais pour faciliter la visée.
                ((BoxCollider)go.GetComponent<Collider>()).size = new Vector3(1, 6, 1);
            }
            var doc = Interactive<DocumentObject>(go);
            doc.Configure(title, body, prompt, evidence, item);
            return doc;
        }

        /// <summary>Photographie encadrée : texte au recto (-Z) et indice au verso (+Z).</summary>
        public static InspectableObject Photo(Transform parent, string id, Vector3 position, float yaw, string front, string back,
            string name, string description, EvidenceData onClue, bool standing = true)
        {
            var go = Box("Photo_" + id, parent, position, new Vector3(0.2f, 0.15f, 0.012f), MaterialLibrary.Photo, true, yaw);
            if (!standing) go.transform.rotation = Quaternion.Euler(90, yaw, 0);
            Box("Frame", go.transform, new Vector3(0, 0, 0), new Vector3(1.08f, 1.1f, 0.8f), MaterialLibrary.WoodDark, false, 0, true);
            var frontText = Text(go.transform, front, new Vector3(0, 0, -0.75f), 0, new Vector2(0.18f, 0.13f), new Color(0.1f, 0.1f, 0.12f), 0.3f);
            frontText.fontStyle = FontStyles.Italic;
            Text(go.transform, back, new Vector3(0, 0, 0.75f), 180, new Vector2(0.18f, 0.13f), new Color(0.15f, 0.1f, 0.3f), 0.3f);
            var inspect = Interactive<InspectableObject>(go);
            inspect.Configure(name, description, null, back, Vector3.forward, onClue, 0.24f);
            return inspect;
        }

        public static void SetSerialized(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetSerialized(Object target, string property, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetSerialized(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        public static void SetSerialized(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p != null) { p.boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
    }
}

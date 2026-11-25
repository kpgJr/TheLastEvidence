using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LastEvidence.EditorTools
{
    /// <summary>Matériaux URP simples (couleurs de la direction artistique), créés à la demande.</summary>
    public static class MaterialLibrary
    {
        private const string Folder = ProjectGenerator.Root + "/Materials";
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static Material Get(string name, Color color, float smoothness = 0.25f, float metallic = 0f, Color? emission = null)
        {
            if (cache.TryGetValue(name, out var cached) && cached != null) return cached;
            string path = $"{Folder}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            // L'émission est toujours activée (noire par défaut) pour permettre la surbrillance des objets interactifs.
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", emission ?? Color.black);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            cache[name] = mat;
            return mat;
        }

        public static Material Pointer
        {
            get
            {
                string path = $"{Folder}/M_Pointer.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null) return mat;
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                AssetDatabase.CreateAsset(mat, path);
                return mat;
            }
        }

        // Raccourcis de la palette
        public static Material Wall => Get("Wall", new Color(0.42f, 0.40f, 0.37f), 0.1f);
        public static Material WallDark => Get("WallDark", new Color(0.2f, 0.22f, 0.25f), 0.1f);
        public static Material Ceiling => Get("Ceiling", new Color(0.3f, 0.3f, 0.3f), 0.05f);
        public static Material FloorWood => Get("FloorWood", new Color(0.30f, 0.20f, 0.13f), 0.35f);
        public static Material FloorTile => Get("FloorTile", new Color(0.36f, 0.37f, 0.38f), 0.5f);
        public static Material Carpet => Get("Carpet", new Color(0.30f, 0.11f, 0.13f), 0.05f);
        public static Material Concrete => Get("Concrete", new Color(0.28f, 0.29f, 0.3f), 0.15f);
        public static Material Wood => Get("Wood", new Color(0.36f, 0.23f, 0.14f), 0.4f);
        public static Material WoodDark => Get("WoodDark", new Color(0.17f, 0.11f, 0.07f), 0.45f);
        public static Material Door => Get("Door", new Color(0.25f, 0.16f, 0.1f), 0.4f);
        public static Material Metal => Get("Metal", new Color(0.35f, 0.37f, 0.4f), 0.55f, 0.8f);
        public static Material MetalDark => Get("MetalDark", new Color(0.14f, 0.15f, 0.17f), 0.5f, 0.7f);
        public static Material Brass => Get("Brass", new Color(0.83f, 0.6f, 0.32f), 0.7f, 1f);
        public static Material Paper => Get("Paper", new Color(0.77f, 0.71f, 0.6f), 0.1f);
        public static Material Photo => Get("Photo", new Color(0.86f, 0.84f, 0.8f), 0.6f);
        public static Material Cork => Get("Cork", new Color(0.55f, 0.4f, 0.26f), 0.05f);
        public static Material Fabric => Get("Fabric", new Color(0.2f, 0.29f, 0.37f), 0.05f);
        public static Material Leather => Get("Leather", new Color(0.2f, 0.09f, 0.08f), 0.5f);
        public static Material Glass => Get("Glass", new Color(0.7f, 0.8f, 0.85f), 0.95f);
        public static Material Plant => Get("Plant", new Color(0.15f, 0.28f, 0.14f), 0.2f);
        public static Material RedString => Get("RedString", new Color(0.6f, 0.1f, 0.12f), 0.2f);
        public static Material Black => Get("Black", new Color(0.04f, 0.04f, 0.05f), 0.6f);
        public static Material ScreenOff => Get("ScreenOff", new Color(0.02f, 0.02f, 0.03f), 0.9f);
        public static Material ScreenOn => Get("ScreenOn", new Color(0.1f, 0.2f, 0.3f), 0.9f, 0f, new Color(0.25f, 0.55f, 0.8f));
        public static Material Window => Get("Window", new Color(0.03f, 0.05f, 0.08f), 0.95f, 0f, new Color(0.03f, 0.05f, 0.1f));
        public static Material LampShade => Get("LampShade", new Color(0.8f, 0.65f, 0.4f), 0.1f, 0f, new Color(0.6f, 0.4f, 0.15f));
        public static Material RedLight => Get("RedLight", new Color(0.4f, 0.05f, 0.05f), 0.5f, 0f, new Color(0.8f, 0.05f, 0.05f));
    }
}

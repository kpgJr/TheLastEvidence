using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace LastEvidence.EditorTools
{
    /// <summary>
    /// Génère automatiquement tout le contenu du prototype : réglages (URP, XR, Android),
    /// données (ScriptableObjects), matériaux, scènes et paramètres de build.
    /// Menu : The Last Evidence ▸ Générer le projet complet.
    /// Ligne de commande : -executeMethod LastEvidence.EditorTools.ProjectGenerator.GenerateAllBatch
    /// </summary>
    public static class ProjectGenerator
    {
        public const string Root = "Assets/_Project";
        public const string SettingsFolder = Root + "/Settings";
        public const string ScenesFolder = Root + "/Scenes";
        public const string TmpPackagePath = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [MenuItem("The Last Evidence/1. Importer TextMeshPro (une seule fois)", priority = 0)]
        public static void ImportTmpEssentials()
        {
            if (Directory.Exists("Assets/TextMesh Pro")) { Debug.Log("TMP Essential Resources déjà importées."); return; }
            UnityEditor.AssetPackage.Package.Import(TmpPackagePath, false);
            AssetDatabase.Refresh();
        }

        [MenuItem("The Last Evidence/2. Générer le projet complet", priority = 1)]
        public static void GenerateAll()
        {
            if (!Directory.Exists("Assets/TextMesh Pro"))
            {
                Debug.LogError("Importez d'abord les ressources TextMeshPro (menu The Last Evidence ▸ 1).");
                return;
            }
            EnsureFolders();
            ConfigurePlayerSettings();
            ConfigureRenderPipeline();
            ConfigureXR();
            var content = ContentFactory.CreateAll();
            SceneFactory.CreateMainMenu();
            SceneFactory.CreateEnding();
            SceneFactory.CreateInvestigation(content);
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[The Last Evidence] Génération terminée.");
        }

        /// <summary>Point d'entrée en ligne de commande (batchmode).</summary>
        public static void GenerateAllBatch()
        {
            try
            {
                GenerateAll();
                EditorApplication.Exit(0);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void ImportTmpBatch()
        {
            ImportTmpEssentials();
            EditorApplication.Exit(0);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Animations", "Audio/Music", "Audio/SFX", "Audio/Ambience", "Materials", "Models",
                "Prefabs/Player", "Prefabs/Interactables", "Prefabs/Puzzles", "Prefabs/Environment", "Prefabs/UI",
                "Scenes", "ScriptableObjects/Items", "ScriptableObjects/Evidence", "ScriptableObjects/Suspects",
                "ScriptableObjects/Puzzles", "Settings", "Textures", "UI", "Resources", "Tests"
            };
            foreach (var f in folders) EnsureFolder($"{Root}/{f}");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "UdeM";
            PlayerSettings.productName = "The Last Evidence";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.udem.thelastevidence");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.udem.thelastevidence");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            // Nouveau Input System uniquement (1 = Input System Package).
            var playerSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettings.Length > 0)
            {
                var so = new SerializedObject(playerSettings[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null) { prop.intValue = 1; so.ApplyModifiedProperties(); }
            }
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        private static void ConfigureRenderPipeline()
        {
            string rendererPath = SettingsFolder + "/LastEvidence_Renderer.asset";
            string assetPath = SettingsFolder + "/LastEvidence_URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                    "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, assetPath);
            }
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 2;
            pipeline.shadowDistance = 25f;
            pipeline.maxAdditionalLightsCount = 8;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        /// <summary>OpenXR pour Android (Meta Quest) avec le profil des manettes Oculus Touch.</summary>
        private static void ConfigureXR()
        {
            var perTarget = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (perTarget == null)
            {
                EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget buildTargetSettings);
                if (buildTargetSettings == null)
                {
                    EnsureFolder("Assets/XR");
                    buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, buildTargetSettings, true);
                }
                if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                    buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
                if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                    buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
                perTarget = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
                EditorUtility.SetDirty(buildTargetSettings);
            }

            if (perTarget != null)
            {
                perTarget.InitManagerOnStart = true;
                var manager = perTarget.Manager;
                if (manager != null)
                    XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
                EditorUtility.SetDirty(perTarget);
            }

            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXR != null)
            {
                EnableFeature<MetaQuestFeature>(openXR);
                EnableFeature<OculusTouchControllerProfile>(openXR);
                EnableFeature<MetaQuestTouchPlusControllerProfile>(openXR);
                openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                EditorUtility.SetDirty(openXR);
            }
            else Debug.LogWarning("Réglages OpenXR Android introuvables : installez le module Android puis relancez la génération.");
        }

        private static void EnableFeature<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null) { Debug.LogWarning($"Fonctionnalité OpenXR introuvable : {typeof(T).Name}"); return; }
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesFolder}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Investigation.unity", true),
                new EditorBuildSettingsScene($"{ScenesFolder}/Ending.unity", true),
            };
        }

        [MenuItem("The Last Evidence/3. Construire l'APK Meta Quest", priority = 20)]
        public static void BuildQuest()
        {
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { $"{ScenesFolder}/MainMenu.unity", $"{ScenesFolder}/Investigation.unity", $"{ScenesFolder}/Ending.unity" },
                locationPathName = "Builds/Android/TheLastEvidence.apk",
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Build Quest : {report.summary.result} ({report.summary.totalSize / (1024 * 1024)} Mo)");
        }

        public static void BuildQuestBatch()
        {
            ConfigureXR();
            BuildQuest();
            EditorApplication.Exit(0);
        }
    }
}

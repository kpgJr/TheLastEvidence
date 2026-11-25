using System;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Paramètres du joueur (PlayerPrefs) : volumes, sensibilité, luminosité, affichage.</summary>
    public static class GameSettings
    {
        public static event Action Changed;

        private const string Prefix = "LE_";

        public static float MasterVolume { get => Get("Master", 0.9f); set => Set("Master", value); }
        public static float MusicVolume { get => Get("Music", 0.6f); set => Set("Music", value); }
        public static float SfxVolume { get => Get("Sfx", 0.9f); set => Set("Sfx", value); }
        public static float MouseSensitivity { get => Get("Sensitivity", 1f); set => Set("Sensitivity", value); }
        /// <summary>Exposition (EV) ajoutée au post-traitement : -1 à +1.5.</summary>
        public static float Brightness { get => Get("Brightness", 0f); set => Set("Brightness", value); }

        public static int QualityLevel
        {
            get => PlayerPrefs.GetInt(Prefix + "Quality", QualitySettings.GetQualityLevel());
            set { PlayerPrefs.SetInt(Prefix + "Quality", value); QualitySettings.SetQualityLevel(value, true); Notify(); }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1;
            set { PlayerPrefs.SetInt(Prefix + "Fullscreen", value ? 1 : 0); ApplyDisplay(); Notify(); }
        }

        public static int ResolutionIndex
        {
            get => PlayerPrefs.GetInt(Prefix + "Resolution", -1);
            set { PlayerPrefs.SetInt(Prefix + "Resolution", value); ApplyDisplay(); Notify(); }
        }

        private static float Get(string key, float def) => PlayerPrefs.GetFloat(Prefix + key, def);

        private static void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(Prefix + key, value);
            Notify();
        }

        private static void Notify() => Changed?.Invoke();

        public static void Save() => PlayerPrefs.Save();

        /// <summary>Applique les paramètres d'affichage au démarrage (bureau uniquement).</summary>
        public static void ApplyAll()
        {
            if (PlayerPrefs.HasKey(Prefix + "Quality"))
                QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
            ApplyDisplay();
        }

        private static void ApplyDisplay()
        {
            if (PlatformMode.IsVR || Application.isEditor) return;
            var resolutions = Screen.resolutions;
            int idx = ResolutionIndex;
            if (idx >= 0 && idx < resolutions.Length)
            {
                var r = resolutions[idx];
                Screen.SetResolution(r.width, r.height,
                    Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            else
            {
                Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            }
        }
    }
}

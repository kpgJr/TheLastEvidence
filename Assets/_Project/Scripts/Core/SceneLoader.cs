using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastEvidence
{
    /// <summary>Chargement des scènes et transmission des intentions (nouvelle partie / continuer).</summary>
    public static class SceneLoader
    {
        public const string MainMenuScene = "MainMenu";
        public const string InvestigationScene = "Investigation";
        public const string EndingScene = "Ending";

        /// <summary>Vrai lorsque la scène d'enquête doit charger la dernière sauvegarde.</summary>
        public static bool ContinueRequested { get; set; }

        public static void NewGame()
        {
            SaveManager.DeleteSave();
            ContinueRequested = false;
            Load(InvestigationScene);
        }

        public static void ContinueGame()
        {
            ContinueRequested = true;
            Load(InvestigationScene);
        }

        public static void LoadMainMenu() => Load(MainMenuScene);

        public static void LoadEnding() => Load(EndingScene);

        private static void Load(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}

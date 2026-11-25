using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Menu principal : nouvelle partie, continuer, paramètres, crédits, quitter.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        private UIPanel menu;
        private UIPanel credits;

        private void Start()
        {
            GameInput.EnsureCreated();
            GameSettings.ApplyAll();
            AudioManager.EnsureExists().PlayMusic();
            if (!PlatformMode.IsVR) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

            menu = UIPanel.Create("THE LAST EVIDENCE", new Vector2(820, 900), false);
            if (menu == null) return;
            menu.CloseOnCancel = false;
            var subtitle = UIFactory.CreateText(menu.Body, "<i>Une enquête de l'inspecteur indépendant</i>", 24, TextAlignmentOptions.Center, UIStyle.TextMuted);
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            UIFactory.AddVerticalLayout(menu.Body.gameObject, 16, 24);
            UIFactory.CreateButton(menu.Body, "Nouvelle partie", NewGame, 32);
            var cont = UIFactory.CreateButton(menu.Body, "Continuer", SceneLoader.ContinueGame, 32);
            cont.interactable = SaveManager.HasSave();
            UIFactory.CreateButton(menu.Body, "Paramètres", () => SettingsUI.Instance?.Open(), 32);
            UIFactory.CreateButton(menu.Body, "Crédits", () => credits.Open(), 32);
            UIFactory.CreateButton(menu.Body, "Quitter", Quit, 32);
            var mode = UIFactory.CreateText(menu.Body, PlatformMode.IsVR ? "Mode VR (Meta Quest)" : "Mode clavier / souris",
                20, TextAlignmentOptions.Center, UIStyle.TextMuted);
            mode.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            credits = UIPanel.Create("Crédits", new Vector2(1000, 760));
            var text = UIFactory.CreateText(credits.Body,
                "<b>The Last Evidence</b>\nPrototype d'escape room d'enquête - Unity 6, URP\n\n" +
                "Conception, programmation et narration : l'équipe du projet\n" +
                "Sons : musique libre de droit\n" +
                "Modèles : primitives Unity",
                26, TextAlignmentOptions.Center);
            text.rectTransform.Stretch();

            menu.Open();
        }

        private void NewGame()
        {
            if (SaveManager.HasSave())
                ConfirmDialog.Instance?.Show("Commencer une nouvelle partie ?\nLa sauvegarde existante sera effacée.",
                    "Nouvelle partie", "Annuler", SceneLoader.NewGame, null);
            else SceneLoader.NewGame();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

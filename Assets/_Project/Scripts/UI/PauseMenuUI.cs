using UnityEngine;

namespace LastEvidence
{
    /// <summary>Menu pause : reprendre, paramètres, sauvegarder, retour au menu principal.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        private UIPanel panel;

        public UIPanel Panel => panel;

        private void Start()
        {
            panel = UIPanel.Create("Pause", new Vector2(700, 760), false);
            if (panel == null) return;
            panel.PausesGame = true;
            UIFactory.AddVerticalLayout(panel.Body.gameObject, 18, 20);
            UIFactory.CreateButton(panel.Body, "Reprendre", panel.Close, 32);
            UIFactory.CreateButton(panel.Body, "Paramètres", () => SettingsUI.Instance?.Open(), 32);
            UIFactory.CreateButton(panel.Body, "Sauvegarder", () =>
            {
                bool ok = SaveManager.Instance != null && SaveManager.Instance.Save();
                GameEvents.RaiseNotification(ok ? "Partie sauvegardée" : "Échec de la sauvegarde");
            }, 32);
            UIFactory.CreateButton(panel.Body, "Menu principal", () =>
                ConfirmDialog.Instance?.Show("Retourner au menu principal ?\nLa progression non sauvegardée sera perdue.",
                    "Menu principal", "Annuler", SceneLoader.LoadMainMenu, null), 32);
            if (UIManager.Instance != null) UIManager.Instance.PauseMenu = this;
        }

        public void Open() => panel?.Open();
    }
}

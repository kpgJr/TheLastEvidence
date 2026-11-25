using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Pile des panneaux ouverts et raccourcis globaux (inventaire, journal, pause, retour).
    /// Bloque les déplacements dès qu'un panneau est ouvert.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private readonly List<UIPanel> stack = new List<UIPanel>();

        public InventoryUI InventoryUI { get; set; }
        public InvestigationJournal Journal { get; set; }
        public PauseMenuUI PauseMenu { get; set; }

        public bool AnyPanelOpen => stack.Count > 0;
        public UIPanel Top => stack.Count > 0 ? stack[stack.Count - 1] : null;

        private void Awake()
        {
            Instance = this;
            GameInput.EnsureCreated();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnPanelOpened(UIPanel panel)
        {
            if (stack.Contains(panel)) stack.Remove(panel);
            if (stack.Count == 0) UIRoot.Instance?.RecenterModal();
            stack.Add(panel);
            UpdateGameState();
        }

        public void OnPanelClosed(UIPanel panel)
        {
            stack.Remove(panel);
            UpdateGameState();
        }

        public void CloseAll()
        {
            for (int i = stack.Count - 1; i >= 0; i--) stack[i].Close();
        }

        private void UpdateGameState()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State == GameState.Ended) return;
            if (stack.Count == 0)
            {
                if (gm.State != GameState.Inspecting) gm.SetState(GameState.Playing);
                return;
            }
            bool paused = stack.Exists(p => p.PausesGame);
            gm.SetState(paused ? GameState.Paused : GameState.InMenu);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.State == GameState.Ended || gm.State == GameState.Inspecting)) return;

            bool pausePressed = GameInput.Pause.WasPressedThisFrame();
            if (pausePressed || GameInput.Cancel.WasPressedThisFrame())
            {
                var top = Top;
                if (top != null) { if (top.CloseOnCancel) top.Close(); }
                else if (pausePressed && gm != null && PauseMenu != null) PauseMenu.Open();
                return;
            }

            // Inventaire et journal uniquement pendant l'enquête.
            if (gm == null) return;
            if (GameInput.Inventory.WasPressedThisFrame() && InventoryUI != null)
            {
                if (InventoryUI.Panel.IsOpen) InventoryUI.Panel.Close();
                else if (!AnyPanelOpen) InventoryUI.Panel.Open();
            }
            else if (GameInput.Journal.WasPressedThisFrame() && Journal != null)
            {
                if (Journal.Panel.IsOpen) Journal.Panel.Close();
                else if (!AnyPanelOpen) Journal.Panel.Open();
            }
        }
    }
}

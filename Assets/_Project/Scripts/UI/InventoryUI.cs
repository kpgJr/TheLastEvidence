using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Inventaire (Tab / bouton A) : grille d'objets, description, utilisation.</summary>
    public class InventoryUI : MonoBehaviour
    {
        private UIPanel panel;
        private RectTransform grid;
        private TextMeshProUGUI details;
        private Button useButton;
        private readonly List<InventorySlot> slots = new List<InventorySlot>();
        private ItemData selected;

        public UIPanel Panel => panel;

        private void Start()
        {
            panel = UIPanel.Create("Inventaire", new Vector2(1300, 860));
            if (panel == null) return;
            panel.Opened += Refresh;
            var gridRoot = UIFactory.CreateImage("Grid", panel.Body, new Color(0, 0, 0, 0.25f));
            gridRoot.rectTransform.Anchor(0, 0, 0.58f, 1);
            grid = UIFactory.CreateRect("Slots", gridRoot.transform).Stretch(12, 12, 12, 12);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(160, 160);
            layout.spacing = new Vector2(12, 12);

            var right = UIFactory.CreateRect("Details", panel.Body).Anchor(0.6f, 0, 1, 1);
            details = UIFactory.CreateText(right, "", 25);
            details.rectTransform.Anchor(0, 0.16f, 1, 1);
            useButton = UIFactory.CreateButton(right, "Utiliser / Lire", UseSelected, 26);
            ((RectTransform)useButton.transform).Anchor(0.1f, 0, 0.9f, 0.13f);

            if (UIManager.Instance != null) UIManager.Instance.InventoryUI = this;
        }

        private void OnEnable() => GameEvents.ItemAdded += OnItemAdded;
        private void OnDisable() => GameEvents.ItemAdded -= OnItemAdded;
        private void OnItemAdded(ItemData item) { if (panel != null && panel.IsOpen) Refresh(); }

        private void Refresh()
        {
            foreach (var s in slots) if (s != null) Destroy(s.gameObject);
            slots.Clear();
            var inv = InventoryManager.Instance;
            if (inv == null) return;
            foreach (var item in inv.Inventory.Items) slots.Add(InventorySlot.Create(grid, item, Select));
            if (selected != null && !inv.Has(selected.id)) selected = null;
            if (selected == null && inv.Inventory.Count > 0) selected = inv.Inventory.Items[0];
            UpdateDetails();
        }

        private void Select(ItemData item)
        {
            selected = item;
            InventoryManager.Instance?.Select(item);
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            foreach (var s in slots) s.SetSelected(s.Item == selected);
            if (selected == null)
            {
                details.text = "<i>Votre inventaire est vide.</i>";
                useButton.gameObject.SetActive(false);
                return;
            }
            string category = selected.category switch
            {
                ItemCategory.Key => "Clé", ItemCategory.Tool => "Outil", ItemCategory.Document => "Document",
                ItemCategory.Evidence => "Preuve", _ => "Objet spécial"
            };
            string protectedNote = selected.isQuestItem ? "\n\n<size=80%><color=#9C9384>Objet indispensable à l'enquête.</color></size>" : "";
            details.text = $"<color=#D49A52><b>{selected.displayName}</b></color>\n<size=80%>{category}</size>\n\n{selected.description}{protectedNote}";
            useButton.gameObject.SetActive(true);
        }

        private void UseSelected()
        {
            if (selected == null) return;
            InventoryManager.Instance?.Select(selected);
            if (!string.IsNullOrEmpty(selected.readableText))
            {
                DocumentUI.Instance?.Show(selected.displayName, selected.readableText);
                return;
            }
            GameEvents.RaiseNotification(selected.category == ItemCategory.Key
                ? $"{selected.displayName} sélectionnée : approchez-vous de la serrure correspondante."
                : $"{selected.displayName} sélectionné(e). Il s'utilise automatiquement au bon endroit.");
            panel.Close();
        }
    }
}

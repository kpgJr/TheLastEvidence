using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    [Serializable]
    public class FuseSlot
    {
        public string label;
        public int correctValueIndex;
        public int startValueIndex;
        [Tooltip("Emplacement vide tant que le joueur n'a pas l'objet requis.")]
        public bool requiresItem;
    }

    /// <summary>
    /// Énigme 4 : boîte à fusibles. Chaque circuit doit recevoir le fusible du bon calibre
    /// (indiqué par le schéma affiché au mur). Un fusible manquant doit être trouvé dans les archives.
    /// </summary>
    public class ElectricalPuzzle : PuzzleBase
    {
        [SerializeField] private string[] fuseValues = { "5 A", "10 A", "15 A", "20 A" };
        [SerializeField] private List<FuseSlot> slots = new List<FuseSlot>();
        [SerializeField] private string requiredItemId = "fuse";

        private int[] current;
        private readonly List<Button> slotButtons = new List<Button>();
        private TextMeshProUGUI status;

        protected override Vector2 PanelSize => new Vector2(1100, 780);

        public void Configure(string[] values, List<FuseSlot> fuseSlots, string itemId)
        {
            fuseValues = values; slots = fuseSlots; requiredItemId = itemId;
        }

        /// <summary>Vérifie une configuration (logique pure, testée en Edit Mode).</summary>
        public static bool Check(int[] values, IList<FuseSlot> slotList)
        {
            if (values == null || slotList == null || values.Length != slotList.Count) return false;
            for (int i = 0; i < values.Length; i++)
                if (values[i] != slotList[i].correctValueIndex) return false;
            return true;
        }

        /// <summary>Soumet une configuration de fusibles (tests automatisés, débogage).</summary>
        public bool Submit(int[] values)
        {
            if (IsSolved) return true;
            if (!HasItem) { Fail("Il manque un fusible."); return false; }
            if (Check(values, slots)) { Solve(); return true; }
            Fail("Court-circuit ! Les calibres ne correspondent pas au schéma.");
            return false;
        }

        public int[] CorrectConfiguration => slots.ConvertAll(s => s.correctValueIndex).ToArray();

        private bool HasItem => string.IsNullOrEmpty(requiredItemId) ||
                                (InventoryManager.Instance != null && InventoryManager.Instance.Has(requiredItemId));

        private void EnsureState()
        {
            if (current != null && current.Length == slots.Count) return;
            current = new int[slots.Count];
            for (int i = 0; i < slots.Count; i++) current[i] = slots[i].requiresItem ? -1 : slots[i].startValueIndex;
        }

        protected override void BuildUI(RectTransform body)
        {
            EnsureState();
            var info = UIFactory.CreateText(body,
                "Placez le bon calibre de fusible sur chaque circuit, puis enclenchez le disjoncteur.\nCliquez sur un fusible pour changer son calibre.",
                24, TextAlignmentOptions.Center, UIStyle.TextMuted);
            info.rectTransform.Anchor(0, 0.84f, 1, 1);

            var row = UIFactory.CreateRect("Slots", body).Anchor(0.02f, 0.3f, 0.98f, 0.82f);
            UIFactory.AddHorizontalLayout(row.gameObject, 24, 10);
            for (int i = 0; i < slots.Count; i++)
            {
                int index = i;
                var col = UIFactory.CreateImage("Slot_" + i, row, UIStyle.Anthracite);
                UIFactory.AddVerticalLayout(col.gameObject, 12, 14, true);
                var label = UIFactory.CreateText(col.transform, slots[i].label, 26, TextAlignmentOptions.Center, UIStyle.OldBeige);
                label.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
                var button = UIFactory.CreateButton(col.transform, "", () => Cycle(index), 34);
                button.GetComponent<LayoutElement>().preferredHeight = 200;
                slotButtons.Add(button);
            }

            status = UIFactory.CreateText(body, "", 26, TextAlignmentOptions.Center);
            status.rectTransform.Anchor(0, 0.17f, 1, 0.28f);
            var validate = UIFactory.CreateButton(body, "Enclencher le disjoncteur", Validate, 28);
            validate.GetComponent<Image>().color = UIStyle.DesaturatedBlue;
            ((RectTransform)validate.transform).Anchor(0.3f, 0.02f, 0.7f, 0.15f);
        }

        protected override void RefreshUI()
        {
            EnsureState();
            for (int i = 0; i < slotButtons.Count; i++)
            {
                if (IsSolved) current[i] = slots[i].correctValueIndex;
                else if (current[i] < 0 && slots[i].requiresItem && HasItem) current[i] = slots[i].startValueIndex;
                string text = current[i] < 0 ? "<color=#792D35>VIDE</color>" : fuseValues[current[i]];
                UIFactory.SetButtonLabel(slotButtons[i], text);
                slotButtons[i].interactable = !IsSolved;
            }
            if (status != null)
                status.text = IsSolved ? "<color=#5E8C61>Courant rétabli.</color>"
                    : !HasItem ? "Un fusible manque. Il faudra en trouver un."
                    : "";
        }

        private void Cycle(int index)
        {
            if (IsSolved) return;
            if (current[index] < 0)
            {
                status.text = "Il manque un fusible pour ce circuit.";
                AudioManager.Instance?.PlaySfx(SfxType.Locked);
                return;
            }
            current[index] = (current[index] + 1) % fuseValues.Length;
            AudioManager.Instance?.PlaySfx(SfxType.Switch);
            RefreshUI();
        }

        private void Validate()
        {
            if (IsSolved) return;
            if (Array.Exists(current, v => v < 0))
            {
                Fail("Un circuit n'a pas de fusible.");
                return;
            }
            if (Check(current, slots))
            {
                AudioManager.Instance?.PlaySfx(SfxType.Electric);
                Solve();
            }
            else
            {
                AudioManager.Instance?.PlaySfx(SfxType.Electric);
                Fail("Court-circuit ! Les calibres ne correspondent pas au schéma.");
            }
            RefreshUI();
        }
    }
}

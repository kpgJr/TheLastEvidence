using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastEvidence
{
    /// <summary>Case de l'inventaire : icône (ou pastille de catégorie) et nom de l'objet.</summary>
    public class InventorySlot : MonoBehaviour
    {
        private ItemData item;
        private Image background;

        public ItemData Item => item;

        public static InventorySlot Create(Transform parent, ItemData data, System.Action<ItemData> onClick)
        {
            var button = UIFactory.CreateButton(parent, "", () => onClick(data), 20);
            var slot = button.gameObject.AddComponent<InventorySlot>();
            slot.item = data;
            slot.background = button.GetComponent<Image>();
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.rectTransform.Anchor(0, 0, 1, 0.32f);
            label.text = data.displayName;
            label.fontSize = 18;

            var icon = UIFactory.CreateImage("Icon", button.transform, CategoryColor(data.category));
            icon.raycastTarget = false;
            icon.rectTransform.Anchor(0.25f, 0.36f, 0.75f, 0.94f);
            if (data.icon != null) { icon.sprite = data.icon; icon.color = Color.white; icon.preserveAspect = true; }
            else
            {
                var letter = UIFactory.CreateText(icon.transform, CategoryLetter(data.category), 40, TextAlignmentOptions.Center, UIStyle.DeepBlack);
                letter.rectTransform.Stretch();
            }
            return slot;
        }

        public void SetSelected(bool selected)
        {
            if (background != null) background.color = selected ? UIStyle.DesaturatedBlue : UIStyle.ButtonNormal;
        }

        public static Color CategoryColor(ItemCategory c) => c switch
        {
            ItemCategory.Key => UIStyle.Amber,
            ItemCategory.Tool => UIStyle.DesaturatedBlue * 1.6f,
            ItemCategory.Document => UIStyle.OldBeige,
            ItemCategory.Evidence => UIStyle.DarkRed * 1.5f,
            _ => Color.gray
        };

        public static string CategoryLetter(ItemCategory c) => c switch
        {
            ItemCategory.Key => "Clé",
            ItemCategory.Tool => "Out",
            ItemCategory.Document => "Doc",
            ItemCategory.Evidence => "Pr",
            _ => "*"
        };
    }
}

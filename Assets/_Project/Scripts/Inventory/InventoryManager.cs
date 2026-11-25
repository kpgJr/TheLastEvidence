using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>Gestionnaire d'inventaire de la scène : fait le lien entre la logique et les événements.</summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        public Inventory Inventory { get; } = new Inventory();
        public ItemData SelectedItem { get; private set; }

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        public bool Has(string id) => Inventory.Has(id);

        public bool Add(ItemData item)
        {
            if (!Inventory.Add(item)) return false;
            if (!SaveManager.IsRestoring)
            {
                GameEvents.RaiseNotification($"Objet ajouté : {item.displayName}");
                AudioManager.Instance?.PlaySfx(SfxType.Pickup);
            }
            GameEvents.RaiseItemAdded(item);
            return true;
        }

        public bool Remove(ItemData item)
        {
            if (item != null && item.isQuestItem)
            {
                GameEvents.RaiseNotification("Cet objet est indispensable à l'enquête.");
                return false;
            }
            if (!Inventory.Remove(item)) return false;
            if (SelectedItem == item) SelectedItem = null;
            GameEvents.RaiseItemRemoved(item);
            return true;
        }

        public void Select(ItemData item) => SelectedItem = item;

        public void RestoreIds(List<string> ids, GameDatabase db)
        {
            Inventory.Clear();
            SelectedItem = null;
            if (ids == null || db == null) return;
            foreach (var id in ids)
            {
                var item = db.FindItem(id);
                if (item != null) Inventory.Add(item);
                else Debug.LogWarning($"Objet inconnu dans la sauvegarde : {id}");
            }
        }
    }
}

using System.Collections.Generic;

namespace LastEvidence
{
    /// <summary>Logique pure de l'inventaire (sans MonoBehaviour, testable en Edit Mode).</summary>
    public class Inventory
    {
        private readonly List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;
        public int Count => items.Count;

        public bool Add(ItemData item)
        {
            if (item == null || Has(item.id)) return false;
            items.Add(item);
            return true;
        }

        public bool Has(string id) => !string.IsNullOrEmpty(id) && items.Exists(i => i.id == id);

        /// <summary>Retire un objet. Les objets d'enquête sont protégés sauf si force = true.</summary>
        public bool Remove(ItemData item, bool force = false)
        {
            if (item == null) return false;
            if (item.isQuestItem && !force) return false;
            return items.Remove(item);
        }

        public void Clear() => items.Clear();

        public List<string> GetIds() => items.ConvertAll(i => i.id);
    }
}

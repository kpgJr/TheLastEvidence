using UnityEngine;

namespace LastEvidence
{
    /// <summary>Objet ramassable : ajouté à l'inventaire (et éventuellement aux preuves) puis masqué.</summary>
    public class PickupItem : InteractableBase, ISaveable
    {
        [SerializeField] private ItemData item;
        [SerializeField] private EvidenceData evidence;

        private bool taken;

        public ItemData Item => item;

        public void Configure(ItemData itemData, EvidenceData evidenceData = null)
        {
            item = itemData; evidence = evidenceData;
        }

        protected override string GetAvailablePrompt() => item != null ? $"Ramasser : {item.displayName}" : "Ramasser";

        protected override void OnInteract(PlayerRig player)
        {
            if (taken || item == null) return;
            InventoryManager.Instance?.Add(item);
            if (evidence != null) EvidenceManager.Instance?.Discover(evidence);
            SetTaken(true);
        }

        private void SetTaken(bool value)
        {
            taken = value;
            gameObject.SetActive(!value);
        }

        public string CaptureState() => taken ? "taken" : "";

        public void RestoreState(string state)
        {
            bool shouldBeTaken = state == "taken" ||
                                 (item != null && InventoryManager.Instance != null && InventoryManager.Instance.Has(item.id));
            SetTaken(shouldBeTaken);
        }
    }
}

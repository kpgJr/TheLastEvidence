namespace LastEvidence
{
    /// <summary>Énigme 1 : recherche d'objet. Résolue dès que l'objet requis entre dans l'inventaire.</summary>
    public class CollectItemPuzzle : PuzzleBase
    {
        [UnityEngine.SerializeField] private string requiredItemId = "key_office";

        protected override bool HasUI => false;

        public void Configure(string itemId) => requiredItemId = itemId;

        private void OnEnable()
        {
            GameEvents.ItemAdded += OnItemAdded;
            GameEvents.StateRestored += OnRestored;
        }

        private void OnDisable()
        {
            GameEvents.ItemAdded -= OnItemAdded;
            GameEvents.StateRestored -= OnRestored;
        }

        private void OnItemAdded(ItemData item)
        {
            if (item != null && item.id == requiredItemId && !SaveManager.IsRestoring) Solve();
        }

        private void OnRestored()
        {
            if (!IsSolved && InventoryManager.Instance != null && InventoryManager.Instance.Has(requiredItemId))
                ProgressionManager.Instance?.SetFlag("puzzle." + puzzleId);
        }
    }
}

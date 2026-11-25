using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Document, écran, message vocal... : affiche un texte et enregistre la preuve associée.
    /// </summary>
    public class DocumentObject : InteractableBase
    {
        [SerializeField] private string title;
        [TextArea(4, 16)] [SerializeField] private string body;
        [SerializeField] private string prompt = "Lire le document";
        [SerializeField] private EvidenceData evidence;
        [SerializeField] private ItemData givesItem;
        [SerializeField] private string flagOnRead;
        [SerializeField] private SfxType sound = SfxType.Paper;

        public string Title => title;

        public void Configure(string docTitle, string docBody, string docPrompt, EvidenceData evidenceData,
            ItemData item = null, string readFlag = null, SfxType sfx = SfxType.Paper)
        {
            title = docTitle; body = docBody; prompt = docPrompt; evidence = evidenceData; givesItem = item;
            flagOnRead = readFlag; sound = sfx;
        }

        protected override string GetAvailablePrompt() => prompt;

        protected override void OnInteract(PlayerRig player)
        {
            AudioManager.Instance?.PlaySfx(sound);
            DocumentUI.Instance?.Show(title, body, () =>
            {
                // La preuve est enregistrée à la fermeture pour ne pas masquer le texte par une notification.
                if (evidence != null) EvidenceManager.Instance?.Discover(evidence);
                if (givesItem != null) InventoryManager.Instance?.Add(givesItem);
                if (!string.IsNullOrEmpty(flagOnRead)) ProgressionManager.Instance?.SetFlag(flagOnRead);
            });
        }
    }
}

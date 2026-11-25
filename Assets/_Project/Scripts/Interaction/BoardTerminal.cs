namespace LastEvidence
{
    /// <summary>Tableau d'enquête physique (pièce secrète) : ouvre l'interface du tableau.</summary>
    public class BoardTerminal : InteractableBase
    {
        protected override string GetAvailablePrompt() => "Consulter le tableau d'enquête";

        protected override void OnInteract(PlayerRig player) => EvidenceBoard.Instance?.Open();
    }
}

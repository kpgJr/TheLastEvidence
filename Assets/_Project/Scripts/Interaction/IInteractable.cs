namespace LastEvidence
{
    /// <summary>
    /// Contrat commun à tous les objets interactifs (portes, clés, documents, tiroirs, coffre,
    /// ordinateurs, téléphones, interrupteurs, preuves...). Utilisé par PlayerInteraction
    /// aussi bien au clavier/souris qu'avec la manette VR.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Texte affiché sous la forme « E - &lt;prompt&gt; » (ex. « Ouvrir la porte »).</summary>
        string GetPrompt();

        bool CanInteract();

        void Interact(PlayerRig player);
    }

    /// <summary>Objet pouvant être examiné en détail (touche R / bouton X).</summary>
    public interface IExaminable
    {
        void Examine(PlayerRig player);
    }
}

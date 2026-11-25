namespace LastEvidence
{
    /// <summary>Composant dont l'état doit être sauvegardé. Doit se trouver sur un GameObject portant un PersistentObject.</summary>
    public interface ISaveable
    {
        /// <summary>Retourne l'état sérialisé du composant (chaîne libre, souvent JSON ou mot-clé).</summary>
        string CaptureState();

        /// <summary>Restaure l'état sans déclencher d'effets (sons, notifications).</summary>
        void RestoreState(string state);
    }
}

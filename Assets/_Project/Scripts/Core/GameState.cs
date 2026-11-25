namespace LastEvidence
{
    /// <summary>État global de la partie, utilisé pour bloquer les déplacements et gérer le curseur.</summary>
    public enum GameState
    {
        Playing,
        InMenu,
        Inspecting,
        Paused,
        Ended
    }
}

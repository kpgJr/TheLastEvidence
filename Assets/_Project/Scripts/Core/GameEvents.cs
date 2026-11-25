using System;

namespace LastEvidence
{
    /// <summary>
    /// Bus d'événements statique : permet aux systèmes de communiquer sans dépendre
    /// directement les uns des autres (inventaire, preuves, progression, UI, audio...).
    /// Les abonnés doivent se désabonner dans OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<ItemData> ItemAdded;
        public static event Action<ItemData> ItemRemoved;
        public static event Action<EvidenceData> EvidenceDiscovered;
        public static event Action<string> PuzzleSolved;
        public static event Action<string> PuzzleFailed;
        public static event Action<string> FlagSet;
        public static event Action StateRestored;
        public static event Action<string> Notification;
        public static event Action<string> ObjectiveChanged;
        public static event Action<string, int> HintRevealed;
        public static event Action<GameState> GameStateChanged;
        public static event Action<string> InteractionPromptChanged;
        public static event Action JournalUpdated;
        public static event Action<string> BoardChanged;

        public static void RaiseItemAdded(ItemData item) => ItemAdded?.Invoke(item);
        public static void RaiseItemRemoved(ItemData item) => ItemRemoved?.Invoke(item);
        public static void RaiseEvidenceDiscovered(EvidenceData evidence) => EvidenceDiscovered?.Invoke(evidence);
        public static void RaisePuzzleSolved(string puzzleId) => PuzzleSolved?.Invoke(puzzleId);
        public static void RaisePuzzleFailed(string puzzleId) => PuzzleFailed?.Invoke(puzzleId);
        public static void RaiseFlagSet(string flag) => FlagSet?.Invoke(flag);
        public static void RaiseStateRestored() => StateRestored?.Invoke();
        public static void RaiseNotification(string message) => Notification?.Invoke(message);
        public static void RaiseObjectiveChanged(string objective) => ObjectiveChanged?.Invoke(objective);
        public static void RaiseHintRevealed(string puzzleId, int level) => HintRevealed?.Invoke(puzzleId, level);
        public static void RaiseGameStateChanged(GameState state) => GameStateChanged?.Invoke(state);
        public static void RaiseInteractionPrompt(string prompt) => InteractionPromptChanged?.Invoke(prompt);
        public static void RaiseJournalUpdated() => JournalUpdated?.Invoke();
        public static void RaiseBoardChanged(string evidenceId) => BoardChanged?.Invoke(evidenceId);

        /// <summary>Supprime tous les abonnés (utilisé par les tests et lors d'un changement de scène).</summary>
        public static void ClearAll()
        {
            ItemAdded = null; ItemRemoved = null; EvidenceDiscovered = null; PuzzleSolved = null;
            PuzzleFailed = null; FlagSet = null; StateRestored = null; Notification = null;
            ObjectiveChanged = null; HintRevealed = null; GameStateChanged = null;
            InteractionPromptChanged = null; JournalUpdated = null; BoardChanged = null;
        }
    }
}

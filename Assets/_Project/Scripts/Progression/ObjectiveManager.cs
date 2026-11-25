using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastEvidence
{
    [Serializable]
    public class Objective
    {
        public string id;
        [TextArea(1, 3)] public string text;
        [Tooltip("Drapeau qui termine l'objectif.")]
        public string completionFlag;
    }

    /// <summary>Liste ordonnée des objectifs ; l'objectif courant est le premier non terminé.</summary>
    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }

        [SerializeField] private List<Objective> objectives = new List<Objective>();

        private readonly HashSet<string> completed = new HashSet<string>();

        public IReadOnlyList<Objective> Objectives => objectives;
        public Objective Current { get; private set; }

        public void SetObjectives(List<Objective> list) => objectives = list;

        private void Awake() => Instance = this;

        private void OnEnable()
        {
            GameEvents.FlagSet += OnFlagSet;
            GameEvents.StateRestored += Refresh;
        }

        private void OnDisable()
        {
            GameEvents.FlagSet -= OnFlagSet;
            GameEvents.StateRestored -= Refresh;
            if (Instance == this) Instance = null;
        }

        private void OnFlagSet(string flag) => Refresh();

        public bool IsCompleted(Objective o) => completed.Contains(o.id);

        public void Refresh()
        {
            var pm = ProgressionManager.Instance;
            if (pm != null)
                foreach (var o in objectives)
                    if (pm.HasFlag(o.completionFlag)) completed.Add(o.id);

            Objective next = objectives.Find(o => !completed.Contains(o.id));
            if (next != Current)
            {
                Current = next;
                GameEvents.RaiseObjectiveChanged(next != null ? next.text : "Enquête terminée");
                GameEvents.RaiseJournalUpdated();
            }
        }

        public List<string> GetCompletedIds() => new List<string>(completed);

        public void RestoreCompleted(List<string> ids)
        {
            completed.Clear();
            if (ids != null) foreach (var id in ids) completed.Add(id);
            Current = null;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LastEvidence
{
    /// <summary>
    /// Sauvegarde locale en JSON dans Application.persistentDataPath.
    /// Gère la sauvegarde manuelle, automatique (après chaque étape importante),
    /// le chargement et les fichiers absents ou corrompus.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public const string FileName = "savegame.json";

        public static SaveManager Instance { get; private set; }

        /// <summary>Vrai pendant la restauration : les systèmes ne doivent pas jouer de sons ni notifier.</summary>
        public static bool IsRestoring { get; private set; }

        [SerializeField] private bool autosaveOnProgress = true;
        [SerializeField] private float autosaveDelay = 0.75f;

        private Coroutine pendingAutosave;

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool HasSave()
        {
            return TryReadFile(out _);
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
            }
            catch (Exception e) { Debug.LogWarning($"Impossible de supprimer la sauvegarde : {e.Message}"); }
        }

        public static bool TryReadFile(out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(SavePath)) return false;
                string json = File.ReadAllText(SavePath);
                data = Deserialize(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Sauvegarde illisible ({e.Message}).");
                return false;
            }
        }

        public static string Serialize(SaveData data) => JsonUtility.ToJson(data, true);

        /// <summary>Retourne null si le JSON est vide, corrompu ou d'une version inconnue.</summary>
        public static SaveData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || !data.IsValid() || data.version > SaveData.CurrentVersion) return null;
                return data;
            }
            catch (Exception) { return null; }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.FlagSet += OnFlagSet;
        }

        private void OnDisable()
        {
            GameEvents.FlagSet -= OnFlagSet;
            if (Instance == this) Instance = null;
        }

        private IEnumerator Start()
        {
            // On attend une image : tous les gestionnaires et le rig joueur sont initialisés.
            yield return null;
            if (SceneLoader.ContinueRequested)
            {
                SceneLoader.ContinueRequested = false;
                if (TryReadFile(out var data)) ApplySaveData(data);
                else GameEvents.RaiseNotification("Sauvegarde absente ou corrompue : nouvelle partie.");
            }
            GameEvents.RaiseStateRestored();
        }

        private void OnFlagSet(string flag)
        {
            if (!autosaveOnProgress || IsRestoring) return;
            if (pendingAutosave != null) StopCoroutine(pendingAutosave);
            pendingAutosave = StartCoroutine(AutosaveRoutine());
        }

        private IEnumerator AutosaveRoutine()
        {
            yield return new WaitForSecondsRealtime(autosaveDelay);
            pendingAutosave = null;
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Ended) yield break;
            if (Save()) GameEvents.RaiseNotification("Sauvegarde automatique");
        }

        public bool Save()
        {
            try
            {
                var data = CaptureSaveData();
                string json = Serialize(data);
                string tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, json);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(tempPath, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Échec de la sauvegarde : {e.Message}");
                GameEvents.RaiseNotification("Échec de la sauvegarde");
                return false;
            }
        }

        public SaveData CaptureSaveData()
        {
            var data = new SaveData { savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm") };

            var rig = PlayerRig.Current;
            if (rig != null)
            {
                rig.GetPose(out Vector3 pos, out float yaw, out float pitch);
                data.playerX = pos.x; data.playerY = pos.y; data.playerZ = pos.z;
                data.playerYaw = yaw; data.playerPitch = pitch;
            }

            if (InventoryManager.Instance != null) data.items = InventoryManager.Instance.Inventory.GetIds();
            if (EvidenceManager.Instance != null) data.evidence = EvidenceManager.Instance.GetIds();
            if (ProgressionManager.Instance != null) data.flags = ProgressionManager.Instance.Flags.ToList();
            if (ObjectiveManager.Instance != null) data.completedObjectives = ObjectiveManager.Instance.GetCompletedIds();
            if (HintManager.Instance != null) data.hints = HintManager.Instance.Capture();
            if (InvestigationManager.Instance != null)
            {
                data.boardLinks = InvestigationManager.Instance.CaptureLinks();
                data.accusationAttempts = InvestigationManager.Instance.AccusationAttempts;
            }
            if (GameTimer.Instance != null) data.playTime = GameTimer.Instance.Elapsed;

            foreach (var persistent in FindObjectsByType<PersistentObject>(FindObjectsInactive.Include))
            {
                foreach (var saveable in persistent.GetComponents<ISaveable>())
                {
                    string key = persistent.PersistentId + ":" + saveable.GetType().Name;
                    data.objectStates.Add(new KeyValueEntry(key, saveable.CaptureState()));
                }
            }
            return data;
        }

        public void ApplySaveData(SaveData data)
        {
            IsRestoring = true;
            try
            {
                var db = GameDatabase.Instance;
                InventoryManager.Instance?.RestoreIds(data.items, db);
                EvidenceManager.Instance?.RestoreIds(data.evidence, db);
                ProgressionManager.Instance?.RestoreFlags(data.flags);
                ObjectiveManager.Instance?.RestoreCompleted(data.completedObjectives);
                HintManager.Instance?.Restore(data.hints);
                InvestigationManager.Instance?.RestoreLinks(data.boardLinks, data.accusationAttempts);
                if (GameTimer.Instance != null) GameTimer.Instance.Elapsed = data.playTime;

                var states = new Dictionary<string, string>();
                foreach (var entry in data.objectStates) states[entry.key] = entry.value;
                foreach (var persistent in FindObjectsByType<PersistentObject>(FindObjectsInactive.Include))
                {
                    foreach (var saveable in persistent.GetComponents<ISaveable>())
                    {
                        string key = persistent.PersistentId + ":" + saveable.GetType().Name;
                        if (states.TryGetValue(key, out string state)) saveable.RestoreState(state);
                    }
                }

                PlayerRig.Current?.SetPose(new Vector3(data.playerX, data.playerY, data.playerZ), data.playerYaw, data.playerPitch);
            }
            finally
            {
                IsRestoring = false;
            }
        }
    }
}

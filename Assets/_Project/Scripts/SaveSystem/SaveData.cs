using System;
using System.Collections.Generic;

namespace LastEvidence
{
    [Serializable]
    public class KeyValueEntry
    {
        public string key;
        public string value;
        public KeyValueEntry() { }
        public KeyValueEntry(string key, string value) { this.key = key; this.value = value; }
    }

    /// <summary>Contenu complet d'une sauvegarde, sérialisé en JSON avec JsonUtility.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string savedAt;
        public float playerX, playerY, playerZ;
        public float playerYaw, playerPitch;
        public float playTime;
        public List<string> items = new List<string>();
        public List<string> evidence = new List<string>();
        public List<string> flags = new List<string>();
        public List<string> completedObjectives = new List<string>();
        public List<KeyValueEntry> hints = new List<KeyValueEntry>();
        public List<KeyValueEntry> objectStates = new List<KeyValueEntry>();
        public List<KeyValueEntry> boardLinks = new List<KeyValueEntry>();
        public int accusationAttempts;

        public bool IsValid() => version > 0 && items != null && evidence != null && flags != null;
    }
}

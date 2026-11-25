using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LastEvidence.Tests
{
    public class CoreLogicTests
    {
        private static ItemData MakeItem(string id, bool quest)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.id = id; item.displayName = id; item.isQuestItem = quest;
            return item;
        }

        [Test]
        public void FlagStore_SetHasAndHasAll()
        {
            var flags = new FlagStore();
            Assert.IsTrue(flags.Set("a"));
            Assert.IsFalse(flags.Set("a"), "Un drapeau ne doit être ajouté qu'une fois.");
            flags.Set("b");
            Assert.IsTrue(flags.Has("a"));
            Assert.IsTrue(flags.HasAll(new[] { "a", "b" }));
            Assert.IsFalse(flags.HasAll(new[] { "a", "c" }));
            Assert.IsTrue(flags.HasAll(null));
        }

        [Test]
        public void Inventory_RejectsDuplicatesAndProtectsQuestItems()
        {
            var inv = new Inventory();
            var key = MakeItem("key", true);
            var junk = MakeItem("junk", false);
            Assert.IsTrue(inv.Add(key));
            Assert.IsFalse(inv.Add(key));
            Assert.IsTrue(inv.Add(junk));
            Assert.IsFalse(inv.Remove(key), "Un objet d'enquête ne doit pas pouvoir être supprimé.");
            Assert.IsTrue(inv.Remove(junk));
            Assert.AreEqual(1, inv.Count);
            CollectionAssert.AreEqual(new List<string> { "key" }, inv.GetIds());
        }

        [Test]
        public void PuzzleChecks_ValidateSolutions()
        {
            Assert.IsTrue(KeypadPuzzle.Check("1403", "1403"));
            Assert.IsFalse(KeypadPuzzle.Check("1430", "1403"));
            Assert.IsTrue(PasswordPuzzle.Check(" rex2009 ", "REX2009"));
            Assert.IsFalse(PasswordPuzzle.Check("rex1998", "REX2009"));

            var slots = new List<FuseSlot>
            {
                new FuseSlot { correctValueIndex = 1 }, new FuseSlot { correctValueIndex = 2 }
            };
            Assert.IsTrue(ElectricalPuzzle.Check(new[] { 1, 2 }, slots));
            Assert.IsFalse(ElectricalPuzzle.Check(new[] { 2, 1 }, slots));
            Assert.IsFalse(ElectricalPuzzle.Check(new[] { 1 }, slots));

            var events = new List<TimelineEvent>
            {
                new TimelineEvent { id = "a" }, new TimelineEvent { id = "b" }, new TimelineEvent { id = "c" }
            };
            Assert.AreEqual(3, TimelinePuzzle.CountCorrect(new[] { "a", "b", "c" }, events));
            Assert.AreEqual(1, TimelinePuzzle.CountCorrect(new[] { "a", "c", "b" }, events));
        }

        [Test]
        public void SaveData_RoundTripAndCorruptedFiles()
        {
            var data = new SaveData { playerX = 1.5f, playTime = 42f };
            data.items.Add("key_office");
            data.flags.Add("puzzle.safe");
            data.objectStates.Add(new KeyValueEntry("door.door_office:DoorController", "UO"));
            var loaded = SaveManager.Deserialize(SaveManager.Serialize(data));
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1.5f, loaded.playerX);
            Assert.AreEqual(42f, loaded.playTime);
            CollectionAssert.Contains(loaded.items, "key_office");
            Assert.AreEqual("UO", loaded.objectStates[0].value);

            Assert.IsNull(SaveManager.Deserialize(""));
            Assert.IsNull(SaveManager.Deserialize("{ ceci n'est pas du json"));
            Assert.IsNull(SaveManager.Deserialize("{\"version\": 999}"));
        }
    }

    public class CaseDataTests
    {
        private GameDatabase db;

        [SetUp]
        public void Load()
        {
            db = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);
            Assert.IsNotNull(db, "GameDatabase introuvable : lancez le générateur (menu The Last Evidence).");
        }

        [Test]
        public void Database_HasCompleteCase()
        {
            Assert.GreaterOrEqual(db.evidence.Count, 15, "Le cahier des charges impose au moins 15 preuves.");
            Assert.AreEqual(3, db.suspects.Count);
            var ids = new HashSet<string>();
            foreach (var e in db.evidence) Assert.IsTrue(ids.Add(e.id), $"Identifiant de preuve dupliqué : {e.id}");
            Assert.IsNotNull(db.caseData.culprit);
            Assert.GreaterOrEqual(db.caseData.keyEvidence.Length, db.caseData.minimumKeyEvidence);
            foreach (var e in db.caseData.keyEvidence) Assert.IsTrue(db.evidence.Contains(e));
            Assert.IsNotNull(db.FindItem("key_office"));
        }

        [Test]
        public void Accusation_ThreeEndings()
        {
            var c = db.caseData;
            var key = new List<string>();
            foreach (var e in c.keyEvidence) key.Add(e.id);

            Assert.AreEqual(AccusationOutcome.Good,
                AccusationEvaluator.Evaluate(c, c.culprit.id, c.correctMotiveIndex, key.GetRange(0, c.minimumKeyEvidence), out _));
            Assert.AreEqual(AccusationOutcome.Incomplete,
                AccusationEvaluator.Evaluate(c, c.culprit.id, c.correctMotiveIndex, key.GetRange(0, c.minimumKeyEvidence - 1), out _));
            Assert.AreEqual(AccusationOutcome.Incomplete,
                AccusationEvaluator.Evaluate(c, c.culprit.id, (c.correctMotiveIndex + 1) % c.motives.Length, key, out _));
            Assert.AreEqual(AccusationOutcome.Bad,
                AccusationEvaluator.Evaluate(c, "claire", c.correctMotiveIndex, key, out _));
        }
    }
}

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastEvidence.Tests
{
    /// <summary>
    /// Parcours complet de l'enquête en Play Mode (mode bureau) : vérifie que les zones se débloquent
    /// dans l'ordre, que les six énigmes se résolvent, que la sauvegarde fonctionne et que la fin se charge.
    /// </summary>
    public class PlaythroughTests
    {
        private PlayerRig rig;

        [UnitySetUp]
        public IEnumerator LoadInvestigation()
        {
            PlatformMode.Override(false);
            SaveManager.DeleteSave();
            SceneLoader.ContinueRequested = false;
            SceneManager.LoadScene(SceneLoader.InvestigationScene);
            for (int i = 0; i < 10; i++) yield return null;
            rig = PlayerRig.Current;
            Assert.IsNotNull(rig, "Le rig joueur n'a pas été créé.");
            Assert.IsInstanceOf<PlayerController>(rig);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            SaveManager.DeleteSave();
            PlatformMode.Override(null);
            yield return null;
        }

        private static T Find<T>(System.Func<T, bool> predicate) where T : Object =>
            Object.FindObjectsByType<T>(FindObjectsInactive.Include).FirstOrDefault(predicate);

        private static DoorController Door(string id) => Find<DoorController>(d => d.DoorId == id);
        private static PickupItem Pickup(string itemId) => Find<PickupItem>(p => p.Item != null && p.Item.id == itemId);
        private static DocumentObject Doc(string name) => Find<DocumentObject>(d => d.gameObject.name == name);
        private static T Puzzle<T>(string id) where T : PuzzleBase => Find<T>(p => p.PuzzleId == id);
        private static bool Flag(string f) => ProgressionManager.Instance.HasFlag(f);

        private void Read(DocumentObject doc)
        {
            Assert.IsNotNull(doc);
            doc.Interact(rig);
            UIManager.Instance.CloseAll();
        }

        [UnityTest]
        public IEnumerator FullPlaythrough_GoodEnding()
        {
            // --- Zone 1 : hall
            Read(Doc("Doc_message"));
            Assert.IsTrue(EvidenceManager.Instance.Has("ev_message"));
            Assert.AreEqual("o2", ObjectiveManager.Instance.Current.id);

            var officeDoor = Door("door_office");
            officeDoor.Interact(rig);
            Assert.IsTrue(officeDoor.IsLocked, "La porte du bureau ne doit pas s'ouvrir sans clé.");
            Assert.IsNotNull(officeDoor.GetComponent<Collider>(), "Une porte fermée doit bloquer le joueur.");

            Find<DrawerController>(d => d.gameObject.name == "ClockFace").Interact(rig);
            Pickup("key_office").Interact(rig);
            Assert.IsTrue(InventoryManager.Instance.Has("key_office"));
            Assert.IsTrue(Flag("puzzle.key"), "Énigme 1 (clé) non résolue.");
            officeDoor.Interact(rig);
            Assert.IsFalse(officeDoor.IsLocked);
            Assert.IsTrue(officeDoor.IsOpen);

            // --- Zone 2 : bureau
            Assert.IsTrue(Door("door_archives").IsLocked);
            var safe = Puzzle<KeypadPuzzle>("safe");
            Assert.IsFalse(safe.Submit("0000"), "Un mauvais code ne doit pas ouvrir le coffre.");
            Assert.IsFalse(Flag("puzzle.safe"));
            Assert.IsTrue(safe.Submit("1403"));
            yield return null;
            Assert.IsTrue(Find<DrawerController>(d => d.gameObject.name == "SafeDoor").IsOpen, "Le coffre doit s'ouvrir.");
            Read(Doc("Doc_audit"));
            Read(Doc("Doc_pwd_note"));
            Assert.IsTrue(InventoryManager.Instance.Has("password_note"));
            Read(Doc("Doc_agenda"));
            Find<InspectableObject>(i => i.gameObject.name == "Photo_family").OnClueRevealed();
            Find<InspectableObject>(i => i.gameObject.name == "WhiskyGlass").OnClueRevealed();

            var computer = Puzzle<PasswordPuzzle>("computer");
            Assert.IsFalse(computer.Submit("motdepasse"));
            Assert.IsTrue(computer.Submit("rex2009"));
            for (int i = 0; i < computer.EmailCount; i++) computer.ReadEmail(i);
            yield return null;
            Assert.IsFalse(Door("door_archives").IsLocked, "L'ordinateur doit déverrouiller la porte des archives.");

            // --- Zone 3 : archives
            Read(Doc("Doc_transfers"));
            Find<InspectableObject>(i => i.gameObject.name == "Photo_gala").OnClueRevealed();
            Pickup("fuse").Interact(rig);
            Pickup("key_surveillance").Interact(rig);
            var survDoor = Door("door_surveillance");
            survDoor.Interact(rig);
            Assert.IsFalse(survDoor.IsLocked);

            // --- Zone 4 : surveillance
            var registry = Find<DocumentObject>(d => d.gameObject.name == "Monitor_Registry");
            registry.Interact(rig);
            UIManager.Instance.CloseAll();
            Assert.IsFalse(EvidenceManager.Instance.Has("ev_access_log"), "Sans courant, le registre est inaccessible.");

            var power = Puzzle<ElectricalPuzzle>("power");
            Assert.IsFalse(power.Submit(new[] { 0, 0, 0, 0 }));
            Assert.IsTrue(power.Submit(power.CorrectConfiguration));
            yield return null;
            Read(registry);
            Read(Find<DocumentObject>(d => d.gameObject.name == "Monitor_Cameras"));
            Read(Find<DocumentObject>(d => d.gameObject.name == "Recorder"));
            Assert.IsTrue(EvidenceManager.Instance.Has("ev_access_log"));

            var timeline = Puzzle<TimelinePuzzle>("timeline");
            var wrong = timeline.CorrectOrderIds; wrong.Reverse();
            Assert.IsFalse(timeline.Submit(wrong));
            Assert.IsTrue(timeline.Submit(timeline.CorrectOrderIds));
            yield return null;
            Assert.IsTrue(Find<DrawerController>(d => d.gameObject.name == "SecretPanel").IsOpen, "Le passage secret doit s'ouvrir.");
            // Le passage doit aussi être physiquement praticable (aucun meuble devant l'ouverture).
            yield return new WaitForSeconds(0.8f);
            Physics.SyncTransforms();
            var blockers = Physics.OverlapBox(new Vector3(9f, 1.0f, 16f), new Vector3(0.3f, 0.7f, 1.2f), Quaternion.identity,
                    ~0, QueryTriggerInteraction.Ignore)
                .Where(col => col.GetComponentInParent<PlayerRig>() == null)
                .Select(col => col.gameObject.name).ToArray();
            Assert.IsEmpty(blockers, "Le passage secret est obstrué par : " + string.Join(", ", blockers));

            // --- Zone 5 : pièce secrète
            Read(Find<DocumentObject>(d => d.gameObject.name == "HiddenCameraRecorder"));

            // Sauvegarde intermédiaire
            Assert.IsTrue(SaveManager.Instance.Save());
            Assert.IsTrue(SaveManager.HasSave());
            Assert.IsTrue(SaveManager.TryReadFile(out var saved));
            CollectionAssert.Contains(saved.flags, "puzzle.timeline");
            CollectionAssert.Contains(saved.items, "fuse");

            // Tableau d'enquête : associer les preuves décisives à Marc.
            foreach (var id in new[] { "ev_audit", "ev_transfers", "ev_access_log", "ev_hidden_video", "ev_gala_photo" })
            {
                Assert.IsTrue(EvidenceManager.Instance.Has(id), $"Preuve non découverte : {id}");
                InvestigationManager.Instance.Link(id, "marc");
            }
            Assert.IsTrue(Door("door_main").IsLocked);
            var outcome = AccusationManager.Instance.Accuse(0, 0);
            Assert.AreEqual(AccusationOutcome.Good, outcome);
            yield return null;
            Assert.IsFalse(Door("door_main").IsLocked, "La porte principale doit se déverrouiller après l'accusation.");
            Assert.AreEqual(6, PuzzleManager.TotalCount);
            Assert.AreEqual(6, PuzzleManager.SolvedCount);
            foreach (var name in new[] { "Doc_police", "Phone", "Doc_newspaper", "Doc_threat", "Doc_will", "Doc_guard" })
                Read(Find<DocumentObject>(d => d.gameObject.name == name));
            Assert.AreEqual(GameDatabase.Instance.evidence.Count, EvidenceManager.Instance.Count, "Toutes les preuves doivent être trouvables.");

            AccusationManager.Instance.FinishGame();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(SceneLoader.EndingScene, SceneManager.GetActiveScene().name);
            Assert.IsFalse(SaveManager.HasSave(), "La sauvegarde est effacée à la fin de l'enquête.");
        }

        [UnityTest]
        public IEnumerator WrongAccusation_GivesBadEnding_AndSaveRestores()
        {
            Find<DrawerController>(d => d.gameObject.name == "ClockFace").Interact(rig);
            Pickup("key_office").Interact(rig);
            Door("door_office").Interact(rig);
            Assert.IsTrue(SaveManager.Instance.Save());

            // Rechargement : la progression doit être restaurée.
            SceneLoader.ContinueGame();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsTrue(InventoryManager.Instance.Has("key_office"));
            Assert.IsFalse(Door("door_office").IsLocked);
            Assert.IsFalse(Pickup("key_office").gameObject.activeSelf, "La clé ramassée ne doit pas réapparaître.");

            Assert.AreEqual(AccusationOutcome.Bad, AccusationManager.Instance.Accuse(2, 2));

            // Nouvelle partie : tout est réinitialisé.
            SceneLoader.NewGame();
            for (int i = 0; i < 10; i++) yield return null;
            Assert.IsFalse(InventoryManager.Instance.Has("key_office"));
            Assert.IsTrue(Door("door_office").IsLocked);
            Assert.AreEqual(0, EvidenceManager.Instance.Count);
        }
    }
}

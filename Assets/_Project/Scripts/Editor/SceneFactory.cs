using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastEvidence.EditorTools.LevelBuilder;
using M = LastEvidence.EditorTools.MaterialLibrary;

namespace LastEvidence.EditorTools
{
    /// <summary>Construit les trois scènes : MainMenu, Investigation (5 zones) et Ending.</summary>
    public static class SceneFactory
    {
        private static readonly Color Warm = new Color(1f, 0.78f, 0.52f);
        private static readonly Color Cold = new Color(0.7f, 0.82f, 1f);

        // ================================================================== Éléments communs

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.24f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.04f, 0.045f, 0.055f);
        }

        private static VolumeProfile GetVolumeProfile()
        {
            string path = ProjectGenerator.SettingsFolder + "/PostProcess_Profile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.2f);
            color.contrast.Override(12f);
            color.saturation.Override(-18f);
            color.colorFilter.Override(new Color(1f, 0.96f, 0.9f));
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.0f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.18f);
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.ACES);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void CreateGlobalVolume()
        {
            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetVolumeProfile();
            var brightness = go.AddComponent<BrightnessController>();
            SetSerialized(brightness, "volume", volume);
        }

        private static GameObject CreateUIRoot(bool gameplay)
        {
            var ui = new GameObject("UI");
            ui.AddComponent<UIRoot>();
            ui.AddComponent<UIManager>();
            ui.AddComponent<ConfirmDialog>();
            ui.AddComponent<SettingsUI>();
            if (gameplay)
            {
                ui.AddComponent<HUDManager>();
                ui.AddComponent<NotificationManager>();
                ui.AddComponent<DocumentUI>();
                ui.AddComponent<InventoryUI>();
                ui.AddComponent<InvestigationJournal>();
                ui.AddComponent<EvidenceBoard>();
                ui.AddComponent<PauseMenuUI>();
            }
            return ui;
        }

        private static void PlayerSpawn(Vector3 position, float yaw, bool gameplay)
        {
            var spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var boot = spawn.AddComponent<PlayerRigBootstrap>();
            SetSerialized(boot, "gameplayRig", gameplay);
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string name)
        {
            EditorSceneManager.SaveScene(scene, $"{ProjectGenerator.ScenesFolder}/{name}.unity");
        }

        /// <summary>Petit décor de bureau sombre servant d'arrière-plan aux menus.</summary>
        private static void MenuBackdrop(Transform parent)
        {
            Floor("Floor", parent, -4, 4, -4, 4, M.FloorWood, SurfaceType.Wood);
            WallX(parent, 4, -4, 4, M.Wall);
            WallX(parent, -4, -4, 4, M.Wall);
            WallZ(parent, -4, -4, 4, M.Wall);
            WallZ(parent, 4, -4, 4, M.Wall);
            Box("Window", parent, new Vector3(0, 1.7f, 3.88f), new Vector3(1.6f, 1.2f, 0.02f), M.Window, false);
            Box("Desk", parent, new Vector3(0, 0.39f, 2.4f), new Vector3(1.8f, 0.78f, 0.8f), M.WoodDark);
            Box("Lamp", parent, new Vector3(0.6f, 0.95f, 2.4f), new Vector3(0.25f, 0.2f, 0.25f), M.LampShade, false);
            PointLight("DeskLamp", parent, new Vector3(0.6f, 1.3f, 2.3f), Warm, 2.2f, 5f);
            PointLight("Fill", parent, new Vector3(0, 2.6f, 0), Cold, 0.5f, 8f);
            var rain = Group("RainSound", parent);
            rain.transform.position = new Vector3(0, 1.7f, 3.7f);
            rain.AddComponent<AmbientSoundSource>().Configure(AmbienceType.Rain, 0.45f, 12f);
        }

        private static GameObject Systems(bool gameplay)
        {
            var systems = new GameObject("Systems");
            systems.AddComponent<RenderQualityAdapter>();
            systems.AddComponent<SceneAudioStarter>();
            return systems;
        }

        // ================================================================== Menu et fin

        public static void CreateMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();
            Systems(false);
            MenuBackdrop(Group("Environment").transform);
            CreateGlobalVolume();
            PlayerSpawn(new Vector3(0, 0, 0), 0, false);
            CreateUIRoot(false).AddComponent<MainMenuUI>();
            SaveScene(scene, "MainMenu");
        }

        public static void CreateEnding()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();
            Systems(false);
            MenuBackdrop(Group("Environment").transform);
            CreateGlobalVolume();
            PlayerSpawn(new Vector3(0, 0, 0), 0, false);
            CreateUIRoot(false).AddComponent<EndingUI>();
            SaveScene(scene, "Ending");
        }

        // ================================================================== Enquête

        public static void CreateInvestigation(GeneratedContent c)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLighting();

            var systems = Systems(true);
            systems.AddComponent<GameManager>();
            systems.AddComponent<ProgressionManager>();
            var objectives = systems.AddComponent<ObjectiveManager>();
            objectives.SetObjectives(CreateObjectives());
            systems.AddComponent<InventoryManager>();
            systems.AddComponent<EvidenceManager>();
            systems.AddComponent<InvestigationManager>().SetCase(c.caseData);
            systems.AddComponent<HintManager>();
            systems.AddComponent<PuzzleManager>();
            systems.AddComponent<GameTimer>();
            systems.AddComponent<SaveManager>();
            systems.AddComponent<ObjectInspectionManager>();

            CreateGlobalVolume();
            CreateUIRoot(true);
            PlayerSpawn(new Vector3(0, 0, 1.6f), 0, true);

            var env = Group("Environment").transform;
            var puzzles = Group("Puzzles").transform;
            BuildShell(env);
            BuildHall(Group("Zone1_Hall", env).transform, c, puzzles);
            BuildOffice(Group("Zone2_Office", env).transform, c, puzzles);
            BuildArchives(Group("Zone3_Archives", env).transform, c);
            BuildSurveillance(Group("Zone4_Surveillance", env).transform, c, puzzles);
            BuildSecretRoom(Group("Zone5_SecretRoom", env).transform, c, puzzles);

            SaveScene(scene, "Investigation");
        }

        private static List<Objective> CreateObjectives()
        {
            Objective O(string id, string text, string flag) => new Objective { id = id, text = text, completionFlag = flag };
            return new List<Objective>
            {
                O("o1", "Lire le message anonyme laissé dans le hall", "evidence.ev_message"),
                O("o2", "Trouver la clé du bureau de la victime", "item.key_office"),
                O("o3", "Entrer dans le bureau de Victor Lemaire", "door.door_office"),
                O("o4", "Ouvrir le coffre-fort du bureau (code à 4 chiffres)", "puzzle.safe"),
                O("o5", "Déverrouiller l'ordinateur de Victor", "puzzle.computer"),
                O("o6", "Fouiller la salle des archives", "item.key_surveillance"),
                O("o7", "Entrer dans la salle de surveillance", "door.door_surveillance"),
                O("o8", "Rétablir le courant (boîte à fusibles)", "puzzle.power"),
                O("o9", "Consulter les preuves et reconstituer la chronologie", "puzzle.timeline"),
                O("o10", "Désigner le coupable depuis la pièce secrète", AccusationManager.CaseClosedFlag),
                O("o11", "Quitter le bâtiment par la porte principale", "game.exit"),
            };
        }

        private static T Puzzle<T>(Transform parent, string objectName, string id, string name, string solvedMessage,
            params string[] hints) where T : PuzzleBase
        {
            var go = Group(objectName, parent);
            var p = go.AddComponent<T>();
            p.ConfigureBase(id, name, hints, solvedMessage);
            return p;
        }

        private static void Terminal(GameObject go, PuzzleBase puzzle, string prompt, string solvedPrompt,
            string[] requires = null, string unavailablePrompt = null, string unavailableMessage = null)
        {
            var t = Interactive<PuzzleTerminal>(go);
            t.Configure(puzzle, prompt, solvedPrompt);
            if (requires != null) t.SetAvailability(requires, unavailablePrompt, unavailableMessage);
        }

        private static void Table(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
        {
            var root = Group(name, parent).transform;
            Box("Top", root, center + Vector3.up * (size.y - 0.025f), new Vector3(size.x, 0.05f, size.z), mat);
            float lx = size.x / 2 - 0.05f, lz = size.z / 2 - 0.05f;
            foreach (var o in new[] { new Vector3(lx, 0, lz), new Vector3(-lx, 0, lz), new Vector3(lx, 0, -lz), new Vector3(-lx, 0, -lz) })
                Box("Leg", root, center + o + Vector3.up * (size.y / 2 - 0.025f), new Vector3(0.06f, size.y - 0.05f, 0.06f), mat, false);
            // Un seul collider simple pour les pieds.
            var block = Box("Collider", root, center + Vector3.up * ((size.y - 0.05f) / 2), new Vector3(size.x, size.y - 0.05f, size.z), mat);
            Object.DestroyImmediate(block.GetComponent<Renderer>());
            Object.DestroyImmediate(block.GetComponent<MeshFilter>());
        }

        private static void Chair(Transform parent, Vector3 pos, float yaw, Material mat)
        {
            var root = Group("Chair", parent).transform;
            root.position = pos;
            root.rotation = Quaternion.Euler(0, yaw, 0);
            Box("Seat", root, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.06f, 0.5f), mat, true, 0, true);
            Box("Back", root, new Vector3(0, 0.75f, 0.22f), new Vector3(0.5f, 0.6f, 0.06f), mat, true, 0, true);
            Box("Base", root, new Vector3(0, 0.22f, 0), new Vector3(0.08f, 0.44f, 0.08f), M.MetalDark, false, 0, true);
        }

        private static void Window(Transform parent, Vector3 pos, float yaw)
        {
            Box("Window", parent, pos, new Vector3(1.4f, 1.1f, 0.02f), M.Window, false, yaw);
            var frame = Box("WindowFrame", parent, pos + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 0.005f), new Vector3(1.5f, 1.2f, 0.01f), M.WoodDark, false, yaw);
            frame.transform.SetSiblingIndex(0);
            Box("Mullion", parent, pos, new Vector3(0.04f, 1.1f, 0.03f), M.WoodDark, false, yaw);
        }

        private static void Ambient(Transform parent, string name, Vector3 pos, AmbienceType type, float volume, float distance, string flag = null)
        {
            var go = Group(name, parent);
            go.transform.position = pos;
            go.AddComponent<AmbientSoundSource>().Configure(type, volume, distance, flag);
        }

        // ------------------------------------------------------------------ Structure

        private static void BuildShell(Transform env)
        {
            var shell = Group("Structure", env).transform;
            // Sols et plafonds
            Floor("Floor_Hall", shell, -6, 6, 0, 8, M.FloorWood, SurfaceType.Wood);
            Floor("Floor_Office", shell, -6, 0, 8, 16, M.Carpet, SurfaceType.Carpet);
            Floor("Floor_Archives", shell, 0, 6, 8, 16, M.FloorTile, SurfaceType.Tile);
            Floor("Floor_Surveillance", shell, 6, 12, 8, 16, M.Concrete, SurfaceType.Concrete);
            Floor("Floor_Secret", shell, 6, 12, 16, 22, M.FloorWood, SurfaceType.Wood);
            Floor("Floor_Porch", shell, -1.5f, 1.5f, -2.5f, 0, M.Concrete, SurfaceType.Concrete);

            // Murs (ouvertures de portes : hall→extérieur x=0, hall→bureau x=-3, bureau→archives z=12, archives→surveillance z=12, passage secret x=9)
            WallX(shell, 0, -6, 6, M.Wall, 0f);
            WallZ(shell, -6, 0, 8, M.Wall);
            WallZ(shell, 6, 0, 8, M.Wall);
            WallX(shell, 8, -6, 6, M.Wall, -3f);
            WallX(shell, 8, 6, 12, M.WallDark);
            WallZ(shell, -6, 8, 16, M.Wall);
            WallZ(shell, 0, 8, 16, M.Wall, 12f);
            WallZ(shell, 6, 8, 16, M.WallDark, 12f);
            WallZ(shell, 12, 8, 22, M.WallDark);
            WallX(shell, 16, -6, 6, M.Wall);
            WallX(shell, 16, 6, 12, M.WallDark, 9f);
            WallZ(shell, 6, 16, 22, M.WallDark);
            WallX(shell, 22, 6, 12, M.WallDark);
            // Porche extérieur
            WallZ(shell, -1.5f, -2.5f, 0, M.Concrete);
            WallZ(shell, 1.5f, -2.5f, 0, M.Concrete);
            WallX(shell, -2.5f, -1.5f, 1.5f, M.Concrete);
        }

        // ------------------------------------------------------------------ Zone 1 : Hall

        private static void BuildHall(Transform z, GeneratedContent c, Transform puzzles)
        {
            // Porte principale (sortie) : se déverrouille quand l'accusation est portée.
            Door(z, "door_main", new Vector3(-0.55f, 0, 0), 0, 100f, true, null, new[] { AccusationManager.CaseClosedFlag },
                "La porte principale est verrouillée de l'extérieur. Il faut d'abord résoudre l'affaire.",
                "La porte principale est déverrouillée. Vous pouvez quitter les lieux.");
            var exit = Box("ExitTrigger", z, new Vector3(0, 1.1f, -1.4f), new Vector3(2.6f, 2.2f, 1.2f), M.Black);
            Object.DestroyImmediate(exit.GetComponent<Renderer>());
            exit.GetComponent<BoxCollider>().isTrigger = true;
            exit.AddComponent<ExitTrigger>();
            Ambient(z, "Rain_Outside", new Vector3(0, 1.5f, -1.5f), AmbienceType.Rain, 0.6f, 14f);
            Ambient(z, "Wind_Outside", new Vector3(-5.5f, 2f, 4f), AmbienceType.Wind, 0.35f, 12f);

            // Porte du bureau (clé).
            Door(z, "door_office", new Vector3(-3.55f, 0, 8), 0, -100f, true, "key_office", null,
                "La porte du bureau est verrouillée. Il vous faut une clé.");

            // Table et message anonyme (tutoriel).
            Table(z, "SmallTable", new Vector3(1.3f, 0, 2.8f), new Vector3(0.8f, 0.75f, 0.5f), M.Wood);
            Paper(z, "message", new Vector3(1.3f, 0.755f, 2.75f), 10f, false, "<b>À l'attention\ndu détective</b>",
                "Message anonyme", c.Ev("ev_message").description, "Lire le message", c.Ev("ev_message"));

            // Tableau d'affichage avec le rapport de police.
            Box("BulletinBoard", z, new Vector3(-5.86f, 1.6f, 3.5f), new Vector3(0.04f, 1.0f, 1.5f), M.Cork);
            Paper(z, "police", new Vector3(-5.83f, 1.6f, 3.2f), -90f, true, "<b>POLICE</b>\nRapport\npréliminaire",
                "Rapport de police préliminaire", c.Ev("ev_police").description, "Lire le rapport de police", c.Ev("ev_police"));
            Paper(z, "notice", new Vector3(-5.83f, 1.7f, 3.95f), -90f, true, "Avis aux locataires :\nascenseur en panne",
                "Avis aux locataires", "L'ascenseur est hors service jusqu'à nouvel ordre. - La direction", "Lire l'avis", null,
                null, new Vector2(0.18f, 0.14f));

            // Horloge murale : un compartiment caché derrière le cadran contient la clé du bureau.
            var clock = Group("WallClock", z).transform;
            clock.position = new Vector3(2.8f, 1.85f, 7.86f);
            Cylinder("ClockBody", clock, new Vector3(0, 0, 0.015f), new Vector3(0.62f, 0.03f, 0.62f), M.WoodDark, false, new Vector3(90, 0, 0), true);
            var hinge = Group("FaceHinge", clock).transform;
            hinge.localPosition = new Vector3(-0.29f, 0, -0.065f);
            var face = Cylinder("ClockFace", hinge, new Vector3(0.29f, 0, 0), new Vector3(0.56f, 0.01f, 0.56f), M.Paper, true, new Vector3(90, 0, 0), true);
            var faceRoot = face.transform;
            Text(faceRoot, "XII", new Vector3(0, 0, 0.0f), new Vector3(-90, 0, 0), new Vector2(0.1f, 0.05f), null, 0.4f).transform.localPosition = new Vector3(0, -1.2f, -0.36f);
            Text(faceRoot, "VI", new Vector3(0, 0, 0.0f), new Vector3(-90, 0, 0), new Vector2(0.1f, 0.05f), null, 0.4f).transform.localPosition = new Vector3(0, -1.2f, 0.36f);
            Text(faceRoot, "IX", new Vector3(0, 0, 0.0f), new Vector3(-90, 0, 0), new Vector2(0.1f, 0.05f), null, 0.4f).transform.localPosition = new Vector3(-0.36f, -1.2f, 0);
            Text(faceRoot, "III", new Vector3(0, 0, 0.0f), new Vector3(-90, 0, 0), new Vector2(0.1f, 0.05f), null, 0.4f).transform.localPosition = new Vector3(0.36f, -1.2f, 0);
            // Aiguilles arrêtées sur 11 h (23 h) : petite aiguille vers XI, grande vers XII.
            var hour = Box("HourHand", hinge, new Vector3(0.29f - 0.0375f, 0.065f, -0.015f), new Vector3(0.015f, 0.15f, 0.005f), M.Black, false, 0, true);
            hour.transform.localRotation = Quaternion.Euler(0, 0, 30);
            Box("MinuteHand", hinge, new Vector3(0.29f, 0.1f, -0.016f), new Vector3(0.01f, 0.2f, 0.005f), M.Black, false, 0, true);
            var clockDrawer = Interactive<DrawerController>(face, "clock.face");
            clockDrawer.Configure(hinge, Vector3.zero, new Vector3(0, 105, 0), "Examiner l'horloge", "Refermer le cadran",
                true, false, true, SfxType.Drawer);
            Ambient(z, "ClockTick", clock.position, AmbienceType.Clock, 0.35f, 6f);
            var key = Box("Key_Office", z, new Vector3(2.8f, 1.85f, 7.83f), new Vector3(0.03f, 0.1f, 0.01f), M.Brass);
            key.GetComponent<BoxCollider>().size = new Vector3(3f, 1.5f, 3f);
            Interactive<PickupItem>(key, "pickup.key_office").Configure(c.Item("key_office"));

            var keyPuzzle = Puzzle<CollectItemPuzzle>(puzzles, "Puzzle1_Key", "key", "La clé du bureau",
                "Énigme résolue : la clé du bureau !",
                "Relisez le message anonyme : il parle du temps.",
                "Un objet du hall est arrêté à l'heure où le corps a été découvert.",
                "Examinez l'horloge murale du hall, à droite de la porte du bureau : le cadran s'ouvre.");
            keyPuzzle.Configure("key_office");

            // Accueil avec tiroir (carte de visite de l'assistante).
            Box("ReceptionDesk", z, new Vector3(-3, 0.5f, 5.2f), new Vector3(1.8f, 1.0f, 0.6f), M.WoodDark);
            var drawer = Drawer(z, "reception", new Vector3(-3, 0.8f, 4.88f), 0, new Vector3(0.5f, 0.14f, 0.4f), out var contents);
            var card = Paper(contents, "card", Vector3.zero, 0, false, "S. Tremblay\nassistante", "Carte de visite",
                "Sophie Tremblay - Assistante de direction, Lemaire Capital.\nAu dos, griffonné : « Ne faites confiance à personne. Tout est resté dans le bâtiment. »",
                "Lire la carte", null, null, new Vector2(0.09f, 0.05f));
            card.transform.localPosition = new Vector3(0, 0.01f, -0.05f);

            // Mobilier et éclairage.
            Box("Bench", z, new Vector3(5.5f, 0.23f, 3f), new Vector3(0.5f, 0.45f, 1.8f), M.Leather);
            var pot = Cylinder("PlantPot", z, new Vector3(-5.4f, 0.25f, 0.6f), new Vector3(0.4f, 0.25f, 0.4f), M.WoodDark);
            Sphere("Plant", pot.transform.parent, new Vector3(-5.4f, 0.85f, 0.6f), 0.8f, M.Plant);
            Box("CoatRack", z, new Vector3(5.5f, 0.9f, 0.6f), new Vector3(0.08f, 1.8f, 0.08f), M.WoodDark);
            Box("Rug", z, new Vector3(0, 0.005f, 3.5f), new Vector3(3f, 0.01f, 4f), M.Carpet, false);
            Window(z, new Vector3(-5.89f, 1.6f, 6.4f), 90);
            Window(z, new Vector3(5.89f, 1.6f, 5.5f), -90);

            var lampRoot = Group("FloorLamp", z).transform;
            var lampBase = Box("LampBase", lampRoot, new Vector3(4.8f, 0.7f, 6.8f), new Vector3(0.12f, 1.4f, 0.12f), M.MetalDark);
            var shade = Cylinder("Shade", lampRoot, new Vector3(4.8f, 1.5f, 6.8f), new Vector3(0.45f, 0.18f, 0.45f), M.LampShade, false);
            var lampLight = PointLight("LampLight", lampRoot, new Vector3(4.8f, 1.6f, 6.8f), Warm, 1.8f, 5f);
            Interactive<SwitchController>(lampBase, "switch.hall_lamp").Configure(new[] { lampLight }, new[] { shade.GetComponent<Renderer>() }, true);

            PointLight("Ceiling_A", z, new Vector3(-2.5f, 2.7f, 3.5f), Warm, 2.2f, 7f);
            PointLight("Ceiling_B", z, new Vector3(2.5f, 2.7f, 4f), Warm, 1.8f, 7f).gameObject.AddComponent<FlickeringLight>();
        }

        // ------------------------------------------------------------------ Zone 2 : Bureau

        private static void BuildOffice(Transform z, GeneratedContent c, Transform puzzles)
        {
            Door(z, "door_archives", new Vector3(0, 0, 11.45f), -90, 100f, true, null, new[] { "puzzle.computer" },
                "Porte verrouillée électroniquement. Elle se commande depuis l'ordinateur de Victor.",
                "Un déclic : l'ordinateur a déverrouillé la porte des archives.");

            // Bureau de Victor
            Box("Desk", z, new Vector3(-3, 0.39f, 13.6f), new Vector3(2.0f, 0.78f, 0.9f), M.WoodDark);
            Chair(z, new Vector3(-3, 0, 14.4f), 180, M.Leather);
            Chair(z, new Vector3(-3.4f, 0, 12.4f), 0, M.Fabric);
            Box("Rug", z, new Vector3(-3, 0.005f, 12.5f), new Vector3(3.2f, 0.01f, 3.2f), M.Fabric, false);

            // Ordinateur (énigme 3)
            var computer = Puzzle<PasswordPuzzle>(puzzles, "Puzzle3_Computer", "computer", "L'ordinateur de Victor",
                "Ordinateur déverrouillé : la porte des archives s'ouvre.",
                "La note trouvée dans le coffre-fort décrit le mot de passe.",
                "Le « fidèle compagnon » de Victor figure sur une photo de son bureau : retournez-la.",
                "Le chien s'appelle Rex et la photo date de l'été 2009 : REX2009.");
            computer.Configure("REX2009", "Victor Lemaire", new List<EmailEntry>
            {
                new EmailEntry { from = "Victor Lemaire → Me Gagnon", subject = "Plainte et dissolution", body = c.Ev("ev_email_lawyer").description, evidence = c.Ev("ev_email_lawyer") },
                new EmailEntry { from = "Julien Roy", subject = "Mes affaires personnelles", body = c.Ev("ev_email_julien").description, evidence = c.Ev("ev_email_julien") },
                new EmailEntry { from = "Sophie Tremblay", subject = "Gala de ce soir", body = "« Je serai au gala avec Marc ce soir. Je prendrai des photos pour la revue interne. Bon courage pour votre rendez-vous de 21 h. - Sophie »" },
                new EmailEntry { from = "Système de sécurité", subject = "Porte des archives", body = "Commande à distance reçue : la porte de la salle des archives est déverrouillée." },
            });
            var monitor = Box("Monitor", z, new Vector3(-3.3f, 1.02f, 13.85f), new Vector3(0.6f, 0.38f, 0.04f), M.Black);
            Box("Screen", monitor.transform, new Vector3(0, 0, -0.6f), new Vector3(0.92f, 0.88f, 0.2f), M.ScreenOn, false, 0, true);
            Box("Stand", z, new Vector3(-3.3f, 0.82f, 13.9f), new Vector3(0.08f, 0.1f, 0.08f), M.Black, false);
            Text(monitor.transform, "Session verrouillée\nV. Lemaire", new Vector3(0, 0, -0.75f), 0, new Vector2(0.5f, 0.3f), new Color(0.8f, 0.9f, 1f), 0.5f);
            Terminal(monitor, computer, "Utiliser l'ordinateur", "Consulter les courriels");

            // Agenda, verre, téléphone, photo du chien, lampe
            Paper(z, "agenda", new Vector3(-2.4f, 0.785f, 13.4f), -8, false, "<b>AGENDA</b>\n14 oct.", "Agenda de Victor - 14 octobre",
                c.Ev("ev_agenda").description, "Lire l'agenda", c.Ev("ev_agenda"), null, new Vector2(0.22f, 0.16f), M.Leather);

            var glass = Cylinder("WhiskyGlass", z, new Vector3(-2.05f, 0.83f, 13.25f), new Vector3(0.07f, 0.05f, 0.07f), M.Glass);
            var residue = Cylinder("Residue", glass.transform, new Vector3(0, -1.01f, 0), new Vector3(0.8f, 0.02f, 0.8f), M.Paper, false, null, true);
            residue.name = "Residue";
            Interactive<InspectableObject>(glass).Configure("Verre de whisky",
                "Le verre dans lequel Victor buvait ce soir-là. Il a été rincé à la hâte… mais pas complètement.",
                null, "Sous la lumière, un fin dépôt de poudre blanche est collé au fond du verre.", Vector3.down, c.Ev("ev_glass"), 0.14f);

            var phone = Box("Phone", z, new Vector3(-3.95f, 0.82f, 13.45f), new Vector3(0.22f, 0.08f, 0.18f), M.Black);
            Box("Light", phone.transform, new Vector3(0.35f, 0.55f, -0.3f), new Vector3(0.1f, 0.1f, 0.1f), M.RedLight, false, 0, true);
            Interactive<DocumentObject>(phone).Configure("Répondeur - 1 nouveau message", c.Ev("ev_claire_voicemail").description,
                "Écouter le répondeur", c.Ev("ev_claire_voicemail"), null, null, SfxType.Computer);

            Photo(z, "dog", new Vector3(-3.75f, 0.86f, 13.8f), 0, "Victor et son chien\nau bord du lac", "« Rex - été 2009 »",
                "Photo encadrée", "Victor, plus jeune, accroupi près d'un grand chien au bord d'un lac.", null);

            var deskLampBase = Box("DeskLamp", z, new Vector3(-2.2f, 0.95f, 13.85f), new Vector3(0.05f, 0.3f, 0.05f), M.Brass);
            var deskShade = Cylinder("DeskLampShade", z, new Vector3(-2.2f, 1.12f, 13.8f), new Vector3(0.2f, 0.08f, 0.2f), M.LampShade, false);
            var deskLight = PointLight("DeskLampLight", z, new Vector3(-2.2f, 1.05f, 13.6f), Warm, 1.6f, 3.5f);
            Interactive<SwitchController>(deskLampBase, "switch.desk_lamp").Configure(new[] { deskLight }, new[] { deskShade.GetComponent<Renderer>() }, true);

            // Tiroir du bureau : indice du coffre.
            var drawer = Drawer(z, "office_desk", new Vector3(-2.4f, 0.65f, 13.14f), 0, new Vector3(0.5f, 0.12f, 0.5f), out var contents);
            var note = Paper(contents, "safe_hint", Vector3.zero, 0, false, "Coffre :\nJJMM", "Note griffonnée",
                "« Le coffre : le jour où tout a commencé. JJMM. »", "Lire la note", null, null, new Vector2(0.12f, 0.09f));
            note.transform.localPosition = new Vector3(0, 0.005f, -0.05f);

            // Plaque commémorative (date de fondation).
            var plaque = Paper(z, "plaque", new Vector3(-4.6f, 1.7f, 15.88f), 0, true,
                "<b>LEMAIRE CAPITAL</b>\nFondée le 14 mars 1998", "Plaque commémorative",
                "LEMAIRE CAPITAL\nFondée le 14 mars 1998 par Victor Lemaire et Marc Dubois.\n« La confiance est notre premier capital. »",
                "Lire la plaque", null, null, new Vector2(0.5f, 0.3f), M.Brass);
            plaque.name = "Plaque";

            // Bibliothèque et photo de famille (verso).
            Box("Bookshelf", z, new Vector3(-5.75f, 1.0f, 10.5f), new Vector3(0.4f, 2.0f, 1.8f), M.WoodDark);
            for (int i = 0; i < 4; i++)
                Box("Books", z, new Vector3(-5.55f, 0.35f + i * 0.45f, 10.5f + (i % 2 == 0 ? 0.2f : -0.25f)), new Vector3(0.05f, 0.3f, 1.0f),
                    i % 2 == 0 ? M.Leather : M.Fabric, false);
            Photo(z, "family", new Vector3(-5.5f, 1.32f, 11.1f), -90, "Les 20 ans de\nLemaire Capital", "« Marc ne supporte plus\nque je vérifie ses comptes. - V. »",
                "Photo des 20 ans de l'entreprise", "Victor, Marc et Claire lèvent leur verre lors de la fête des 20 ans de Lemaire Capital.",
                c.Ev("ev_family_photo"));

            // Coffre-fort (énigme 2)
            var safeRoot = Group("Safe", z).transform;
            safeRoot.position = new Vector3(-1.2f, 0, 15.45f);
            Box("Back", safeRoot, new Vector3(0, 0.4f, 0.27f), new Vector3(0.7f, 0.8f, 0.06f), M.MetalDark, true, 0, true);
            Box("Left", safeRoot, new Vector3(-0.32f, 0.4f, 0), new Vector3(0.06f, 0.8f, 0.6f), M.MetalDark, true, 0, true);
            Box("Right", safeRoot, new Vector3(0.32f, 0.4f, 0), new Vector3(0.06f, 0.8f, 0.6f), M.MetalDark, true, 0, true);
            Box("Top", safeRoot, new Vector3(0, 0.77f, 0), new Vector3(0.7f, 0.06f, 0.6f), M.MetalDark, true, 0, true);
            Box("Bottom", safeRoot, new Vector3(0, 0.03f, 0), new Vector3(0.7f, 0.06f, 0.6f), M.MetalDark, true, 0, true);
            Box("Shelf", safeRoot, new Vector3(0, 0.4f, 0.02f), new Vector3(0.58f, 0.02f, 0.5f), M.MetalDark, false, 0, true);
            var safeHinge = Group("DoorHinge", safeRoot).transform;
            safeHinge.localPosition = new Vector3(-0.35f, 0.4f, -0.31f);
            var safeDoor = Box("SafeDoor", safeHinge, new Vector3(0.35f, 0, 0), new Vector3(0.7f, 0.8f, 0.04f), M.Metal, true, 0, true);
            Cylinder("Dial", safeDoor.transform, new Vector3(0.2f, 0, -0.8f), new Vector3(0.18f, 1.5f, 0.12f), M.Brass, false, new Vector3(90, 0, 0), true);
            var safeDrawer = Interactive<DrawerController>(safeDoor, "safe.door");
            safeDrawer.Configure(safeHinge, Vector3.zero, new Vector3(0, 110, 0), "", "", false, true, false, SfxType.DoorOpen,
                null, "Le coffre-fort s'ouvre.");
            safeDrawer.SetAvailability(new[] { "puzzle.safe" }, "Coffre-fort verrouillé", "Le coffre est fermé. Un clavier numérique est fixé au mur.");

            Paper(safeRoot, "audit", new Vector3(-1.2f, 0.065f, 15.45f), 0, false, "<b>CONFIDENTIEL</b>\nAudit interne", "Rapport d'audit interne",
                c.Ev("ev_audit").description, "Lire le rapport d'audit", c.Ev("ev_audit"));
            Paper(safeRoot, "pwd_note", new Vector3(-1.2f, 0.415f, 15.45f), 0, false, "Note de V.", "Note manuscrite de Victor",
                c.Item("password_note").readableText, "Lire la note", null, c.Item("password_note"), new Vector2(0.14f, 0.1f));

            var keypad = Box("Keypad", z, new Vector3(-0.55f, 1.15f, 15.88f), new Vector3(0.16f, 0.22f, 0.03f), M.Black);
            Text(keypad.transform, "1 2 3\n4 5 6\n7 8 9\n  0", new Vector3(0, 0, -0.6f), 0, new Vector2(0.13f, 0.18f), new Color(0.9f, 0.7f, 0.4f), 0.4f);
            var safePuzzle = Puzzle<KeypadPuzzle>(puzzles, "Puzzle2_Safe", "safe", "Le coffre-fort", "Coffre-fort ouvert !",
                "Une note dans le tiroir du bureau explique comment trouver le code.",
                "« Le jour où tout a commencé » : la création de l'entreprise de Victor.",
                "La plaque au mur indique la fondation le 14 mars 1998. Au format JJMM : 1403.");
            safePuzzle.Configure("1403");
            Terminal(keypad, safePuzzle, "Utiliser le clavier du coffre", "Clavier du coffre (ouvert)");

            Window(z, new Vector3(-5.89f, 1.6f, 13.5f), 90);
            Ambient(z, "Rain_Office", new Vector3(-5.6f, 1.6f, 13.5f), AmbienceType.Rain, 0.35f, 8f);
            PointLight("Ceiling", z, new Vector3(-3, 2.7f, 11.5f), Warm, 1.6f, 7f);
        }

        // ------------------------------------------------------------------ Zone 3 : Archives

        private static void BuildArchives(Transform z, GeneratedContent c)
        {
            Door(z, "door_surveillance", new Vector3(6, 0, 11.45f), -90, 100f, true, "key_surveillance", null,
                "Porte de la salle de surveillance verrouillée. Une clé de gardien doit traîner quelque part.");

            // Classeurs (contre le mur nord), deux tiroirs chacun.
            float[] xs = { 1.4f, 3.0f, 4.6f };
            for (int i = 0; i < xs.Length; i++)
            {
                Box("Cabinet_" + (i + 1), z, new Vector3(xs[i], 0.65f, 15.55f), new Vector3(0.9f, 1.3f, 0.7f), M.Metal);
                Text(z, $"Classeur {i + 1}", Vector3.zero, 0, new Vector2(0.4f, 0.08f), UIStyle.OldBeige, 0.4f)
                    .transform.position = new Vector3(xs[i], 1.35f, 15.19f);
            }
            Transform contents;
            Drawer(z, "cab1_top", new Vector3(1.4f, 0.95f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            Paper(contents, "transfers", Vector3.zero, 0, false, "M.D. Holdings\nvirements", "Dossier « M.D. Holdings »",
                c.Ev("ev_transfers").description, "Lire les relevés", c.Ev("ev_transfers")).transform.localPosition = new Vector3(0, 0.01f, -0.1f);
            Drawer(z, "cab1_bottom", new Vector3(1.4f, 0.45f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            Paper(contents, "invoices", Vector3.zero, 0, false, "Factures\n2023", "Factures diverses",
                "Factures de fournitures de bureau et de maintenance. Rien d'intéressant.", "Lire les factures", null).transform.localPosition = new Vector3(0, 0.01f, -0.1f);

            Drawer(z, "cab2_top", new Vector3(3.0f, 0.95f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            Paper(contents, "will", Vector3.zero, 0, false, "Testament\n(copie)", "Testament de Victor (copie)",
                c.Ev("ev_will").description, "Lire le testament", c.Ev("ev_will")).transform.localPosition = new Vector3(0, 0.01f, -0.1f);
            Drawer(z, "cab2_bottom", new Vector3(3.0f, 0.45f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            var fuse = Cylinder("Fuse", contents, new Vector3(0, 0.03f, -0.1f), new Vector3(0.03f, 0.04f, 0.03f), M.Brass, true, new Vector3(0, 0, 90), true);
            fuse.GetComponent<BoxCollider>().size = new Vector3(3, 2, 3);
            Interactive<PickupItem>(fuse, "pickup.fuse").Configure(c.Item("fuse"));

            Drawer(z, "cab3_top", new Vector3(4.6f, 0.95f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            var key = Box("Key_Surveillance", contents, new Vector3(0, 0.01f, -0.1f), new Vector3(0.12f, 0.01f, 0.03f), M.Brass, true, 0, true);
            key.GetComponent<BoxCollider>().size = new Vector3(1.5f, 4f, 3f);
            Interactive<PickupItem>(key, "pickup.key_surveillance").Configure(c.Item("key_surveillance"));
            Drawer(z, "cab3_bottom", new Vector3(4.6f, 0.45f, 15.19f), 0, new Vector3(0.7f, 0.3f, 0.6f), out contents, 0.45f, M.Metal);
            Paper(contents, "julien_box", Vector3.zero, 0, false, "Affaires\nJ. Roy", "Carton « J. Roy »",
                "Un carton presque vide étiqueté « Affaires personnelles - J. Roy ». Il ne reste qu'une tasse et un vieux badge d'accès désactivé… " +
                "Quelqu'un est déjà venu récupérer le reste.", "Examiner le carton", null).transform.localPosition = new Vector3(0, 0.01f, -0.1f);

            // Mur sud : coupure de journal et calendrier.
            Paper(z, "newspaper", new Vector3(2.0f, 1.6f, 8.11f), 180, true, "<b>LE COURRIER</b>\nLicenciement contesté chez Lemaire Capital",
                "Article de journal", c.Ev("ev_newspaper").description, "Lire l'article", c.Ev("ev_newspaper"), null, new Vector2(0.3f, 0.36f));
            Paper(z, "calendar", new Vector3(4.0f, 1.6f, 8.11f), 180, true, "<b>OCTOBRE</b>\n14 : Gala", "Calendrier d'octobre",
                "14 oct. - Gala de la Chambre de commerce, 20 h - 23 h 30 (Marc et Sophie)\n15 oct. - Réunion du conseil\n17 oct. - Rendez-vous avec Me Gagnon (Victor)",
                "Consulter le calendrier", null, null, new Vector2(0.35f, 0.45f));

            // Table de travail : lettre de menace et photos du gala.
            Table(z, "WorkTable", new Vector3(3, 0, 11.3f), new Vector3(1.6f, 0.78f, 0.9f), M.Wood);
            Paper(z, "threat", new Vector3(2.6f, 0.785f, 11.2f), 15, false, "Vous me\nle paierez.", "Lettre signée J. Roy",
                c.Ev("ev_threat").description, "Lire la lettre", c.Ev("ev_threat"));
            Photo(z, "gala", new Vector3(3.4f, 0.79f, 11.3f), 0, "Gala de la Chambre\nde commerce - table 4",
                "20 h 15 : Marc, smoking et écharpe blanche.\n21 h 30 : sa chaise est vide. - S.T.",
                "Photos du gala", "Tirages du gala de la Chambre de commerce, glissés dans une enveloppe sans nom.", c.Ev("ev_gala_photo"), false);

            // Cartons d'archives
            for (int i = 0; i < 5; i++)
                Box("ArchiveBox", z, new Vector3(5.5f, 0.2f + (i % 2) * 0.4f, 9 + i * 0.5f), new Vector3(0.5f, 0.38f, 0.45f), M.Cork);
            var light = PointLight("Fluorescent", z, new Vector3(3, 2.8f, 12), Cold, 1.6f, 8f);
            light.gameObject.AddComponent<FlickeringLight>();
            Box("FluorescentTube", z, new Vector3(3, 2.95f, 12), new Vector3(1.2f, 0.05f, 0.12f), M.ScreenOn, false);
        }

        // ------------------------------------------------------------------ Zone 4 : Surveillance

        private static void BuildSurveillance(Transform z, GeneratedContent c, Transform puzzles)
        {
            string[] power = { "puzzle.power" };
            const string noPower = "Éteint (pas de courant)";
            const string noPowerMsg = "Pas de courant. La boîte à fusibles est près de la porte.";

            // Boîte à fusibles (énigme 4) et schéma au mur.
            var electrical = Puzzle<ElectricalPuzzle>(puzzles, "Puzzle4_Power", "power", "Le circuit électrique",
                "Courant rétabli ! Les écrans de surveillance s'allument.",
                "Le schéma fixé au mur indique le calibre de chaque circuit.",
                "Un fusible a été retiré. Les archives contiennent des fournitures de rechange.",
                "Éclairage 10 A, Écrans 15 A, Enregistreur 5 A, Serveur vidéo 20 A.");
            electrical.Configure(new[] { "5 A", "10 A", "15 A", "20 A" }, new List<FuseSlot>
            {
                new FuseSlot { label = "Éclairage", correctValueIndex = 1, startValueIndex = 2 },
                new FuseSlot { label = "Écrans", correctValueIndex = 2, startValueIndex = 0 },
                new FuseSlot { label = "Enregistreur", correctValueIndex = 0, startValueIndex = 1 },
                new FuseSlot { label = "Serveur vidéo", correctValueIndex = 3, startValueIndex = 3, requiresItem = true },
            }, "fuse");
            var fuseBox = Box("FuseBox", z, new Vector3(7.0f, 1.5f, 8.18f), new Vector3(0.5f, 0.65f, 0.15f), M.MetalDark);
            Text(fuseBox.transform, "<b>DANGER</b>\nFusibles", new Vector3(0, 0.2f, 0.55f), 180, new Vector2(0.4f, 0.2f), new Color(0.9f, 0.75f, 0.2f), 0.5f);
            Terminal(fuseBox, electrical, "Ouvrir la boîte à fusibles", "Boîte à fusibles (courant rétabli)");
            Paper(z, "diagram", new Vector3(8.1f, 1.55f, 8.11f), 180, true,
                "<b>SCHÉMA</b>\nÉclairage 10 A\nÉcrans 15 A\nEnregistreur 5 A\nServeur 20 A", "Schéma électrique",
                "Éclairage - 10 A\nÉcrans de surveillance - 15 A\nEnregistreur - 5 A\nServeur vidéo - 20 A", "Lire le schéma", null,
                null, new Vector2(0.4f, 0.5f));

            // Poste de surveillance (mur est).
            Box("ConsoleDesk", z, new Vector3(11.5f, 0.39f, 12f), new Vector3(0.8f, 0.78f, 4f), M.MetalDark);
            Chair(z, new Vector3(10.6f, 0, 12f), -90, M.Fabric);
            var screensOn = new List<GameObject>();
            var screensOff = new List<GameObject>();
            GameObject Monitor(string name, float zPos, float y)
            {
                var m = Box(name, z, new Vector3(11.7f, y, zPos), new Vector3(0.05f, 0.45f, 0.7f), M.Black);
                var on = Box("ScreenOn", m.transform, new Vector3(-0.6f, 0, 0), new Vector3(0.2f, 0.9f, 0.94f), M.ScreenOn, false, 0, true);
                var off = Box("ScreenOff", m.transform, new Vector3(-0.6f, 0, 0), new Vector3(0.2f, 0.9f, 0.94f), M.ScreenOff, false, 0, true);
                screensOn.Add(on); screensOff.Add(off);
                return m;
            }
            var camMonitor = Monitor("Monitor_Cameras", 11.2f, 1.05f);
            Monitor("Monitor_Feed2", 11.2f, 1.55f);
            var logMonitor = Monitor("Monitor_Registry", 12.0f, 1.05f);
            Monitor("Monitor_Feed3", 12.0f, 1.55f);
            var securityMonitor = Monitor("Monitor_Security", 12.8f, 1.05f);
            Monitor("Monitor_Feed4", 12.8f, 1.55f);

            var cam = Interactive<DocumentObject>(camMonitor);
            cam.Configure("Caméra porte arrière - 14 octobre, 21 h 09", c.Ev("ev_camera").description, "Consulter les enregistrements vidéo",
                c.Ev("ev_camera"), null, null, SfxType.Computer);
            cam.SetAvailability(power, noPower, noPowerMsg);
            var log = Interactive<DocumentObject>(logMonitor);
            log.Configure("Registre électronique des entrées - 14 octobre", c.Ev("ev_access_log").description, "Consulter le registre des entrées",
                c.Ev("ev_access_log"), null, null, SfxType.Computer);
            log.SetAvailability(power, noPower, noPowerMsg);

            // Énigme 5 : système de sécurité de Victor (chronologie).
            var timeline = Puzzle<TimelinePuzzle>(puzzles, "Puzzle5_Timeline", "timeline", "Système de sécurité - séquence du 14 octobre",
                "Séquence validée. Un mécanisme se déclenche dans le mur nord !",
                "L'onglet Chronologie du journal classe les événements datés par vos preuves.",
                "Le registre des entrées, le rapport du gardien et l'enregistrement téléphonique donnent des heures précises.",
                "Claire (19 h 02), Julien (20 h 40), porte arrière (21 h 10), fusible retiré (21 h 12), Victor reçoit Marc (21 h 31), corps découvert (23 h).");
            timeline.Configure(new List<TimelineEvent>
            {
                new TimelineEvent { id = "claire", text = "Claire rend visite à son oncle et se dispute avec lui" },
                new TimelineEvent { id = "julien", text = "Julien Roy entre avec son ancien badge pour récupérer ses affaires" },
                new TimelineEvent { id = "backdoor", text = "Quelqu'un entre par la porte arrière avec le code de maintenance" },
                new TimelineEvent { id = "fuse", text = "Un fusible est retiré : les caméras s'éteignent" },
                new TimelineEvent { id = "drink", text = "Victor reçoit Marc dans son bureau et lui sert un verre" },
                new TimelineEvent { id = "body", text = "Le gardien découvre le corps de Victor" },
            }, new[] { 4, 2, 5, 0, 3, 1 });
            Terminal(securityMonitor, timeline, "Accéder au système de sécurité de Victor", "Système de sécurité (séquence validée)",
                power, noPower, noPowerMsg);

            // Enregistreur téléphonique et rapport du gardien.
            // Coin nord-ouest : ne doit pas obstruer le passage secret (x = 9, z = 16).
            var recorder = Box("Recorder", z, new Vector3(7.0f, 0.95f, 15.5f), new Vector3(0.4f, 0.15f, 0.3f), M.MetalDark);
            Box("Shelf", z, new Vector3(7.0f, 0.43f, 15.55f), new Vector3(1.4f, 0.86f, 0.5f), M.WoodDark);
            var rec = Interactive<DocumentObject>(recorder);
            rec.Configure("Enregistreur de la ligne du bureau - 21 h 31", c.Ev("ev_voicemail").description, "Écouter l'enregistreur téléphonique",
                c.Ev("ev_voicemail"), null, null, SfxType.Computer);
            rec.SetAvailability(power, noPower, noPowerMsg);
            Table(z, "GuardTable", new Vector3(8.2f, 0, 10.2f), new Vector3(0.9f, 0.75f, 0.6f), M.Wood);
            Paper(z, "guard", new Vector3(8.2f, 0.755f, 10.2f), -10, false, "Rapport\nde ronde", "Rapport du gardien",
                c.Ev("ev_guard").description, "Lire le rapport du gardien", c.Ev("ev_guard"));

            // Panneau secret (s'ouvre quand la chronologie est validée).
            var secret = Box("SecretPanel", z, new Vector3(9, DoorHeight / 2, 16f), new Vector3(1.2f, DoorHeight, 0.2f), M.WallDark);
            var panel = secret.AddComponent<DrawerController>();
            panel.Configure(secret.transform, new Vector3(1.25f, 0, 0), Vector3.zero, "", "", false, true, false, SfxType.DoorOpen,
                null, "Un passage secret s'est ouvert dans le mur nord !");
            panel.SetAvailability(new[] { "puzzle.timeline" }, "", null);
            var p = secret.AddComponent<PersistentObject>();
            p.SetId("secret.panel");

            // Éclairage : lumière de secours rouge, plafonniers au retour du courant.
            PointLight("EmergencyLight", z, new Vector3(6.6f, 2.6f, 8.6f), new Color(1f, 0.15f, 0.1f), 0.5f, 5f);
            Box("EmergencyLamp", z, new Vector3(6.6f, 2.75f, 8.25f), new Vector3(0.2f, 0.1f, 0.1f), M.RedLight, false);
            var lightA = PointLight("Ceiling_A", z, new Vector3(8, 2.7f, 12), Cold, 1.6f, 7f).gameObject;
            var lightB = PointLight("Ceiling_B", z, new Vector3(10.5f, 2.6f, 12), Cold, 1.2f, 5f).gameObject;
            var reactor = Group("PowerReactor", z).AddComponent<FlagReactor>();
            var enable = new List<GameObject>(screensOn) { lightA, lightB };
            reactor.Configure(power, enable.ToArray(), screensOff.ToArray());
            Ambient(z, "ElectricHum", new Vector3(11.5f, 1.2f, 12f), AmbienceType.Hum, 0.25f, 6f, "puzzle.power");
        }

        // ------------------------------------------------------------------ Zone 5 : Pièce secrète

        private static void BuildSecretRoom(Transform z, GeneratedContent c, Transform puzzles)
        {
            // Tableau d'enquête physique.
            var board = Box("InvestigationBoard", z, new Vector3(9, 1.6f, 21.86f), new Vector3(3.2f, 1.6f, 0.05f), M.Cork);
            Interactive<BoardTerminal>(board);
            string[] names = { "Marc Dubois", "Claire Lemaire", "Julien Roy" };
            Color[] colors = { new Color(0.45f, 0.55f, 0.68f), new Color(0.75f, 0.55f, 0.45f), new Color(0.55f, 0.62f, 0.45f) };
            for (int i = 0; i < 3; i++)
            {
                var portrait = Box("Portrait_" + i, z, new Vector3(8f + i, 1.95f, 21.82f), new Vector3(0.3f, 0.38f, 0.01f),
                    MaterialLibrary.Get("Portrait" + i, colors[i]), false);
                Text(portrait.transform, names[i], new Vector3(0, -0.62f, -0.8f), 180, new Vector2(0.4f, 0.08f), Color.white, 0.4f)
                    .transform.localRotation = Quaternion.identity;
            }
            Box("String1", z, new Vector3(8.5f, 1.6f, 21.83f), new Vector3(1.0f, 0.01f, 0.01f), M.RedString, false);
            Box("String2", z, new Vector3(9.5f, 1.45f, 21.83f), new Vector3(1.0f, 0.01f, 0.01f), M.RedString, false);
            Text(board.transform, "<b>QUI A TUÉ VICTOR LEMAIRE ?</b>", new Vector3(0, 0.42f, -0.8f), 0, new Vector2(2.5f, 0.15f), new Color(0.1f, 0.05f, 0.05f), 1f);

            // Console d'accusation (énigme 6) - mécanisme de déverrouillage de la sortie.
            var accusation = Puzzle<AccusationManager>(puzzles, "Puzzle6_Accusation", "accusation", "Désigner le coupable",
                "Accusation correcte : l'affaire est résolue !",
                "Associez chaque preuve au suspect qu'elle concerne sur le tableau d'enquête.",
                "Comparez alibis et preuves : qui a menti sur son emploi du temps tout en ayant un mobile fort ?",
                "Marc Dubois, pour dissimuler sa fraude. Associez-lui au moins 4 preuves décisives (audit, virements, registre, photos du gala, vidéo cachée…).");
            Table(z, "AccusationDesk", new Vector3(9, 0, 19.3f), new Vector3(1.6f, 0.78f, 0.8f), M.WoodDark);
            var console = Box("AccusationConsole", z, new Vector3(9, 0.95f, 19.45f), new Vector3(0.6f, 0.32f, 0.06f), M.Black);
            Box("Screen", console.transform, new Vector3(0, 0, -0.6f), new Vector3(0.92f, 0.85f, 0.2f), M.ScreenOn, false, 0, true);
            Text(console.transform, "ACCUSATION\nFINALE", new Vector3(0, 0, -0.75f), 0, new Vector2(0.5f, 0.25f), new Color(0.9f, 0.5f, 0.4f), 0.6f);
            Terminal(console, accusation, "Porter l'accusation", "Accusation portée");
            Chair(z, new Vector3(9, 0, 18.5f), 0, M.Leather);

            // Enregistreur de la caméra cachée (preuve finale).
            Box("Shelf", z, new Vector3(6.4f, 0.5f, 18.5f), new Vector3(0.5f, 1.0f, 1.2f), M.WoodDark);
            var hidden = Box("HiddenCameraRecorder", z, new Vector3(6.45f, 1.15f, 18.5f), new Vector3(0.3f, 0.3f, 0.45f), M.Black);
            Box("Screen", hidden.transform, new Vector3(0.6f, 0.1f, 0), new Vector3(0.2f, 0.7f, 0.85f), M.ScreenOn, false, 0, true);
            Box("RecLight", hidden.transform, new Vector3(0.55f, 0.42f, 0.35f), new Vector3(0.1f, 0.1f, 0.1f), M.RedLight, false, 0, true);
            Interactive<DocumentObject>(hidden).Configure("Caméra cachée - bureau de Victor, 14 octobre", c.Ev("ev_hidden_video").description,
                "Visionner l'enregistrement de la caméra cachée", c.Ev("ev_hidden_video"), null, null, SfxType.Computer);

            PointLight("SecretLight", z, new Vector3(9, 2.6f, 19), new Color(1f, 0.55f, 0.35f), 1.4f, 7f);
            PointLight("BoardLight", z, new Vector3(9, 2.3f, 21), Warm, 1.0f, 3.5f);
        }
    }
}

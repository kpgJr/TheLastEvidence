# The Last Evidence

Escape room d'enquête policière en 3D, à la première personne, sous Unity 6 (URP).
Le jeu est **jouable au clavier et à la souris (Windows, macOS, Linux)** et **en réalité virtuelle sur Meta Quest 2, 3 et 3S** (OpenXR). La logique de jeu est commune aux deux modes : seuls le rig du joueur, les entrées et le placement des interfaces changent.

- Moteur : Unity **6000.6.4f1**. Packages : URP 17.6, Input System 1.20, uGUI/TextMeshPro 2.6, XR Interaction Toolkit 3.6, OpenXR 1.18, XR Management 4.7, Test Framework 1.8.
- Histoire, matrice des preuves et solutions : [`Docs/ENQUETE.md`](Docs/ENQUETE.md).

## Démarrage rapide

1. Ouvrir le dossier `TheLastEvidence` dans Unity Hub (version 6000.6.4f1).
2. Les scènes sont déjà générées. Ouvrir `Assets/_Project/Scenes/MainMenu.unity` et appuyer sur **Play**. Dans l'éditeur, le jeu tourne en mode clavier/souris.
3. Pour tout régénérer après une modification des générateurs : menu **The Last Evidence ▸ 2. Générer le projet complet**. Le menu **1** importe les ressources TextMeshPro, à faire une seule fois.

### Commandes clavier/souris

| Touche | Action |
|---|---|
| W A S D (ZQSD sur AZERTY) / flèches | Se déplacer |
| Souris | Regarder |
| Maj | Courir |
| Ctrl | S'accroupir |
| E | Interagir |
| F | Lampe torche |
| Tab | Inventaire |
| J | Journal d'enquête |
| R | Examiner l'objet visé |
| Échap | Pause, ou retour dans un menu |

Les touches sont liées à leur **position physique** : sur un clavier AZERTY, ZQSD fonctionne sans réglage. Elles peuvent aussi être remappées dans **Paramètres ▸ Contrôles**.

### Commandes VR (Meta Quest)

| Commande | Action |
|---|---|
| Joystick gauche | Se déplacer (cliquer pour courir) |
| Joystick droit | Tourner par paliers de 45° |
| Gâchette droite | Interagir / cliquer dans les menus (rayon de la manette droite) |
| Poignée droite | Interagir (« prendre ») — les doigts des mains virtuelles suivent gâchettes et poignées |
| Gâchette gauche | Lampe torche |
| A | Inventaire |
| Y | Journal |
| X | Examiner |
| B | Retour, ou fermer un menu |
| Menu (gauche) | Pause |

Pendant l'examen d'un objet : le joystick droit le fait tourner et le joystick gauche règle le zoom.

## Tester avec le casque Meta Quest

Unity ne propose pas Quest Link sous Linux : il faut construire un **APK Android** et l'installer sur le casque.

1. **Unity Hub -> Installs -> 6000.6.4f1 -> Add modules** : cocher **Android Build Support**, avec *OpenJDK* et *Android SDK & NDK Tools*.
2. Dans Unity : relancer **The Last Evidence -> 2. Générer le projet complet**. Cette étape active OpenXR, *Meta Quest Support* et le profil des manettes Touch pour Android. Sans le module Android, elle est ignorée et un avertissement s'affiche.
3. **File -> Build Profiles -> Android -> Switch Platform**.
4. Casque : activer le **mode développeur** (application Meta Horizon sur téléphone -> Appareils -> Mode développeur), brancher le Quest en USB-C, puis accepter « Autoriser le débogage USB » dans le casque.
5. **Build Profiles -> Build And Run**, ou le menu **The Last Evidence 3. Construire l'APK Meta Quest** suivi de `adb install -r Builds/Android/TheLastEvidence.apk`.
6. Dans le casque : **Bibliothèque -> Sources inconnues -> The Last Evidence**.

Le build Android démarre toujours en mode VR. Les builds PC démarrent en mode clavier/souris.

## Architecture

```
Assets/_Project/
  Scenes/            MainMenu, Investigation, Ending
  Scripts/
    Core/            GameManager (état), GameEvents (bus d'événements), GameInput (actions), PlatformMode (VR ou bureau),
                     GameSettings, SceneLoader, GameTimer, GameDatabase, RenderQualityAdapter
    Player/          PlayerRig (abstraction), PlayerController + PlayerCamera (bureau), VRPlayerController (Quest),
                     RigFactory, PlayerInteraction (raycast 2,5 m), FlashlightController, VRPointer, FootstepController
    Interaction/     IInteractable, InteractableBase, DoorController, DrawerController, PickupItem, DocumentObject,
                     InspectableObject + ObjectInspectionManager, SwitchController, PuzzleTerminal, FlagReactor, ExitTrigger
    Inventory/       ItemData (SO), Inventory (logique pure), InventoryManager
    Evidence/        EvidenceData, SuspectData (SO), EvidenceManager
    Investigation/   CaseData (SO), InvestigationManager (tableau), EvidenceBoard, InvestigationJournal,
                     AccusationManager (énigme 6), AccusationEvaluator (logique pure), EndingResult
    Puzzles/         PuzzleBase, PuzzleManager, HintManager, CollectItemPuzzle, KeypadPuzzle, PasswordPuzzle,
                     ElectricalPuzzle, TimelinePuzzle
    Progression/     ProgressionManager (drapeaux), FlagStore, ProgressCondition, ObjectiveManager
    SaveSystem/      SaveManager (JSON dans persistentDataPath), SaveData, ISaveable, PersistentObject
    Audio/           AudioManager, ProceduralAudio (sons synthétisés), AmbientSoundSource, SceneAudioStarter
    UI/              UIRoot (canvas écran ou VR), UIManager (pile de panneaux), UIPanel, UIFactory, HUD, menus…
    Editor/          ProjectGenerator, ContentFactory (histoire), SceneFactory (5 zones), LevelBuilder, MaterialLibrary
  ScriptableObjects/ Items, Evidence, Suspects, Case_Lemaire
  Resources/         GameDatabase (index des objets, preuves et suspects par identifiant)
  Tests/             EditMode (logique) et PlayMode (partie complète automatisée)
```

### Principes

- **Progression par drapeaux** : chaque événement important pose un drapeau (`item.<id>`, `evidence.<id>`, `puzzle.<id>`, `door.<id>`, `case.closed`). Portes, objectifs, lumières et panneaux réagissent via `ProgressCondition`, ce qui suffit aussi à restaurer l'état après un chargement.
- **Événements** : les systèmes communiquent par `GameEvents`, sans référence directe entre eux.
- **Ajouter une énigme** : hériter de `PuzzleBase`, implémenter `BuildUI` et `RefreshUI`, appeler `Solve()`, puis la relier au monde avec un `PuzzleTerminal`. Les indices (3 niveaux) se configurent dans l'Inspector.
- **Ajouter un objet interactif** : implémenter `IInteractable` (ou hériter de `InteractableBase`). Pour qu'il soit sauvegardé, ajouter `ISaveable` et un `PersistentObject`.
- **Interfaces** : construites en code par `UIFactory`. `UIRoot` crée un canvas écran en mode bureau ou un canvas en espace monde face au joueur en VR (rayon XRI et `XRUIInputModule`).
- **Sauvegarde** : automatique à chaque nouveau drapeau, ou manuelle depuis le menu pause. Les fichiers absents ou corrompus sont détectés et le jeu repart d'une nouvelle partie.

## Tests

- *Window -> General -> Test Runner* : **EditMode** (6 tests : drapeaux, inventaire, validation des énigmes, sauvegarde, données de l'affaire, trois fins) et **PlayMode** (2 tests : partie complète jusqu'à la bonne fin ; mauvaise accusation, chargement de sauvegarde et nouvelle partie).
- En ligne de commande :
  `Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults results.xml`
- Captures des zones : **The Last Evidence -> Outils -> Captures d'écran des zones**, enregistrées dans `Logs/Screenshots`.

## Travail en équipe (Git)

Les fichiers `.gitignore` et `.gitattributes` sont adaptés à Unity : sérialisation en texte forcée, fichiers `.meta` versionnés.

## Limites connues du prototype

- Les visuels sont faits de primitives Unity et les sons sont synthétisés.
- En VR, le post-traitement et les ombres sont désactivés pour tenir les performances du Quest.

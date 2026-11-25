using UnityEngine;
using UnityEngine.InputSystem;

namespace LastEvidence
{
    /// <summary>
    /// Définit toutes les actions d'entrée du jeu (nouveau Input System) directement en code :
    /// clavier/souris ET manettes Meta Quest partagent les mêmes actions.
    /// Les touches clavier sont liées à la position physique : sur un clavier AZERTY,
    /// les touches Z/Q/S/D (même emplacement que W/A/S/D) fonctionnent automatiquement.
    /// Les remappages personnalisés sont sauvegardés dans PlayerPrefs.
    /// </summary>
    public static class GameInput
    {
        public const string BindingOverridesKey = "LE_BindingOverrides";

        private static InputActionAsset asset;

        public static InputActionAsset Asset { get { EnsureCreated(); return asset; } }

        public static InputAction Move { get; private set; }
        public static InputAction Look { get; private set; }
        public static InputAction Turn { get; private set; }
        public static InputAction Run { get; private set; }
        public static InputAction Crouch { get; private set; }
        public static InputAction Interact { get; private set; }
        public static InputAction Flashlight { get; private set; }
        public static InputAction Inventory { get; private set; }
        public static InputAction Journal { get; private set; }
        public static InputAction Examine { get; private set; }
        public static InputAction Pause { get; private set; }
        public static InputAction Cancel { get; private set; }
        public static InputAction Zoom { get; private set; }
        public static InputAction InspectDrag { get; private set; }

        // Poses VR (utilisées par les TrackedPoseDriver)
        public static InputAction HeadPosition { get; private set; }
        public static InputAction HeadRotation { get; private set; }
        public static InputAction HeadTrackingState { get; private set; }
        // Pose « visée » (aim) : origine du rayon de pointage.
        public static InputAction LeftPosition { get; private set; }
        public static InputAction LeftRotation { get; private set; }
        public static InputAction RightPosition { get; private set; }
        public static InputAction RightRotation { get; private set; }
        // Pose « poignée » (grip) : centre de la main, utilisée pour les mains virtuelles.
        public static InputAction LeftGripPosition { get; private set; }
        public static InputAction LeftGripRotation { get; private set; }
        public static InputAction RightGripPosition { get; private set; }
        public static InputAction RightGripRotation { get; private set; }
        public static InputAction RightTrigger { get; private set; }
        // Valeurs analogiques (0..1) pour animer les doigts.
        public static InputAction LeftTriggerValue { get; private set; }
        public static InputAction LeftGripValue { get; private set; }
        public static InputAction RightTriggerValue { get; private set; }
        public static InputAction RightGripValue { get; private set; }

        /// <summary>Actions remappables proposées dans les paramètres (clavier uniquement).</summary>
        public static InputAction[] RebindableActions
        {
            get
            {
                EnsureCreated();
                return new[] { Interact, Run, Crouch, Flashlight, Inventory, Journal, Examine };
            }
        }

        public static void EnsureCreated()
        {
            if (asset != null) return;

            asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "LastEvidenceInput";
            var player = asset.AddActionMap("Player");

            Move = player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = player.AddAction("Look", InputActionType.PassThrough, "<Mouse>/delta", expectedControlLayout: "Vector2");
            Turn = player.AddAction("Turn", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}", expectedControlLayout: "Vector2");
            Turn.AddBinding("<Gamepad>/rightStick");

            Run = player.AddAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            Run.AddBinding("<XRController>{LeftHand}/{Primary2DAxisClick}");
            Crouch = player.AddAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
            Interact = player.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            Interact.AddBinding("<XRController>{RightHand}/{TriggerButton}");
            // Le bouton de poignée droit permet aussi de « prendre » / interagir (geste naturel en VR).
            Interact.AddBinding("<XRController>{RightHand}/{GripButton}");
            Flashlight = player.AddAction("Flashlight", InputActionType.Button, "<Keyboard>/f");
            Flashlight.AddBinding("<XRController>{LeftHand}/{TriggerButton}");
            Inventory = player.AddAction("Inventory", InputActionType.Button, "<Keyboard>/tab");
            Inventory.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
            Journal = player.AddAction("Journal", InputActionType.Button, "<Keyboard>/j");
            Journal.AddBinding("<XRController>{LeftHand}/{SecondaryButton}");
            Examine = player.AddAction("Examine", InputActionType.Button, "<Keyboard>/r");
            Examine.AddBinding("<XRController>{LeftHand}/{PrimaryButton}");
            Pause = player.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape");
            Pause.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            Cancel = player.AddAction("Cancel", InputActionType.Button, "<XRController>{RightHand}/{SecondaryButton}");
            Zoom = player.AddAction("Zoom", InputActionType.PassThrough, "<Mouse>/scroll/y");
            InspectDrag = player.AddAction("InspectDrag", InputActionType.Button, "<Mouse>/leftButton");

            var xr = asset.AddActionMap("XRTracking");
            HeadPosition = xr.AddAction("HeadPosition", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlLayout: "Vector3");
            HeadRotation = xr.AddAction("HeadRotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlLayout: "Quaternion");
            HeadTrackingState = xr.AddAction("HeadTrackingState", InputActionType.Value, "<XRHMD>/trackingState", expectedControlLayout: "Integer");
            // Une seule liaison par action de pose : lier pointerPosition ET devicePosition à la même
            // action faisait alterner la valeur entre les deux poses (rayon instable).
            LeftPosition = xr.AddAction("LeftPosition", InputActionType.Value, "<XRController>{LeftHand}/pointerPosition", expectedControlLayout: "Vector3");
            LeftRotation = xr.AddAction("LeftRotation", InputActionType.Value, "<XRController>{LeftHand}/pointerRotation", expectedControlLayout: "Quaternion");
            RightPosition = xr.AddAction("RightPosition", InputActionType.Value, "<XRController>{RightHand}/pointerPosition", expectedControlLayout: "Vector3");
            RightRotation = xr.AddAction("RightRotation", InputActionType.Value, "<XRController>{RightHand}/pointerRotation", expectedControlLayout: "Quaternion");
            LeftGripPosition = xr.AddAction("LeftGripPosition", InputActionType.Value, "<XRController>{LeftHand}/devicePosition", expectedControlLayout: "Vector3");
            LeftGripRotation = xr.AddAction("LeftGripRotation", InputActionType.Value, "<XRController>{LeftHand}/deviceRotation", expectedControlLayout: "Quaternion");
            RightGripPosition = xr.AddAction("RightGripPosition", InputActionType.Value, "<XRController>{RightHand}/devicePosition", expectedControlLayout: "Vector3");
            RightGripRotation = xr.AddAction("RightGripRotation", InputActionType.Value, "<XRController>{RightHand}/deviceRotation", expectedControlLayout: "Quaternion");
            LeftTriggerValue = xr.AddAction("LeftTriggerValue", InputActionType.Value, "<XRController>{LeftHand}/{Trigger}", expectedControlLayout: "Axis");
            LeftGripValue = xr.AddAction("LeftGripValue", InputActionType.Value, "<XRController>{LeftHand}/{Grip}", expectedControlLayout: "Axis");
            RightTriggerValue = xr.AddAction("RightTriggerValue", InputActionType.Value, "<XRController>{RightHand}/{Trigger}", expectedControlLayout: "Axis");
            RightGripValue = xr.AddAction("RightGripValue", InputActionType.Value, "<XRController>{RightHand}/{Grip}", expectedControlLayout: "Axis");
            RightTrigger = xr.AddAction("RightTrigger", InputActionType.Button, "<XRController>{RightHand}/{TriggerButton}");

            LoadBindingOverrides();
            asset.Enable();
        }

        public static void SaveBindingOverrides()
        {
            EnsureCreated();
            PlayerPrefs.SetString(BindingOverridesKey, asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static void LoadBindingOverrides()
        {
            string json = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            try { asset.LoadBindingOverridesFromJson(json); }
            catch (System.Exception e) { Debug.LogWarning($"Remappage invalide ignoré : {e.Message}"); }
        }

        public static void ResetBindingOverrides()
        {
            EnsureCreated();
            asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(BindingOverridesKey);
        }

        /// <summary>Index du premier binding clavier/souris d'une action (pour l'affichage et le remappage).</summary>
        public static int GetKeyboardBindingIndex(InputAction action)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (b.effectivePath.StartsWith("<Keyboard>") || b.effectivePath.StartsWith("<Mouse>")) return i;
            }
            return -1;
        }

        public static string GetKeyboardDisplay(InputAction action)
        {
            int index = GetKeyboardBindingIndex(action);
            return index < 0 ? "-" : action.GetBindingDisplayString(index);
        }
    }
}

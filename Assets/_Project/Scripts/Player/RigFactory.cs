using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LastEvidence
{
    /// <summary>
    /// Construit le rig du joueur en code, en mode bureau ou en mode VR.
    /// Les deux rigs exposent la même API (PlayerRig) au reste du jeu.
    /// </summary>
    public static class RigFactory
    {
        public static PlayerRig Create(Vector3 position, float yaw, bool vr, bool gameplay)
        {
            GameInput.EnsureCreated();
            return vr ? CreateVR(position, yaw, gameplay) : CreateDesktop(position, yaw, gameplay);
        }

        private static Camera CreateCamera(Transform parent, string name, float nearClip)
        {
            var camGo = new GameObject(name);
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(parent, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = nearClip;
            cam.farClipPlane = 60f;
            cam.backgroundColor = UIStyle.DeepBlack;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            // Le post-traitement est coûteux sur Quest : uniquement en mode bureau.
            data.renderPostProcessing = !PlatformMode.IsVR;
            data.antialiasing = PlatformMode.IsVR ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing;
            return cam;
        }

        private static Light CreateFlashlight(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("Flashlight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 14f;
            light.spotAngle = 52f;
            light.innerSpotAngle = 25f;
            light.intensity = 6f;
            light.color = new Color(1f, 0.93f, 0.8f);
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        private static PlayerRig CreateDesktop(Vector3 position, float yaw, bool gameplay)
        {
            var root = new GameObject("Player_Desktop");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.3f; cc.height = 1.8f; cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f; cc.slopeLimit = 45f; cc.skinWidth = 0.03f;

            var cam = CreateCamera(root.transform, "PlayerCamera", 0.05f);
            cam.fieldOfView = 70f;
            cam.transform.localPosition = new Vector3(0, 1.68f, 0);
            var look = cam.gameObject.AddComponent<PlayerCamera>();
            look.Init(root.transform);
            var anchor = CreateAnchor(cam.transform, "InspectionAnchor", new Vector3(0, 0, 0.55f));

            var controller = root.AddComponent<PlayerController>();
            controller.Init(cam, look, anchor);
            if (!gameplay)
            {
                controller.enabled = false;
                look.enabled = false;
                return controller;
            }
            var interaction = root.AddComponent<PlayerInteraction>();
            interaction.Init(controller);
            var flashlight = root.AddComponent<FlashlightController>();
            flashlight.Init(CreateFlashlight(cam.transform, new Vector3(0.15f, -0.15f, 0.1f)));
            root.AddComponent<FootstepController>();
            return controller;
        }

        private static TrackedPoseDriver AddPoseDriver(GameObject go, InputAction position, InputAction rotation, InputAction trackingState)
        {
            var driver = go.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.positionInput = new InputActionProperty(position);
            driver.rotationInput = new InputActionProperty(rotation);
            if (trackingState != null) driver.trackingStateInput = new InputActionProperty(trackingState);
            else driver.ignoreTrackingState = true;
            return driver;
        }

        /// <summary>Main virtuelle attachée à la manette (pose de visée, même repère que le laser).</summary>
        private static VRHand CreateHand(Transform controller, bool right)
        {
            var db = GameDatabase.Instance;
            Material mat = db != null ? (db.handMaterial != null ? db.handMaterial : db.defaultMaterial) : null;
            return VRHand.Create(controller, right,
                right ? GameInput.RightTriggerValue : GameInput.LeftTriggerValue,
                right ? GameInput.RightGripValue : GameInput.LeftGripValue, mat);
        }

        private static PlayerRig CreateVR(Vector3 position, float yaw, bool gameplay)
        {
            var root = new GameObject("Player_VR");
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.22f; cc.height = 1.7f; cc.center = new Vector3(0, 0.85f, 0);
            cc.stepOffset = 0.3f; cc.skinWidth = 0.02f;

            var offset = new GameObject("CameraOffset").transform;
            offset.SetParent(root.transform, false);

            var cam = CreateCamera(offset, "Main Camera", 0.03f);
            AddPoseDriver(cam.gameObject, GameInput.HeadPosition, GameInput.HeadRotation, GameInput.HeadTrackingState);

            var origin = root.AddComponent<XROrigin>();
            origin.Origin = root;
            origin.CameraFloorOffsetObject = offset.gameObject;
            origin.Camera = cam;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var left = new GameObject("LeftController");
            left.transform.SetParent(offset, false);
            AddPoseDriver(left, GameInput.LeftPosition, GameInput.LeftRotation, null);
            CreateHand(left.transform, false);

            var right = new GameObject("RightController");
            right.transform.SetParent(offset, false);
            AddPoseDriver(right, GameInput.RightPosition, GameInput.RightRotation, null);
            CreateHand(right.transform, true);

            // Rayon UI de l'XR Interaction Toolkit (menus en espace monde).
            var rayGo = new GameObject("UIRayInteractor");
            rayGo.transform.SetParent(right.transform, false);
            var ray = rayGo.AddComponent<XRRayInteractor>();
            ray.enableUIInteraction = true;
            ray.blockUIOnInteractableSelection = false;
            ray.maxRaycastDistance = 10f;
            ray.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            ray.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            ray.uiPressInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;

            var anchor = CreateAnchor(cam.transform, "InspectionAnchor", new Vector3(0, -0.05f, 0.4f));

            var controller = root.AddComponent<VRPlayerController>();
            controller.Init(cam, offset, left.transform, right.transform, anchor, ray);
            controller.LocomotionEnabled = gameplay;

            PlayerInteraction interaction = null;
            if (gameplay)
            {
                interaction = root.AddComponent<PlayerInteraction>();
                interaction.Init(controller);
                var flashlight = root.AddComponent<FlashlightController>();
                flashlight.Init(CreateFlashlight(left.transform, new Vector3(0, 0, 0.05f)));
                root.AddComponent<FootstepController>();
            }

            // Visuel du rayon + réticule.
            var pointerGo = new GameObject("Pointer");
            pointerGo.transform.SetParent(right.transform, false);
            var line = pointerGo.AddComponent<LineRenderer>();
            // Laser façon Meta : bien visible, s'affine vers la cible.
            line.widthMultiplier = 1f;
            line.startWidth = 0.007f;
            line.endWidth = 0.003f;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var db = GameDatabase.Instance;
            if (db != null && db.pointerMaterial != null) line.sharedMaterial = db.pointerMaterial;
            var reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "Reticle";
            Object.Destroy(reticle.GetComponent<Collider>());
            reticle.transform.localScale = Vector3.one * 0.015f;
            if (db != null && db.pointerMaterial != null) reticle.GetComponent<Renderer>().sharedMaterial = db.pointerMaterial;
            var pointer = pointerGo.AddComponent<VRPointer>();
            pointer.Init(ray, interaction, reticle.transform);
            return controller;
        }
    }
}

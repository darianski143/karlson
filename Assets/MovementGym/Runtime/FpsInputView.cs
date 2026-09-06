using UnityEngine;
using UnityEngine.InputSystem;

namespace Jimmy.Movement
{
    [RequireComponent(typeof(RigidbodyMotor))]
    public sealed class FpsInputView : MonoBehaviour
    {
        public InputActionAsset inputActions;
        public Camera playerCamera;
        public Vector3 spawnPosition;
        public InputActionAsset ActiveActions => actions;
        public bool InputCaptured => captured && !waitingForNeutral;
        public bool WaitingForNeutral => waitingForNeutral;
        public bool externalRespawnControl;
        public bool requireNeutralOnEnable;
        private bool waitingForNeutral;
        private RigidbodyMotor motor;
        private InputActionAsset actions;
        private InputAction move, look, jump, crouch, grapple;
        private float cameraRoll;
        private float yaw, pitch;
        private bool captured;

        private void Awake()
        {
            motor = GetComponent<RigidbodyMotor>();
            actions = Instantiate(inputActions);
            move = actions.FindAction("Player/Move", true);
            look = actions.FindAction("Player/Look", true);
            jump = actions.FindAction("Player/Jump", true);
            crouch = actions.FindAction("Player/Crouch", true);
            grapple = new InputAction("Grapple", InputActionType.Button);
            grapple.AddBinding("<Mouse>/rightButton");
            grapple.AddBinding("<Gamepad>/leftShoulder");
            yaw = transform.eulerAngles.y;
        }
        private void OnEnable() { actions.FindActionMap("Player").Enable(); grapple.Enable(); waitingForNeutral = requireNeutralOnEnable; Capture(true); }
        private void OnDisable() { actions.Disable(); grapple.Disable(); motor.ClearInput(); Capture(false); }
        private void OnDestroy() { grapple?.Dispose(); if (actions != null) Destroy(actions); }
        private void OnApplicationFocus(bool focused) { if (!focused) { motor.ClearInput(); Capture(false); } }
        private void Capture(bool value)
        {
            captured = value;
            Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !value;
        }
        private void Update()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame == true) { Capture(false); motor.ClearInput(); }
            else if (!captured && Mouse.current?.leftButton.wasPressedThisFrame == true) Capture(true);
            if (!externalRespawnControl && (transform.position.y < -12 || (captured && Keyboard.current?.rKey.wasPressedThisFrame == true)))
            {
                motor.Teleport(spawnPosition);
                yaw = pitch = 0;
            }
            if (!captured) return;
            if (waitingForNeutral)
            {
                if (ButtonHeld(jump) || ButtonHeld(crouch) || ButtonHeld(grapple) ||
                    ButtonHeld(actions.FindAction("Player/Attack")))
                { motor.SetInput(Vector2.zero, false); return; }
                waitingForNeutral = false;
            }
            motor.SetInput(move.ReadValue<Vector2>(), crouch.IsPressed());
            if (jump.WasPressedThisFrame()) motor.QueueJump(Time.time);
            var traversal = motor.Traversal;
            if (traversal != null)
            {
                if (grapple.WasPressedThisFrame())
                    traversal.TryGrapple(new Ray(playerCamera.transform.position, playerCamera.transform.forward), Time.time);
                if (!grapple.IsPressed()) traversal.ReleaseGrapple();
            }
            Vector2 delta = look.ReadValue<Vector2>();
            float scale = look.activeControl?.device is Gamepad ? motor.settings.gamepadLookSpeed * Time.deltaTime : motor.settings.mouseSensitivity;
            yaw += delta.x * scale;
            pitch = Mathf.Clamp(pitch - delta.y * scale, -88f, 88f);
            // Rotation is yaw only; Rigidbody translation remains physics-owned.
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }
        // Fresh Button actions need not report a press already held before enable.
        // Read their resolved controls so a scene restart cannot bypass the neutral gate.
        private static bool ButtonHeld(InputAction action)
        {
            foreach (var control in action.controls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.isPressed) return true;
            return false;
        }
        private void LateUpdate()
        {
            float target = motor.Height - 0.12f;
            Vector3 local = playerCamera.transform.localPosition;
            local.y = Mathf.Lerp(local.y, target, 1f - Mathf.Exp(-motor.settings.cameraHeightSmoothing * Time.deltaTime));
            playerCamera.transform.localPosition = local;
            var traversal = motor.Traversal;
            float targetRoll = 0, targetFov = motor.settings.fieldOfView;
            float blend = 1f;
            if (traversal != null && traversal.settings != null)
            {
                var feedback = traversal.settings;
                targetRoll = -traversal.WallSide * feedback.wallRollDegrees * feedback.cameraEffectIntensity;
                targetFov += (Mathf.InverseLerp(feedback.speedFovStart, feedback.speedFovFull, motor.Velocity.magnitude) *
                    feedback.speedFovBonus + (traversal.IsGrappling ? feedback.grappleFovBonus : 0)) * feedback.cameraEffectIntensity;
                blend = feedback.cameraEffectIntensity <= 0 ? 1f : 1f - Mathf.Exp(-feedback.cameraResponse * Time.deltaTime);
                traversal.UpdateRope(playerCamera.transform);
            }
            cameraRoll = Mathf.Lerp(cameraRoll, targetRoll, blend);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, cameraRoll);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, blend);
        }
    }
}

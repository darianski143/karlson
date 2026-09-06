using UnityEngine;
using UnityEngine.InputSystem;

namespace Jimmy.Movement
{
    public sealed class MovementTelemetry : MonoBehaviour
    {
        public RigidbodyMotor motor;
        public bool visible = true;
        private void Update() { if (Keyboard.current?.f3Key.wasPressedThisFrame == true) visible = !visible; }
        private void OnGUI()
        {
            if (!visible || motor == null) return;
            GUI.Box(new Rect(16, 16, 440, 162), "JIMMY / MOVEMENT LAB 01");
            GUI.Label(new Rect(30, 44, 414, 130),
                $"Speed {motor.PlanarSpeed:F2} m/s   Vertical {motor.Velocity.y:F2} m/s\n" +
                $"State {motor.State}   Ground {motor.IsGrounded}\n" +
                $"Slope {Vector3.Angle(Vector3.up, motor.GroundNormal):F1} degrees\n" +
                "WASD / stick: move | mouse / stick: look | Space / A: jump\n" +
                "C / Ctrl / B: crouch + slide | R: reset | F3: telemetry\n" +
                "Esc: release cursor | click: capture");
            GUI.Label(new Rect(Screen.width / 2f - 4, Screen.height / 2f - 10, 20, 20), "+");
        }
    }
}

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
            var traversal = motor.Traversal;
            string extra = traversal == null ? "" :
                $"Wall side {traversal.WallSide}   Used walls {traversal.UsedWallCount}\n" +
                $"Rope {traversal.RopeDistance:F1}/{traversal.RopeLength:F1} m   Tangent {traversal.TangentialSpeed:F1} m/s\n";
            GUI.Box(new Rect(16, 16, 500, 210), "JIMMY / MOVEMENT LAB 02");
            GUI.Label(new Rect(30, 44, 476, 178),
                $"Horizontal {motor.PlanarSpeed:F2}   Vertical {motor.Velocity.y:F2}   Total {motor.Velocity.magnitude:F2} m/s\n" +
                $"State {motor.State}   Ground {motor.IsGrounded}\n" + extra +
                "WASD / stick: move | mouse / stick: look | Space / A: jump\n" +
                "C / Ctrl / B: crouch + slide | RMB / LB: hold grapple\n" +
                "R: reset | F3: telemetry | Esc: release cursor | click: capture");

        }
    }
}

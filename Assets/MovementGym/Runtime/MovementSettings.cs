using UnityEngine;

namespace Jimmy.Movement
{
    [CreateAssetMenu(menuName = "Jimmy/Movement Settings")]
    public sealed class MovementSettings : ScriptableObject
    {
        [Header("Locomotion (metres, seconds)")]
        [Min(0)] public float groundSpeed = 10f;
        [Min(0)] public float groundAcceleration = 65f;
        [Min(0)] public float groundBraking = 35f;
        [Min(0)] public float airSpeed = 10f;
        [Min(0)] public float airAcceleration = 18f;
        [Min(0.1f)] public float gravity = 25f;
        [Min(0)] public float jumpHeight = 1.6f;
        [Min(0)] public float jumpBuffer = 0.14f;
        [Min(0)] public float coyoteTime = 0.12f;
        [Header("Capsule / ground")]
        [Min(0.1f)] public float radius = 0.35f;
        [Min(0.2f)] public float standingHeight = 1.8f;
        [Min(0.2f)] public float crouchingHeight = 0.95f;
        [Range(0, 75)] public float maxSlope = 50f;
        [Range(0.01f, 0.3f)] public float groundProbe = 0.1f;
        public LayerMask groundMask = ~0;
        [Header("Crouch / slide")]
        [Min(0)] public float crouchSpeed = 4f;
        [Min(0)] public float slideEntrySpeed = 7f;
        [Min(0)] public float slideBoost = 2f;
        [Min(0)] public float slideFriction = 4f;
        [Min(0)] public float slideSteering = 3f;
        [Min(0.01f)] public float slideDuration = 1.25f;
        [Header("View")]
        [Min(0)] public float mouseSensitivity = 0.09f;
        [Min(0)] public float gamepadLookSpeed = 150f;
        [Range(50, 110)] public float fieldOfView = 85f;
        [Min(0)] public float cameraHeightSmoothing = 18f;

        private void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            crouchingHeight = Mathf.Max(radius * 2f + 0.02f, crouchingHeight);
            standingHeight = Mathf.Max(crouchingHeight, standingHeight);
        }
    }
}

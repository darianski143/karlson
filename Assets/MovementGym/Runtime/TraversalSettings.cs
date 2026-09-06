using UnityEngine;

namespace Jimmy.Movement
{
    [CreateAssetMenu(menuName = "Jimmy/Traversal Settings")]
    public sealed class TraversalSettings : ScriptableObject
    {
        [Header("Wall run")]
        public bool wallRunEnabled = true;
        public LayerMask wallMask = ~0;
        [Min(0.01f)] public float wallReach = 0.65f;
        [Range(0, 0.5f)] public float wallNormalMaxUp = 0.15f;
        [Min(0)] public float wallMinimumSpeed = 5f;
        [Range(0, 1)] public float wallForwardInput = 0.1f;
        [Min(0)] public float wallSpeed = 12f;
        [Min(0)] public float wallAcceleration = 12f;
        [Range(0, 1)] public float wallGravityScale = 0.18f;
        [Min(0)] public float wallMaxFallSpeed = 3f;
        [Min(0)] public float wallAdhesion = 1.5f;
        [Min(0.01f)] public float wallDuration = 1.5f;
        [Min(0)] public float wallDetachCooldown = 0.2f;
        [Min(0)] public float wallJumpOutSpeed = 6f;
        [Min(0)] public float wallJumpUpSpeed = 8f;
        [Min(0)] public float launchGroundGrace = 0.12f;

        [Header("Grapple (static marked anchors; hold to attach)")]
        public bool grappleEnabled = true;
        public LayerMask grappleMask = ~0;
        [Min(0.1f)] public float grappleRange = 32f;
        [Min(0.1f)] public float ropeMinLength = 2f;
        [Min(0)] public float grappleAirAcceleration = 7f;
        [Min(0)] public float grappleAirSpeed = 15f;
        [Min(0)] public float grappleRetryDelay = 0.2f;
        [Min(0)] public float ropeObstructionGrace = 0.1f;
        [Min(0.001f)] public float ropeContactSkin = 0.08f;
        [Range(0, 1)] public float ropeAttachmentHeight = 0.55f;

        [Header("Camera comfort (zero intensity disables every camera effect)")]
        [Range(0, 1)] public float cameraEffectIntensity = 0.6f;
        [Range(0, 15)] public float wallRollDegrees = 6f;
        [Min(0)] public float cameraResponse = 8f;
        [Min(0)] public float speedFovStart = 10f;
        [Min(0.1f)] public float speedFovFull = 25f;
        [Range(0, 25)] public float speedFovBonus = 9f;
        [Range(0, 10)] public float grappleFovBonus = 2f;
        public bool showRope = true;
        [Min(0.001f)] public float ropeWidth = 0.035f;
        public Color ropeColor = new Color(0.3f, 0.95f, 1f);
        public Vector3 ropeViewOffset = new Vector3(0.18f, -0.15f, 0.3f);
    }
}

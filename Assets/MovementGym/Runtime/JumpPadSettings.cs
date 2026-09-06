using UnityEngine;

namespace Jimmy.Movement
{
    [CreateAssetMenu(menuName = "Jimmy/Jump Pad Settings")]
    public sealed class JumpPadSettings : ScriptableObject
    {
        [Min(0)] public float launchSpeed = 18f;
        [Min(0)] public float retriggerDelay = 0.4f;
        [Min(0)] public float groundIgnoreDuration = 0.15f;
    }
}

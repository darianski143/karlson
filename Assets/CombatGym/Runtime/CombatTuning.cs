using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Loadout and Feedback")]
    public sealed class CombatTuning : ScriptableObject
    {
        public WeaponDefinition[] weapons;
        public bool unlockAll;
        [Range(0, 1)] public float cameraIntensity = 0.5f;
        [Min(0)] public float cameraRecovery = 12;
        [Range(0, 10)] public float maximumViewKick = 4;
        [Range(0, 3)] public float shakeDegrees = 0.25f;
        [Min(0)] public float shakeFrequency = 24;
        [Min(0)] public float hitConfirmDuration = 0.18f;
        [Min(0)] public float muzzleDuration = 0.045f;
        [Min(0.01f)] public float tracerDuration = 0.09f;
        [Min(0.001f)] public float tracerWidth = 0.025f;
        [Min(0.01f)] public float impactDuration = 0.3f;
        [Min(0.01f)] public float impactSize = 0.12f;
        [Min(0.01f)] public float explosionFlashDuration = 0.12f;
        [Range(1, 256)] public int maximumEffects = 64;
        [Min(0)] public float spectacleSpeed = 14;
        public Material tracerMaterial, impactMaterial, explosionMaterial;
        public bool telemetry = true;
    }
}

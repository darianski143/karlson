using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Explosion")]
    public sealed class ExplosionSettings : ScriptableObject
    {
        [Min(0.1f)] public float radius = 6;
        [Min(0)] public float damage = 80;
        [Min(0)] public float impulse = 16;
        [Range(0, 1)] public float upwardBias = 0.25f;
        [Range(0, 1)] public float ownerDamageScale = 0.2f;
        public AnimationCurve falloff = AnimationCurve.Linear(0, 1, 1, 0);
        public bool respectCover = true;
        [Range(0, 1)] public float coveredDamageScale = 0.15f;
        [Range(0, 1)] public float coveredImpulseScale = 0.2f;
        [Min(0.001f)] public float raySkin = 0.03f;
        [Min(0.01f)] public float chainDelay = 0.15f;
        [Range(0, 32)] public int maximumChainDepth = 8;
        public LayerMask affectedMask = ~0;
        public LayerMask coverMask = ~0;
        public float Strength(float distance) => Mathf.Clamp01(falloff.Evaluate(Mathf.Clamp01(distance / Mathf.Max(radius, 0.001f))));
    }
}

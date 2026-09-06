using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Enemy")]
    public sealed class EnemySettings : ScriptableObject
    {
        [Min(0.1f)] public float range = 22;
        [Min(0.05f)] public float windup = 0.75f;
        [Min(0.05f)] public float recovery = 1.5f;
        [Min(0)] public float damage = 20;
        [Min(0)] public float impulse = 1;
        public Vector3 eyeOffset = new Vector3(0, 0.6f, 0);
        public LayerMask visibilityMask = ~0;
    }
}

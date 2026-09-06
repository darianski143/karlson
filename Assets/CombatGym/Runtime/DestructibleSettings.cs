using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Destructible")]
    public sealed class DestructibleSettings : ScriptableObject
    {
        [Min(0.01f)] public float health = 25;
        [Min(0.01f)] public float fragmentMass = 1;
        [Min(0)] public float fragmentImpulseScale = 0.5f;
        [Min(0)] public float scatterSpeed = 0.6f;
        [Min(0.1f)] public float fragmentLifetime = 8;
    }
}

using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Health")]
    public sealed class HealthSettings : ScriptableObject
    {
        [Min(0.01f)] public float maximumHealth = 100;
        public bool instantDeath;
        [Min(0)] public float playerRespawnDelay = 0.6f;
        [Min(0.1f)] public float corpseMass = 15;
        [Min(0)] public float corpseAngularDamping = 2;
        [Min(0.1f)] public float corpseMaximumAngularSpeed = 8;
    }
}

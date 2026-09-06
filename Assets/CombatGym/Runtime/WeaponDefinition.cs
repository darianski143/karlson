using UnityEngine;
namespace Jimmy.Combat
{
    [CreateAssetMenu(menuName = "Jimmy/Combat/Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public WeaponKind kind;
        public string displayName = "Pistol";
        [Min(0)] public float damage = 40;
        [Min(0.01f)] public float shotInterval = 0.3f;
        [Min(0.1f)] public float range = 100;
        [Range(1, 32)] public int pellets = 1;
        [Range(0, 30)] public float spreadDegrees;
        [Min(0)] public float targetImpulse = 3;
        [Min(0)] public float playerRecoil = 0.25f;
        [Min(0)] public float viewKick = 0.8f;
        [Header("Grenade")]
        [Min(0.1f)] public float projectileSpeed = 20;
        [Min(0)] public float inheritVelocity = 0.6f;
        [Min(0)] public float projectileGravity = 20;
        [Min(0.01f)] public float projectileRadius = 0.15f;
        [Min(0.01f)] public float spawnDistance = 0.6f;
        [Min(0.01f)] public float fuse = 1.3f;
        [Min(0)] public float armingDelay = 0.08f;
        public bool explodeOnImpact;
        public ExplosionSettings explosion;
        public Material projectileMaterial;
        public LayerMask hitMask = ~0;
    }
}

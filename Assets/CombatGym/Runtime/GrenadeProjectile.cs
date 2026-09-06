using UnityEngine;
namespace Jimmy.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class GrenadeProjectile : MonoBehaviour
    {
        public WeaponDefinition weapon;
        public GameObject owner;
        public bool Detonated { get; private set; }
        private float spawnedAt;
        public void Initialize(WeaponDefinition definition, GameObject source, Vector3 velocity, float now)
        {
            weapon = definition; owner = source; spawnedAt = now;
            var body = GetComponent<Rigidbody>(); body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = velocity;
            var sphere = GetComponent<SphereCollider>(); sphere.radius = weapon.projectileRadius;
            if (owner != null)
                foreach (var collider in owner.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(sphere, collider);
        }
        public static GrenadeProjectile Spawn(WeaponDefinition weapon, Ray aim, GameObject owner, Vector3 inheritedVelocity)
        {
            var obj = new GameObject("Ballistic grenade", typeof(Rigidbody), typeof(SphereCollider), typeof(GrenadeProjectile));
            Vector3 position = aim.origin + aim.direction * weapon.spawnDistance;
            if (CombatQueries.Raycast(aim, weapon.spawnDistance, weapon.hitMask, owner.transform, out var obstruction))
                position = obstruction.point - aim.direction * weapon.projectileRadius;
            obj.transform.position = position;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Grenade visual"; visual.GetComponent<Collider>().enabled = false;
            Object.Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(obj.transform, false); visual.transform.localScale = Vector3.one * weapon.projectileRadius * 2;
            visual.GetComponent<Renderer>().sharedMaterial = weapon.projectileMaterial;
            var projectile = obj.GetComponent<GrenadeProjectile>();
            projectile.Initialize(weapon, owner, aim.direction * weapon.projectileSpeed + inheritedVelocity * weapon.inheritVelocity, Time.time);
            return projectile;
        }
        private void FixedUpdate()
        {
            if (weapon != null && !Detonated)
                GetComponent<Rigidbody>().AddForce(Vector3.down * weapon.projectileGravity, ForceMode.Acceleration);
        }
        private void Update() => Tick(Time.time);
        public void Tick(float now)
        {
            if (weapon != null && !Detonated && now - spawnedAt >= weapon.fuse) Detonate();
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (weapon != null && weapon.explodeOnImpact && Time.time - spawnedAt >= weapon.armingDelay) Detonate();
        }
        public void Detonate()
        {
            if (Detonated || weapon == null) return;
            Detonated = true; GetComponent<Collider>().enabled = false;
            ExplosionService.Detonate(transform.position, weapon.explosion, owner);
            if (Application.isPlaying) Destroy(gameObject);
        }
    }
}

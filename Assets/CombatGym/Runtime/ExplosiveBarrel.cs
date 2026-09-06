using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        public HealthSettings durability;
        public ExplosionSettings explosion;
        public bool Armed { get; private set; }
        public bool Exploded { get; private set; }
        public int ExplosionCount { get; private set; }
        private float damage, explodeAt;
        private int chainDepth;
        private GameObject source;
        public bool ReceiveDamage(DamageHit hit)
        {
            if (Armed || Exploded || hit.Amount <= 0 || explosion == null || durability == null ||
                hit.ChainDepth >= explosion.maximumChainDepth) return false;
            damage += hit.Amount;
            if (damage >= durability.maximumHealth || durability.instantDeath)
            {
                Armed = true; source = hit.Source; chainDepth = hit.ChainDepth;
                explodeAt = Time.time + explosion.chainDelay;
            }
            return true;
        }
        private void Update() => Tick(Time.time);
        public void Tick(float now)
        {
            if (!Armed || Exploded || now < explodeAt) return;
            Exploded = true; ExplosionCount++;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var body = GetComponent<Rigidbody>();
            if (body != null) body.isKinematic = true;
            ExplosionService.Detonate(transform.position, explosion, source, chainDepth + 1);
        }
    }
}

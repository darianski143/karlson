using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class Breakable : MonoBehaviour, IDamageable, IDestructible, IImpulseReceiver
    {
        public DestructibleSettings settings;
        public Renderer intactRenderer;
        public Collider intactCollider;
        public Rigidbody[] fragments;
        public bool IsBroken { get; private set; }
        private float damage;
        public bool ReceiveDamage(DamageHit hit)
        {
            if (settings == null || IsBroken || hit.Amount <= 0) return false;
            damage += hit.Amount;
            if (damage >= settings.health) Break(hit);
            return true;
        }
        public void Break(DamageHit hit)
        {
            if (IsBroken || settings == null) return;
            IsBroken = true;
            if (intactRenderer != null) intactRenderer.enabled = false;
            if (intactCollider != null) intactCollider.enabled = false;
            foreach (var fragment in fragments)
            {
                if (fragment == null) continue;
                fragment.gameObject.SetActive(true); fragment.isKinematic = false;
                fragment.mass = settings.fragmentMass;
                fragment.linearVelocity = (fragment.worldCenterOfMass - transform.position).normalized * settings.scatterSpeed;
                if (Application.isPlaying) Destroy(fragment.gameObject, settings.fragmentLifetime);
            }
        }
        public void ApplyImpulse(Vector3 deltaVelocity, Vector3 point)
        {
            if (!IsBroken) return;
            foreach (var fragment in fragments)
                if (fragment != null && !fragment.isKinematic)
                    fragment.AddForce(deltaVelocity * settings.fragmentImpulseScale, ForceMode.VelocityChange);
        }
    }
}

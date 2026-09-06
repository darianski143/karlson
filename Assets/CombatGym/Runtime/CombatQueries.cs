using UnityEngine;
namespace Jimmy.Combat
{
    public static class CombatQueries
    {
        public static T Find<T>(Collider collider) where T : class
        {
            foreach (var component in collider.GetComponentsInParent<MonoBehaviour>())
                if (component is T contract && component.isActiveAndEnabled) return contract;
            return null;
        }
        public static Transform Identity(Collider collider)
        {
            var damage = Find<IDamageable>(collider) as Component;
            var impulse = Find<IImpulseReceiver>(collider) as Component;
            return damage != null ? damage.transform : impulse != null ? impulse.transform :
                collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;
        }
        public static bool Raycast(Ray ray, float range, LayerMask mask, Transform ignoredRoot, out RaycastHit closest)
        {
            closest = default;
            float distance = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(ray, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (ignoredRoot != null && hit.collider.transform.IsChildOf(ignoredRoot)) continue;
                if (hit.distance >= distance) continue;
                distance = hit.distance; closest = hit;
            }
            return distance < float.PositiveInfinity;
        }
        public static bool Deliver(Collider collider, DamageHit hit, Vector3 impulse)
        {
            bool accepted = Find<IDamageable>(collider)?.ReceiveDamage(hit) ?? false;
            var receiver = Find<IImpulseReceiver>(collider);
            if (receiver != null) receiver.ApplyImpulse(impulse, hit.Point);
            else if (collider.attachedRigidbody != null && !collider.attachedRigidbody.isKinematic)
                collider.attachedRigidbody.AddForceAtPosition(impulse, hit.Point, ForceMode.VelocityChange);
            return accepted;
        }
    }
}

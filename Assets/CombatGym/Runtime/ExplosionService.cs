using System.Collections.Generic;
using Jimmy.Movement;
using UnityEngine;
namespace Jimmy.Combat
{
    public static class ExplosionService
    {
        private struct Effect
        {
            public Collider collider;
            public Vector3 point, impulse;
            public DamageHit damage;
        }
        public static int Detonate(Vector3 center, ExplosionSettings settings, GameObject owner, int chainDepth = 0)
        {
            if (settings == null) return 0;
            var targets = new Dictionary<Transform, Collider>();
            foreach (var collider in Physics.OverlapSphere(center, settings.radius, settings.affectedMask, QueryTriggerInteraction.Ignore))
            {
                Transform key = CombatQueries.Identity(collider);
                if (!targets.TryGetValue(key, out var previous) ||
                    (collider.ClosestPoint(center) - center).sqrMagnitude < (previous.ClosestPoint(center) - center).sqrMagnitude)
                    targets[key] = collider;
            }
            // Snapshot cover before damage can remove a window/door in this same blast.
            var effects = new List<Effect>();
            foreach (var pair in targets)
            {
                var collider = pair.Value; Vector3 point = collider.ClosestPoint(center);
                Vector3 delta = point - center;
                float distance = delta.magnitude, strength = settings.Strength(distance);
                if (strength <= 0) continue;
                bool covered = settings.respectCover && distance > settings.raySkin &&
                    CombatQueries.Raycast(new Ray(center, delta.normalized), distance - settings.raySkin,
                        settings.coverMask, null, out var cover) && CombatQueries.Identity(cover.collider) != pair.Key;
                bool self = owner != null && pair.Key.IsChildOf(owner.transform);
                Vector3 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.up;
                Vector3 impulseDirection = (direction + Vector3.up * settings.upwardBias).normalized;
                effects.Add(new Effect
                {
                    collider = collider, point = point,
                    damage = new DamageHit(settings.damage * strength * (covered ? settings.coveredDamageScale : 1) *
                        (self ? settings.ownerDamageScale : 1), point, direction, owner, chainDepth),
                    impulse = impulseDirection * settings.impulse * strength * (covered ? settings.coveredImpulseScale : 1)
                });
            }
            int damaged = 0;
            bool hitOther = false;
            foreach (var effect in effects)
                if (effect.collider != null && CombatQueries.Deliver(effect.collider, effect.damage, effect.impulse))
                {
                    damaged++;
                    if (owner == null || !effect.collider.transform.IsChildOf(owner.transform)) hitOther = true;
                }
            CombatEffects.Instance?.Explosion(center, settings.radius);
            if (hitOther && owner != null) owner.GetComponent<PlayerCombat>()?.ConfirmHit();
            float speed = owner != null && owner.TryGetComponent<RigidbodyMotor>(out var motor) ? motor.Velocity.magnitude : 0;
            TimeDirector.Moment(chainDepth > 0 ? CombatMoment.ChainExplosion : CombatMoment.Boost, speed);
            return damaged;
        }
    }
}

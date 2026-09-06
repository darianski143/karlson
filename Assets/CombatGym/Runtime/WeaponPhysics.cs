using UnityEngine;
namespace Jimmy.Combat
{
    public readonly struct ShotReport
    {
        public readonly int Rays, DamageHits;
        public ShotReport(int rays, int damageHits) { Rays = rays; DamageHits = damageHits; }
    }
    public static class WeaponPhysics
    {
        public static Vector3 PelletDirection(Vector3 forward, int index, int count, float spreadDegrees)
        {
            if (count <= 1 || spreadDegrees <= 0) return forward.normalized;
            float radius = Mathf.Sqrt((index + 0.5f) / count) * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            // Golden-angle disk: deterministic, independent of global random state.
            float angle = index * 2.39996323f;
            return Quaternion.LookRotation(forward) *
                new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 1).normalized;
        }
        public static ShotReport Hitscan(WeaponDefinition weapon, Ray aim, GameObject owner)
        {
            int count = weapon.kind == WeaponKind.Shotgun ? Mathf.Max(1, weapon.pellets) : 1;
            int accepted = 0;
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = PelletDirection(aim.direction, i, count, weapon.spreadDegrees);
                Vector3 end = aim.origin + direction * weapon.range;
                if (CombatQueries.Raycast(new Ray(aim.origin, direction), weapon.range, weapon.hitMask,
                    owner != null ? owner.transform : null, out var hit))
                {
                    end = hit.point;
                    if (CombatQueries.Deliver(hit.collider, new DamageHit(weapon.damage, hit.point, direction, owner),
                        direction * weapon.targetImpulse)) accepted++;
                    CombatEffects.Instance?.Impact(hit.point);
                }
                CombatEffects.Instance?.Trace(aim.origin, end);
            }
            return new ShotReport(count, accepted);
        }
    }
}

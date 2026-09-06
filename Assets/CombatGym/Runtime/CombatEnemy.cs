using Jimmy.Movement;
using UnityEngine;
namespace Jimmy.Combat
{
    public enum EnemyState { Idle, Telegraph, Recovering, Dead }
    [RequireComponent(typeof(CombatHealth))]
    public sealed class CombatEnemy : MonoBehaviour
    {
        public EnemySettings settings;
        public Transform target;
        public LineRenderer warningBeam;
        public EnemyState State { get; private set; }
        private Vector3 lockedAim;
        private float nextAction;
        private Vector3 Eye => transform.position + settings.eyeOffset;
        public bool HasLineOfSight()
        {
            if (settings == null || target == null) return false;
            var motor = target.GetComponent<RigidbodyMotor>();
            Vector3 aim = target.position + (motor != null ? Vector3.up * motor.Height * 0.5f : Vector3.zero);
            Vector3 delta = aim - Eye;
            return delta.magnitude <= settings.range &&
                CombatQueries.Raycast(new Ray(Eye, delta.normalized), delta.magnitude + 0.01f, settings.visibilityMask, transform, out var hit) &&
                hit.collider.transform.IsChildOf(target);
        }
        private void Update() => Tick(Time.time);
        public void Tick(float now)
        {
            if (settings == null) return;
            var health = GetComponent<CombatHealth>(); health.Initialize();
            if (health.IsDead) { State = EnemyState.Dead; ShowBeam(false); return; }
            if (target == null || (target.TryGetComponent<CombatHealth>(out var targetHealth) && targetHealth.IsDead))
            { State = EnemyState.Idle; ShowBeam(false); return; }
            if (State == EnemyState.Telegraph)
            {
                if (now < nextAction) return;
                Vector3 direction = (lockedAim - Eye).normalized;
                Vector3 end = Eye + direction * settings.range;
                if (CombatQueries.Raycast(new Ray(Eye, direction), settings.range, settings.visibilityMask, transform, out var hit))
                {
                    end = hit.point;
                    CombatQueries.Deliver(hit.collider, new DamageHit(settings.damage, hit.point, direction, gameObject), direction * settings.impulse);
                }
                CombatEffects.Instance?.Trace(Eye, end);
                State = EnemyState.Recovering; nextAction = now + settings.recovery; ShowBeam(false);
            }
            else if (now >= nextAction && HasLineOfSight())
            {
                var motor = target.GetComponent<RigidbodyMotor>();
                lockedAim = target.position + (motor != null ? Vector3.up * motor.Height * 0.5f : Vector3.zero);
                State = EnemyState.Telegraph; nextAction = now + settings.windup; ShowBeam(true);
            }
        }
        private void ShowBeam(bool visible)
        {
            if (warningBeam == null) return;
            warningBeam.enabled = visible;
            if (visible) { warningBeam.positionCount = 2; warningBeam.SetPosition(0, Eye); warningBeam.SetPosition(1, lockedAim); }
        }
        private void OnDisable() => ShowBeam(false);
    }
}

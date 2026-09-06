using UnityEngine;

namespace Jimmy.Combat
{
    public enum WeaponKind { Pistol, Shotgun, GrenadeLauncher }
    public enum CombatMoment { Boost, TraversalHit, ChainExplosion }
    public readonly struct DamageHit
    {
        public readonly float Amount;
        public readonly Vector3 Point, Direction;
        public readonly GameObject Source;
        public readonly int ChainDepth;
        public DamageHit(float amount, Vector3 point, Vector3 direction, GameObject source = null, int chainDepth = 0)
        { Amount = amount; Point = point; Direction = direction; Source = source; ChainDepth = chainDepth; }
    }
    public interface IDamageable { bool ReceiveDamage(DamageHit hit); }
    public interface IImpulseReceiver { void ApplyImpulse(Vector3 deltaVelocity, Vector3 point); }
    public interface IDestructible { bool IsBroken { get; } void Break(DamageHit hit); }
}

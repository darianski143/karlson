using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class CombatHealth : MonoBehaviour, IDamageable
    {
        public HealthSettings settings;
        public float Health { get; private set; }
        public bool IsDead { get; private set; }
        public int AcceptedHits { get; private set; }
        private bool initialized;
        private void Awake() => Initialize();
        public void Initialize()
        {
            if (initialized || settings == null) return;
            initialized = true; ResetHealth();
        }
        public void ResetHealth()
        {
            Health = settings.maximumHealth; IsDead = false; AcceptedHits = 0;
        }
        public bool ReceiveDamage(DamageHit hit)
        {
            Initialize();
            if (!initialized || IsDead || hit.Amount <= 0) return false;
            AcceptedHits++;
            Health = settings.instantDeath ? 0 : Mathf.Max(0, Health - hit.Amount);
            if (Health <= 0)
            {
                IsDead = true;
                GetComponent<PhysicsDeath>()?.Activate();
            }
            return true;
        }
    }
}

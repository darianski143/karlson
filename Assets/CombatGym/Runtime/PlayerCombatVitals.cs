using Jimmy.Movement;
using UnityEngine;
namespace Jimmy.Combat
{
    [RequireComponent(typeof(CombatHealth), typeof(RigidbodyMotor))]
    public sealed class PlayerCombatVitals : MonoBehaviour
    {
        private float respawnAt = -1;
        private void Update()
        {
            var health = GetComponent<CombatHealth>();
            if (!health.IsDead) { respawnAt = -1; return; }
            if (respawnAt < 0) respawnAt = Time.time + health.settings.playerRespawnDelay;
            if (Time.time < respawnAt) return;
            GetComponent<RigidbodyMotor>().Teleport(GetComponent<FpsInputView>().spawnPosition);
            health.ResetHealth(); respawnAt = -1;
        }
    }
}

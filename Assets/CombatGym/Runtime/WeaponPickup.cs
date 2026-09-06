using UnityEngine;
namespace Jimmy.Combat
{
    [RequireComponent(typeof(Collider))]
    public sealed class WeaponPickup : MonoBehaviour
    {
        public WeaponDefinition weapon;
        public bool Collected { get; private set; }
        private void OnTriggerEnter(Collider other)
        {
            var combat = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerCombat>() : null;
            if (combat != null) Collect(combat);
        }
        public bool Collect(PlayerCombat player)
        {
            if (Collected || player == null || !player.Pickup(weapon)) return false;
            Collected = true; gameObject.SetActive(false); return true;
        }
    }
}

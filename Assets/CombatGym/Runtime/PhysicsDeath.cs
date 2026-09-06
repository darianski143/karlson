using UnityEngine;
namespace Jimmy.Combat
{
    // Stable greybox ragdoll substitute: one dynamic capsule, no unstable joints.
    [RequireComponent(typeof(Rigidbody), typeof(CombatHealth))]
    public sealed class PhysicsDeath : MonoBehaviour
    {
        public bool Activated { get; private set; }
        public void Activate()
        {
            if (Activated) return;
            Activated = true;
            var settings = GetComponent<CombatHealth>().settings;
            var body = GetComponent<Rigidbody>();
            body.mass = settings.corpseMass;
            body.isKinematic = false;
            body.useGravity = true;
            body.constraints = RigidbodyConstraints.None;
            body.angularDamping = settings.corpseAngularDamping;
            body.maxAngularVelocity = settings.corpseMaximumAngularSpeed;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}

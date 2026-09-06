using Jimmy.Movement;
using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class PhysicsImpulseBody : MonoBehaviour, IImpulseReceiver
    {
        public void ApplyImpulse(Vector3 deltaVelocity, Vector3 point)
        {
            var motor = GetComponent<RigidbodyMotor>();
            if (motor != null) { motor.AddVelocityChange(deltaVelocity); return; }
            var body = GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
                body.AddForceAtPosition(deltaVelocity, point, ForceMode.VelocityChange);
        }
    }
}

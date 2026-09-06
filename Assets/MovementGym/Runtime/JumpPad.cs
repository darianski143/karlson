using System.Collections.Generic;
using UnityEngine;

namespace Jimmy.Movement
{
    [RequireComponent(typeof(Collider))]
    public sealed class JumpPad : MonoBehaviour
    {
        public JumpPadSettings settings;
        private readonly Dictionary<RigidbodyMotor, float> nextLaunch = new Dictionary<RigidbodyMotor, float>();
        private void OnTriggerEnter(Collider other)
        {
            var motor = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<RigidbodyMotor>() : null;
            if (motor != null) TryLaunch(motor, Time.time);
        }
        public bool TryLaunch(RigidbodyMotor motor, float now)
        {
            if (settings == null || motor == null || (nextLaunch.TryGetValue(motor, out float next) && now < next)) return false;
            nextLaunch[motor] = now + settings.retriggerDelay;
            motor.Launch(transform.up, settings.launchSpeed, now, settings.groundIgnoreDuration);
            return true;
        }
        private void OnDisable() => nextLaunch.Clear();
    }
}

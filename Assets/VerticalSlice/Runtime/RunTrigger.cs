using Jimmy.Movement;
using UnityEngine;

namespace Jimmy.Run
{
    public enum RunTriggerKind { Core, Extraction, Kill }
    [RequireComponent(typeof(Collider))]
    public sealed class RunTrigger : MonoBehaviour
    {
        public GameFlow flow;
        public RunTriggerKind kind;
        public string deathReason = "Lost in the coolant void";
        private void OnTriggerEnter(Collider other)
        {
            var motor = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<RigidbodyMotor>() : null;
            Activate(motor);
        }
        public bool Activate(RigidbodyMotor motor)
        {
            if (flow == null || motor == null || motor != flow.player) return false;
            switch (kind)
            {
                case RunTriggerKind.Core:
                    if (!flow.CollectCore()) return false;
                    gameObject.SetActive(false); return true;
                case RunTriggerKind.Extraction: return flow.FinishRun();
                case RunTriggerKind.Kill: return flow.Die(deathReason);
                default: return false;
            }
        }
    }
}

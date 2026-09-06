using UnityEngine;

namespace Jimmy.Run
{
    [CreateAssetMenu(menuName = "Jimmy/Run/Level Settings")]
    public sealed class RunSettings : ScriptableObject
    {
        public string levelTitle = "RELAY BREACH";
        public string bestTimeKey = "jimmy.relay-breach.v1.best";
        public bool autoStart = true;
        public bool autoRestartAfterDeath = true;
        [Min(0)] public float deathRestartDelay = 0.9f;
        public float fallHeight = -12f;
        public Bounds playableBounds = new Bounds(new Vector3(0, 18, 82), new Vector3(100, 64, 196));
        public bool debugTelemetry;
        public string objectiveText = "Recover the relay core, then reach extraction";
    }
}

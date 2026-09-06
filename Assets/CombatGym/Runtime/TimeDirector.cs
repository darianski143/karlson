using UnityEngine;
namespace Jimmy.Combat
{
    public sealed class TimeDirector : MonoBehaviour
    {
        public static TimeDirector Instance { get; private set; }
        public TimeSettings settings;
        public bool IsSlowed { get; private set; }
        private float baselineScale, baselineStep, restoreAt, cooldownUntil;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
        private void OnEnable()
        {
            if (Instance != null && Instance != this) { enabled = false; return; }
            Instance = this;
        }
        public bool Request(CombatMoment moment, float speed, float now)
        {
            if (settings == null || !settings.contextualSlowMotion || IsSlowed || now < cooldownUntil ||
                Time.timeScale <= 0 || (moment != CombatMoment.ChainExplosion && speed < settings.minimumSpeed)) return false;
            baselineScale = Time.timeScale; baselineStep = Time.fixedDeltaTime;
            Time.timeScale = Mathf.Min(baselineScale, settings.slowScale);
            Time.fixedDeltaTime = baselineStep * Time.timeScale / baselineScale;
            restoreAt = now + settings.realDuration; cooldownUntil = restoreAt + settings.realCooldown;
            IsSlowed = true; return true;
        }
        public static void Moment(CombatMoment moment, float speed)
        {
            if (Instance != null) Instance.Request(moment, speed, Time.unscaledTime);
        }
        public void Tick(float now)
        {
            if (IsSlowed && (settings == null || !settings.contextualSlowMotion || now >= restoreAt)) Restore();
        }
        private void Update() => Tick(Time.unscaledTime);
        public void Restore()
        {
            if (!IsSlowed) return;
            Time.timeScale = baselineScale; Time.fixedDeltaTime = baselineStep; IsSlowed = false;
        }
        private void OnDisable() { Restore(); if (Instance == this) Instance = null; }
        private void OnDestroy() => Restore();
    }
}

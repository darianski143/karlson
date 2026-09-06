using System.Collections;
using Jimmy.Combat;
using Jimmy.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jimmy.Run
{
    [DefaultExecutionOrder(-50)]
    public sealed class GameFlow : MonoBehaviour
    {
        public RunSettings settings;
        public RigidbodyMotor player;
        public TimeDirector timeDirector;
        public RunTrigger core;
        public RunTrigger extraction;
        public RunSession Session { get; private set; } = new RunSession();
        public IBestTimeStore BestTimes { get; set; }
        public bool HasCore { get; private set; }
        public bool NewBest { get; private set; }
        public string DeathReason { get; private set; }
        public RunState State => Session.State;
        public double ElapsedSeconds => Session.Elapsed;
        private FpsInputView view;
        private PlayerCombat combat;
        private CombatHealth health;
        private float baselineScale, baselineStep;
        private bool clockCaptured, loading;
        private static double Now => Time.realtimeSinceStartupAsDouble;

        private void Awake()
        {
            if (settings == null || player == null || timeDirector == null || core == null || extraction == null)
            {
                Debug.LogError("GameFlow requires level settings, player, time director, core and extraction.", this);
                enabled = false; return;
            }
            baselineScale = Time.timeScale > 0 ? Time.timeScale : 1;
            baselineStep = Time.fixedDeltaTime; clockCaptured = true;
            BestTimes = new LocalBestTimes(settings.bestTimeKey);
            view = player.GetComponent<FpsInputView>();
            combat = player.GetComponent<PlayerCombat>();
            health = player.GetComponent<CombatHealth>();
            view.externalRespawnControl = true;
            player.GetComponent<PlayerCombatVitals>().enabled = false;
            var telemetry = player.GetComponent<MovementTelemetry>();
            if (telemetry != null) { telemetry.visible = settings.debugTelemetry; telemetry.enabled = settings.debugTelemetry; }
        }
        private void Start()
        {
            if (settings.autoStart) BeginRun();
        }
        public bool BeginRun() => Session.Begin(Now);
        private void Update()
        {
            if (Keyboard.current?.rKey.wasPressedThisFrame == true || Gamepad.current?.startButton.wasPressedThisFrame == true)
            {
                RestartRun(); return;
            }
            Tick(Now);
        }
        public void Tick(double now)
        {
            Session.Tick(now);
            if (State == RunState.Running)
            {
                if (health.IsDead) Die("Run ended — incoming fire");
                else if (player.transform.position.y < settings.fallHeight || !settings.playableBounds.Contains(player.transform.position))
                    Die("Outside the relay facility");
            }
            if (State == RunState.Dead && settings.autoRestartAfterDeath && now - Session.StateSince >= settings.deathRestartDelay)
                RestartRun();
        }
        public bool CollectCore()
        {
            if (State != RunState.Running || HasCore) return false;
            HasCore = true; return true;
        }
        public bool FinishRun()
        {
            if (!HasCore || !Session.Finish(Now)) return false;
            NewBest = BestTimes.Record(Session.Elapsed);
            StopPlayerAndSlowMotion(); return true;
        }
        public bool Die(string reason)
        {
            if (!Session.Die(Now)) return false;
            DeathReason = reason; StopPlayerAndSlowMotion(); return true;
        }
        private void StopPlayerAndSlowMotion()
        {
            RestoreClock();
            timeDirector.enabled = false;
            combat.enabled = false;
            view.enabled = false;
            var body = player.GetComponent<Rigidbody>();
            // Clear buffered input, pending impulses and traversal before freezing.
            if (!body.isKinematic) player.Teleport(body.position);
            else player.ClearInput();
            player.enabled = false; body.isKinematic = true;
        }
        public bool RestartRun()
        {
            if (loading || !Session.Restart(Now)) return false;
            loading = true;
            StopPlayerAndSlowMotion();
            StartCoroutine(ReloadScene());
            return true;
        }
        private IEnumerator ReloadScene()
        {
            RestoreClock();
            var scene = gameObject.scene;
            var operation = SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
            if (operation != null) yield return operation;
        }
        public void RestoreClock()
        {
            if (!clockCaptured) return;
            if (timeDirector != null) timeDirector.Restore();
            Time.timeScale = baselineScale; Time.fixedDeltaTime = baselineStep;
        }
        private void OnDisable() => RestoreClock();
        private void OnDestroy() => RestoreClock();
    }
}

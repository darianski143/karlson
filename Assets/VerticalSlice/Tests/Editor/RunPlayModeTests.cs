using System.Collections;
using System.Linq;
using Jimmy.Combat;
using Jimmy.Movement;
using Jimmy.Run.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Jimmy.Run.Tests
{
    public sealed class RunPlayModeTests
    {
        private Mouse testMouse;
        private InputSettings.EditorInputBehaviorInPlayMode? previousEditorInputBehavior;
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
        private RunSettings temporarySettings;
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (testMouse != null) { InputSystem.RemoveDevice(testMouse); testMouse = null; }
            if (previousEditorInputBehavior.HasValue)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior.Value;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                previousEditorInputBehavior = null;
            }
            var flow = Object.FindAnyObjectByType<GameFlow>();
            if (flow != null) flow.RestoreClock();
            if (Application.isPlaying) yield return new ExitPlayMode();
            if (temporarySettings != null) Object.DestroyImmediate(temporarySettings);
        }
        private static IEnumerator Advance(double seconds)
        {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until) yield return null;
        }
        private static IEnumerator WaitForFreshRun(GameFlow old)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 10;
            while (Time.realtimeSinceStartupAsDouble < deadline)
            {
                var current = Object.FindAnyObjectByType<GameFlow>();
                if (old == null && current != null && current.State == RunState.Running)
                {
                    current.BestTimes = new MemoryBestTimes();
                    yield return Advance(0.08);
                    yield break;
                }
                yield return null;
            }
            Assert.Fail("Scene restart did not produce a fresh running session.");
        }
        [UnityTest] public IEnumerator TimerUsesRealTimeFinishRequiresCoreAndRestartIsFast()
        {
            EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return Advance(0.08);
            var flow = Object.FindAnyObjectByType<GameFlow>(); flow.BestTimes = new MemoryBestTimes();
            Assert.That(flow.State, Is.EqualTo(RunState.Running));
            double before = flow.ElapsedSeconds;
            float originalScale = Time.timeScale, originalStep = Time.fixedDeltaTime;
            Assert.That(flow.timeDirector.Request(CombatMoment.Boost,50,Time.unscaledTime), Is.True);
            yield return Advance(0.12);
            Assert.That(flow.ElapsedSeconds - before, Is.GreaterThan(0.09));
            Assert.That(flow.FinishRun(), Is.False);
            flow.player.Teleport(new Vector3(0,0.15f,126));
            yield return Advance(0.08);
            Assert.That(flow.HasCore, Is.True); Assert.That(flow.core.gameObject.activeSelf, Is.False);
            flow.player.Teleport(new Vector3(0,0.15f,164));
            yield return Advance(0.08);
            Assert.That(flow.State, Is.EqualTo(RunState.Finished));
            double result = flow.ElapsedSeconds;
            Assert.That(flow.BestTimes.Best, Is.EqualTo(result)); Assert.That(flow.NewBest, Is.True);
            yield return Advance(0.1);
            Assert.That(flow.ElapsedSeconds, Is.EqualTo(result));
            Assert.That(Time.timeScale, Is.EqualTo(originalScale));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(originalStep).Within(0.000001f));
            Assert.That(flow.player.GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(flow.Die("late damage"), Is.False);
            Assert.That(flow.RestartRun(), Is.True); Assert.That(flow.State, Is.EqualTo(RunState.Restarting));
            Assert.That(flow.RestartRun(), Is.False);
            yield return WaitForFreshRun(flow);
            var fresh = Object.FindAnyObjectByType<GameFlow>();
            Assert.That(fresh.HasCore, Is.False); Assert.That(fresh.ElapsedSeconds, Is.LessThan(1));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator DeathAndRestartRestoreAllRunObjectsAndClock()
        {
            EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return Advance(0.08);
            var flow = Object.FindAnyObjectByType<GameFlow>(); flow.BestTimes = new MemoryBestTimes();
            var oldPlayer = flow.player;
            float scale = Time.timeScale, step = Time.fixedDeltaTime;
            var combat = oldPlayer.GetComponent<PlayerCombat>();
            var pickup = GameObject.Find("Shotgun routing pickup").GetComponent<WeaponPickup>();
            Assert.That(pickup.Collect(combat), Is.True);
            oldPlayer.AddVelocityChange(new Vector3(5,8,10)); oldPlayer.QueueJump(Time.time);
            var panel = GameObject.Find("Pressure access door").GetComponent<Breakable>();
            panel.ReceiveDamage(new DamageHit(100, panel.transform.position, Vector3.forward, oldPlayer.gameObject));
            Assert.That(panel.IsBroken, Is.True);
            var enemy = GameObject.Find("West sentry").GetComponent<CombatHealth>();
            enemy.ReceiveDamage(new DamageHit(1,enemy.transform.position,Vector3.forward,oldPlayer.gameObject));
            Assert.That(enemy.IsDead, Is.True);
            var barrel = GameObject.Find("Coolant barrel A").GetComponent<ExplosiveBarrel>();
            barrel.ReceiveDamage(new DamageHit(100,barrel.transform.position,Vector3.forward,oldPlayer.gameObject)); barrel.Tick(Time.time+1);
            Assert.That(barrel.Exploded, Is.True);
            GrenadeProjectile.Spawn(combat.tuning.weapons[2], new Ray(new Vector3(0,20,10),Vector3.forward), oldPlayer.gameObject, Vector3.zero);
            flow.timeDirector.Restore();
            Assert.That(flow.timeDirector.Request(CombatMoment.ChainExplosion,50,Time.unscaledTime+10), Is.True);
            oldPlayer.GetComponent<CombatHealth>().ReceiveDamage(new DamageHit(1000,oldPlayer.transform.position,Vector3.forward));
            yield return Advance(0.06);
            Assert.That(flow.State, Is.EqualTo(RunState.Dead));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(step).Within(0.000001f));
            Assert.That(flow.RestartRun(), Is.True);
            yield return WaitForFreshRun(flow);
            var fresh = Object.FindAnyObjectByType<GameFlow>();
            Assert.That(oldPlayer == null, Is.True);
            Assert.That(fresh.player.GetComponent<CombatHealth>().Health,
                Is.EqualTo(fresh.player.GetComponent<CombatHealth>().settings.maximumHealth));
            Assert.That(fresh.player.PlanarSpeed, Is.LessThan(0.1f));
            Assert.That(fresh.player.Traversal.IsGrappling, Is.False);
            Assert.That(fresh.player.Traversal.UsedWallCount, Is.Zero);
            Assert.That(fresh.player.IsCrouching, Is.False);
            Assert.That(fresh.player.GetComponent<PlayerCombat>().Equipped.kind, Is.EqualTo(WeaponKind.Pistol));
            Assert.That(fresh.player.GetComponent<PlayerCombat>().Equip(1), Is.False);
            Assert.That(fresh.player.GetComponent<PlayerCombat>().ShotsFired, Is.Zero);
            Assert.That(GameObject.Find("Shotgun routing pickup").GetComponent<WeaponPickup>().Collected, Is.False);
            var resetPanel = GameObject.Find("Pressure access door").GetComponent<Breakable>();
            Assert.That(resetPanel.IsBroken, Is.False); Assert.That(resetPanel.intactCollider.enabled, Is.True);
            Assert.That(resetPanel.fragments.All(f => f != null && !f.gameObject.activeSelf && f.isKinematic), Is.True);
            Assert.That(GameObject.Find("West sentry").GetComponent<CombatHealth>().IsDead, Is.False);
            Assert.That(GameObject.Find("West sentry").GetComponent<Rigidbody>().isKinematic, Is.True);
            Assert.That(GameObject.Find("Coolant barrel A").GetComponent<ExplosiveBarrel>().Armed, Is.False);
            Assert.That(Object.FindAnyObjectByType<GrenadeProjectile>(), Is.Null);
            Assert.That(TimeDirector.Instance, Is.EqualTo(fresh.timeDirector));
            Assert.That(Time.timeScale, Is.EqualTo(scale));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(step).Within(0.000001f));
            Assert.That(fresh.player.GetComponent<PlayerCombat>().TryFire(new Ray(fresh.player.transform.position,Vector3.up),Time.time), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator KillVolumeAndOutOfBoundsRestartAutomatically()
        {
            EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return Advance(0.06);
            var flow = Object.FindAnyObjectByType<GameFlow>(); flow.BestTimes = new MemoryBestTimes();
            temporarySettings = Object.Instantiate(flow.settings);
            temporarySettings.fallHeight = -100;
            temporarySettings.playableBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            flow.settings = temporarySettings;
            flow.player.Teleport(new Vector3(0,-12.5f,20));
            yield return Advance(0.08);
            Assert.That(flow.State, Is.EqualTo(RunState.Dead));
            Assert.That(flow.DeathReason, Does.Contain("coolant"));
            yield return WaitForFreshRun(flow);
            flow = Object.FindAnyObjectByType<GameFlow>();
            flow.player.Teleport(new Vector3(100,0.15f,0));
            yield return Advance(0.06);
            Assert.That(flow.State, Is.EqualTo(RunState.Dead));
            yield return WaitForFreshRun(flow);
            var fresh = Object.FindAnyObjectByType<GameFlow>();
            Assert.That(Vector3.Distance(fresh.player.transform.position,new Vector3(0,0.15f,0)), Is.LessThan(0.4f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator HeldFireDoesNotLeakAcrossRestart()
        {
            EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            yield return new EnterPlayMode();
            yield return Advance(0.08);
            var flow = Object.FindAnyObjectByType<GameFlow>(); flow.BestTimes = new MemoryBestTimes();
            // Headless has no focused Game View: route synthetic pointer events to play mode.
            previousEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            testMouse = InputSystem.AddDevice<Mouse>();
            flow.player.GetComponent<FpsInputView>().ActiveActions.devices = new InputDevice[] { testMouse };
            yield return Advance(0.08);
            InputSystem.QueueStateEvent(testMouse,new MouseState().WithButton(MouseButton.Left));
            yield return Advance(0.08);
            // The first click can recapture a headless Game View. Release its neutral gate.
            InputSystem.QueueStateEvent(testMouse,new MouseState());
            yield return Advance(0.08);
            InputSystem.QueueStateEvent(testMouse,new MouseState().WithButton(MouseButton.Left));
            yield return Advance(0.08);
            Assert.That(flow.player.GetComponent<PlayerCombat>().ShotsFired, Is.EqualTo(1), $"Synthetic fire: captured={flow.player.GetComponent<FpsInputView>().InputCaptured}, neutral={flow.player.GetComponent<FpsInputView>().WaitingForNeutral}, pressed={flow.player.GetComponent<FpsInputView>().ActiveActions.FindAction("Player/Attack").IsPressed()}, mouse={testMouse.leftButton.isPressed}");
            flow.RestartRun();
            yield return WaitForFreshRun(flow);
            var fresh = Object.FindAnyObjectByType<GameFlow>();
            yield return Advance(0.08);
            Assert.That(fresh.player.GetComponent<PlayerCombat>().ShotsFired, Is.Zero);
            Assert.That(fresh.player.GetComponent<FpsInputView>().WaitingForNeutral, Is.True);
            InputSystem.QueueStateEvent(testMouse,new MouseState());
            yield return Advance(0.04);
            Assert.That(fresh.player.GetComponent<FpsInputView>().InputCaptured, Is.True, "Releasing fire should reopen input.");
            InputSystem.QueueStateEvent(testMouse,new MouseState().WithButton(MouseButton.Left));
            yield return Advance(0.06);
            var inputView = fresh.player.GetComponent<FpsInputView>();
            var attack = inputView.ActiveActions.FindAction("Player/Attack");
            Assert.That(fresh.player.GetComponent<PlayerCombat>().ShotsFired, Is.EqualTo(1),
                $"captured={inputView.InputCaptured}, neutral={inputView.WaitingForNeutral}, viewEnabled={inputView.enabled}, combatEnabled={fresh.player.GetComponent<PlayerCombat>().enabled}, actionEnabled={attack.enabled}, pressed={attack.IsPressed()}, mousePressed={testMouse.leftButton.isPressed}, controls={string.Join(",", attack.controls.Select(c => c.path))}, state={fresh.State}");
            LogAssert.NoUnexpectedReceived();
        }
    }
}

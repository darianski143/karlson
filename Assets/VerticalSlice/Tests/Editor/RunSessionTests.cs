using System;
using System.Linq;
using Jimmy.Combat;
using Jimmy.Movement;
using Jimmy.Run.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Run.Tests
{
    public sealed class RunSessionTests
    {
        [Test] public void ReadyRunningDeadRestartingTransitionsAreGuarded()
        {
            var run = new RunSession();
            Assert.That(run.State, Is.EqualTo(RunState.Ready));
            Assert.That(run.Finish(0), Is.False); Assert.That(run.Die(0), Is.False);
            Assert.That(run.Begin(10), Is.True); Assert.That(run.Begin(11), Is.False);
            run.Tick(15); Assert.That(run.Elapsed, Is.EqualTo(5));
            Assert.That(run.Die(16), Is.True); Assert.That(run.State, Is.EqualTo(RunState.Dead));
            run.Tick(99); Assert.That(run.Elapsed, Is.EqualTo(6));
            Assert.That(run.Finish(100), Is.False);
            Assert.That(run.Restart(101), Is.True); Assert.That(run.Restart(102), Is.False);
            Assert.That(run.State, Is.EqualTo(RunState.Restarting));
        }
        [Test] public void FinishFreezesTimerAndIgnoresInvalidClockSamples()
        {
            var run = new RunSession(); run.Begin(100); run.Tick(105);
            run.Tick(103); run.Tick(double.NaN); Assert.That(run.Elapsed, Is.EqualTo(5));
            Assert.That(run.Finish(112.345), Is.True);
            run.Tick(900); Assert.That(run.Elapsed, Is.EqualTo(12.345).Within(0.00001));
            Assert.That(run.Die(113), Is.False); Assert.That(run.Finish(114), Is.False);
            Assert.That(run.Restart(115), Is.True);
        }
        [Test] public void BestTimeKeepsOnlyFasterValidResults()
        {
            var store = new MemoryBestTimes(); Assert.That(store.Best, Is.Null);
            Assert.That(store.Record(60.25), Is.True); Assert.That(store.Record(70), Is.False);
            Assert.That(store.Record(60.25), Is.False); Assert.That(store.Record(40.1), Is.True);
            Assert.That(store.Record(0), Is.False); Assert.That(store.Record(double.NaN), Is.False);
            Assert.That(store.Best, Is.EqualTo(40.1));
        }
        [Test] public void LocalBestPersistsByLevelKeyAndRejectsCorruptData()
        {
            string key = "jimmy.tests.run." + Guid.NewGuid().ToString("N");
            try
            {
                PlayerPrefs.SetString(key, "not a score");
                var first = new LocalBestTimes(key); Assert.That(first.Best, Is.Null);
                Assert.That(first.Record(51.234), Is.True);
                var second = new LocalBestTimes(key); Assert.That(second.Best, Is.EqualTo(51.234));
                Assert.That(second.Record(70), Is.False);
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
        [TestCase(0, "00:00.000")]
        [TestCase(59.9996, "01:00.000")]
        [TestCase(75.123, "01:15.123")]
        public void TimeFormattingHandlesMinuteRollover(double seconds, string expected) =>
            Assert.That(RunHud.FormatTime(seconds), Is.EqualTo(expected));

        [Test] public void LevelHasRequiredReferencesThreeRoutesAndSafeSpawn()
        {
            var scene = EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
            Assert.That(flow, Is.Not.Null); Assert.That(flow.settings, Is.Not.Null);
            Assert.That(flow.player, Is.Not.Null); Assert.That(flow.timeDirector, Is.Not.Null);
            Assert.That(flow.core.flow, Is.EqualTo(flow)); Assert.That(flow.extraction.flow, Is.EqualTo(flow));
            Assert.That(flow.core.kind, Is.EqualTo(RunTriggerKind.Core));
            Assert.That(flow.extraction.kind, Is.EqualTo(RunTriggerKind.Extraction));
            Assert.That(flow.settings.debugTelemetry, Is.False);
            Assert.That(flow.player.GetComponent<MovementTelemetry>().enabled, Is.False);
            Assert.That(flow.player.GetComponent<PlayerCombatVitals>().enabled, Is.False);
            Assert.That(flow.player.GetComponent<FpsInputView>().externalRespawnControl, Is.True);
            Assert.That(flow.player.GetComponent<PlayerCombat>().tuning.telemetry, Is.False);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == RelayLevelBuilder.ScenePath && s.enabled), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path.EndsWith("MovementGym.unity")), Is.True);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            var routes = UnityEngine.Object.FindObjectsByType<LevelRoute>(FindObjectsSortMode.None);
            Assert.That(routes.Select(r => r.kind).Distinct().Count(), Is.EqualTo(3));
            Assert.That(UnityEngine.Object.FindObjectsByType<RunTrigger>(FindObjectsSortMode.None).Any(t => t.kind == RunTriggerKind.Kill), Is.True);
            Physics.SyncTransforms();
            var spawn = flow.player.transform.position;
            Assert.That(Vector3.Distance(spawn, new Vector3(0,0.15f,0)), Is.LessThan(0.01f));
            Assert.That(CombatQueries.Raycast(new Ray(spawn + Vector3.up, Vector3.down), 3, ~0, flow.player.transform, out var floor), Is.True);
            Assert.That(floor.point.y, Is.EqualTo(0).Within(0.05f));
            Assert.That(Vector3.Dot(floor.normal, Vector3.up), Is.GreaterThan(0.95f));
            var body = flow.player.GetComponent<Rigidbody>();
            var capsule = flow.player.GetComponent<CapsuleCollider>();
            foreach (var overlap in Physics.OverlapCapsule(spawn + Vector3.up * capsule.radius,
                spawn + Vector3.up * (capsule.height - capsule.radius), capsule.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore))
                Assert.That(overlap.attachedRigidbody, Is.EqualTo(body), "Spawn capsule intersects level geometry.");
        }
        [Test] public void SafeRouteHasContinuousWalkableFloor()
        {
            EditorSceneManager.OpenScene(RelayLevelBuilder.ScenePath);
            var flow = UnityEngine.Object.FindAnyObjectByType<GameFlow>();
            var route = UnityEngine.Object.FindObjectsByType<LevelRoute>(FindObjectsSortMode.None).Single(r => r.kind == RouteKind.Safe);
            Physics.SyncTransforms();
            float length = 0;
            for (int i = 1; i < route.waypoints.Length; i++)
            {
                Vector3 start = route.waypoints[i-1], end = route.waypoints[i];
                float segment = Vector3.Distance(start,end); length += segment;
                for (int step = 0; step <= Mathf.CeilToInt(segment); step++)
                {
                    Vector3 point = Vector3.Lerp(start,end,step/Mathf.Max(1,segment));
                    Assert.That(CombatQueries.Raycast(new Ray(point + Vector3.up * 0.5f, Vector3.down), 0.8f, ~0, flow.player.transform, out var hit),
                        Is.True, "No safe floor at " + point);
                    Assert.That(Vector3.Dot(hit.normal,Vector3.up), Is.GreaterThan(0.9f), "Non-walkable floor at " + point);
                    Assert.That(hit.point.y, Is.EqualTo(0).Within(0.1f), "Unexpected obstacle at " + point);
                }
            }
            Assert.That(length, Is.GreaterThan(280));
        }
    }
}

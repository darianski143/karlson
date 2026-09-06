using System.Collections;
using Jimmy.Movement;
using Jimmy.Movement.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jimmy.Combat.Tests
{
    public sealed class CombatPlayModeTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (TimeDirector.Instance != null) TimeDirector.Instance.Restore();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        private static IEnumerator Advance(float seconds)
        {
            float start = Time.time;
            while (Time.time - start < seconds) yield return null;
        }
        [UnityTest] public IEnumerator RealPickupShotgunRecoilAndBallisticGrenade()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            TimeDirector.Instance.enabled = false;
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            var view = motor.GetComponent<FpsInputView>(); view.enabled = false;
            var combat = motor.GetComponent<PlayerCombat>();
            Assert.That(combat.Equipped.kind, Is.EqualTo(WeaponKind.Pistol));
            var pickup = GameObject.Find("Shotgun pickup").GetComponent<WeaponPickup>();
            motor.Teleport(pickup.transform.position - Vector3.up * 0.6f);
            yield return Advance(0.12f);
            Assert.That(pickup.Collected, Is.True);
            Assert.That(combat.Equipped.kind, Is.EqualTo(WeaponKind.Shotgun));
            var body = motor.GetComponent<Rigidbody>(); body.position = new Vector3(88, 6, 0); body.linearVelocity = Vector3.forward * 10;
            Assert.That(combat.TryFire(new Ray(body.position + Vector3.up, Vector3.down), Time.time), Is.True);
            yield return Advance(0.04f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(5)); Assert.That(body.linearVelocity.z, Is.GreaterThan(9));
            var weapon = combat.tuning.weapons[2];
            var grenade = GrenadeProjectile.Spawn(weapon, new Ray(new Vector3(80, 8, 0), Vector3.forward), motor.gameObject, Vector3.forward * 5);
            yield return Advance(0.12f);
            Assert.That(grenade.GetComponent<Rigidbody>().linearVelocity.y, Is.LessThan(-1));
            Assert.That(grenade.GetComponent<Rigidbody>().linearVelocity.z, Is.GreaterThan(weapon.projectileSpeed));
            yield return Advance(weapon.fuse + 0.15f);
            Assert.That(grenade == null, Is.True);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator ExplosionBoostWorksOnLivePlayer()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            TimeDirector.Instance.enabled = false;
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            motor.GetComponent<FpsInputView>().enabled = false;
            motor.Teleport(new Vector3(95, 5, 0));
            yield return Advance(0.04f);
            var body = motor.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.forward * 11;
            var explosion = motor.GetComponent<PlayerCombat>().tuning.weapons[2].explosion;
            ExplosionService.Detonate(body.position + Vector3.down * 0.5f, explosion, motor.gameObject);
            yield return Advance(0.04f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(9));
            Assert.That(body.linearVelocity.z, Is.EqualTo(11).Within(0.1f));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator TimeDirectorRestoresOnDisableAndPlayExit()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            var director = TimeDirector.Instance;
            float scale = Time.timeScale, step = Time.fixedDeltaTime;
            Assert.That(director.Request(CombatMoment.Boost, 50, Time.unscaledTime), Is.True);
            Assert.That(Time.timeScale, Is.LessThan(scale));
            director.enabled = false;
            Assert.That(Time.timeScale, Is.EqualTo(scale)); Assert.That(Time.fixedDeltaTime, Is.EqualTo(step));
            director.enabled = true;
            Assert.That(director.Request(CombatMoment.ChainExplosion, 50, Time.unscaledTime + 10), Is.True);
            yield return new ExitPlayMode();
            Assert.That(Time.timeScale, Is.EqualTo(scale)); Assert.That(Time.fixedDeltaTime, Is.EqualTo(step));
        }
        [UnityTest] public IEnumerator CameraCombatEffectsCanBeDisabled()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            var feedback = motor.GetComponent<CombatFeedback>();
            var original = feedback.settings;
            var temporary = Object.Instantiate(original); temporary.cameraIntensity = 0; feedback.settings = temporary;
            feedback.Fired(3);
            yield return Advance(0.08f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, feedback.playerCamera.transform.localEulerAngles.x)), Is.LessThan(0.01f));
            feedback.settings = original; Object.Destroy(temporary);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Jimmy.Movement.Editor;

namespace Jimmy.Movement.Tests
{
    public sealed class VerticalPlayModeTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        private static IEnumerator Advance(float seconds)
        {
            float start = Time.time;
            while (Time.time - start < seconds) yield return null;
        }
        [UnityTest] public IEnumerator PadTriggerThenWallJumpWorksInRealScene()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            motor.GetComponent<FpsInputView>().enabled = false;
            Assert.That(GameObject.Find("Vertical Traversal / 02").transform.childCount, Is.GreaterThanOrEqualTo(4));
            motor.Teleport(new Vector3(60, 0.5f, 9));
            var body = motor.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.down * 2;
            yield return Advance(0.16f);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(8), "Real trigger should launch the player.");
            body.position = new Vector3(60, 6, 16); body.linearVelocity = Vector3.forward * 10;
            motor.SetInput(Vector2.up, false);
            yield return Advance(0.1f);
            Assert.That(motor.Traversal.IsWallRunning, Is.True);
            float forward = body.linearVelocity.z; motor.QueueJump(Time.time);
            yield return Advance(0.04f);
            Assert.That(body.linearVelocity.z, Is.GreaterThanOrEqualTo(forward - 0.3f));
            Assert.That(body.linearVelocity.x, Is.GreaterThan(4));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest] public IEnumerator GrappleSwingReleaseAndCameraComfort()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            var view = motor.GetComponent<FpsInputView>();
            var traversal = motor.Traversal;
            // Clone tuning so the comfort check cannot edit a persistent asset.
            var original = traversal.settings;
            var temporary = Object.Instantiate(original); traversal.settings = temporary;
            view.enabled = false;
            motor.Teleport(new Vector3(42, 4.1f, 11));
            var target = Object.FindAnyObjectByType<GrappleAnchor>().transform;
            Assert.That(traversal.TryGrapple(new Ray(traversal.AttachmentPoint, target.position - traversal.AttachmentPoint), Time.time), Is.True);
            motor.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 10;
            yield return Advance(0.6f);
            Assert.That(traversal.IsGrappling, Is.True);
            Assert.That(traversal.RopeDistance, Is.LessThanOrEqualTo(traversal.RopeLength + 0.08f));
            Vector3 before = motor.Velocity; traversal.enabled = false;
            Assert.That(traversal.IsGrappling, Is.False);
            Assert.That(motor.Velocity, Is.EqualTo(before));
            temporary.cameraEffectIntensity = 0;
            view.enabled = true;
            yield return Advance(0.05f);
            Assert.That(view.playerCamera.fieldOfView, Is.EqualTo(motor.settings.fieldOfView).Within(0.01f));
            Assert.That(Mathf.DeltaAngle(0, view.playerCamera.transform.localEulerAngles.z), Is.EqualTo(0).Within(0.01f));
            traversal.settings = original; Object.Destroy(temporary);
            LogAssert.NoUnexpectedReceived();
        }
    }
}

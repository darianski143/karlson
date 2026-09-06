using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Jimmy.Movement.Editor;

namespace Jimmy.Movement.Tests
{
    public sealed class GymPlayModeSmokeTests
    {
        [UnityTearDown]
        public IEnumerator LeavePlayMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator GymStartsAndRespawnsInPlayMode()
        {
            EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            yield return new EnterPlayMode();
            var motor = Object.FindAnyObjectByType<RigidbodyMotor>();
            Assert.That(motor, Is.Not.Null);
            float started = Time.time;
            while (Time.time - started < 1f) yield return null;
            Assert.That(motor.IsGrounded, Is.True,
                $"Position={motor.transform.position} velocity={motor.Velocity} state={motor.State} mode={Physics.simulationMode}");
            var view = motor.GetComponent<FpsInputView>();
            Assert.That(view.playerCamera.fieldOfView, Is.EqualTo(motor.settings.fieldOfView));
            motor.Teleport(new Vector3(0, -20, 0));
            yield return null;
            yield return null;
            Assert.That(Vector3.Distance(motor.transform.position, view.spawnPosition), Is.LessThan(0.5f));
            LogAssert.NoUnexpectedReceived();
            yield return new ExitPlayMode();
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;
using Jimmy.Movement.Editor;

namespace Jimmy.Movement.Tests
{
    public sealed class MovementMotorTests
    {
        private GameObject player, floor, ceiling;
        private MovementSettings settings;
        private RigidbodyMotor motor;
        private Rigidbody body;
        private SimulationMode previousMode;
        private float now;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            settings = ScriptableObject.CreateInstance<MovementSettings>();
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -0.5f, 0); floor.transform.localScale = new Vector3(100, 1, 100);
            player = new GameObject("Test player");
            player.SetActive(false);
            body = player.AddComponent<Rigidbody>(); player.AddComponent<CapsuleCollider>();
            motor = player.AddComponent<RigidbodyMotor>(); motor.settings = settings;
            player.SetActive(true);
            // EditMode does not invoke ordinary MonoBehaviour.Awake automatically.
            motor.Initialize();
            player.transform.position = new Vector3(0, 0.02f, 0);
            Physics.SyncTransforms(); now = 0;
        }
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player); Object.DestroyImmediate(floor);
            if (ceiling != null) Object.DestroyImmediate(ceiling);
            Object.DestroyImmediate(settings); Physics.simulationMode = previousMode;
        }
        private void Tick(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                now += 0.02f; Physics.SyncTransforms(); motor.Simulate(0.02f, now); Physics.Simulate(0.02f);
            }
        }
        [Test] public void GroundAccelerationAndBraking()
        {
            motor.SetInput(Vector2.up, false); Tick(40);
            Assert.That(motor.PlanarSpeed, Is.EqualTo(settings.groundSpeed).Within(0.2f));
            motor.SetInput(Vector2.zero, false); Tick(30);
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.1f));
        }
        [Test] public void AirAccelerationPreservesVerticalAndExternalMomentum()
        {
            var velocity = RigidbodyMotor.Accelerate(new Vector3(0, 3, 18), Vector3.forward, 10, 20, 0.02f);
            Assert.That(velocity, Is.EqualTo(new Vector3(0, 3, 18)));
            body.position = new Vector3(0, 10, 0); motor.SetInput(Vector2.right, false); Tick();
            Assert.That(body.linearVelocity.x, Is.EqualTo(settings.airAcceleration * 0.02f).Within(0.01f));
        }
        [Test] public void CoyoteJumpWorksOnce()
        {
            Tick(); floor.SetActive(false); Tick(3); motor.QueueJump(now); Tick();
            Assert.That(body.linearVelocity.y, Is.GreaterThan(7));
            float before = body.linearVelocity.y; motor.QueueJump(now); Tick();
            Assert.That(body.linearVelocity.y, Is.LessThan(before));
        }
        [Test] public void ExpiredCoyoteCannotJump()
        {
            Tick(); floor.SetActive(false); Tick(10); motor.QueueJump(now); Tick();
            Assert.That(body.linearVelocity.y, Is.LessThan(0));
        }
        [Test] public void BufferedJumpFiresOnLanding()
        {
            body.position = new Vector3(0, 0.3f, 0); body.linearVelocity = Vector3.down * 3;
            motor.QueueJump(now); Tick(5);
            Assert.That(body.linearVelocity.y, Is.GreaterThan(5));
        }
        [Test] public void ExpiredJumpBufferDoesNotFire()
        {
            body.position = new Vector3(0, 3, 0); motor.QueueJump(now); Tick(50);
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(body.linearVelocity.y, Is.LessThanOrEqualTo(0.1f));
        }
        [Test] public void CrouchCannotStandInsideCeiling()
        {
            motor.SetInput(Vector2.zero, true); Tick();
            ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.position = new Vector3(0, 1.4f, 0); ceiling.transform.localScale = new Vector3(4, 0.5f, 4);
            motor.SetInput(Vector2.zero, false); Tick(); Assert.That(motor.IsCrouching, Is.True);
            ceiling.SetActive(false); Tick(); Assert.That(motor.IsCrouching, Is.False);
        }
        [Test] public void SlideRequiresSpeedAndFreshPressAndExpires()
        {
            motor.SetInput(Vector2.zero, true); Tick(); Assert.That(motor.State, Is.EqualTo(MovementState.Crouching));
            motor.SetInput(Vector2.up, false); Tick(30);
            motor.SetInput(Vector2.up, true); Tick(); Assert.That(motor.State, Is.EqualTo(MovementState.Sliding));
            Tick(80); Assert.That(motor.State, Is.EqualTo(MovementState.Crouching));
        }
        [Test] public void ExternalUpwardImpulseSurvivesFollowingTick()
        {
            Tick(); motor.AddVelocityChange(new Vector3(20, 10, 0)); Tick(2);
            Assert.That(body.linearVelocity.x, Is.GreaterThan(19));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(7));
            Assert.That(motor.IsGrounded, Is.False);
        }
        [Test] public void SteepSurfaceIsNotGround()
        {
            floor.transform.rotation = Quaternion.Euler(0, 0, 65);
            floor.transform.localScale = new Vector3(4, 0.2f, 4);
            floor.transform.position = new Vector3(0, -0.2f, 0); Tick();
            Assert.That(motor.IsGrounded, Is.False);
        }
        [Test] public void WalkableSlopeSupportsMovement()
        {
            floor.transform.rotation = Quaternion.Euler(0, 0, 25);
            floor.transform.localScale = new Vector3(20, 0.2f, 20);
            floor.transform.position = new Vector3(0, -0.2f, 0);
            motor.Teleport(new Vector3(0, -0.03f, 0));
            motor.SetInput(Vector2.right, false); Tick(20);
            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(Vector3.Angle(motor.GroundNormal, Vector3.up), Is.EqualTo(25).Within(1));
            Assert.That(body.position.x, Is.GreaterThan(0.5f));
        }
        [Test] public void RespawnClearsMotionAndBufferedInput()
        {
            motor.AddVelocityChange(Vector3.one * 50); motor.QueueJump(now);
            motor.Teleport(new Vector3(0, 5, 0)); Tick();
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.01f)); Assert.That(body.linearVelocity.y, Is.LessThan(0));
        }
        [Test] public void GymHasConfiguredPlayerAndNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath, OpenSceneMode.Additive);
            try
            {
                int players = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
                    foreach (var controller in root.GetComponentsInChildren<RigidbodyMotor>())
                    {
                        players++; Assert.That(controller.settings, Is.Not.Null);
                        Assert.That(controller.GetComponent<FpsInputView>().inputActions, Is.Not.Null);
                        Assert.That(controller.GetComponentInChildren<Camera>(), Is.Not.Null);
                    }
                }
                Assert.That(players, Is.EqualTo(1));
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}

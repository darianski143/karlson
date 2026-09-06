using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Movement.Tests
{
    public sealed class TraversalTests
    {
        private GameObject player, wall, anchor;
        private RigidbodyMotor motor;
        private VerticalTraversal traversal;
        private Rigidbody body;
        private MovementSettings movement;
        private TraversalSettings settings;
        private SimulationMode oldMode;
        private float now;
        [SetUp] public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            oldMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
            movement = ScriptableObject.CreateInstance<MovementSettings>();
            settings = ScriptableObject.CreateInstance<TraversalSettings>();
            player = new GameObject("Traversal test player");
            player.SetActive(false); body = player.AddComponent<Rigidbody>(); player.AddComponent<CapsuleCollider>();
            motor = player.AddComponent<RigidbodyMotor>(); motor.settings = movement;
            traversal = player.AddComponent<VerticalTraversal>(); traversal.settings = settings;
            player.SetActive(true); motor.Initialize(); motor.Teleport(new Vector3(0, 4, 0)); now = 0;
        }
        [TearDown] public void Teardown()
        {
            Object.DestroyImmediate(player);
            if (wall != null) Object.DestroyImmediate(wall);
            if (anchor != null) Object.DestroyImmediate(anchor);
            Object.DestroyImmediate(movement); Object.DestroyImmediate(settings);
            Physics.simulationMode = oldMode;
        }
        private void MakeWall(int side)
        {
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(side * 0.9f, 5, 15);
            wall.transform.localScale = new Vector3(0.3f, 20, 80);
            wall.AddComponent<WallSurface>(); Physics.SyncTransforms();
        }
        private void Tick(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                now += 0.02f; Physics.SyncTransforms(); motor.Simulate(0.02f, now); Physics.Simulate(0.02f);
            }
        }
        private void Attach()
        {
            anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            anchor.transform.position = new Vector3(0, 12, 10); anchor.transform.localScale = Vector3.one * 2;
            anchor.AddComponent<GrappleAnchor>(); Physics.SyncTransforms();
            var origin = traversal.AttachmentPoint;
            Assert.That(traversal.TryGrapple(new Ray(origin, anchor.transform.position - origin), now), Is.True);
        }
        [TestCase(-1)] [TestCase(1)]
        public void DetectsAndRunsBothSides(int side)
        {
            MakeWall(side); body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, false); Tick(5);
            Assert.That(traversal.IsWallRunning, Is.True);
            Assert.That(traversal.WallSide, Is.EqualTo(side));
            Assert.That(motor.State, Is.EqualTo(side < 0 ? MovementState.WallRunningLeft : MovementState.WallRunningRight));
        }
        [Test] public void RejectsSlopedWallsAndTriggers()
        {
            MakeWall(1); wall.transform.rotation = Quaternion.Euler(0, 0, 40); Physics.SyncTransforms();
            Assert.That(traversal.DetectWall(1, out _), Is.False);
            wall.transform.rotation = Quaternion.identity; wall.GetComponent<Collider>().isTrigger = true; Physics.SyncTransforms();
            Assert.That(traversal.DetectWall(1, out _), Is.False);
        }
        [Test] public void WallJumpPreservesForwardMomentum()
        {
            MakeWall(1); body.linearVelocity = Vector3.forward * 18; motor.SetInput(Vector2.up, false); Tick();
            float forward = body.linearVelocity.z; motor.QueueJump(now); Tick();
            Assert.That(body.linearVelocity.z, Is.EqualTo(forward).Within(0.1f));
            Assert.That(body.linearVelocity.x, Is.LessThan(-5));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(7));
            Assert.That(traversal.IsWallRunning, Is.False);
        }
        [Test] public void SameWallCannotBeReusedUntilGrounded()
        {
            MakeWall(1); body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, false); Tick();
            motor.QueueJump(now); Tick(); Tick(15);
            // Move back beside it without respawn/reset: reattachment must remain blocked.
            body.position = new Vector3(0, 4, 10); body.linearVelocity = Vector3.forward * 10; Tick();
            Assert.That(traversal.IsWallRunning, Is.False);
            Assert.That(traversal.UsedWallCount, Is.EqualTo(1));
            Vector3 velocity = Vector3.zero;
            traversal.Step(ref velocity, Vector2.zero, false, false, true, 0.02f, now);
            Tick(15); body.position = new Vector3(0, 4, 10); body.linearVelocity = Vector3.forward * 10; Tick();
            Assert.That(traversal.IsWallRunning, Is.True);
        }
        [Test] public void WallTimeoutAndSegmentIdentityPreventRefill()
        {
            MakeWall(1); settings.wallDuration = 0.1f;
            body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, false); Tick(20);
            Assert.That(traversal.IsWallRunning, Is.False);
            Assert.That(traversal.UsedWallCount, Is.EqualTo(1));
        }
        [Test] public void ColliderSegmentsShareOneWallBudget()
        {
            MakeWall(1);
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.transform.SetParent(wall.transform, false);
            segment.transform.localPosition = Vector3.zero;
            var first = wall.GetComponent<Collider>();
            segment.GetComponent<Collider>().enabled = false;
            body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, false); Tick();
            motor.QueueJump(now); Tick(); Tick(15);
            first.enabled = false; segment.GetComponent<Collider>().enabled = true;
            body.position = new Vector3(0, 4, 10); body.linearVelocity = Vector3.forward * 10; Tick();
            Assert.That(traversal.IsWallRunning, Is.False);
            Assert.That(traversal.UsedWallCount, Is.EqualTo(1));
        }
        [Test] public void DifferentWallCanBeUsedWithoutRefillingTheFirst()
        {
            MakeWall(1); body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, false); Tick();
            motor.QueueJump(now); Tick(); Tick(15);
            var other = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                other.transform.position = new Vector3(-0.9f, 5, 15);
                other.transform.localScale = new Vector3(0.3f, 20, 80); other.AddComponent<WallSurface>();
                body.position = new Vector3(0, 4, 10); body.linearVelocity = Vector3.forward * 10; Tick();
                Assert.That(traversal.WallSide, Is.EqualTo(-1));
                Assert.That(traversal.UsedWallCount, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(other); }
        }
        [Test] public void SlideJumpTransitionsIntoWallRunThenWallJump()
        {
            MakeWall(1);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                floor.transform.position = new Vector3(0, -0.5f, 0);
                floor.transform.localScale = new Vector3(4, 1, 20);
                motor.Teleport(new Vector3(0, 0.02f, 0));
                body.linearVelocity = Vector3.forward * 10; motor.SetInput(Vector2.up, true); Tick();
                Assert.That(motor.State, Is.EqualTo(MovementState.Sliding));
                motor.SetInput(Vector2.up, false); motor.QueueJump(now); Tick();
                Assert.That(traversal.IsWallRunning, Is.True);
                motor.QueueJump(now); Tick();
                Assert.That(traversal.IsWallRunning, Is.False);
                Assert.That(body.linearVelocity.x, Is.LessThan(-5));
                Assert.That(body.linearVelocity.z, Is.GreaterThan(10));
            }
            finally { Object.DestroyImmediate(floor); }
        }
        [Test] public void RopeLimitsDistanceOverManyPhysicsSteps()
        {
            Attach(); body.linearVelocity = Vector3.right * 12; motor.SetInput(Vector2.zero, false);
            for (int i = 0; i < 150; i++)
            {
                Tick();
                Assert.That(traversal.IsGrappling, Is.True);
                Assert.That(traversal.RopeDistance, Is.LessThanOrEqualTo(traversal.RopeLength + 0.03f));
            }
        }
        [Test] public void SlackRopeDoesNotPushAndReleasePreservesVelocity()
        {
            Vector3 velocity = new Vector3(3, -2, 5);
            Assert.That(VerticalTraversal.ConstrainVelocity(Vector3.zero, Vector3.up * 3, 10, velocity, 0.02f), Is.EqualTo(velocity));
            Attach(); body.linearVelocity = Vector3.right * 12; Tick(20);
            Vector3 before = body.linearVelocity; float tangent = traversal.TangentialSpeed;
            traversal.ReleaseGrapple();
            Assert.That(body.linearVelocity, Is.EqualTo(before));
            Assert.That(tangent, Is.GreaterThan(1));
            Assert.That(traversal.IsGrappling, Is.False);
        }
        [Test] public void OccluderBlocksGrappleAndDestroyedAnchorReleases()
        {
            Attach(); traversal.ReleaseGrapple();
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(traversal.AttachmentPoint, anchor.transform.position, 0.5f);
            wall.transform.localScale = Vector3.one * 3; Physics.SyncTransforms();
            Assert.That(traversal.TryGrapple(new Ray(traversal.AttachmentPoint, anchor.transform.position - traversal.AttachmentPoint), 1), Is.False);
            wall.SetActive(false); Physics.SyncTransforms();
            Assert.That(traversal.TryGrapple(new Ray(traversal.AttachmentPoint, anchor.transform.position - traversal.AttachmentPoint), 2), Is.True);
            anchor.SetActive(false); Tick(); Assert.That(traversal.IsGrappling, Is.False);
        }
        [Test] public void JumpPadPreservesLateralVelocityAndHasCooldown()
        {
            var padObject = new GameObject("Pad", typeof(BoxCollider), typeof(JumpPad));
            var pad = padObject.GetComponent<JumpPad>(); var config = ScriptableObject.CreateInstance<JumpPadSettings>(); pad.settings = config;
            try
            {
                body.linearVelocity = new Vector3(8, -10, 4);
                Assert.That(pad.TryLaunch(motor, now), Is.True); Tick();
                Assert.That(body.linearVelocity.x, Is.EqualTo(8).Within(0.01f));
                Assert.That(body.linearVelocity.z, Is.EqualTo(4).Within(0.01f));
                Assert.That(body.linearVelocity.y, Is.GreaterThan(17));
                Assert.That(pad.TryLaunch(motor, now), Is.False);
                Assert.That(pad.TryLaunch(motor, now + config.retriggerDelay), Is.True);
            }
            finally { Object.DestroyImmediate(padObject); Object.DestroyImmediate(config); }
        }
        [Test] public void RespawnClearsTraversal()
        {
            Attach(); motor.Teleport(new Vector3(0, 4, 0));
            Assert.That(traversal.IsGrappling, Is.False); Assert.That(traversal.UsedWallCount, Is.Zero);

        }
    }
}

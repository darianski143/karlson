using UnityEngine;

namespace Jimmy.Movement
{
    public enum MovementState { Airborne, Grounded, Crouching, Sliding, WallRunningLeft, WallRunningRight, Grappling }

    // The root is at the player's feet; collision geometry grows upwards.
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class RigidbodyMotor : MonoBehaviour
    {
        public MovementSettings settings;
        public MovementState State { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsCrouching { get; private set; }
        public Vector3 GroundNormal { get; private set; } = Vector3.up;
        public Vector3 Velocity => body.linearVelocity;
        public float PlanarSpeed => Vector3.ProjectOnPlane(Velocity, Vector3.up).magnitude;
        public float Height => capsule.height;
        public VerticalTraversal Traversal => GetComponent<VerticalTraversal>();

        public void DetachGround(float now, float duration)
        {
            IsGrounded = false;
            lastGroundedAt = float.NegativeInfinity;
            ignoreGroundUntil = Mathf.Max(ignoreGroundUntil, now + duration);
        }

        public void Launch(Vector3 direction, float speed, float now, float groundGrace)
        {
            Initialize();
            direction.Normalize();
            Traversal?.Interrupt(now);
            Vector3 effectiveVelocity = body.linearVelocity + pendingVelocityChange;
            pendingVelocityChange += direction * Mathf.Max(0, speed - Vector3.Dot(effectiveVelocity, direction));
            DetachGround(now, groundGrace);
            jumpRequestedAt = float.NegativeInfinity;
        }

        private Rigidbody body;
        private CapsuleCollider capsule;
        private PhysicsMaterial frictionless;
        private readonly RaycastHit[] groundHits = new RaycastHit[16];
        private readonly Collider[] clearanceHits = new Collider[16];
        private Vector2 moveInput;
        private bool crouchHeld, crouchWasHeld;
        private float jumpRequestedAt = float.NegativeInfinity;
        private float lastGroundedAt = float.NegativeInfinity;
        private float ignoreGroundUntil = float.NegativeInfinity;
        private float slideRemaining;
        private Vector3 pendingVelocityChange;

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (body != null) return;
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = 0;
            frictionless = new PhysicsMaterial("Motor frictionless")
            {
                dynamicFriction = 0, staticFriction = 0, bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            capsule.sharedMaterial = frictionless;
            capsule.direction = 1;
            if (settings != null) SetHeight(settings.standingHeight);
        }

        private void OnDestroy()
        {
            if (frictionless != null)
            {
                if (Application.isPlaying) Destroy(frictionless);
                else DestroyImmediate(frictionless);
            }
        }

        public void SetInput(Vector2 move, bool crouch)
        {
            moveInput = Vector2.ClampMagnitude(move, 1f);
            crouchHeld = crouch;
        }

        public void QueueJump(float time) => jumpRequestedAt = time;

        // Delta velocity, independent of mass. Grapple/recoil/wall-jump can use this
        // without reaching into Rigidbody or being erased by a speed clamp.
        public void AddVelocityChange(Vector3 deltaVelocity) => pendingVelocityChange += deltaVelocity;

        public void ClearInput()
        {
            moveInput = Vector2.zero;
            crouchHeld = false;
            jumpRequestedAt = float.NegativeInfinity;
            Traversal?.Interrupt(Time.time);
        }

        public void Teleport(Vector3 feetPosition)
        {
            body.position = feetPosition;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            pendingVelocityChange = Vector3.zero;
            lastGroundedAt = jumpRequestedAt = ignoreGroundUntil = float.NegativeInfinity;
            slideRemaining = 0;
            IsGrounded = IsCrouching = crouchWasHeld = false;
            State = MovementState.Airborne;
            ClearInput();
            Traversal?.ResetTraversal();
            SetHeight(settings.standingHeight);
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime, Time.time);

        // Explicit timestep also permits deterministic input replay and physics tests.
        public void Simulate(float dt, float now)
        {
            if (settings == null || dt <= 0) return;
            var traversal = Traversal;
            ProbeGround(now);
            if (traversal != null && traversal.IsGrappling) IsGrounded = false;
            if (IsGrounded) lastGroundedAt = now;
            bool wantsCrouch = crouchHeld || (IsCrouching && !CanStand());
            IsCrouching = wantsCrouch;
            SetHeight(wantsCrouch ? settings.crouchingHeight : settings.standingHeight);

            Vector3 velocity = body.linearVelocity;
            Vector3 wish = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (IsGrounded) wish = Vector3.ProjectOnPlane(wish, GroundNormal).normalized * moveInput.magnitude;
            if (crouchHeld && !crouchWasHeld && IsGrounded && PlanarSpeed >= settings.slideEntrySpeed)
            {
                slideRemaining = settings.slideDuration;
                velocity += Vector3.ProjectOnPlane(velocity, GroundNormal).normalized * settings.slideBoost;
            }
            crouchWasHeld = crouchHeld;
            if (!IsGrounded || !crouchHeld) slideRemaining = 0;

            bool jumping = (traversal == null || !traversal.IsWallRunning) && now - jumpRequestedAt <= settings.jumpBuffer &&
                           now - lastGroundedAt <= settings.coyoteTime && now >= ignoreGroundUntil;
            if (jumping)
            {
                velocity.y = Mathf.Sqrt(2f * settings.gravity * settings.jumpHeight);
                IsGrounded = false;
                lastGroundedAt = jumpRequestedAt = float.NegativeInfinity;
                ignoreGroundUntil = now + 0.1f;
                slideRemaining = 0;
            }

            if (IsGrounded)
            {
                Vector3 tangent = Vector3.ProjectOnPlane(velocity, GroundNormal);
                if (slideRemaining > 0)
                {
                    slideRemaining = Mathf.Max(0, slideRemaining - dt);
                    tangent = Vector3.MoveTowards(tangent, Vector3.zero, settings.slideFriction * dt);
                    tangent += Vector3.ProjectOnPlane(Vector3.down * settings.gravity, GroundNormal) * dt;
                    tangent = Accelerate(tangent, wish, settings.groundSpeed, settings.slideSteering, dt);
                }
                else
                {
                    tangent = Vector3.MoveTowards(tangent, Vector3.zero, settings.groundBraking * dt);
                    tangent = Accelerate(tangent, wish, wantsCrouch ? settings.crouchSpeed : settings.groundSpeed, settings.groundAcceleration, dt);
                }
                velocity = tangent - GroundNormal * 2f;
            }
            else
            {
                wish = transform.right * moveInput.x + transform.forward * moveInput.y;
                bool grappling = traversal != null && traversal.IsGrappling;
                velocity = Accelerate(velocity, wish,
                    grappling ? traversal.settings.grappleAirSpeed : settings.airSpeed,
                    grappling ? traversal.settings.grappleAirAcceleration : settings.airAcceleration, dt);
                velocity += Vector3.down * settings.gravity * dt;
            }

            velocity += pendingVelocityChange;
            if (Vector3.Dot(pendingVelocityChange, Vector3.up) > 0.01f)
                DetachGround(now, 0.1f);
            pendingVelocityChange = Vector3.zero;
            if (traversal != null && traversal.Step(ref velocity, moveInput, crouchHeld,
                now - jumpRequestedAt <= settings.jumpBuffer, IsGrounded, dt, now))
                jumpRequestedAt = float.NegativeInfinity;
            body.linearVelocity = velocity;
            State = traversal != null && traversal.IsGrappling ? MovementState.Grappling :
                traversal != null && traversal.IsWallRunning ?
                    (traversal.WallSide < 0 ? MovementState.WallRunningLeft : MovementState.WallRunningRight) :
                !IsGrounded ? MovementState.Airborne : slideRemaining > 0 ? MovementState.Sliding :
                wantsCrouch ? MovementState.Crouching : MovementState.Grounded;
        }

        public static Vector3 Accelerate(Vector3 velocity, Vector3 wish, float speed, float acceleration, float dt)
        {
            float amount = wish.magnitude;
            if (amount < 0.001f) return velocity;
            Vector3 direction = wish / amount;
            float missingSpeed = speed * amount - Vector3.Dot(velocity, direction);
            return velocity + direction * Mathf.Clamp(missingSpeed, 0, acceleration * dt);
        }

        private void SetHeight(float height)
        {
            capsule.radius = settings.radius;
            capsule.height = Mathf.Max(height, settings.radius * 2f);
            capsule.center = Vector3.up * capsule.height * 0.5f;
        }

        private void ProbeGround(float now)
        {
            IsGrounded = false;
            GroundNormal = Vector3.up;
            if (now < ignoreGroundUntil) return;
            float radius = settings.radius * 0.9f;
            Vector3 origin = body.position + Vector3.up * (settings.radius + 0.04f);
            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
                settings.groundProbe + 0.04f + settings.radius - radius, settings.groundMask, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                // An initial overlap reports the inverse cast direction as its normal.
                // It is not evidence of a walkable face (notably on steep walls).
                if (hit.collider.attachedRigidbody == body || hit.distance <= 0.0001f || hit.distance >= closest ||
                    Vector3.Angle(hit.normal, Vector3.up) > settings.maxSlope) continue;
                closest = hit.distance;
                GroundNormal = hit.normal;
                IsGrounded = true;
            }
        }

        private bool CanStand()
        {
            // Only test the added headroom: testing the entire capsule would mistake
            // the supporting floor/slope for an overhead obstruction.
            float radius = settings.radius * 0.95f;
            Vector3 bottom = body.position + Vector3.up * (settings.crouchingHeight - radius);
            Vector3 top = body.position + Vector3.up * (settings.standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, clearanceHits,
                settings.groundMask, QueryTriggerInteraction.Ignore);
            if (count == clearanceHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (clearanceHits[i].attachedRigidbody != body) return false;
            return true;
        }
    }
}

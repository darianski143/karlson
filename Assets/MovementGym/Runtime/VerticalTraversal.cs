using System.Collections.Generic;
using UnityEngine;

namespace Jimmy.Movement
{
    [RequireComponent(typeof(RigidbodyMotor))]
    public sealed class VerticalTraversal : MonoBehaviour
    {
        public TraversalSettings settings;
        public LineRenderer ropeLine;
        public bool IsWallRunning { get; private set; }
        public int WallSide { get; private set; }
        public Vector3 WallNormal { get; private set; }
        public bool IsGrappling { get; private set; }
        public Vector3 AnchorPoint { get; private set; }
        public float RopeLength { get; private set; }
        public int UsedWallCount => usedWalls.Count;
        public float RopeDistance => Vector3.Distance(AttachmentPoint, AnchorPoint);
        public float TangentialSpeed => IsGrappling ?
            Vector3.ProjectOnPlane(Motor.Velocity, AttachmentPoint - AnchorPoint).magnitude : Motor.PlanarSpeed;
        public Vector3 AttachmentPoint => Body.position + (IsGrappling ? ropeStartOffset : Vector3.up * Motor.Height * settings.ropeAttachmentHeight);
        private RigidbodyMotor Motor => motor != null ? motor : motor = GetComponent<RigidbodyMotor>();
        private Rigidbody Body => body != null ? body : body = GetComponent<Rigidbody>();
        private RigidbodyMotor motor;
        private Rigidbody body;
        private readonly HashSet<Object> usedWalls = new HashSet<Object>();
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private Object currentWall;
        private float wallTime, detachedUntil, grappleRetryAt, obstructionTime;
        private Collider anchorCollider;
        private Vector3 anchorLocalPoint;
        private Vector3 ropeStartOffset;

        public void ResetTraversal()
        {
            EndWall(0);
            usedWalls.Clear();
            detachedUntil = grappleRetryAt = 0;
            ReleaseGrapple();
        }

        private void OnDisable()
        {
            EndWall(Time.time);
            ReleaseGrapple();
        }

        private void EndWall(float now)
        {
            if (IsWallRunning && settings != null) detachedUntil = now + settings.wallDetachCooldown;
            IsWallRunning = false;
            WallSide = 0;
            currentWall = null;
            wallTime = 0;
        }

        public void Interrupt(float now)
        {
            EndWall(now);
            ReleaseGrapple();
        }

        public bool DetectWall(int side, out RaycastHit result)
        {
            result = default;
            if (settings == null) return false;
            Vector3 origin = Body.position + Vector3.up * Motor.Height * 0.5f;
            int count = Physics.RaycastNonAlloc(origin, transform.right * side, hits,
                Motor.settings.radius + settings.wallReach, settings.wallMask, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.attachedRigidbody == Body || hit.distance >= nearest ||
                    Mathf.Abs(hit.normal.y) > settings.wallNormalMaxUp) continue;
                result = hit;
                nearest = hit.distance;
            }
            return nearest < float.PositiveInfinity;
        }

        private static Object WallIdentity(Collider collider)
        {
            var surface = collider.GetComponentInParent<WallSurface>();
            return surface != null ? (Object)surface : collider;
        }

        // Called exclusively by the motor, after ordinary acceleration/gravity.
        // Returns true only when a buffered jump was consumed as a wall jump.
        public bool Step(ref Vector3 velocity, Vector2 input, bool crouch, bool jump,
            bool grounded, float dt, float now)
        {
            if (settings == null || !isActiveAndEnabled) return false;
            if (grounded)
            {
                usedWalls.Clear();
                EndWall(now);
            }
            if (IsGrappling)
            {
                EndWall(now);
                if (!AnchorIsValid() || !settings.grappleEnabled)
                    ReleaseGrapple();
                else
                {
                    obstructionTime = RopeBlocked() ? obstructionTime + dt : 0;
                    if (obstructionTime > settings.ropeObstructionGrace) ReleaseGrapple();
                    else
                    {
                        velocity = ConstrainVelocity(Body.position + ropeStartOffset,
                            AnchorPoint, RopeLength, velocity, dt);
                        return false;
                    }
                }
            }

            bool canRun = settings.wallRunEnabled && !grounded && !crouch &&
                input.y > settings.wallForwardInput && now >= detachedUntil &&
                Vector3.ProjectOnPlane(velocity, Vector3.up).magnitude >= settings.wallMinimumSpeed;
            RaycastHit wall = default;
            int selectedSide = 0;
            if (canRun)
            {
                // Prefer the active side, so a narrow corridor cannot flicker sides.
                int first = WallSide != 0 ? WallSide : -1;
                for (int i = 0; i < 2; i++)
                {
                    int side = i == 0 ? first : -first;
                    if (!DetectWall(side, out var candidate)) continue;
                    Object identity = WallIdentity(candidate.collider);
                    if (usedWalls.Contains(identity) && (!IsWallRunning || identity != currentWall)) continue;
                    wall = candidate;
                    selectedSide = side;
                    break;
                }
            }
            if (selectedSide == 0) { EndWall(now); return false; }
            Object wallId = WallIdentity(wall.collider);
            if (!IsWallRunning || currentWall != wallId)
            {
                if (IsWallRunning) { EndWall(now); return false; }
                currentWall = wallId;
                usedWalls.Add(wallId);
                wallTime = 0;
                IsWallRunning = true;
            }
            WallSide = selectedSide;
            WallNormal = wall.normal;
            wallTime += dt;
            if (jump)
            {
                velocity = WallJumpVelocity(velocity, WallNormal, settings.wallJumpOutSpeed, settings.wallJumpUpSpeed);
                EndWall(now);
                Motor.DetachGround(now, settings.launchGroundGrace);
                return true;
            }
            if (wallTime >= settings.wallDuration) { EndWall(now); return false; }
            Vector3 tangent = Vector3.Cross(Vector3.up, WallNormal).normalized;
            if (Vector3.Dot(tangent, transform.forward) < 0) tangent = -tangent;
            float along = Vector3.Dot(velocity, tangent);
            along += Mathf.Clamp(settings.wallSpeed - along, 0, settings.wallAcceleration * dt);
            // The motor already applied full gravity; restore only the wall-run share.
            float vertical = Mathf.Max(-settings.wallMaxFallSpeed,
                velocity.y + Motor.settings.gravity * (1f - settings.wallGravityScale) * dt);
            velocity = tangent * along + Vector3.up * vertical - WallNormal * settings.wallAdhesion;
            return false;
        }

        public static Vector3 WallJumpVelocity(Vector3 velocity, Vector3 normal, float outward, float upward)
        {
            Vector3 alongWall = Vector3.ProjectOnPlane(velocity, normal);
            alongWall.y = Mathf.Max(alongWall.y, upward);
            return alongWall + normal * outward;
        }

        public bool TryGrapple(Ray aim, float now)
        {
            if (settings == null || !settings.grappleEnabled || IsGrappling || now < grappleRetryAt) return false;
            grappleRetryAt = now + settings.grappleRetryDelay;
            int count = Physics.RaycastNonAlloc(aim, hits, settings.grappleRange, settings.grappleMask, QueryTriggerInteraction.Ignore);
            Collider candidate = null;
            RaycastHit closest = default;
            float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.attachedRigidbody == Body || hit.distance >= distance) continue;
                candidate = hit.collider; closest = hit; distance = hit.distance;
            }
            // The nearest solid object must itself be an anchor: never attach through walls.
            if (candidate == null || candidate.attachedRigidbody != null ||
                candidate.GetComponentInParent<GrappleAnchor>() == null) return false;
            Vector3 attachment = AttachmentPoint;
            float length = Vector3.Distance(attachment, closest.point);
            if (length < settings.ropeMinLength || length > settings.grappleRange) return false;
            AnchorPoint = closest.point;
            anchorCollider = candidate;
            anchorLocalPoint = candidate.transform.InverseTransformPoint(AnchorPoint);
            RopeLength = length;
            ropeStartOffset = attachment - Body.position;
            obstructionTime = 0;
            IsGrappling = true;
            EndWall(now);
            if (RopeBlocked()) { ReleaseGrapple(); return false; }
            return true;
        }

        private bool AnchorIsValid()
        {
            return anchorCollider != null && anchorCollider.enabled && anchorCollider.gameObject.activeInHierarchy &&
                Vector3.Distance(anchorCollider.transform.TransformPoint(anchorLocalPoint), AnchorPoint) <= settings.ropeContactSkin;
        }

        private bool RopeBlocked()
        {
            Vector3 origin = Body.position + ropeStartOffset;
            Vector3 delta = AnchorPoint - origin;
            int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits,
                Mathf.Max(0, delta.magnitude - settings.ropeContactSkin), settings.grappleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (hits[i].collider.attachedRigidbody != Body && hits[i].collider != anchorCollider) return true;
            return false;
        }

        public void ReleaseGrapple()
        {
            // Deliberately do not write velocity: the current tangent is the launch.
            IsGrappling = false;
            anchorCollider = null;
            obstructionTime = 0;
            if (ropeLine != null) ropeLine.enabled = false;
        }

        // An inextensible, unilateral constraint: slack rope never pushes the player.
        // Predict the next physics position and remove only the excess displacement.
        // Position is never teleported, so collision detection remains with PhysX.
        public static Vector3 ConstrainVelocity(Vector3 position, Vector3 anchor, float length, Vector3 velocity, float dt)
        {
            if (dt <= 0 || length <= 0) return velocity;
            Vector3 predicted = position + velocity * dt;
            Vector3 radial = predicted - anchor;
            if (radial.sqrMagnitude <= length * length) return velocity;
            return (anchor + radial.normalized * length - position) / dt;
        }

        public void UpdateRope(Transform cameraTransform)
        {
            if (ropeLine == null || settings == null) return;
            ropeLine.enabled = IsGrappling && settings.showRope;
            if (!ropeLine.enabled) return;
            ropeLine.positionCount = 2;
            ropeLine.startWidth = ropeLine.endWidth = settings.ropeWidth;
            ropeLine.startColor = ropeLine.endColor = settings.ropeColor;
            ropeLine.SetPosition(0, cameraTransform.TransformPoint(settings.ropeViewOffset));
            ropeLine.SetPosition(1, AnchorPoint);
        }
    }
}

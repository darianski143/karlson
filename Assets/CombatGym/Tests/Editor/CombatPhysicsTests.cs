using System.Collections.Generic;
using Jimmy.Movement;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Combat.Tests
{
    public sealed class CombatPhysicsTests
    {
        private readonly List<ScriptableObject> assets = new List<ScriptableObject>();
        private SimulationMode oldMode;
        private float oldScale, oldStep;
        [SetUp] public void Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            oldMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.Script;
            oldScale = Time.timeScale; oldStep = Time.fixedDeltaTime;
        }
        [TearDown] public void Cleanup()
        {
            foreach (var director in Object.FindObjectsByType<TimeDirector>(FindObjectsSortMode.None)) director.Restore();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var asset in assets) Object.DestroyImmediate(asset);
            assets.Clear(); Physics.simulationMode = oldMode; Time.timeScale = oldScale; Time.fixedDeltaTime = oldStep;
        }
        private T Config<T>() where T : ScriptableObject
        { var asset = ScriptableObject.CreateInstance<T>(); assets.Add(asset); return asset; }
        private CombatHealth Target(Vector3 position, float health = 100)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.position = position;
            var target = obj.AddComponent<CombatHealth>(); target.settings = Config<HealthSettings>(); target.settings.maximumHealth = health;
            target.Initialize(); return target;
        }
        private RigidbodyMotor Player()
        {
            var obj = new GameObject("Combat player"); obj.SetActive(false);
            obj.AddComponent<Rigidbody>(); obj.AddComponent<CapsuleCollider>();
            var motor = obj.AddComponent<RigidbodyMotor>(); motor.settings = Config<MovementSettings>();
            obj.AddComponent<FpsInputView>(); obj.AddComponent<PhysicsImpulseBody>();
            obj.SetActive(true); motor.Initialize(); obj.transform.position = Vector3.up * 5;
            motor.Teleport(Vector3.up * 5); Physics.SyncTransforms(); return motor;
        }
        [Test] public void DamageIsBoundedAndDeathIsIdempotent()
        {
            var target = Target(Vector3.forward * 10, 50);
            Assert.That(target.ReceiveDamage(new DamageHit(-1, Vector3.zero, Vector3.forward)), Is.False);
            Assert.That(target.ReceiveDamage(new DamageHit(20, Vector3.zero, Vector3.forward)), Is.True);
            Assert.That(target.Health, Is.EqualTo(30));
            target.ReceiveDamage(new DamageHit(100, Vector3.zero, Vector3.forward));
            Assert.That(target.Health, Is.Zero); Assert.That(target.IsDead, Is.True);
            Assert.That(target.ReceiveDamage(new DamageHit(10, Vector3.zero, Vector3.forward)), Is.False);
        }
        [Test] public void PistolHitsPreciselyAndSolidCoverStopsIt()
        {
            var target = Target(Vector3.forward * 10);
            var gun = Config<WeaponDefinition>(); Physics.SyncTransforms();
            var result = WeaponPhysics.Hitscan(gun, new Ray(Vector3.zero, Vector3.forward), null);
            Assert.That(result.Rays, Is.EqualTo(1)); Assert.That(result.DamageHits, Is.EqualTo(1));
            Assert.That(target.Health, Is.EqualTo(100 - gun.damage));
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.transform.position = Vector3.forward * 5; Physics.SyncTransforms();
            result = WeaponPhysics.Hitscan(gun, new Ray(Vector3.zero, Vector3.forward), null);
            Assert.That(result.DamageHits, Is.Zero);
        }
        [Test] public void ShotgunEmitsConfiguredPelletsInsideCone()
        {
            var target = Target(Vector3.forward * 10, 1000); target.transform.localScale = new Vector3(10, 10, 1);
            var gun = Config<WeaponDefinition>(); gun.kind = WeaponKind.Shotgun; gun.pellets = 8; gun.spreadDegrees = 4; gun.damage = 5;
            Physics.SyncTransforms();
            var result = WeaponPhysics.Hitscan(gun, new Ray(Vector3.zero, Vector3.forward), null);
            Assert.That(result.Rays, Is.EqualTo(8)); Assert.That(result.DamageHits, Is.EqualTo(8));
            Assert.That(target.Health, Is.EqualTo(960));
            for (int i = 0; i < gun.pellets; i++)
                Assert.That(Vector3.Angle(Vector3.forward, WeaponPhysics.PelletDirection(Vector3.forward, i, gun.pellets, gun.spreadDegrees)),
                    Is.LessThanOrEqualTo(gun.spreadDegrees));
        }
        [Test] public void HitscanIgnoresOwnersChildColliders()
        {
            var owner = new GameObject("Owner");
            var shield = GameObject.CreatePrimitive(PrimitiveType.Cube); shield.transform.SetParent(owner.transform); shield.transform.position = Vector3.forward * 2;
            var target = Target(Vector3.forward * 10); Physics.SyncTransforms();
            var result = WeaponPhysics.Hitscan(Config<WeaponDefinition>(), new Ray(Vector3.zero, Vector3.forward), owner);
            Assert.That(result.DamageHits, Is.EqualTo(1));
        }
        [Test] public void PlayerImpulsePreservesExistingLateralMomentum()
        {
            var motor = Player(); var body = motor.GetComponent<Rigidbody>(); body.linearVelocity = new Vector3(7, 2, 4);
            motor.GetComponent<PhysicsImpulseBody>().ApplyImpulse(Vector3.up * 8, body.position);
            Physics.SyncTransforms(); motor.Simulate(0.02f, 0.02f);
            Assert.That(body.linearVelocity.x, Is.EqualTo(7)); Assert.That(body.linearVelocity.z, Is.EqualTo(4));
            Assert.That(body.linearVelocity.y, Is.GreaterThan(9));
        }
        [Test] public void ShotgunRecoilAndSwitchingDoNotResetMomentumOrCooldown()
        {
            var motor = Player(); var gun = Config<WeaponDefinition>(); gun.kind = WeaponKind.Shotgun; gun.playerRecoil = 8;
            var pistol = Config<WeaponDefinition>(); var tuning = Config<CombatTuning>(); tuning.weapons = new[] { gun, pistol }; tuning.unlockAll = true;
            var combat = motor.gameObject.AddComponent<PlayerCombat>(); combat.tuning = tuning; combat.Initialize();
            motor.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 12;
            Assert.That(combat.TryFire(new Ray(motor.transform.position, Vector3.down), 0), Is.True);
            Assert.That(combat.Equip(1), Is.True);
            Assert.That(combat.TryFire(new Ray(motor.transform.position, Vector3.down), 0.01f), Is.False);
            motor.Simulate(0.02f, 0.02f);
            Assert.That(motor.Velocity.z, Is.EqualTo(12)); Assert.That(motor.Velocity.y, Is.GreaterThan(7));
        }
        [Test] public void PickupUnlocksOnlyKnownWeaponAndCollectsOnce()
        {
            var motor = Player(); var pistol = Config<WeaponDefinition>(); var shotgun = Config<WeaponDefinition>();
            var tuning = Config<CombatTuning>(); tuning.weapons = new[] { pistol, shotgun };
            var combat = motor.gameObject.AddComponent<PlayerCombat>(); combat.tuning = tuning; combat.Initialize();
            Assert.That(combat.Equip(1), Is.False);
            var pickup = new GameObject("Pickup", typeof(BoxCollider), typeof(WeaponPickup)).GetComponent<WeaponPickup>(); pickup.weapon = shotgun;
            Assert.That(pickup.Collect(combat), Is.True); Assert.That(pickup.Collect(combat), Is.False);
            Assert.That(combat.Equipped, Is.EqualTo(shotgun));
            Assert.That(combat.Pickup(Config<WeaponDefinition>()), Is.False);
        }
        [Test] public void ExplosionFallsOffAndDeduplicatesCompoundTargets()
        {
            var near = Target(Vector3.right * 2, 200); var far = Target(Vector3.right * 8, 200);
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube); child.transform.SetParent(near.transform, false);
            child.transform.localScale = Vector3.one * 0.5f;
            var settings = Config<ExplosionSettings>(); settings.radius = 10; settings.respectCover = false;
            Physics.SyncTransforms(); ExplosionService.Detonate(Vector3.zero, settings, null);
            Assert.That(near.Health, Is.LessThan(far.Health)); Assert.That(near.AcceptedHits, Is.EqualTo(1));
            Assert.That(settings.Strength(0), Is.EqualTo(1)); Assert.That(settings.Strength(10), Is.Zero);
        }
        [Test] public void ExplosionRespectsCoverAndPushesRigidbodies()
        {
            var target = Target(Vector3.forward * 8, 200);
            var body = target.gameObject.AddComponent<Rigidbody>(); body.useGravity = false; target.gameObject.AddComponent<PhysicsImpulseBody>();
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.transform.position = Vector3.forward * 4; cover.transform.localScale = new Vector3(6, 6, 0.2f);
            var settings = Config<ExplosionSettings>(); settings.radius = 10; Physics.SyncTransforms();
            ExplosionService.Detonate(Vector3.zero, settings, null); float coveredDamage = 200 - target.Health;
            Physics.Simulate(0.02f); Assert.That(body.linearVelocity.magnitude, Is.GreaterThan(0));
            cover.SetActive(false); target.ResetHealth(); Physics.SyncTransforms();
            ExplosionService.Detonate(Vector3.zero, settings, null);
            Assert.That(200 - target.Health, Is.GreaterThan(coveredDamage * 2));
        }
        [Test] public void ExplosionBoostsOwnerWithoutResettingForwardVelocity()
        {
            var motor = Player(); motor.gameObject.AddComponent<CombatHealth>().settings = Config<HealthSettings>();
            var body = motor.GetComponent<Rigidbody>(); body.linearVelocity = Vector3.forward * 11;
            var settings = Config<ExplosionSettings>(); Physics.SyncTransforms();
            ExplosionService.Detonate(body.position + Vector3.down * 0.5f, settings, motor.gameObject);
            motor.Simulate(0.02f, 0.02f);
            Assert.That(motor.Velocity.z, Is.EqualTo(11).Within(0.01f));
            Assert.That(motor.Velocity.y, Is.GreaterThan(10));
        }
        [Test] public void GrenadeFuseDetonatesExactlyOnce()
        {
            var target = Target(Vector3.right * 2, 500);
            var definition = Config<WeaponDefinition>(); definition.explosion = Config<ExplosionSettings>();
            var projectile = new GameObject("Grenade", typeof(Rigidbody), typeof(SphereCollider), typeof(GrenadeProjectile)).GetComponent<GrenadeProjectile>();
            projectile.Initialize(definition, null, Vector3.forward * 3, 0); Physics.SyncTransforms();
            projectile.Tick(definition.fuse - 0.01f); Assert.That(projectile.Detonated, Is.False);
            projectile.Tick(definition.fuse + 0.01f); projectile.Tick(99);
            Assert.That(projectile.Detonated, Is.True); Assert.That(target.AcceptedHits, Is.EqualTo(1));
        }
        [Test] public void FracturedPanelDisablesIntactColliderAndActivatesPiecesOnce()
        {
            var root = new GameObject("Panel"); var breakable = root.AddComponent<Breakable>(); breakable.settings = Config<DestructibleSettings>();
            breakable.intactCollider = root.AddComponent<BoxCollider>();
            var fragment = GameObject.CreatePrimitive(PrimitiveType.Cube); fragment.transform.SetParent(root.transform);
            var body = fragment.AddComponent<Rigidbody>(); body.isKinematic = true; fragment.SetActive(false);
            breakable.fragments = new[] { body };
            breakable.ReceiveDamage(new DamageHit(100, Vector3.zero, Vector3.forward));
            Assert.That(breakable.IsBroken, Is.True); Assert.That(breakable.intactCollider.enabled, Is.False);
            Assert.That(body.gameObject.activeSelf, Is.True); Assert.That(body.isKinematic, Is.False);
            Assert.That(breakable.ReceiveDamage(new DamageHit(100, Vector3.zero, Vector3.forward)), Is.False);
            breakable.ApplyImpulse(Vector3.forward * 4, Vector3.zero); Physics.Simulate(0.02f);
            Assert.That(body.linearVelocity.z, Is.GreaterThan(0));
        }
        private ExplosiveBarrel Barrel(Vector3 position, ExplosionSettings explosion)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.position = position;
            var barrel = obj.AddComponent<ExplosiveBarrel>(); barrel.explosion = explosion; barrel.durability = Config<HealthSettings>(); barrel.durability.maximumHealth = 1;
            return barrel;
        }
        [Test] public void BarrelChainIsDelayedBoundedAndIdempotent()
        {
            var explosion = Config<ExplosionSettings>(); explosion.radius = 5; explosion.maximumChainDepth = 2; explosion.respectCover = false;
            var first = Barrel(Vector3.zero, explosion); var second = Barrel(Vector3.right * 3, explosion); var third = Barrel(Vector3.right * 6, explosion);
            Physics.SyncTransforms(); first.ReceiveDamage(new DamageHit(5, Vector3.zero, Vector3.right));
            Assert.That(first.Exploded, Is.False);
            first.Tick(Time.time + 1); Assert.That(second.Armed, Is.True);
            second.Tick(Time.time + 2); first.Tick(Time.time + 3);
            Assert.That(first.ExplosionCount, Is.EqualTo(1)); Assert.That(second.ExplosionCount, Is.EqualTo(1));
            Assert.That(third.Armed, Is.False);
        }
        [Test] public void EnemyTelegraphsBeforeFiringAndCoverBlocksSight()
        {
            var enemyHealth = Target(Vector3.zero);
            var target = Target(new Vector3(0, 0.6f, 10));
            var enemy = enemyHealth.gameObject.AddComponent<CombatEnemy>(); enemy.settings = Config<EnemySettings>(); enemy.target = target.transform;
            Physics.SyncTransforms(); Assert.That(enemy.HasLineOfSight(), Is.True);
            enemy.Tick(0); Assert.That(enemy.State, Is.EqualTo(EnemyState.Telegraph)); Assert.That(target.Health, Is.EqualTo(100));
            enemy.Tick(enemy.settings.windup - 0.01f); Assert.That(target.Health, Is.EqualTo(100));
            enemy.Tick(enemy.settings.windup + 0.01f); Assert.That(target.Health, Is.LessThan(100));
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.transform.position = new Vector3(0, 0.6f, 5); cover.transform.localScale = Vector3.one * 3;
            Physics.SyncTransforms(); Assert.That(enemy.HasLineOfSight(), Is.False);
        }
        [Test] public void InstantDeathCancelsEnemyAndCreatesStablePhysicalBody()
        {
            var health = Target(Vector3.zero); health.settings.instantDeath = true;
            var body = health.gameObject.AddComponent<Rigidbody>(); body.isKinematic = true;
            var death = health.gameObject.AddComponent<PhysicsDeath>();
            var enemy = health.gameObject.AddComponent<CombatEnemy>(); enemy.settings = Config<EnemySettings>();
            health.ReceiveDamage(new DamageHit(1, Vector3.zero, Vector3.forward)); enemy.Tick(1);
            Assert.That(health.IsDead, Is.True); Assert.That(death.Activated, Is.True);
            Assert.That(body.isKinematic, Is.False); Assert.That(enemy.State, Is.EqualTo(EnemyState.Dead));
            Assert.That(health.GetComponents<Rigidbody>().Length, Is.EqualTo(1));
        }
        [Test] public void TimeDirectorScalesStepAndRestoresOriginalClock()
        {
            Time.timeScale = 0.8f; Time.fixedDeltaTime = 0.02f;
            var director = new GameObject("Time").AddComponent<TimeDirector>(); director.settings = Config<TimeSettings>();
            Assert.That(director.Request(CombatMoment.Boost, 30, 0), Is.True);
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.02f * Time.timeScale / 0.8f).Within(0.00001f));
            director.Tick(director.settings.realDuration + 0.01f);
            Assert.That(Time.timeScale, Is.EqualTo(0.8f)); Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.02f).Within(0.000001f));
            Assert.That(director.Request(CombatMoment.Boost, 30, director.settings.realDuration + 0.02f), Is.False);
        }
        [Test] public void DisablingSlowMotionRestoresImmediatelyAndLowSpeedDoesNotTrigger()
        {
            var director = new GameObject("Time").AddComponent<TimeDirector>(); director.settings = Config<TimeSettings>();
            Assert.That(director.Request(CombatMoment.Boost, 0, 0), Is.False);
            Assert.That(director.Request(CombatMoment.Boost, 30, 0), Is.True);
            director.settings.contextualSlowMotion = false; director.Tick(0.01f);
            Assert.That(Time.timeScale, Is.EqualTo(oldScale)); Assert.That(Time.fixedDeltaTime, Is.EqualTo(oldStep));
            Assert.That(director.Request(CombatMoment.ChainExplosion, 100, 10), Is.False);
        }
        [Test] public void RecoilOnWallEntryTickIsNotAbsorbed()
        {
            var motor = Player(); var traversal = motor.gameObject.AddComponent<VerticalTraversal>(); traversal.settings = Config<TraversalSettings>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(0.9f, 5, 0); wall.transform.localScale = new Vector3(0.3f, 20, 20);
            motor.SetInput(Vector2.up, false); motor.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 10;
            motor.AddVelocityChange(Vector3.left * 8); Physics.SyncTransforms(); motor.Simulate(0.02f, 0.02f);
            Assert.That(traversal.IsWallRunning, Is.False);
            Assert.That(motor.Velocity.x, Is.LessThan(-6)); Assert.That(motor.Velocity.z, Is.GreaterThanOrEqualTo(10));
        }
        [Test] public void OutwardCombatImpulseReleasesWallWithoutErasingMomentum()
        {
            var motor = Player(); var traversal = motor.gameObject.AddComponent<VerticalTraversal>(); traversal.settings = Config<TraversalSettings>();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(0.9f, 5, 0); wall.transform.localScale = new Vector3(0.3f, 20, 20);
            motor.SetInput(Vector2.up, false); motor.GetComponent<Rigidbody>().linearVelocity = Vector3.forward * 10;
            Physics.SyncTransforms(); motor.Simulate(0.02f, 0.02f); Assert.That(traversal.IsWallRunning, Is.True);
            motor.AddVelocityChange(Vector3.left * 8); motor.Simulate(0.02f, 0.04f);
            Assert.That(traversal.IsWallRunning, Is.False); Assert.That(motor.Velocity.x, Is.LessThan(-6));
            Assert.That(motor.Velocity.z, Is.GreaterThanOrEqualTo(10));
        }
    }
}

# Milestone 3 — Combat Physics

This milestone lives in `D:/Work/Jimmy_Unity/JIMMY`, on top of the approved `4cbb125` vertical-traversal checkpoint. It uses original code and Unity primitives; no KARLSON code, models or textures are used. Milestone 3 changes are intentionally left uncommitted.

## Play and controls

Open `Assets/MovementGym/Scenes/MovementGym.unity`. If the scene was open during the update, reopen the saved scene from disk; keep any unsaved personal edits separately. Combat Gym is beyond the vertical-traversal area, east/right, connected by the bridge at z=-5.

- Fire: **left mouse / gamepad X (west face button)**, using the existing Player/Attack action. Each press fires one shot.
- Equip: **1 pistol, 2 shotgun, 3 grenade launcher**. Existing Next/Previous actions also work (gamepad D-pad right/left).
- Start with pistol. Walk over the orange shotgun pickup at x=88 or cyan launcher pickup at x=100 to unlock and equip it.
- Grapple remains RMB / LB; movement, jump and slide bindings are unchanged.
- Training ammunition is unlimited. Switching weapons preserves the shared firing cooldown.
- R resets position using the existing movement control. Player death respawns and restores health; inventory remains unlocked. Reload the scene to reset destroyed props, enemy and pickups.

## Combat Gym stands

1. **Pistol accuracy, x=75:** orange targets and a backstop. Pistol rays follow the crosshair exactly; surfaces block shots.
2. **Shotgun boost, x=87:** pick up the shotgun, jump and fire downward while moving forward to reach elevated platforms. The physical recoil adds velocity rather than replacing it.
3. **Grenade boost, x=100:** fire toward the floor marker and time a jump with the fuse. The projectile follows local gravity, inherits part of the player's velocity and detonates once. Self-damage is reduced, but the launch impulse remains.
4. **Enemy, x=113:** a stationary enemy acquires line of sight, displays a red warning line, then fires at the locked aim point after the windup. Strafe or move behind cover. Death enables a single stable physical capsule as the greybox ragdoll substitute.
5. **Destruction, z=29–32:** cyan window, segmented door, three explosive barrels and a movable crate. Window/door pieces are authored in advance; they activate as separate rigidbodies when broken. Barrels propagate a delayed, bounded chain reaction.

## Configuration

All gameplay tuning assets are in `Assets/CombatGym/Settings`:

- **Pistol / Shotgun / Grenade launcher:** damage, cadence, pellet count/spread, range, target impulse, player recoil, view kick, launch speed, inherited velocity, local gravity, fuse, contact detonation and projectile radius.
- **Grenade explosion / Barrel explosion:** radius, damage, velocity impulse, falloff curve, upward bias, owner damage multiplier, cover multipliers, chain delay/depth and masks.
- **Player health / Enemy health / Target health / Barrel health:** health or instant-death mode. Player respawn delay and corpse mass/damping/angular-speed limit also live in health profiles.
- **Glass fracture / Door fracture:** health, fragment mass, scatter, impulse share and lifetime.
- **Enemy behaviour:** range, windup, recovery, damage, impulse, eye offset and visibility mask.
- **Default combat:** weapon list, unlock-all testing option, camera feedback, bounded effect count, effect lifetimes/materials, hit confirmation and telemetry.
- **Contextual time:** enable/disable, slow scale, real-time duration/cooldown and speed threshold.

Set **Default combat > cameraIntensity = 0** to disable combat camera recoil/shake. Set **Contextual time > contextualSlowMotion = false** to disable slow-motion; active slow-motion restores the previous clock on the next update. The existing traversal camera-intensity setting independently controls wall roll and movement FOV.

## Architecture and physics

`IDamageable`, `IImpulseReceiver` and `IDestructible` are small contracts. `CombatQueries` resolves the logical target through parent components and skips the shooter's own colliders. Hitscan uses deterministic disk-distributed shotgun pellets, with no mutation of Unity's global random state.

`PhysicsImpulseBody` routes player velocity changes through `RigidbodyMotor.AddVelocityChange`. Ordinary rigidbodies receive a velocity-change force. Wall traversal releases adhesion for an outward external impulse so it cannot erase recoil; tangent motion remains intact. The grapple continues to constrain radial motion, as expected for a taut rope.

Explosions deduplicate compound targets and snapshot cover before damage can destroy a blocking panel. Both damage and impulse use the configured falloff, with independent cover attenuation. Owner self-damage does not produce a false hit-confirm marker. The affected rigidbody's existing velocity is retained.

Barrels arm once and detonate later in Update, not recursively from the damage callback. A chain-depth cap stops unbounded propagation, and exploded barrels cannot detonate again. Destructibles disable the intact collider before activating separated fragments. Fragment lifetimes and the active visual-effect count are bounded.

`TimeDirector` is the scene's central owner of contextual slow-motion. Fast boost/hit events and barrel-chain explosions may request a bounded slow interval. It captures the previous time scale and fixed timestep, scales the timestep by the time-scale ratio to keep real-time physics cadence consistent, and restores both on expiry, disable or destruction/scene exit. Cooldown prevents repeated extension. Combat cooldowns and grenade physics/fuses use scaled game time; slow-motion duration and camera feedback use unscaled time.

Feedback is deliberately placeholder quality: geometric weapons, brief muzzle flash, lines for tracers, simple impact/explosion shapes, hit confirmation and combat telemetry. Effects allocate a bounded number of short-lived objects; this is not a production VFX/pooling/audio pipeline.

## Validation and remaining playtest

Verified on **2026-09-06** with Unity **6000.6.0f1**: script compilation and Combat Gym generation completed with exit code 0, and **53/53 tests passed** with test-runner exit code 0. The suite includes real Play Mode pickup, ballistic projectile, player explosion boost, camera disable and clock restoration checks. Detailed reports are saved in the main project's ignored `Logs/m3-tests.xml`, `Logs/m3-tests.log` and `Logs/m3-import.log`. No rendered/manual playthrough was performed by the agent.

Headless validation uses a synchronized copy while the user's main Unity editor remains open. The complete test filter is `Jimmy.`, covering movement, vertical traversal and combat.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<validation-project>' -executeMethod Jimmy.Combat.Editor.CombatGymBuilder.UpgradeGym -quit -logFile Logs/m3-import.log
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<validation-project>' -runTests -testPlatform EditMode -testFilter 'Jimmy.' -testResults Logs/m3-tests.xml -logFile Logs/m3-tests.log
```

The scene builder (**Jimmy > Movement Gym > Add Combat Gym**) preserves an existing Combat Gym group and tuning assets on repeated runs.

Automated coverage includes bounded damage/death, pistol occlusion, pellet count/cone, shooter exclusion, recoil and switching cooldown, pickups, explosion falloff/cover/deduplication, player boost, grenade fuse/ballistics, fracture activation, bounded barrel chains, enemy LOS/telegraph/death, time restoration, actual pickup triggers, camera-effect disable and previous movement/traversal regressions.

Manual playtest remains necessary for weapon feel, boost distances and timing, enemy readability/difficulty, fracture stability in crowded interactions, chain-reaction pacing, camera/flash comfort, viewmodel clipping and performance. There is no weapon animation/audio, reload/ammo economy, mobile enemy navigation or articulated ragdoll in this greybox milestone.

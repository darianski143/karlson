# Milestone 2 — Vertical Traversal

Work lives in the main project `D:/Work/Jimmy_Unity/JIMMY`. Milestone 1 checkpoint: `16ff01e` (`feat: add movement gym foundation`). Existing movement tuning and unrelated project/quality/tutorial modifications are preserved.

## Play

Open `Assets/MovementGym/Scenes/MovementGym.unity`. The extension is east/right of the original deck, connected by a wide bridge. Reload the scene from disk if it was already open before the update, keeping any unsaved personal edits separately.

- **Wall-run:** airborne, close to a vertical wall on either side, hold forward and release crouch. Minimum entry speed is configurable (default 5 m/s).
- **Wall-jump:** Space / gamepad A while wall-running. The velocity along the wall is retained; outward and upward components are supplied by the profile.
- **Grapple:** aim at the cyan marked sphere, press and hold right mouse / left bumper. Release the button to release the rope. It attaches on a fresh press; missed shots do not automatically latch later.
- **Jump-pad:** move through the orange pad. The pad launches along its tilted up axis and retains perpendicular momentum.
- Existing crouch/slide/reset/telemetry controls remain. Escape/focus loss releases traversal along with input; physics continues.

## Three routes

A — `x=24`: slide through the low tunnel, follow the stripe close to the left wall, release crouch and jump alongside the wall before the launch deck ends. Wall-jump toward the orange landing. The opposite wall also supports running.

B — `x=42`: use the ramp to the raised launch, aim at the cyan sphere, hold grapple and run over the edge. Swing across the void and release on the rising arc toward the far orange deck. There is no reel-in or automatic pull-to-anchor.

C — `x=60`: run over the forward-tilted jump-pad. Hold forward alongside the left wall, wall-run, then jump out toward the elevated orange deck.

The route builder is **Jimmy > Movement Gym > Add Vertical Traversal**. It appends a named group, adds the component/rope to the player, and preserves existing route geometry and tuning on later invocations. It is not a scene reset tool.

## Configuration and architecture

`Assets/MovementGym/Settings/DefaultTraversal.asset` contains wall detection, entry speed, duration, gravity, jump impulse, cooldown, grapple reach/rope limits/air control, obstruction handling, rope appearance and all camera feedback parameters. `DefaultJumpPad.asset` contains launch speed, repeat delay and ground-detachment time. Milestone 1 tuning remains in `DefaultMovement.asset`.

Camera effects are deliberately modest. Set **cameraEffectIntensity = 0** to disable roll and all FOV changes immediately; lower values scale every effect. Wall roll, speed FOV and grapple FOV also have separate amplitudes. `showRope` independently controls the visible tether. There is no camera shake.

`RigidbodyMotor` remains the only component writing locomotion velocity. With Milestone 3, it calls `VerticalTraversal.Step` after base locomotion, applies queued external impulses after wall control, then applies the rope constraint last so combat recoil is preserved. See [CombatPhysics.md](CombatPhysics.md). Wall jumps consume buffered jump input. `Launch` preserves perpendicular velocity, interrupts the grapple/wall-run, and prevents ground adhesion from swallowing the launch. Disabling traversal or respawning clears active attachments.

Wall identities are retained in a set until actual ground detection or respawn: leaving a wall, waiting, grappling, or touching a different wall does not restore the first wall. A short detach cooldown prevents immediate switching. Put `WallSurface` on a shared parent to make several collider segments count as one wall.

Grapple targets explicitly opt in with `GrappleAnchor`; the nearest solid hit must be marked. Only static anchors are supported. The rope captures its initial attachment length and uses a unilateral predictive displacement constraint in each physics tick. Slack rope exerts no correction; taut rope limits the next free-motion position to a sphere around the anchor. It never teleports the Rigidbody. Releasing does not write velocity, preserving the current tangent. Obstruction, disabled/destroyed/moved anchors, reset and jump-pad launch detach it.

This lightweight rope does not wrap around geometry and uses a first-order discrete solver; collisions can briefly introduce small constraint error that is corrected on the next tick. Pendulum energy decays slightly with timestep. It is not a climbing winch, elastic spring, or moving-platform grapple.

## Validation

Verified on **2026-09-06**, Unity **6000.6.0f1**: headless script compilation and scene upgrade exited with code 0; the complete suite passed **30/30 tests**, including Play Mode integration, and exited with code 0. The synchronized validation copy was used because the main Unity editor remained open. Generated scene/prefab/profile assets were transferred back with hash verification. Results are also saved in the main project's ignored `Logs/m2-tests.xml` and `Logs/m2-tests.log`. No graphical/manual playthrough was performed by the agent.

Run the complete suite, including the existing Milestone 1 regression tests, with:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<validation-project>' -runTests -testPlatform EditMode -testFilter Jimmy.Movement.Tests -testResults Logs/m2-tests.xml -logFile Logs/m2-tests.log
```

Do not add `-quit` to the test command. If the main editor is open, use a synchronized validation copy to avoid locking/interfering with it. Scene upgrade/compilation uses `-executeMethod Jimmy.Movement.Editor.VerticalGymBuilder.UpgradeGym -quit`.

Tests exercise left/right detection, rejected slopes/triggers, wall-jump momentum, reattachment/timeout/segment identity, switching walls, slide-to-wall transitions, rope distance across repeated PhysX steps, slack/release, blocked anchors, invalid anchors, pad momentum/cooldown, respawn, and real-scene Play Mode trigger/camera integration.

Manual playtest is still required for route difficulty, wall proximity, swing timing, camera comfort, rope readability and behaviour at different rendering/physics rates. No weapons or combat are implemented.

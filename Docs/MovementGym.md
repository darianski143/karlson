# Milestone 1 — Movement Gym

Milestone 2 adds vertical traversal to this gym; see [VerticalTraversal.md](VerticalTraversal.md) for the new controls, routes and tuning.

Original greybox and controller for JIMMY. No KARLSON code or assets are used.

## Open and play

Use Unity **6000.6.0f1**, open `Assets/MovementGym/Scenes/MovementGym.unity`, and press Play. If the scene is missing, use **Jimmy > Movement Gym > Create Missing Gym**. The generator preserves an existing gym and its tuning assets. SampleScene remains first in the build list; explicitly select MovementGym for development or a standalone build.

- WASD / left stick: movement. Mouse / right stick: look.
- Space / gamepad A: jump. C, left Ctrl / gamepad B: hold crouch.
- Press crouch while moving at least 7 m/s on the ground to slide.
- R: return to spawn. Falling below -12 m also respawns.
- Escape releases the cursor; click to capture again. This does not pause physics.
- F3 toggles temporary telemetry (horizontal/vertical speed, state, ground and slope).

The cyan runway provides 4 m distance markers and a 1.15 m tunnel. Centre blocks test 0.35/0.75/1.25 m jumps. Orange platforms have 2/3/4 m gaps. The right side provides a 25-degree ramp, a 60-degree rejection slope and walls reserved for later traversal mechanics. There is no automatic step climbing: jump onto blocks.

## Tune and extend

Edit `Assets/MovementGym/Settings/DefaultMovement.asset` in the Inspector. Values use metres and seconds; acceleration is m/s². Starting tuning: 10 m/s running, 65 ground acceleration, 18 air acceleration, 25 gravity, 1.6 m theoretical jump height, 140 ms jump buffer, 120 ms coyote time. Fixed timestep and global gravity remain at project defaults; gravity is local to this motor.

`RigidbodyMotor` owns velocity, capsule and locomotion state in FixedUpdate. Its transform origin is at the feet; use unit scale and an upright Y capsule. Input is buffered from Update. Ground queries ignore the player's own Rigidbody and triggers and reject steep normals. Standing requires headroom. Ground friction decays momentum, including external impulses; air control caps acceleration along the desired direction rather than clamping total velocity. Slides start only on a fresh crouch press, preserve momentum, steer weakly, respond to downhill gravity, and expire. Releasing crouch ends the slide even when an obstruction requires the capsule to remain short.

`FpsInputView` owns a private copy of the supplied New Input System action asset, yaw/pitch, cursor capture and camera-height smoothing. The gym input asset derives from the repository template with an added left Ctrl binding. The original input asset is unchanged. `MovementTelemetry` is optional and can be removed for shipping.

Call `SetInput` / `QueueJump` for other input sources and `AddVelocityChange(deltaVelocity)` for recoil, grapple pulls or wall-jump impulses. Impulses apply after locomotion in the next physics tick. A positive vertical impulse temporarily disables ground attachment. Wall-run, wall-jump and grapple are extension points only, not implemented features; add their states/constraints around the motor's acceleration phase. Avoid having another component write Rigidbody velocity independently. `Teleport` resets input, jump timers, slide, stance and momentum.

## Verification

Verified on **2026-09-06** in Unity **6000.6.0f1**: scene generation and script compilation completed with exit code 0; **14/14 tests passed**, test runner exit code 0. Local detailed results are in `Logs/movement-tests.xml` and `Logs/movement-tests.log` (ignored by Git). This was a headless run; rendered presentation and subjective movement feel still need a manual playthrough.

The EditMode suite under `Assets/MovementGym/Tests/Editor` uses manual PhysX steps. It covers acceleration/braking, air control, coyote expiry and one-jump consumption, buffered landing and expiry, crouch clearance, slide entry/expiry, external impulses, walkable and steep slopes, respawn and scene references. One additional integration test enters Play Mode and checks the real scene's grounding, camera FOV and fall recovery. Tests restore the prior physics simulation mode; Unity may reserialize its physics settings during testing without changing their values.

Batch scene generation/compilation:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -executeMethod Jimmy.Movement.Editor.MovementGymBuilder.CreateGym -quit -logFile Logs/gym-import.log
```

Batch tests (do not add `-quit`; the test runner exits itself):

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath (Get-Location).Path -runTests -testPlatform EditMode -testFilter Jimmy.Movement.Tests -testResults Logs/movement-tests.xml -logFile Logs/movement-tests.log
```

Manual feel checks: run and release input, steer in the air, jump just after a ledge, press jump just before landing, hold crouch under the tunnel and release while inside, slide down the ramp, fall and respawn. Inspect at different frame rates; automated physics tests cannot establish final game feel or rendered presentation.

## Scope

URP 17.6.0, Input System 1.20.0 and Test Framework 1.8.0 were already installed. Existing pipeline, package versions, quality settings and SampleScene are preserved. Only the new scene is appended to build settings. No enemies, weapons, wall traversal, grapple, animation, audio, checkpoints or production UI are included in this milestone.

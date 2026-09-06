# Milestone 4 — Relay Breach vertical slice

Work lives in `D:/Work/Jimmy_Unity/JIMMY`, on top of approved combat checkpoint `708cab8`. Milestone 4 is intentionally uncommitted. The existing Movement Gym scene and GymPlayer prefab are preserved.

## Open and run

Open **Assets/VerticalSlice/Scenes/RelayBreach.unity** and press Play. Reopen the saved scene if Unity already had an older copy open. The scene is appended to Build Settings; SampleScene and MovementGym remain available. Select RelayBreach as the entry scene when making a slice-only build.

The run starts immediately, without a countdown. Recover the glowing relay core at the convergence hub, then enter the extraction pad. Extraction rejects a player without the core. The finish overlay shows the frozen result and the local best.

- WASD / left stick: move; mouse / right stick: look.
- Space / A: jump and wall-jump; C / Ctrl / B: crouch or slide.
- Hold RMB / LB on a cyan anchor to grapple; release to keep swing momentum.
- LMB / X: fire. 1/2/3 or D-pad left/right: equip unlocked weapons.
- Walk over routing pickups to unlock shotgun or grenade launcher.
- **R / gamepad Start: full restart**, including while dead, finished or outside cursor capture.
- Escape releases the cursor; click recaptures it. The real-time run clock continues during focus loss.
- Fire/jump/crouch/grapple must be released when necessary before fresh input resumes after loading, preventing buffered actions from leaking into the next run.

Death and falls show a brief overlay and restart automatically by default. Manual restart skips the delay. There are no checkpoints or lives to exhaust; a failed attempt cannot consume a pickup or destroy a route permanently.

## Routes

**Green — safe service loop.** Turn left from spawn, follow the west deck, cross the green bridge to the east deck, and return left at the core hub. This route has continuous, wide, walkable floor and cover. A pistol is enough for the sentries. The authored waypoint distance is about 310 metres; at the baseline 10 m/s run speed, continuous sprint-free running alone takes roughly 31 seconds, before turns, aiming and hesitation.

**Cyan — fast relay gap.** Jump alongside either wall near the cyan stripe, wall-jump onto the raised catch, shoot through the glass, then grapple the cyan anchor and release over the green bridge. Continue along the narrow central catwalk, past its sentry, to the core. The bridge also provides a fallback connection to the safe route.

**Amber — advanced pressure gantry.** Collect the shotgun at the right of spawn. Jump and shoot downward to reach the first shelf; collect the launcher there and use a timed grenade boost for the higher shelf. Another shotgun boost can also reach it, so losing a grenade does not lock the route. Break the pressure door and follow the elevated gantry. Drop left onto the core hub at its end.

All routes meet at the core, then share the final extraction approach. Cover and nearby explosive barrels provide alternatives for dealing with the final sentry. The core is required even if a player finds a movement shortcut.

The design target is **30–90 seconds for a first successful run**. This is not a measured human playtest result. Experienced fast/boost routes may finish below that range. The safe route's floor continuity and references are checked automatically; timing, jump landings and readability of the skilled routes still need a manual playthrough.

## State, timer and complete restart

`RunSession` is a pure state/clock model:

- Ready → Running on scene startup.
- Running → Dead on lethal health damage, kill volume or bounds violation.
- Running → Finished only after core collection and extraction.
- Ready/Running/Dead/Finished → Restarting on a restart request.
- A completed scene reload constructs a new Ready session and starts the next run.

Terminal states reject duplicate finish/death events. The result clock freezes on death/finish; invalid/backward clock samples cannot reduce elapsed time. Timing uses `Time.realtimeSinceStartupAsDouble`, so contextual slow-motion does not reduce the recorded duration. Focus loss remains part of the attempt.

`LocalBestTimes` stores only positive, finite improvements in a level-specific PlayerPrefs key. The default key is `jimmy.relay-breach.v1.best`. Change the key/version when making timing-incompatible level edits. Tests use in-memory stores or temporary test-only keys rather than overwriting production results.

`GameFlow` owns run death/restart in this scene. The old gym-only position reset and player-health auto-respawn are disabled on the scene instance, not removed from the shared prefab. A restart clears buffered input/traversal/motion, restores the previous time scale and fixed timestep, and reloads the entire scene asynchronously. Fresh scene instances reset inventory/cooldown, health, pickups, enemies, physical corpses, panels/fragments, barrels, projectiles and transient effects. Old manager singletons release ownership on scene exit.

The player is frozen on death/finish, while the background world may continue briefly. The timer/result stays frozen, and the disabled TimeDirector cannot start another slow-motion interval before reload.

## Tuning and optional debugging

`Assets/VerticalSlice/Settings/Relay run.asset` controls title, score key, automatic start/restart, death delay, fall threshold, playable bounds and optional telemetry.

`Relay combat.asset` is a scene-specific clone of combat feedback/loadout settings: training HUD telemetry is off and only the pistol starts unlocked. Existing weapon definitions are reused. `Relay sentry.asset` gives the slice a more readable one-second windup and two-second recovery. Existing movement and traversal tuning assets remain shared and unchanged.

Movement debug telemetry is disabled by default. Enable it through the run profile for inspection. The normal HUD shows only real elapsed time, objective state, weapon, health, best and restart hint; the existing combat component supplies crosshair/hit confirmation. Existing camera-effect and slow-motion controls remain available in their profiles.

The builder is **Jimmy > Vertical Slice > Create Relay Breach**. It preserves an existing scene rather than regenerating over edits. LevelRoute components provide inspection/test waypoints and instructions; they do not implement another movement controller.

## Validation and manual follow-up

Use the synchronized validation copy while the main Unity editor is open:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<validation-project>' -executeMethod Jimmy.Run.Editor.RelayLevelBuilder.CreateLevel -quit -logFile Logs/m4-import.log
& 'C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe' -batchmode -nographics -projectPath '<validation-project>' -runTests -testPlatform EditMode -testFilter 'Jimmy.*' -testResults Logs/m4-tests.xml -logFile Logs/m4-tests.log
```

Coverage includes state transitions, real-time/frozen timer, score persistence, mandatory references, safe spawn and route floor, core/finish triggers, full world restart, death/bounds/kill-volume recovery, time-scale/fixed-step restoration and held-input recovery, plus all earlier movement/traversal/combat tests.

Manual evaluation: first-success duration for all routes; wall-jump catch and grapple release landings; shotgun/grenade shelf access; route signs and enemy sightlines; fall/bounds edge cases; restart responsiveness on the target machine; HUD/camera comfort. This remains an industrial sci-fi greybox: no final art/audio polish, multiplayer or level editor.

## Shared input change required by GameFlow

FpsInputView exposes opt-in externalRespawnControl so Relay Breach owns full-scene death/restart instead of the gym's position-only reset. Its opt-in requireNeutralOnEnable gate reads resolved button controls, including buttons already held when a new action asset is enabled. This prevents held fire/jump/crouch/grapple from leaking into a restarted run. Both flags remain false on the unchanged GymPlayer prefab. No VerticalTraversal functionality was changed for Milestone 4.

## Verified delivery

2026-09-06, Unity 6000.6.0f1: RelayLevelBuilder generated and saved RelayBreach successfully (exit 0). The final full Jimmy.* run passed 66/66 tests, including 13 VerticalSlice tests, at 20:34:44 UTC and exited 0. Reports are present in the main project's Logs/m4-tests.xml, Logs/m4-tests.log and Logs/m4-import.log.

All 53 VerticalSlice files were synchronized to the main D: project and checked byte-for-byte against the validation copy, including RelayBreach.unity, three Settings profiles, six materials and metadata. The 45 unique serialized GUID references resolve to project assets, Unity built-ins or URP package assets. Build Settings only appends enabled RelayBreach; SampleScene and MovementGym remain. The three protected user files, MovementGym scene and GymPlayer prefab match their pre-M4 hashes. No commit was made.

The 30–90 second first-success duration remains a playtest target, not a measured human result. Automated checks do not replace manual validation of all three routes or final visual polish.

using System;
using System.Linq;
using Jimmy.Combat;
using Jimmy.Movement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Run.Editor
{
    public static class RelayLevelBuilder
    {
        public const string ScenePath = "Assets/VerticalSlice/Scenes/RelayBreach.unity";
        private const string Root = "Assets/VerticalSlice/";
        [MenuItem("Jimmy/Vertical Slice/Create Relay Breach")]
        public static void CreateLevel()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("Relay Breach already exists; preserving scene edits.");
                RegisterScene(); return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Folder(Root.TrimEnd('/')); Folder(Root + "Scenes"); Folder(Root + "Settings"); Folder(Root + "Materials");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = Asset<RunSettings>("Relay run");
            var combatTuning = Clone<CombatTuning>("Assets/CombatGym/Settings/Default combat.asset", "Relay combat");
            combatTuning.telemetry = false; combatTuning.unlockAll = false; EditorUtility.SetDirty(combatTuning);
            var enemyTuning = Clone<EnemySettings>("Assets/CombatGym/Settings/Enemy behaviour.asset", "Relay sentry");
            enemyTuning.windup = 1f; enemyTuning.recovery = 2f; enemyTuning.range = 18; EditorUtility.SetDirty(enemyTuning);
            var green = Material("Safe green", new Color(0.18f, 0.62f, 0.38f));
            var cyan = Material("Fast cyan", new Color(0.1f, 0.66f, 0.82f));
            var amber = Material("Advanced amber", new Color(0.98f, 0.48f, 0.12f));
            var steel = Material("Industrial steel", new Color(0.22f, 0.27f, 0.32f));
            var floor = Material("Deck", new Color(0.45f, 0.49f, 0.52f));
            var glow = Material("Core glow", new Color(0.4f, 1, 0.85f), true);
            var red = AssetDatabase.LoadAssetAtPath<Material>("Assets/CombatGym/Materials/Warning red.mat");
            var world = new GameObject("Relay facility").transform;
            var manager = new GameObject("Run director");
            var flow = manager.AddComponent<GameFlow>(); flow.settings = settings;
            var director = manager.AddComponent<TimeDirector>();
            director.settings = AssetDatabase.LoadAssetAtPath<TimeSettings>("Assets/CombatGym/Settings/Contextual time.asset");
            flow.timeDirector = director; manager.AddComponent<CombatEffects>().settings = combatTuning;
            manager.AddComponent<RunHud>().flow = flow;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MovementGym/Prefabs/GymPlayer.prefab");
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            playerObject.name = "Runner"; playerObject.transform.position = new Vector3(0, 0.15f, 0);
            playerObject.transform.rotation = Quaternion.identity;
            flow.player = playerObject.GetComponent<RigidbodyMotor>();
            var view = playerObject.GetComponent<FpsInputView>();
            view.spawnPosition = playerObject.transform.position; view.externalRespawnControl = true; view.requireNeutralOnEnable = true;
            playerObject.GetComponent<MovementTelemetry>().visible = false;
            playerObject.GetComponent<MovementTelemetry>().enabled = false;
            playerObject.GetComponent<PlayerCombatVitals>().enabled = false;
            playerObject.GetComponent<PlayerCombat>().tuning = combatTuning;
            playerObject.GetComponent<CombatFeedback>().settings = combatTuning;
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerObject.GetComponent<PlayerCombat>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerObject.GetComponent<CombatFeedback>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerObject.GetComponent<MovementTelemetry>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerObject.GetComponent<PlayerCombatVitals>());
            var spawn = new GameObject("Safe spawn"); spawn.transform.position = playerObject.transform.position; spawn.transform.SetParent(world);
            Box(world, "Start deck", new Vector3(0, -0.5f, 2), new Vector3(18, 1, 20), floor);
            Label(world, "RELAY BREACH", new Vector3(0, 5, 8), 0.17f);
            Label(world, "GREEN: SAFE  /  CYAN: FAST  /  AMBER: BOOST\nRecover the core. Reach extraction. R / Start retries.", new Vector3(0, 3, 9), 0.065f);

            var safe = Route(world, "01 / SAFE — service loop", RouteKind.Safe,
                "Continuous wide floor with cover. Pistol is enough. Follow green toward the core.",
                new[] { new Vector3(0,0,0), new Vector3(0,0,8), new Vector3(-38,0,8),
                    new Vector3(-38,0,62), new Vector3(38,0,62), new Vector3(38,0,126),
                    new Vector3(0,0,126), new Vector3(0,0,164) });
            Box(safe, "West access", new Vector3(-20,-0.5f,8), new Vector3(44,1,8), green);
            Box(safe, "West service deck", new Vector3(-38,-0.5f,35), new Vector3(8,1,62), floor);
            Box(safe, "Cross-facility bridge", new Vector3(0,-0.5f,62), new Vector3(84,1,8), green);
            Box(safe, "East service deck", new Vector3(38,-0.5f,94), new Vector3(8,1,72), floor);
            Box(safe, "West outer rail", new Vector3(-42,0.6f,35), new Vector3(0.2f,1.2f,50), steel);
            Box(safe, "West inner rail", new Vector3(-34,0.6f,35), new Vector3(0.2f,1.2f,42), steel);
            Box(safe, "East outer rail", new Vector3(42,0.6f,94), new Vector3(0.2f,1.2f,52), steel);
            Box(safe, "Bridge near rail", new Vector3(0,0.6f,58), new Vector3(62,1.2f,0.2f), steel);
            // Keep the cyan connection at the bridge centre open.
            Box(safe, "Bridge far rail left", new Vector3(-19,0.6f,66), new Vector3(24,1.2f,0.2f), steel);
            Box(safe, "Bridge far rail right", new Vector3(19,0.6f,66), new Vector3(24,1.2f,0.2f), steel);
            Cover(safe, new Vector3(-35.5f,1,30), steel);
            Cover(safe, new Vector3(40.5f,1,86), steel);
            Enemy(safe, "West sentry", new Vector3(-35.5f,1,43), enemyTuning, flow.player.transform, amber, red, combatTuning);
            Enemy(safe, "East sentry", new Vector3(40.5f,1,102), enemyTuning, flow.player.transform, amber, red, combatTuning);
            Label(safe, "SAFE > WEST SERVICE LOOP", new Vector3(-19,2.5f,10), 0.075f);
            Label(safe, "TURN RIGHT > EAST DECK", new Vector3(-38,3,64), 0.075f);
            Label(safe, "CORE < LEFT AT THE FAR HUB", new Vector3(38,3,126), 0.075f);

            var fast = Route(world, "02 / FAST — relay gap", RouteKind.Fast,
                "Jump beside cyan wall; wall-jump to the raised catch. Shoot glass, grapple cyan anchor, release onto bridge.",
                new[] { new Vector3(0,0,8), new Vector3(-1.2f,0,16), new Vector3(-1.2f,5,27),
                    new Vector3(0,3,39), new Vector3(0,3,45), new Vector3(0,0,62),
                    new Vector3(0,0,96), new Vector3(0,0,126), new Vector3(0,0,164) });
            Box(fast, "Wall approach", new Vector3(0,-0.5f,13), new Vector3(6,1,10), cyan);
            Wall(fast, "Cyan left wall", new Vector3(-2.2f,2,25), new Vector3(0.6f,12,26), steel);
            Wall(fast, "Cyan right wall", new Vector3(2.2f,2,25), new Vector3(0.6f,12,26), steel);
            Box(fast, "Wall approach stripe", new Vector3(-1.2f,0.02f,14), new Vector3(0.25f,0.04f,8), cyan);
            Box(fast, "Raised catch", new Vector3(0,2.5f,39), new Vector3(10,1,14), cyan);
            Fracture(fast, "Cyan breakable glass", new Vector3(0,4.3f,43), new Vector3(8,2.6f,0.15f),
                "Assets/CombatGym/Settings/Glass fracture.asset", cyan);
            var anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere); anchor.name = "Relay grapple anchor";
            anchor.transform.SetParent(fast); anchor.transform.position = new Vector3(0,13,53); anchor.transform.localScale = Vector3.one * 2.5f;
            anchor.GetComponent<Renderer>().sharedMaterial = glow; anchor.AddComponent<GrappleAnchor>();
            Box(fast, "Fast central catwalk", new Vector3(0,-0.5f,96), new Vector3(6,1,64), cyan);
            Cover(fast, new Vector3(-1.8f,1,92), steel);
            Enemy(fast, "Catwalk sentry", new Vector3(1.8f,1,105), enemyTuning, flow.player.transform, amber, red, combatTuning);
            Label(fast, "FAST / JUMP BY WALL", new Vector3(0,5,18), 0.075f);
            Label(fast, "SHOOT GLASS > HOLD RMB / LB ON CYAN ANCHOR\nRELEASE OVER GREEN BRIDGE", new Vector3(0,7,45), 0.06f);

            var advanced = Route(world, "03 / ADVANCED — pressure gantry", RouteKind.Advanced,
                "Pick up shotgun, jump and fire down to first shelf. Launcher on shelf enables grenade boost; shotgun can also reach next shelf. Break the door and drop left into core hub.",
                new[] { new Vector3(4,0,5), new Vector3(12,0,16), new Vector3(12,4,28),
                    new Vector3(12,8,47), new Vector3(12,8,124), new Vector3(0,0,126), new Vector3(0,0,164) });
            Pickup(advanced, "Shotgun routing pickup", new Vector3(4,0.8f,5), combatTuning.weapons[1], amber);
            Box(advanced, "Boost approach", new Vector3(12,-0.5f,12), new Vector3(8,1,16), amber);
            Box(advanced, "Shotgun shelf", new Vector3(12,3.5f,28), new Vector3(8,1,12), floor);
            Pickup(advanced, "Launcher routing pickup", new Vector3(12,4.8f,27), combatTuning.weapons[2], cyan);
            Box(advanced, "Grenade shelf", new Vector3(12,7.5f,47), new Vector3(8,1,14), amber);
            Box(advanced, "Pressure gantry", new Vector3(12,7.5f,89), new Vector3(6,1,80), floor);
            Box(advanced, "Gantry outer rail", new Vector3(15,8.6f,88), new Vector3(0.2f,1.2f,68), steel);
            Fracture(advanced, "Pressure access door", new Vector3(12,9.3f,50), new Vector3(6,2.6f,0.25f),
                "Assets/CombatGym/Settings/Door fracture.asset", steel);
            Label(advanced, "BOOST / JUMP + SHOOT DOWN", new Vector3(12,5,22), 0.07f);
            Label(advanced, "LAUNCHER > GRENADE BOOST\nSHOTGUN ALSO WORKS", new Vector3(12,10.5f,42), 0.07f);
            Label(advanced, "CORE < DROP LEFT", new Vector3(12,10,125), 0.09f);

            Box(world, "Core convergence hub", new Vector3(0,-0.5f,126), new Vector3(84,1,12), green);
            Box(world, "Extraction approach", new Vector3(0,-0.5f,145), new Vector3(12,1,34), floor);
            Box(world, "Extraction deck", new Vector3(0,-0.5f,164), new Vector3(18,1,20), green);
            Cover(world, new Vector3(3.5f,1,141), steel);
            Enemy(world, "Extraction sentry", new Vector3(4,1,150), enemyTuning, flow.player.transform, amber, red, combatTuning);
            Barrel(world, "Coolant barrel A", new Vector3(4,0.8f,146), amber);
            Barrel(world, "Coolant barrel B", new Vector3(6.5f,0.8f,146), amber);
            flow.core = Trigger(world, "Relay core", new Vector3(0,1.1f,126), new Vector3(2.4f,2.2f,2.4f), RunTriggerKind.Core, flow);
            var coreVisual = Box(flow.core.transform, "Energy core visual", new Vector3(0,1.2f,126), Vector3.one * 0.8f, glow);
            UnityEngine.Object.DestroyImmediate(coreVisual.GetComponent<Collider>()); coreVisual.transform.rotation = Quaternion.Euler(20,45,15);
            Label(world, "RELAY CORE > WALK THROUGH", new Vector3(0,4,128), 0.11f);
            flow.extraction = Trigger(world, "Extraction field", new Vector3(0,1.2f,164), new Vector3(5,2.4f,5), RunTriggerKind.Extraction, flow);
            Box(world, "Extraction pad", new Vector3(0,0.02f,164), new Vector3(5,0.04f,5), cyan);
            Box(world, "Extraction terminal", new Vector3(0,1.5f,169), new Vector3(5,3,0.5f), steel);
            Label(world, "EXTRACT CORE", new Vector3(0,3.5f,168), 0.13f);
            Trigger(world, "Coolant kill volume", new Vector3(0,-13,82), new Vector3(110,2,200), RunTriggerKind.Kill, flow);
            Box(world, "West level bound", new Vector3(-48,6,82), new Vector3(1,12,184), steel);
            Box(world, "East level bound", new Vector3(48,6,82), new Vector3(1,12,184), steel);
            Box(world, "South level bound", new Vector3(0,6,-12), new Vector3(97,12,1), steel);
            Box(world, "North level bound", new Vector3(0,6,178), new Vector3(97,12,1), steel);
            var sun = new GameObject("Relay sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.4f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(45,-30,0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.55f,0.59f,0.65f);
            view.playerCamera.backgroundColor = new Color(0.06f,0.1f,0.15f); view.playerCamera.farClipPlane = 250;
            PrefabUtility.RecordPrefabInstancePropertyModifications(view.playerCamera);
            EditorSceneManager.SaveScene(scene, ScenePath); RegisterScene(); AssetDatabase.SaveAssets();
            Debug.Log("RELAY_BREACH_READY");
        }
        private static void RegisterScene()
        {
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        }
        private static Transform Route(Transform parent, string name, RouteKind kind, string instructions, Vector3[] points)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent);
            var route = obj.AddComponent<LevelRoute>(); route.kind = kind; route.instructions = instructions; route.waypoints = points;
            return obj.transform;
        }
        private static RunTrigger Trigger(Transform parent, string name, Vector3 position, Vector3 size, RunTriggerKind kind, GameFlow flow)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = position;
            var collider = obj.AddComponent<BoxCollider>(); collider.size = size; collider.isTrigger = true;
            var trigger = obj.AddComponent<RunTrigger>(); trigger.flow = flow; trigger.kind = kind; return trigger;
        }
        private static void Enemy(Transform parent, string name, Vector3 position, EnemySettings settings, Transform player, Material material, Material beamMaterial, CombatTuning tuning)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Capsule); obj.name = name; obj.transform.SetParent(parent); obj.transform.position = position;
            obj.GetComponent<Renderer>().sharedMaterial = material; obj.AddComponent<Rigidbody>().isKinematic = true;
            obj.AddComponent<CombatHealth>().settings = AssetDatabase.LoadAssetAtPath<HealthSettings>("Assets/CombatGym/Settings/Enemy health.asset");
            obj.AddComponent<PhysicsImpulseBody>(); obj.AddComponent<PhysicsDeath>();
            var ai = obj.AddComponent<CombatEnemy>(); ai.settings = settings; ai.target = player;
            var beam = new GameObject("Sentry telegraph").AddComponent<LineRenderer>(); beam.transform.SetParent(obj.transform, false);
            beam.sharedMaterial = beamMaterial; beam.startWidth = beam.endWidth = tuning.tracerWidth; beam.enabled = false; ai.warningBeam = beam;
        }
        private static void Barrel(Transform parent, string name, Vector3 position, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder); obj.name = name; obj.transform.SetParent(parent); obj.transform.position = position;
            obj.transform.localScale = new Vector3(1,0.8f,1); obj.GetComponent<Renderer>().sharedMaterial = material;
            var barrel = obj.AddComponent<ExplosiveBarrel>();
            barrel.explosion = AssetDatabase.LoadAssetAtPath<ExplosionSettings>("Assets/CombatGym/Settings/Barrel explosion.asset");
            barrel.durability = AssetDatabase.LoadAssetAtPath<HealthSettings>("Assets/CombatGym/Settings/Barrel health.asset");
        }
        private static void Pickup(Transform parent, string name, Vector3 position, WeaponDefinition weapon, Material material)
        {
            var obj = Box(parent, name, position, new Vector3(0.8f,0.6f,0.8f), material);
            obj.GetComponent<Collider>().isTrigger = true; obj.AddComponent<WeaponPickup>().weapon = weapon;
        }
        private static void Cover(Transform parent, Vector3 position, Material material) =>
            Box(parent, "Sentry cover", position, new Vector3(2,2,1.5f), material);
        private static void Wall(Transform parent, string name, Vector3 position, Vector3 size, Material material) =>
            Box(parent, name, position, size, material).AddComponent<WallSurface>();
        private static void Fracture(Transform parent, string name, Vector3 position, Vector3 size, string settingsPath, Material material)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
            var panel = Box(root.transform, "Intact panel", position, size, material);
            var breakable = root.AddComponent<Breakable>(); breakable.settings = AssetDatabase.LoadAssetAtPath<DestructibleSettings>(settingsPath);
            breakable.intactCollider = panel.GetComponent<Collider>(); breakable.intactRenderer = panel.GetComponent<Renderer>();
            breakable.fragments = new Rigidbody[6];
            for (int i = 0; i < 6; i++)
            {
                Vector3 cell = new Vector3(size.x/3,size.y/2,size.z);
                Vector3 offset = new Vector3((i%3-1)*cell.x, (i/3-0.5f)*cell.y,0);
                var piece = Box(root.transform, "Prefractured section", position+offset, cell*0.96f, material);
                var body = piece.AddComponent<Rigidbody>(); body.isKinematic = true; piece.SetActive(false); breakable.fragments[i] = body;
            }
        }
        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent);
            obj.transform.position = position; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static void Label(Transform parent, string text, Vector3 position, float size)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(parent); label.transform.position = position;
            label.text = text; label.characterSize = size; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        }
        private static T Asset<T>(string name) where T : ScriptableObject
        {
            string path = Root+"Settings/"+name+".asset"; var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset,path); return asset;
        }
        private static T Clone<T>(string source, string name) where T : ScriptableObject
        {
            string path = Root+"Settings/"+name+".asset"; var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<T>(source)); asset.name = name;
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Root+"Materials/"+name+".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = name; material.color = color; AssetDatabase.CreateAsset(material,path); return material;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
        }
    }
}

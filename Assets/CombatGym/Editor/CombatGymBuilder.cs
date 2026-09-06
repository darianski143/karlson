using System;
using Jimmy.Movement;
using Jimmy.Movement.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Combat.Editor
{
    public static class CombatGymBuilder
    {
        private const string Root = "Assets/CombatGym/";
        [MenuItem("Jimmy/Movement Gym/Add Combat Gym")]
        public static void UpgradeGym()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Folder(Root.TrimEnd('/')); Folder(Root + "Settings"); Folder(Root + "Materials");
            var scene = EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            var amber = Material("Amber FX", new Color(1, 0.65f, 0.08f), true);
            var red = Material("Warning red", new Color(1, 0.12f, 0.05f), true);
            var steel = Material("Steel", new Color(0.24f, 0.28f, 0.32f));
            var deck = Material("Deck", new Color(0.43f, 0.44f, 0.47f));
            var cyan = Material("Glass cyan", new Color(0.15f, 0.7f, 0.8f));
            var orange = Material("Boost orange", new Color(0.92f, 0.43f, 0.12f));
            var grenadeExplosion = Asset<ExplosionSettings>("Grenade explosion", null);
            var barrelExplosion = Asset<ExplosionSettings>("Barrel explosion", x => { x.damage = 120; x.radius = 6; x.impulse = 18; });
            var pistol = Asset<WeaponDefinition>("Pistol", x => { x.kind = WeaponKind.Pistol; x.displayName = "Pistol"; });
            var shotgun = Asset<WeaponDefinition>("Shotgun", x => {
                x.kind = WeaponKind.Shotgun; x.displayName = "Shotgun"; x.damage = 12; x.pellets = 8;
                x.spreadDegrees = 4; x.shotInterval = 0.75f; x.playerRecoil = 8; x.targetImpulse = 0.7f; x.viewKick = 2;
            });
            var grenade = Asset<WeaponDefinition>("Grenade launcher", x => {
                x.kind = WeaponKind.GrenadeLauncher; x.displayName = "Grenade launcher"; x.shotInterval = 1;
                x.playerRecoil = 1; x.viewKick = 1.3f; x.explosion = grenadeExplosion; x.projectileMaterial = orange;
            });
            var tuning = Asset<CombatTuning>("Default combat", x => {
                x.weapons = new[] { pistol, shotgun, grenade }; x.tracerMaterial = amber; x.impactMaterial = amber; x.explosionMaterial = amber;
            });
            var playerHealth = Asset<HealthSettings>("Player health", null);
            var enemyHealth = Asset<HealthSettings>("Enemy health", x => { x.maximumHealth = 40; x.instantDeath = true; });
            var targetHealth = Asset<HealthSettings>("Target health", x => { x.maximumHealth = 400; });
            var barrelHealth = Asset<HealthSettings>("Barrel health", x => { x.maximumHealth = 20; });
            var enemyTuning = Asset<EnemySettings>("Enemy behaviour", null);
            var glass = Asset<DestructibleSettings>("Glass fracture", x => { x.health = 1; x.fragmentMass = 0.4f; });
            var door = Asset<DestructibleSettings>("Door fracture", x => { x.health = 45; x.fragmentMass = 2; });
            var time = Asset<TimeSettings>("Contextual time", null);
            const string playerPath = "Assets/MovementGym/Prefabs/GymPlayer.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(playerPath);
            try { ConfigurePlayer(prefab, tuning, playerHealth, steel, orange, amber); PrefabUtility.SaveAsPrefabAsset(prefab, playerPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            RigidbodyMotor player = null;
            foreach (var obj in scene.GetRootGameObjects())
                foreach (var motor in obj.GetComponentsInChildren<RigidbodyMotor>(true))
                { ConfigurePlayer(motor.gameObject, tuning, playerHealth, steel, orange, amber); player = motor; }

            if (GameObject.Find("Combat Gym / 03") == null)
            {
                var group = new GameObject("Combat Gym / 03").transform;
                group.gameObject.AddComponent<CombatEffects>().settings = tuning;
                group.gameObject.AddComponent<TimeDirector>().settings = time;
                Box(group, "Combat access bridge", new Vector3(66, -0.5f, -5), new Vector3(8, 1, 8), deck);
                Box(group, "Combat deck", new Vector3(92, -0.5f, 14), new Vector3(52, 1, 48), deck);
                Label(group, "COMBAT LAB / 03", new Vector3(91, 5, -1), 0.16f);

                Box(group, "Accuracy backstop", new Vector3(75, 3, 29), new Vector3(12, 6, 0.5f), steel);
                for (int i = 0; i < 3; i++)
                {
                    var target = Box(group, "Accuracy target " + (i + 1), new Vector3(71 + i * 4, 1.8f + i * 0.5f, 24), new Vector3(1, 1, 0.25f), orange);
                    target.AddComponent<CombatHealth>().settings = targetHealth;
                }
                Label(group, "01 / PISTOL ACCURACY", new Vector3(75, 5, 28));
                Label(group, "Aim at orange targets. LMB / X fires.", new Vector3(75, 2, 3), 0.06f);

                Pickup(group, "Shotgun pickup", new Vector3(88, 0.8f, -4), shotgun, orange);
                Box(group, "Shotgun boost mark", new Vector3(87, 0.02f, 5), new Vector3(3, 0.04f, 3), orange);
                Box(group, "Shotgun landing low", new Vector3(87, 2, 12), new Vector3(4, 0.5f, 4), steel);
                Box(group, "Shotgun landing high", new Vector3(87, 4, 19), new Vector3(4, 0.5f, 4), orange);
                Label(group, "02 / SHOTGUN BOOST", new Vector3(87, 6, 20));
                Label(group, "Jump + aim down + fire. Keep your forward speed.", new Vector3(87, 2.5f, 2), 0.055f);

                Pickup(group, "Grenade launcher pickup", new Vector3(100, 0.8f, -4), grenade, cyan);
                Box(group, "Grenade boost mark", new Vector3(100, 0.02f, 5), new Vector3(3, 0.04f, 3), cyan);
                Box(group, "Grenade boost landing", new Vector3(100, 3.8f, 12), new Vector3(5, 0.5f, 5), orange);
                Label(group, "03 / GRENADE BOOST", new Vector3(100, 6, 17));
                Label(group, "Fire toward the floor. Time a jump with the fuse.", new Vector3(100, 2.5f, 2), 0.055f);

                var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule); enemy.name = "Telegraph enemy";
                enemy.transform.SetParent(group); enemy.transform.position = new Vector3(113, 1, 21);
                enemy.GetComponent<Renderer>().sharedMaterial = orange;
                var enemyBody = enemy.AddComponent<Rigidbody>(); enemyBody.isKinematic = true;
                enemy.AddComponent<CombatHealth>().settings = enemyHealth;
                enemy.AddComponent<PhysicsImpulseBody>(); enemy.AddComponent<PhysicsDeath>();
                var ai = enemy.AddComponent<CombatEnemy>(); ai.settings = enemyTuning; ai.target = player.transform;
                var beamObj = new GameObject("Attack warning"); beamObj.transform.SetParent(enemy.transform, false);
                ai.warningBeam = beamObj.AddComponent<LineRenderer>(); ai.warningBeam.enabled = false; ai.warningBeam.sharedMaterial = red;
                ai.warningBeam.startWidth = ai.warningBeam.endWidth = tuning.tracerWidth;
                Label(group, "04 / ENEMY + PHYSICS BODY", new Vector3(112, 4, 23), 0.085f);
                Box(group, "Enemy cover", new Vector3(110, 1, 12), new Vector3(3, 2, 1), steel);

                Fracture(group, "Breakable glass", new Vector3(91, 1.3f, 29), new Vector3(3, 2.6f, 0.14f), 3, 2, glass, cyan);
                Fracture(group, "Breakable door", new Vector3(98, 1.4f, 29), new Vector3(2.2f, 2.8f, 0.25f), 2, 3, door, steel);
                for (int i = 0; i < 3; i++)
                {
                    var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder); barrel.name = "Chain barrel " + (i + 1);
                    barrel.transform.SetParent(group); barrel.transform.position = new Vector3(105 + i * 3, 0.8f, 32);
                    barrel.transform.localScale = new Vector3(1, 0.8f, 1); barrel.GetComponent<Renderer>().sharedMaterial = orange;
                    var explosive = barrel.AddComponent<ExplosiveBarrel>(); explosive.durability = barrelHealth; explosive.explosion = barrelExplosion;
                }
                var crate = Box(group, "Physics crate", new Vector3(103, 0.6f, 23), Vector3.one * 1.2f, steel);
                crate.AddComponent<Rigidbody>().mass = 5; crate.AddComponent<PhysicsImpulseBody>();
                Label(group, "05 / GLASS - DOOR - CHAIN REACTION", new Vector3(100, 5, 36), 0.09f);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets(); Debug.Log("COMBAT_GYM_READY");
        }
        private static void ConfigurePlayer(GameObject player, CombatTuning tuning, HealthSettings health, Material steel, Material orange, Material flashMaterial)
        {
            var view = player.GetComponent<FpsInputView>();
            var vitals = player.GetComponent<CombatHealth>();
            if (vitals == null) vitals = player.AddComponent<CombatHealth>();
            if (vitals.settings == null) vitals.settings = health;
            if (player.GetComponent<PhysicsImpulseBody>() == null) player.AddComponent<PhysicsImpulseBody>();
            if (player.GetComponent<PlayerCombatVitals>() == null) player.AddComponent<PlayerCombatVitals>();
            var combat = player.GetComponent<PlayerCombat>();
            if (combat == null) combat = player.AddComponent<PlayerCombat>();
            if (combat.tuning == null) combat.tuning = tuning;
            var feedback = player.GetComponent<CombatFeedback>();
            if (feedback == null) feedback = player.AddComponent<CombatFeedback>();
            feedback.settings = tuning; feedback.playerCamera = view.playerCamera; combat.feedback = feedback;
            if (combat.weaponModels == null || combat.weaponModels.Length == 0)
            {
                var rig = new GameObject("Greybox weapons").transform; rig.SetParent(view.playerCamera.transform, false);
                rig.localPosition = new Vector3(0.28f, -0.25f, 0.55f);
                combat.weaponModels = new GameObject[3];
                Vector3[] sizes = { new Vector3(0.12f, 0.14f, 0.35f), new Vector3(0.16f, 0.14f, 0.6f), new Vector3(0.24f, 0.2f, 0.45f) };
                for (int i = 0; i < 3; i++)
                {
                    var model = GameObject.CreatePrimitive(PrimitiveType.Cube); model.name = tuning.weapons[i].displayName;
                    UnityEngine.Object.DestroyImmediate(model.GetComponent<Collider>());
                    model.transform.SetParent(rig, false); model.transform.localScale = sizes[i];
                    model.GetComponent<Renderer>().sharedMaterial = i == 2 ? orange : steel;
                    model.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    model.SetActive(i == 0); combat.weaponModels[i] = model;
                }
                var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere); flash.name = "Muzzle flash";
                UnityEngine.Object.DestroyImmediate(flash.GetComponent<Collider>()); flash.transform.SetParent(rig, false);
                flash.transform.localPosition = new Vector3(0, 0, 0.35f); flash.transform.localScale = Vector3.one * 0.08f;
                flash.GetComponent<Renderer>().sharedMaterial = flashMaterial; flash.SetActive(false); feedback.muzzleFlash = flash;
            }
        }
        private static void Fracture(Transform parent, string name, Vector3 position, Vector3 size, int columns, int rows, DestructibleSettings settings, Material material)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); root.transform.position = position;
            var intact = Box(root.transform, "Intact panel", position, size, material);
            var breakable = root.AddComponent<Breakable>(); breakable.settings = settings;
            breakable.intactRenderer = intact.GetComponent<Renderer>(); breakable.intactCollider = intact.GetComponent<Collider>();
            breakable.fragments = new Rigidbody[columns * rows];
            for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
            {
                Vector3 cell = new Vector3(size.x / columns, size.y / rows, size.z);
                Vector3 offset = new Vector3((x + 0.5f) * cell.x - size.x / 2, (y + 0.5f) * cell.y - size.y / 2, 0);
                var piece = Box(root.transform, "Prefractured piece", position + offset, cell * 0.96f, material);
                var body = piece.AddComponent<Rigidbody>(); body.isKinematic = true;
                breakable.fragments[y * columns + x] = body; piece.SetActive(false);
            }
        }
        private static void Pickup(Transform parent, string name, Vector3 position, WeaponDefinition weapon, Material material)
        {
            var obj = Box(parent, name, position, new Vector3(0.6f, 0.6f, 1), material);
            obj.GetComponent<Collider>().isTrigger = true; obj.AddComponent<WeaponPickup>().weapon = weapon;
        }
        private static T Asset<T>(string name, Action<T> initialize) where T : ScriptableObject
        {
            string path = Root + "Settings/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); initialize?.Invoke(asset); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = name; material.color = color; AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent); obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static void Label(Transform parent, string text, Vector3 position, float size = 0.1f)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(parent);
            label.transform.position = position; label.text = text; label.characterSize = size;
            label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        }
    }
}

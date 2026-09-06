using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Jimmy.Movement.Editor
{
    public static class VerticalGymBuilder
    {
        private const string Root = "Assets/MovementGym/";
        private const string Group = "Vertical Traversal / 02";

        [MenuItem("Jimmy/Movement Gym/Add Vertical Traversal")]
        public static void UpgradeGym()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(MovementGymBuilder.ScenePath);
            var settings = Asset<TraversalSettings>("Settings/DefaultTraversal.asset");
            var padSettings = Asset<JumpPadSettings>("Settings/DefaultJumpPad.asset");
            var rope = Material("Rope", new Color(0.3f, 0.95f, 1), true);
            const string prefabPath = Root + "Prefabs/GymPlayer.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ConfigurePlayer(prefab, settings, rope);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            // Configure any non-prefab player too, preserving scene overrides.
            foreach (var obj in scene.GetRootGameObjects())
                foreach (var motor in obj.GetComponentsInChildren<RigidbodyMotor>(true))
                    ConfigurePlayer(motor.gameObject, settings, rope);

            if (GameObject.Find(Group) == null)
            {
                var group = new GameObject(Group).transform;
                var floor = Material("TraversalDeck", new Color(0.4f, 0.44f, 0.48f));
                var wall = Material("TraversalWall", new Color(0.15f, 0.35f, 0.42f));
                var goal = Material("TraversalGoal", new Color(0.95f, 0.54f, 0.15f));
                var anchor = Material("Anchor", new Color(0.15f, 0.85f, 0.95f), true);
                Box(group, "Access bridge / route starts", new Vector3(40, -0.5f, -5), new Vector3(50, 1, 10), floor);

                var routeA = new GameObject("A / Slide - wall run - wall jump").transform; routeA.SetParent(group);
                Box(routeA, "Run-up deck", new Vector3(24, -0.5f, 6), new Vector3(4, 1, 20), floor);
                Box(routeA, "Slide tunnel", new Vector3(24, 1.4f, 3), new Vector3(4, 0.5f, 4), wall);
                Box(routeA, "Left approach stripe", new Vector3(22.6f, 0.02f, 8), new Vector3(0.3f, 0.04f, 16), goal);
                Wall(routeA, "Left run wall", new Vector3(21.6f, 3, 23), new Vector3(0.6f, 10, 26), wall);
                Wall(routeA, "Right run wall", new Vector3(26.4f, 3, 23), new Vector3(0.6f, 10, 26), wall);
                Box(routeA, "Wall jump landing", new Vector3(24, -0.5f, 41), new Vector3(10, 1, 10), goal);
                Label(routeA, "A / SLIDE > WALL RUN > JUMP", new Vector3(24, 4, 8));
                Label(routeA, "Follow stripe. Release crouch. Jump by wall.\nSpace to wall-jump. Each wall once until landing.", new Vector3(24, 3, -2), 0.055f);

                var routeB = new GameObject("B / Grapple swing - release").transform; routeB.SetParent(group);
                var ramp = Box(routeB, "Swing access ramp", new Vector3(42, 1.65f, -1), new Vector3(7, 0.5f, 12), floor);
                ramp.transform.rotation = Quaternion.Euler(-20, 0, 0);
                Box(routeB, "Swing launch", new Vector3(42, 3.5f, 8), new Vector3(8, 1, 8), floor);
                Box(routeB, "Swing landing", new Vector3(42, 2.5f, 41), new Vector3(12, 1, 12), goal);
                var target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                target.name = "Grapple anchor / static"; target.transform.SetParent(routeB);
                target.transform.position = new Vector3(42, 14, 23); target.transform.localScale = Vector3.one * 2.8f;
                target.GetComponent<Renderer>().sharedMaterial = anchor; target.AddComponent<GrappleAnchor>();
                Label(routeB, "B / SWING > RELEASE", new Vector3(42, 7, 11));
                Label(routeB, "Aim at cyan sphere. Hold RMB / LB.\nRun off edge. Release while rising toward landing.", new Vector3(42, 6, 3), 0.055f);

                var routeC = new GameObject("C / Jump pad - wall interaction").transform; routeC.SetParent(group);
                Box(routeC, "Pad approach", new Vector3(60, -0.5f, 5), new Vector3(8, 1, 16), floor);
                Box(routeC, "Pad base", new Vector3(60, 0.06f, 9), new Vector3(2, 0.12f, 2), goal);
                var pad = new GameObject("Forward launch pad"); pad.transform.SetParent(routeC);
                pad.transform.position = new Vector3(60, 0.25f, 9);
                pad.transform.rotation = Quaternion.Euler(25, 0, 0);
                var trigger = pad.AddComponent<BoxCollider>(); trigger.size = new Vector3(2, 0.5f, 2); trigger.isTrigger = true;
                pad.AddComponent<JumpPad>().settings = padSettings;
                Wall(routeC, "Pad left wall", new Vector3(59, 8, 22), new Vector3(0.6f, 12, 18), wall);
                Wall(routeC, "Pad right wall", new Vector3(63, 8, 22), new Vector3(0.6f, 12, 18), wall);
                Box(routeC, "Elevated wall jump landing", new Vector3(61, 5.5f, 34), new Vector3(10, 1, 8), goal);
                Label(routeC, "C / PAD > WALL > JUMP", new Vector3(61, 13, 23));
                Label(routeC, "Run over orange pad. Hold forward.\nStay alongside wall, then Space to jump out.", new Vector3(60, 3, 3), 0.055f);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("VERTICAL_GYM_READY");
        }

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, Root + path);
            return asset;
        }
        private static void ConfigurePlayer(GameObject player, TraversalSettings settings, Material rope)
        {
            var traversal = player.GetComponent<VerticalTraversal>();
            if (traversal == null) traversal = player.AddComponent<VerticalTraversal>();
            if (traversal.settings == null) traversal.settings = settings;
            if (traversal.ropeLine == null)
            {
                var lineObject = new GameObject("Grapple Rope"); lineObject.transform.SetParent(player.transform, false);
                traversal.ropeLine = lineObject.AddComponent<LineRenderer>();
                traversal.ropeLine.useWorldSpace = true;
                traversal.ropeLine.positionCount = 2;
                traversal.ropeLine.enabled = false;
                traversal.ropeLine.sharedMaterial = rope;
                traversal.ropeLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Root + "Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = name; material.color = color;
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent); obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        private static void Wall(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            Box(parent, name, position, size, material).AddComponent<WallSurface>();
        }
        private static void Label(Transform parent, string text, Vector3 position, float size = 0.1f)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(parent);
            label.transform.position = position; label.text = text; label.characterSize = size;
            label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
        }
    }
}

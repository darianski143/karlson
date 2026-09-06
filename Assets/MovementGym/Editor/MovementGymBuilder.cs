using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Jimmy.Movement.Editor
{
    public static class MovementGymBuilder
    {
        public const string ScenePath = "Assets/MovementGym/Scenes/MovementGym.unity";
        private const string Root = "Assets/MovementGym";

        [MenuItem("Jimmy/Movement Gym/Create Missing Gym")]
        public static void CreateGym()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("Movement Gym already exists; preserving scene edits.");
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Folder(Root + "/Scenes"); Folder(Root + "/Materials"); Folder(Root + "/Settings"); Folder(Root + "/Prefabs");
            var settings = AssetDatabase.LoadAssetAtPath<MovementSettings>(Root + "/Settings/DefaultMovement.asset");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MovementSettings>();
                AssetDatabase.CreateAsset(settings, Root + "/Settings/DefaultMovement.asset");
            }
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (input == null) throw new InvalidOperationException("Existing project input actions are required.");
            // Make a dedicated copy so the template's UI and other bindings remain untouched.
            const string inputPath = Root + "/Settings/MovementControls.inputactions";
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath) == null)
            {
                var copy = UnityEngine.Object.Instantiate(input);
                copy.name = "MovementControls";
                copy.FindAction("Player/Crouch").AddBinding("<Keyboard>/leftCtrl");
                System.IO.File.WriteAllText(inputPath, copy.ToJson());
                UnityEngine.Object.DestroyImmediate(copy);
                AssetDatabase.ImportAsset(inputPath);
            }
            input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var concrete = Material("Concrete", new Color(0.37f, 0.4f, 0.44f));
            var light = Material("Platforms", new Color(0.65f, 0.68f, 0.7f));
            var cyan = Material("Runway", new Color(0.08f, 0.58f, 0.67f));
            var orange = Material("Jump", new Color(0.95f, 0.39f, 0.12f));
            var dark = Material("Obstacles", new Color(0.18f, 0.21f, 0.26f));

            Box("Main deck", new Vector3(0, -0.5f, 3), new Vector3(34, 1, 30), concrete);
            Box("Acceleration runway", new Vector3(-10, 0.015f, 3), new Vector3(4, 0.03f, 26), cyan);
            for (int i = 0; i < 7; i++) Box("Distance marker " + i * 4 + "m", new Vector3(-10, 0.04f, -9 + i * 4), new Vector3(4, 0.02f, 0.09f), light);
            Box("Slide tunnel ceiling 1.15m", new Vector3(-10, 1.4f, 9), new Vector3(4, 0.5f, 6), dark);
            Box("Tunnel left", new Vector3(-12.2f, 0.7f, 9), new Vector3(0.4f, 1.4f, 6), dark);
            Box("Tunnel right", new Vector3(-7.8f, 0.7f, 9), new Vector3(0.4f, 1.4f, 6), dark);
            Box("Step 0.35", new Vector3(0, 0.175f, 0), new Vector3(3, 0.35f, 2), light);
            Box("Step 0.75", new Vector3(0, 0.375f, 3), new Vector3(3, 0.75f, 2), light);
            Box("Step 1.25", new Vector3(0, 0.625f, 6), new Vector3(3, 1.25f, 2), orange);
            Box("Coyote launch", new Vector3(0, 0.5f, 16), new Vector3(4, 1, 4), orange);
            Box("Gap landing 2m", new Vector3(0, 0.5f, 22), new Vector3(4, 1, 4), orange);
            Box("Gap landing 3m", new Vector3(0, 0.5f, 29), new Vector3(4, 1, 4), orange);
            Box("Gap landing 4m", new Vector3(0, 0.5f, 37), new Vector3(4, 1, 4), orange);
            // Inclined boxes intersect the floor at their lower ends: no entry lip.
            Ramp("25 degree slope", new Vector3(8, 0.9f, 1), new Vector3(4, 0.5f, 6), -25, light);
            Box("Ramp landing", new Vector3(8, 0.85f, 5), new Vector3(4, 1.7f, 2), light);
            Ramp("60 degree rejection slope", new Vector3(14, 1.4f, 1), new Vector3(2, 0.5f, 4), -60, dark);
            Box("Future wall traversal L", new Vector3(6, 2, 12), new Vector3(0.4f, 4, 8), dark);
            Box("Future wall traversal R", new Vector3(10, 2, 12), new Vector3(0.4f, 4, 8), dark);
            Label("01 / ACCELERATE + SLIDE", new Vector3(-10, 2.8f, 13), 0.12f);
            Label("02 / JUMP HEIGHT", new Vector3(0, 2.8f, 8), 0.12f);
            Label("03 / SLOPES", new Vector3(10, 4, 6), 0.12f);
            Label("04 / GAPS: 2m > 3m > 4m", new Vector3(0, 3, 39), 0.14f);
            Label("MOVEMENT LAB / 01", new Vector3(0, 3.5f, -3), 0.17f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.59f, 0.65f);
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-10, 0.15f, -8);
            player.AddComponent<Rigidbody>();
            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.radius = settings.radius; capsule.height = settings.standingHeight;
            capsule.center = Vector3.up * settings.standingHeight * 0.5f;
            var motor = player.AddComponent<RigidbodyMotor>(); motor.settings = settings;
            var camera = new GameObject("Player Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = Vector3.up * (settings.standingHeight - 0.12f);
            camera.nearClipPlane = 0.03f; camera.farClipPlane = 180; camera.fieldOfView = settings.fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.09f, 0.12f, 0.17f);
            var view = player.AddComponent<FpsInputView>(); view.inputActions = input; view.playerCamera = camera;
            view.spawnPosition = player.transform.position;
            player.AddComponent<MovementTelemetry>().motor = motor;
            const string prefabPath = Root + "/Prefabs/GymPlayer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                PrefabUtility.SaveAsPrefabAssetAndConnect(player, prefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Opt-in scene; preserve the template's existing build order and startup scene.
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("MOVEMENT_GYM_CREATED " + ScenePath);
        }

        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        private static Material Material(string name, Color color)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
            material = new Material(shader) { name = name, color = color };
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material; obj.isStatic = true; return obj;
        }
        private static void Ramp(string name, Vector3 position, Vector3 size, float angle, Material material)
        {
            Box(name, position, size, material).transform.rotation = Quaternion.Euler(angle, 0, 0);
        }
        private static void Label(string text, Vector3 position, float size)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.text = text;
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.characterSize = size; label.fontSize = 48; label.color = Color.white;
            label.transform.position = position;
        }
    }
}

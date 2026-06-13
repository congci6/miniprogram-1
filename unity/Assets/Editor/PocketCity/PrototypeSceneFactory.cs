using PocketCity.Core;
using PocketCity.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace PocketCity.Editor
{
    public static class PrototypeSceneFactory
    {
        private const string ScenePath = "Assets/Scenes/PocketCityPrototype.unity";
        private const string ConfigPath = "Assets/Resources/CityConfig.asset";

        [MenuItem("Pocket City/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Resources");
            DefaultCityConfigFactory.CreateDefaultCityConfig();
            VisualAssetFactory.CreateVisualAssets();
            var config = AssetDatabase.LoadAssetAtPath<CityConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("CityConfig asset could not be created.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var game = new GameObject("Pocket City Game");
            var controller = game.AddComponent<CityGameController>();
            var bridge = game.AddComponent<WeChatMiniGameBridge>();
            AssignObject(controller, "config", config);

            var map = new GameObject("City Map Renderer");
            var renderer = map.AddComponent<CityMapRenderer>();
            AssignObject(renderer, "controller", controller);
            AssignObject(renderer, "vertexColorMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.VertexColorOverlay));
            AssignObject(renderer, "roadMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Road));
            AssignObject(renderer, "roadLineMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.RoadLine));
            AssignObject(renderer, "residentialMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Residential));
            AssignObject(renderer, "commercialMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Commercial));
            AssignObject(renderer, "mixedUseMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.MixedUse));
            AssignObject(renderer, "officeMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Office));
            AssignObject(renderer, "industrialMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Industrial));
            AssignObject(renderer, "serviceMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Service));
            AssignObject(renderer, "utilityMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Utility));
            AssignObject(renderer, "roofMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Roof));
            AssignObject(renderer, "windowMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Window));
            AssignObject(renderer, "buildingFootprintMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.SoftShadow));
            AssignObject(renderer, "treeTrunkMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.TreeTrunk));
            AssignObject(renderer, "treeCanopyMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.TreeCanopy));
            AssignObject(renderer, "rockMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Rock));
            AssignObject(renderer, "shoreMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.Shore));
            AssignObject(renderer, "grassGridMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.GrassGrid));
            AssignObject(renderer, "lockedAreaMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.LockedArea));
            AssignObject(renderer, "trafficPulseMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.TrafficPulse));
            AssignObject(renderer, "serviceNeedMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.ServiceNeed));
            AssignObject(renderer, "previewOkMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.PreviewOk));
            AssignObject(renderer, "previewBlockedMaterial", VisualAssetFactory.LoadMaterial(MaterialNames.PreviewBlocked));

            var camera = CreateCamera(config);
            var cameraController = camera.gameObject.AddComponent<CityCameraController>();
            cameraController.SetMapSize(config.MapWidth, config.MapHeight);
            var interaction = game.AddComponent<CityInteractionController>();
            AssignObject(interaction, "controller", controller);
            AssignObject(interaction, "mapRenderer", renderer);
            AssignObject(interaction, "worldCamera", camera);

            var save = game.AddComponent<CitySaveController>();
            AssignObject(save, "controller", controller);
            AssignObject(save, "mapRenderer", renderer);
            AssignObject(save, "platformBridge", bridge);

            var hud = game.AddComponent<CityRuntimeHud>();
            AssignObject(hud, "controller", controller);
            AssignObject(hud, "interaction", interaction);
            AssignObject(hud, "saveController", save);
            AssignObject(hud, "cameraController", cameraController);

            CreateLight();
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = game;
            Debug.Log("Created playable Pocket City prototype demo at " + ScenePath);
        }

        [MenuItem("Pocket City/Open Prototype Scene")]
        public static void OpenPrototypeScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                CreatePrototypeScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            UpdateBuildSettings();
            Debug.Log("Opened Pocket City prototype demo at " + ScenePath);
        }

        private static Camera CreateCamera(CityConfig config)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(195, 229, 239, 255);
            camera.orthographic = true;
            camera.orthographicSize = 27f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;

            var center = new Vector3(config.MapWidth * 0.5f, 0f, config.MapHeight * 0.5f);
            cameraObject.transform.position = center + new Vector3(-42f, 48f, -42f);
            cameraObject.transform.LookAt(center);
            return camera;
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Sun Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.28f;
            light.color = new Color32(255, 248, 226, 255);
            lightObject.transform.rotation = Quaternion.Euler(50f, -42f, 0f);
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void AssignObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"Property '{propertyName}' not found on {target.GetType().Name}");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            if (path == "Assets/Scenes")
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            else if (path == "Assets/Resources")
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }
    }
}

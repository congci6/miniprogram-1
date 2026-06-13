#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PocketCity.AI.Editor
{
    /// <summary>
    /// AI图像生成编辑器窗口
    /// </summary>
    public class AIImageGeneratorWindow : EditorWindow
    {
        private AIImageConfig config;
        private string prompt = "";
        private Texture2D generatedTexture;
        private bool isGenerating = false;
        private string statusMessage = "";

        // 预设模板
        private string[] buildingTypes = new[] { "住宅", "商业", "工业", "服务", "公园" };
        private int selectedBuildingType = 0;
        private string buildingName = "新建筑";

        [MenuItem("PocketCity/AI图像生成器")]
        public static void ShowWindow()
        {
            var window = GetWindow<AIImageGeneratorWindow>("AI图像生成");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            // 查找配置文件
            string[] guids = AssetDatabase.FindAssets("t:AIImageConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<AIImageConfig>(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("AI图像生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 配置设置
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("配置", EditorStyles.boldLabel);
            config = (AIImageConfig)EditorGUILayout.ObjectField("AI配置", config, typeof(AIImageConfig), false);

            if (config == null)
            {
                EditorGUILayout.HelpBox("请先创建AIImageConfig！\n右键 > Create > PocketCity > AI Image Config", MessageType.Warning);
                if (GUILayout.Button("创建配置文件"))
                {
                    CreateConfig();
                }
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // 标签页
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(true, "建筑图标", "Button", GUILayout.Height(30)))
            {
                DrawBuildingIconTab();
            }
            if (GUILayout.Toggle(false, "自定义", "Button", GUILayout.Height(30)))
            {
                DrawCustomTab();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // 状态信息
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, isGenerating ? MessageType.Info : MessageType.None);
            }

            // 预览
            if (generatedTexture != null)
            {
                EditorGUILayout.LabelField("生成的图像:");
                float size = Mathf.Min(position.width - 20, 400);
                GUILayout.Box(generatedTexture, GUILayout.Width(size), GUILayout.Height(size));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("保存为PNG"))
                {
                    SaveTexture();
                }
                if (GUILayout.Button("清除"))
                {
                    generatedTexture = null;
                    statusMessage = "";
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBuildingIconTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("生成建筑图标", EditorStyles.boldLabel);

            buildingName = EditorGUILayout.TextField("建筑名称", buildingName);
            selectedBuildingType = EditorGUILayout.Popup("建筑类型", selectedBuildingType, buildingTypes);

            EditorGUILayout.Space();

            GUI.enabled = !isGenerating;
            if (GUILayout.Button("生成建筑图标", GUILayout.Height(40)))
            {
                GenerateBuildingIcon();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawCustomTab()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("自定义提示词", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("提示词 (Prompt):");
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(100));

            EditorGUILayout.Space();

            // 预设提示词
            GUILayout.Label("快速模板:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("UI按钮"))
            {
                prompt = "game UI button, simple icon, flat design, clean, white background";
            }
            if (GUILayout.Button("材料图标"))
            {
                prompt = "game item icon, simple, clean, game art style, white background";
            }
            if (GUILayout.Button("建筑"))
            {
                prompt = "game building, isometric view, simple, clean, game art style";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUI.enabled = !isGenerating && !string.IsNullOrEmpty(prompt);
            if (GUILayout.Button("生成图像", GUILayout.Height(40)))
            {
                GenerateCustomImage();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void GenerateBuildingIcon()
        {
            if (AIImageGenerator.Instance == null)
            {
                // 在场景中创建临时生成器
                var go = new GameObject("AIImageGenerator");
                go.AddComponent<AIImageGenerator>();
                var gen = go.GetComponent<AIImageGenerator>();

                // 通过反射设置config
                var field = typeof(AIImageGenerator).GetField("config",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(gen, config);
                }
            }

            isGenerating = true;
            statusMessage = $"正在生成 {buildingName} 图标...";

            AIImageGenerator.Instance.GenerateBuildingIcon(
                buildingName,
                buildingTypes[selectedBuildingType],
                OnImageGenerated,
                OnGenerationError
            );
        }

        private void GenerateCustomImage()
        {
            if (AIImageGenerator.Instance == null)
            {
                var go = new GameObject("AIImageGenerator");
                go.AddComponent<AIImageGenerator>();
                var gen = go.GetComponent<AIImageGenerator>();

                var field = typeof(AIImageGenerator).GetField("config",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(gen, config);
                }
            }

            isGenerating = true;
            statusMessage = "正在生成图像...";

            AIImageGenerator.Instance.GenerateImage(prompt, OnImageGenerated, OnGenerationError);
        }

        private void OnImageGenerated(Texture2D texture)
        {
            generatedTexture = texture;
            isGenerating = false;
            statusMessage = "✅ 图像生成成功！";
            Repaint();
        }

        private void OnGenerationError(string error)
        {
            isGenerating = false;
            statusMessage = $"❌ 生成失败: {error}";
            EditorUtility.DisplayDialog("生成失败", error, "确定");
            Repaint();
        }

        private void SaveTexture()
        {
            if (generatedTexture == null) return;

            string path = EditorUtility.SaveFilePanel("保存图像", "Assets/Resources/Icons", "generated_image", "png");
            if (string.IsNullOrEmpty(path)) return;

            byte[] bytes = generatedTexture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);

            // 刷新Unity资源
            if (path.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
                string assetPath = "Assets" + path.Substring(Application.dataPath.Length);

                // 设置导入设置
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }

                EditorUtility.DisplayDialog("保存成功", $"图像已保存到: {assetPath}", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("保存成功", $"图像已保存到: {path}", "确定");
            }
        }

        private void CreateConfig()
        {
            string path = "Assets/Resources/AIImageConfig.asset";

            // 确保目录存在
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            config = ScriptableObject.CreateInstance<AIImageConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("创建成功", $"配置文件已创建: {path}", "确定");
        }
    }
}
#endif

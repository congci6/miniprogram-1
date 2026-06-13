using UnityEngine;
using System.Collections.Generic;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 建筑视觉调优工具
    /// 实时调整建筑外观参数
    /// </summary>
    public class BuildingVisualTuner : MonoBehaviour
    {
        [Header("变体参数调整")]
        [Range(0.8f, 1.2f)]
        [SerializeField] private float heightScaleMultiplier = 1.0f;

        [Range(0.8f, 1.2f)]
        [SerializeField] private float widthScaleMultiplier = 1.0f;

        [Header("颜色调整")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float colorBrightness = 1.0f;

        [Range(0.5f, 1.5f)]
        [SerializeField] private float colorSaturation = 1.0f;

        [Header("LOD距离")]
        [Range(20f, 80f)]
        [SerializeField] private float lodHighDistance = 40f;

        [Range(60f, 200f)]
        [SerializeField] private float lodMediumDistance = 120f;

        [Range(150f, 400f)]
        [SerializeField] private float lodLowDistance = 250f;

        [Range(200f, 600f)]
        [SerializeField] private float cullDistance = 400f;

        [Header("预设")]
        [SerializeField] private VisualPreset currentPreset = VisualPreset.Balanced;

        [Header("调试信息")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private int currentBuildingCount = 0;
        [SerializeField] private float currentAvgFPS = 0f;

        private CityMapRenderer mapRenderer;
        private float fpsTimer = 0f;
        private int frameCount = 0;
        private Dictionary<VisualPreset, PresetSettings> presets;

        public enum VisualPreset
        {
            Performance,  // 性能优先
            Balanced,     // 平衡
            Quality,      // 质量优先
            Custom        // 自定义
        }

        private struct PresetSettings
        {
            public float HeightScale;
            public float WidthScale;
            public float Brightness;
            public float Saturation;
            public float LODHigh;
            public float LODMedium;
            public float LODLow;
            public float Cull;
        }

        private void Start()
        {
            mapRenderer = GetComponent<CityMapRenderer>();
            InitializePresets();
            ApplyPreset(currentPreset);
        }

        private void InitializePresets()
        {
            presets = new Dictionary<VisualPreset, PresetSettings>
            {
                [VisualPreset.Performance] = new PresetSettings
                {
                    HeightScale = 1.0f,
                    WidthScale = 1.0f,
                    Brightness = 1.1f,
                    Saturation = 0.9f,
                    LODHigh = 25f,
                    LODMedium = 80f,
                    LODLow = 200f,
                    Cull = 300f
                },
                [VisualPreset.Balanced] = new PresetSettings
                {
                    HeightScale = 1.0f,
                    WidthScale = 1.0f,
                    Brightness = 1.0f,
                    Saturation = 1.0f,
                    LODHigh = 40f,
                    LODMedium = 120f,
                    LODLow = 250f,
                    Cull = 400f
                },
                [VisualPreset.Quality] = new PresetSettings
                {
                    HeightScale = 1.0f,
                    WidthScale = 1.0f,
                    Brightness = 1.0f,
                    Saturation = 1.05f,
                    LODHigh = 60f,
                    LODMedium = 160f,
                    LODLow = 350f,
                    Cull = 500f
                }
            };
        }

        public void ApplyPreset(VisualPreset preset)
        {
            if (preset == VisualPreset.Custom) return;

            if (presets.TryGetValue(preset, out var settings))
            {
                heightScaleMultiplier = settings.HeightScale;
                widthScaleMultiplier = settings.WidthScale;
                colorBrightness = settings.Brightness;
                colorSaturation = settings.Saturation;
                lodHighDistance = settings.LODHigh;
                lodMediumDistance = settings.LODMedium;
                lodLowDistance = settings.LODLow;
                cullDistance = settings.Cull;

                currentPreset = preset;
            }
        }

        private void Update()
        {
            // 更新FPS统计
            frameCount++;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= 1f)
            {
                currentAvgFPS = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 500));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 建筑视觉调优 ===", GUI.skin.box);
            GUILayout.Space(10);

            GUILayout.Label($"建筑数量: {currentBuildingCount}");
            GUILayout.Label($"平均FPS: {currentAvgFPS:F1}");

            // 性能评级
            string perfRating = GetPerformanceRating(currentAvgFPS);
            GUILayout.Label($"性能评级: {perfRating}");

            GUILayout.Space(10);

            // 预设选择
            GUILayout.Label("预设:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("性能")) ApplyPreset(VisualPreset.Performance);
            if (GUILayout.Button("平衡")) ApplyPreset(VisualPreset.Balanced);
            if (GUILayout.Button("质量")) ApplyPreset(VisualPreset.Quality);
            GUILayout.EndHorizontal();
            GUILayout.Label($"当前: {currentPreset}");

            GUILayout.Space(10);

            GUILayout.Label("变体参数:");
            GUILayout.Label($"高度倍率: {heightScaleMultiplier:F2}");
            GUILayout.Label($"宽度倍率: {widthScaleMultiplier:F2}");
            GUILayout.Space(5);

            GUILayout.Label("颜色:");
            GUILayout.Label($"亮度: {colorBrightness:F2}");
            GUILayout.Label($"饱和度: {colorSaturation:F2}");
            GUILayout.Space(5);

            GUILayout.Label("LOD距离:");
            GUILayout.Label($"高细节: {lodHighDistance:F0}m");
            GUILayout.Label($"中细节: {lodMediumDistance:F0}m");
            GUILayout.Label($"低细节: {lodLowDistance:F0}m");
            GUILayout.Label($"剔除: {cullDistance:F0}m");

            GUILayout.Space(10);

            // 缓存信息
            var cacheStats = ProceduralBuildingMeshGenerator.GetCacheStatistics();
            GUILayout.Label("网格缓存:");
            GUILayout.Label($"{cacheStats.CachedMeshCount}/{cacheStats.MaxCacheSize} ({cacheStats.CacheUsagePercent:F0}%)");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private string GetPerformanceRating(float fps)
        {
            if (fps >= 60) return "🌟 优秀";
            if (fps >= 45) return "✅ 良好";
            if (fps >= 30) return "⚠️ 合格";
            return "❌ 需优化";
        }

        // 获取当前LOD设置
        public LODSettings GetLODSettings()
        {
            return new LODSettings
            {
                HighDistance = lodHighDistance,
                MediumDistance = lodMediumDistance,
                LowDistance = lodLowDistance,
                CullDistance = cullDistance
            };
        }

        // 应用颜色调整到材质
        public Color AdjustColor(Color baseColor)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            s *= colorSaturation;
            v *= colorBrightness;
            s = Mathf.Clamp01(s);
            v = Mathf.Clamp01(v);
            return Color.HSVToRGB(h, s, v);
        }

        // 应用尺寸调整
        public Vector3 AdjustScale(Vector3 baseScale)
        {
            return new Vector3(
                baseScale.x * widthScaleMultiplier,
                baseScale.y * heightScaleMultiplier,
                baseScale.z * widthScaleMultiplier
            );
        }

        public void SetBuildingCount(int count)
        {
            currentBuildingCount = count;
        }

        public struct LODSettings
        {
            public float HighDistance;
            public float MediumDistance;
            public float LowDistance;
            public float CullDistance;
        }
    }
}

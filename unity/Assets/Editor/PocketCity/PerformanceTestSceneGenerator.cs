using UnityEngine;
using UnityEditor;
using PocketCity.Core;
using PocketCity.Simulation;

namespace PocketCity.Editor
{
    /// <summary>
    /// 性能测试场景生成器
    /// 创建50、200、500、1000建筑的压力测试场景
    /// </summary>
    public static class PerformanceTestSceneGenerator
    {
        [MenuItem("Pocket City/Performance Test/Generate 50 Buildings Scene")]
        public static void Generate50BuildingsScene()
        {
            GenerateTestScene(50, "PerformanceTest_50Buildings");
        }

        [MenuItem("Pocket City/Performance Test/Generate 200 Buildings Scene")]
        public static void Generate200BuildingsScene()
        {
            GenerateTestScene(200, "PerformanceTest_200Buildings");
        }

        [MenuItem("Pocket City/Performance Test/Generate 500 Buildings Scene")]
        public static void Generate500BuildingsScene()
        {
            GenerateTestScene(500, "PerformanceTest_500Buildings");
        }

        [MenuItem("Pocket City/Performance Test/Generate 1000 Buildings Scene")]
        public static void Generate1000BuildingsScene()
        {
            GenerateTestScene(1000, "PerformanceTest_1000Buildings");
        }

        private static void GenerateTestScene(int targetBuildings, string sceneName)
        {
            Debug.Log($"[性能测试] 开始生成 {targetBuildings} 建筑测试场景");

            var controller = Object.FindObjectOfType<Runtime.CityGameController>();
            if (controller == null)
            {
                Debug.LogError("未找到CityGameController，请先打开原型场景");
                return;
            }

            // 重置城市
            controller.ResetCity();

            var simulation = controller.Simulation;

            if (simulation == null)
            {
                Debug.LogError("无法获取模拟核心");
                return;
            }

            // 铺设道路网格
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(targetBuildings / 2));
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    var pos = new GridPos(x * 4, y * 4);
                    TryBuildRoad(simulation, pos, new GridPos(pos.X + 3, pos.Y));
                    TryBuildRoad(simulation, pos, new GridPos(pos.X, pos.Y + 3));
                }
            }

            // 建造建筑
            var buildingTypes = new[] {
                "residential_pod", "market_corner", "maker_yard",
                "pocket_park", "health_post", "primary_school"
            };

            int builtCount = 0;
            int typeIndex = 0;

            for (int x = 0; x < gridSize && builtCount < targetBuildings; x++)
            {
                for (int y = 0; y < gridSize && builtCount < targetBuildings; y++)
                {
                    var pos = new GridPos(x * 4 + 1, y * 4 + 1);
                    var buildingType = buildingTypes[typeIndex % buildingTypes.Length];

                    if (TryPlaceBuilding(simulation, buildingType, pos))
                    {
                        builtCount++;
                        typeIndex++;
                    }
                }
            }

            Debug.Log($"[性能测试] 场景生成完成: {builtCount}/{targetBuildings} 建筑");
            Debug.Log($"[性能测试] 提示：使用Unity Profiler记录性能数据");
        }

        private static void TryBuildRoad(CitySimulationCore simulation, GridPos from, GridPos to)
        {
            ConstructionPreview preview;
            simulation.TryBuildRoad(from, to, out preview);
        }

        private static bool TryPlaceBuilding(CitySimulationCore simulation, string buildingId, GridPos pos)
        {
            ConstructionPreview preview;
            return simulation.TryPlaceBuilding(buildingId, pos, out preview);
        }
    }

    /// <summary>
    /// 性能测试数据记录器
    /// </summary>
    public class PerformanceTestRecorder : MonoBehaviour
    {
        private float[] fpsSamples = new float[600]; // 10秒 @ 60fps
        private int sampleIndex = 0;
        private float recordStartTime;
        private bool isRecording = false;

        [MenuItem("Pocket City/Performance Test/Start Recording")]
        public static void StartRecording()
        {
            var recorder = Object.FindObjectOfType<PerformanceTestRecorder>();
            if (recorder == null)
            {
                var go = new GameObject("PerformanceRecorder");
                recorder = go.AddComponent<PerformanceTestRecorder>();
            }
            recorder.BeginRecording();
        }

        public void BeginRecording()
        {
            isRecording = true;
            recordStartTime = Time.time;
            sampleIndex = 0;
            Debug.Log("[性能测试] 开始记录 - 将记录10秒性能数据");
        }

        private void Update()
        {
            if (!isRecording) return;

            if (Time.time - recordStartTime > 10f)
            {
                isRecording = false;
                GenerateReport();
                return;
            }

            if (sampleIndex < fpsSamples.Length)
            {
                fpsSamples[sampleIndex++] = 1f / Time.deltaTime;
            }
        }

        private void GenerateReport()
        {
            float avgFps = 0f;
            float minFps = float.MaxValue;
            float maxFps = float.MinValue;

            for (int i = 0; i < sampleIndex; i++)
            {
                avgFps += fpsSamples[i];
                minFps = Mathf.Min(minFps, fpsSamples[i]);
                maxFps = Mathf.Max(maxFps, fpsSamples[i]);
            }
            avgFps /= sampleIndex;

            var controller = FindObjectOfType<Runtime.CityGameController>();
            var buildingCount = controller?.Buildings?.Count ?? 0;

            var report = $@"
=== 性能测试报告 ===
建筑数量: {buildingCount}
记录时长: 10秒
采样数量: {sampleIndex}

FPS统计:
- 平均: {avgFps:F1}
- 最小: {minFps:F1}
- 最大: {maxFps:F1}

下一步: 请使用Unity Profiler查看详细数据
- CPU Main Thread
- GC Alloc
- Draw Calls
- 三角形数
- AdvanceDay()耗时
";

            Debug.Log(report);
            System.IO.File.WriteAllText(
                $"performance_test_{buildingCount}buildings_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt",
                report
            );
        }
    }
}

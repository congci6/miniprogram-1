using UnityEngine;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace PocketCity.Runtime
{
    /// <summary>
    /// 建筑渲染性能基准测试工具
    /// 测量Draw Call、FPS、内存等指标
    /// </summary>
    public class BuildingRenderBenchmark : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private int testDurationSeconds = 10;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool saveToFile = false;

        private int frameCount = 0;
        private float elapsedTime = 0f;
        private int minFPS = int.MaxValue;
        private int maxFPS = 0;
        private float totalFPS = 0f;
        private bool isTesting = false;
        private List<int> fpsHistory = new List<int>();
        private long startMemory = 0;

        private void Start()
        {
            if (runOnStart)
            {
                StartBenchmark();
            }
        }

        private void Update()
        {
            if (!isTesting) return;

            frameCount++;
            elapsedTime += Time.unscaledDeltaTime;

            int currentFPS = Mathf.RoundToInt(1f / Time.unscaledDeltaTime);
            minFPS = Mathf.Min(minFPS, currentFPS);
            maxFPS = Mathf.Max(maxFPS, currentFPS);
            totalFPS += currentFPS;
            fpsHistory.Add(currentFPS);

            if (elapsedTime >= testDurationSeconds)
            {
                CompleteBenchmark();
            }
        }

        public void StartBenchmark()
        {
            frameCount = 0;
            elapsedTime = 0f;
            minFPS = int.MaxValue;
            maxFPS = 0;
            totalFPS = 0f;
            fpsHistory.Clear();
            isTesting = true;
            startMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();

            UnityEngine.Debug.Log("=== 建筑渲染性能测试开始 ===");
        }

        private void CompleteBenchmark()
        {
            isTesting = false;

            var avgFPS = totalFPS / frameCount;
            var endMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            var memoryDelta = (endMemory - startMemory) / 1024f / 1024f;

            var report = GenerateReport(avgFPS, memoryDelta);

            if (logToConsole)
            {
                UnityEngine.Debug.Log(report);
            }

            if (saveToFile)
            {
                SaveReportToFile(report);
            }
        }

        private string GenerateReport(float avgFPS, float memoryDelta)
        {
            var report = new StringBuilder();

            report.AppendLine("\n=== 建筑渲染性能测试报告 ===");
            report.AppendLine($"测试时长: {testDurationSeconds}秒");
            report.AppendLine($"总帧数: {frameCount}");
            report.AppendLine($"\n--- 帧率 (FPS) ---");
            report.AppendLine($"平均FPS: {avgFPS:F1}");
            report.AppendLine($"最低FPS: {minFPS}");
            report.AppendLine($"最高FPS: {maxFPS}");
            report.AppendLine($"FPS标准差: {CalculateStdDev(fpsHistory):F1}");
            report.AppendLine($"1% Low FPS: {CalculatePercentileFPS(0.01f):F0}");
            report.AppendLine($"0.1% Low FPS: {CalculatePercentileFPS(0.001f):F0}");

            report.AppendLine($"\n--- 内存 ---");
            report.AppendLine($"总内存: {(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f):F2} MB");
            report.AppendLine($"已用内存: {(UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1024f / 1024f):F2} MB");
            report.AppendLine($"测试期间增长: {memoryDelta:F2} MB");

            report.AppendLine($"\n--- 性能评级 ---");
            string rating = GetPerformanceRating(avgFPS);
            report.AppendLine($"评级: {rating}");

            report.AppendLine($"\n--- 建议 ---");
            AppendRecommendations(report, avgFPS, memoryDelta);

            report.AppendLine("\n=========================");

            return report.ToString();
        }

        private float CalculateStdDev(List<int> values)
        {
            if (values.Count == 0) return 0f;

            float mean = values.Count > 0 ? (float)values.Average() : 0f;
            float sumOfSquares = values.Sum(v => (v - mean) * (v - mean));
            return Mathf.Sqrt(sumOfSquares / values.Count);
        }

        private float CalculatePercentileFPS(float percentile)
        {
            if (fpsHistory.Count == 0) return 0f;

            var sorted = new List<int>(fpsHistory);
            sorted.Sort();

            int index = Mathf.Max(0, Mathf.FloorToInt(sorted.Count * percentile));
            return sorted[index];
        }

        private string GetPerformanceRating(float avgFPS)
        {
            if (avgFPS >= 60) return "🌟 优秀 (Excellent)";
            if (avgFPS >= 45) return "✅ 良好 (Good)";
            if (avgFPS >= 30) return "⚠️ 合格 (Acceptable)";
            return "❌ 需优化 (Needs Optimization)";
        }

        private void AppendRecommendations(StringBuilder report, float avgFPS, float memoryDelta)
        {
            if (avgFPS < 30)
            {
                report.AppendLine("⚠️ FPS过低，建议:");
                report.AppendLine("  - 启用批处理系统");
                report.AppendLine("  - 降低LOD距离");
                report.AppendLine("  - 减少建筑细节");
                report.AppendLine("  - 检查Draw Call数量");
            }
            else if (avgFPS < 45)
            {
                report.AppendLine("⚠️ 性能可提升:");
                report.AppendLine("  - 考虑启用批处理");
                report.AppendLine("  - 优化LOD切换距离");
            }
            else
            {
                report.AppendLine("✅ 性能良好！");
            }

            if (memoryDelta > 10f)
            {
                report.AppendLine($"\n⚠️ 内存增长较大 (+{memoryDelta:F1}MB):");
                report.AppendLine("  - 检查网格缓存大小");
                report.AppendLine("  - 考虑定期清理缓存");
            }
        }

        private void SaveReportToFile(string report)
        {
            var fileName = $"BenchmarkReport_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = System.IO.Path.Combine(Application.persistentDataPath, fileName);

            try
            {
                System.IO.File.WriteAllText(path, report);
                UnityEngine.Debug.Log($"报告已保存到: {path}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"保存报告失败: {e.Message}");
            }
        }

        // 手动触发测试的公共接口
        public void RunBenchmark(int durationSeconds = 10)
        {
            testDurationSeconds = durationSeconds;
            StartBenchmark();
        }

        // 获取性能快照
        public PerformanceSnapshot GetSnapshot()
        {
            return new PerformanceSnapshot
            {
                FPS = Mathf.RoundToInt(1f / Time.unscaledDeltaTime),
                TotalMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f,
                UsedMemoryMB = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / 1024f / 1024f
            };
        }

        public struct PerformanceSnapshot
        {
            public int FPS;
            public float TotalMemoryMB;
            public float UsedMemoryMB;
        }
    }
}

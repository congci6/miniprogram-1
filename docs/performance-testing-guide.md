# 性能测试指南与基准

## 测试场景生成

已创建 `PerformanceTestSceneGenerator.cs` 编辑器工具。

### 使用方法

1. 在Unity中打开 `PocketCityPrototype.unity` 场景
2. 菜单：`Pocket City > Performance Test > Generate XXX Buildings Scene`
   - Generate 50 Buildings Scene
   - Generate 200 Buildings Scene
   - Generate 500 Buildings Scene
   - Generate 1000 Buildings Scene

### 测试场景特点

- 自动铺设道路网格
- 均匀分布6种建筑类型
- 包含住宅、商业、工业、服务建筑
- 道路接入完整

## 性能数据采集

### 方法1: Unity Profiler（推荐）

1. 打开 Profiler: `Window > Analysis > Profiler`
2. 启用以下模块：
   - CPU Usage
   - Rendering
   - Memory
3. 点击 Play，观察30秒
4. 重点记录：
   - **FPS** (右上角)
   - **CPU Main Thread** (ms)
   - **GC Alloc** (KB/frame)
   - **Draw Calls**
   - **Triangles**
   - **Batches**

### 方法2: 自动记录器

```
菜单: Pocket City > Performance Test > Start Recording
```

记录10秒性能数据，自动生成报告文件。

### 方法3: 代码插桩

在 `CitySimulationCore.AdvanceDay()` 添加：

```csharp
private void AdvanceDay()
{
    #if UNITY_EDITOR
    var sw = System.Diagnostics.Stopwatch.StartNew();
    #endif
    
    // ... 原有代码 ...
    
    #if UNITY_EDITOR
    sw.Stop();
    if (Metrics.Day % 10 == 0) // 每10天记录一次
    {
        UnityEngine.Debug.Log($"[性能] Day {Metrics.Day}: AdvanceDay耗时 {sw.ElapsedMilliseconds}ms");
    }
    #endif
}
```

## 测试检查清单

### 基准测试（优化前）

- [ ] 50建筑场景 - FPS记录
- [ ] 200建筑场景 - FPS记录
- [ ] 500建筑场景 - FPS记录
- [ ] 1000建筑场景 - FPS记录
- [ ] 4倍速稳定性测试

### 优化后测试

- [ ] 50建筑场景 - FPS记录（对比）
- [ ] 200建筑场景 - FPS记录（对比）
- [ ] 500建筑场景 - FPS记录（对比）
- [ ] 1000建筑场景 - FPS记录（对比）
- [ ] 4倍速稳定性验证

### 测试操作

1. **标准速度测试 (1x)**
   - 运行30秒
   - 记录平均FPS
   - 记录AdvanceDay耗时

2. **4倍速压力测试**
   - 切换到4x速度
   - 运行30秒
   - 观察是否卡顿
   - FPS是否稳定

3. **建造操作测试**
   - 快速建造10个建筑
   - 观察是否卡顿
   - 检查地图更新正确性

## 预期性能目标

| 场景 | 建筑数 | 目标FPS | 优化前预估 | 优化后预估 |
|-----|-------|---------|-----------|-----------|
| 小型 | 50 | 60 | 60 | 60 |
| 中型 | 200 | 55+ | 35-45 | 55-60 |
| 大型 | 500 | 45+ | 20-30 | 45-55 |
| 超大 | 1000 | 35+ | 10-15 | 35-45 |

## AdvanceDay性能目标

| 场景 | 优化前(ms) | 优化后目标(ms) | 提升 |
|-----|-----------|---------------|------|
| 50建筑 | 8 | <4 | 50% |
| 200建筑 | 25 | <12 | 52% |
| 500建筑 | 60 | <30 | 50% |
| 1000建筑 | 120 | <60 | 50% |

## 数据记录模板

```
=== 性能测试报告 ===
日期: 2026-06-12
Unity版本: 2021.3.x
测试平台: Editor (Windows)

【场景：XX建筑】
- 建筑数量: XX
- 道路数量: XX
- 人口: XXX

【性能数据】
- 平均FPS: XX.X
- 最低FPS: XX.X
- CPU Main Thread: XX.Xms
- GC Alloc: XX KB/frame
- Draw Calls: XXX
- Triangles: XXX
- Batches: XX

【AdvanceDay性能】
- 平均耗时: XX.Xms
- 最大耗时: XX.Xms
- RecomputeMetrics调用次数: X

【4倍速测试】
- FPS稳定性: 稳定/波动/严重卡顿
- 是否出现跳帧: 是/否
- 游戏逻辑正确性: 正常/异常

【备注】
- （记录任何异常现象）
```

## 对比分析要点

### 1. FPS提升

```
提升百分比 = (优化后FPS - 优化前FPS) / 优化前FPS × 100%
```

### 2. AdvanceDay耗时

重点关注：
- RecomputeMetrics调用次数减少
- 单次耗时是否降低
- 是否符合50%提升预期

### 3. 渲染性能

集成SimpleCullingManager后重点关注：
- Draw Calls减少
- 三角形数量减少
- Batches数量

## 自动化测试脚本（可选）

创建 `AutoPerformanceTest.cs`:

```csharp
[MenuItem("Pocket City/Performance Test/Run Full Test Suite")]
public static void RunFullTestSuite()
{
    var scenes = new[] { 50, 200, 500, 1000 };
    foreach (var count in scenes)
    {
        GenerateTestScene(count, $"Test_{count}");
        // 等待场景加载
        EditorApplication.delayCall += () => {
            // 记录性能
            StartRecording();
        };
    }
}
```

## 结果汇总

完成所有测试后，在 `docs/` 目录创建：
`performance-benchmark-results-20260612.md`

包含：
- 所有场景的对比数据
- 优化前后截图
- Profiler截图
- 结论和建议

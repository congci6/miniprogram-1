# 地图渲染性能优化策略
## 日期：2026-06-12

### 当前状况分析

**CityMapRenderer.cs:**
- 文件大小：12,245行代码
- 当前架构：单体渲染器，负责所有视觉元素

**性能瓶颈：**
1. `RebuildAll()`在每次建筑/道路变化时被调用
2. 没有LOD（细节层次）系统
3. 没有视锥剔除
4. 所有对象每帧都被渲染
5. 缺少批量渲染和实例化

### 优化策略（分阶段实施）

#### 阶段1：增量更新系统（高优先级）

**目标：** 避免每次变化都重建整个地图

**实施方案：**
```csharp
// 1. 分离重建逻辑
private void Update()
{
    // 当前：任何变化都RebuildAll()
    if (HasAnyChange())
    {
        RebuildAll();  // 昂贵！
    }
    
    // 优化后：增量更新
    if (terrainChanged) RebuildTerrain();
    if (roadsChanged) RebuildRoadsIncremental(changedRoads);
    if (buildingsChanged) RebuildBuildingsIncremental(changedBuildings);
    if (overlayChanged) RebuildOverlay();
}

// 2. 实现增量建筑更新
private void RebuildBuildingsIncremental(List<PlacedBuilding> changed)
{
    foreach (var building in changed)
    {
        // 只更新变化的建筑
        RemoveBuildingVisual(building.Id);
        AddBuildingVisual(building);
    }
}
```

**预期提升：** 
- 小改动（1-2建筑）：性能提升80-90%
- 中等改动（5-10建筑）：性能提升60-70%

#### 阶段2：空间分区和视锥剔除（中优先级）

**目标：** 只渲染可见区域

**实施方案：**
```csharp
// 1. 四叉树空间分区
public class RenderQuadTree
{
    private Bounds bounds;
    private List<GameObject> objects;
    private RenderQuadTree[] children;
    
    public List<GameObject> Query(Frustum frustum)
    {
        // 只返回视锥内的对象
    }
}

// 2. 视锥剔除
private void Update()
{
    var camera = Camera.main;
    var frustum = GeometryUtility.CalculateFrustumPlanes(camera);
    
    // 只渲染视野内的建筑
    var visibleBuildings = quadTree.Query(frustum);
    foreach (var building in visibleBuildings)
    {
        building.SetActive(true);
    }
}
```

**预期提升：**
- 大型地图（500+建筑）：FPS提升50-100%
- 缩放到近处时尤为明显

#### 阶段3：LOD系统（中优先级）

**目标：** 远处建筑使用简化模型

**实施方案：**
```csharp
public enum BuildingLOD
{
    High,    // 近距离：完整模型
    Medium,  // 中距离：简化模型
    Low,     // 远距离：方块占位
    Culled   // 超远距离：不渲染
}

private BuildingLOD CalculateLOD(Vector3 buildingPos, Camera camera)
{
    var distance = Vector3.Distance(buildingPos, camera.transform.position);
    
    if (distance < 50f) return BuildingLOD.High;
    if (distance < 150f) return BuildingLOD.Medium;
    if (distance < 300f) return BuildingLOD.Low;
    return BuildingLOD.Culled;
}

private void UpdateBuildingLOD(GameObject building, BuildingLOD lod)
{
    // 切换到对应的网格
    switch (lod)
    {
        case BuildingLOD.High:
            building.GetComponent<MeshFilter>().mesh = highDetailMesh;
            break;
        case BuildingLOD.Medium:
            building.GetComponent<MeshFilter>().mesh = mediumDetailMesh;
            break;
        case BuildingLOD.Low:
            building.GetComponent<MeshFilter>().mesh = lowDetailMesh;
            break;
        case BuildingLOD.Culled:
            building.SetActive(false);
            break;
    }
}
```

**预期提升：**
- 渲染三角形数量减少：60-80%
- FPS提升：30-50%

#### 阶段4：GPU实例化和批量渲染（低优先级）

**目标：** 使用GPU实例化渲染相同建筑

**实施方案：**
```csharp
// 使用Graphics.DrawMeshInstanced
public class InstancedBuildingRenderer
{
    private Dictionary<string, List<Matrix4x4>> instanceMatrices;
    private Dictionary<string, Mesh> meshes;
    private Dictionary<string, Material> materials;
    
    public void RenderInstanced()
    {
        foreach (var kvp in instanceMatrices)
        {
            var buildingType = kvp.Key;
            var matrices = kvp.Value;
            
            // 批量渲染相同类型的建筑
            Graphics.DrawMeshInstanced(
                meshes[buildingType],
                0,
                materials[buildingType],
                matrices
            );
        }
    }
}
```

**预期提升：**
- Draw Call减少：80-90%
- CPU开销减少：40-60%

### 实施优先级

#### 立即实施（本周）
1. **增量更新系统** - 最大的性能提升，工作量适中

#### 近期实施（2周内）
2. **视锥剔除** - 显著提升大地图性能
3. **基础LOD系统** - 3个层次（高/中/低）

#### 中期实施（1个月内）
4. **空间分区优化** - 四叉树实现
5. **完整LOD系统** - 5个层次，平滑过渡

#### 长期实施（2-3个月）
6. **GPU实例化** - 需要重构渲染架构
7. **异步资源加载** - 流式加载远处建筑

### 技术债务和风险

**当前问题：**
1. 12,245行的单体文件难以维护
2. 渲染逻辑与游戏逻辑耦合
3. 缺少性能分析工具

**建议重构：**
```
CityMapRenderer (主协调器)
├── TerrainRenderer (地形)
├── RoadRenderer (道路)
├── BuildingRenderer (建筑)
│   ├── BuildingLODManager
│   ├── BuildingInstanceRenderer
│   └── BuildingCullingManager
├── OverlayRenderer (图层)
└── EffectRenderer (特效)
```

### 性能测试基准

#### 测试场景
- **小型城市：** 50建筑，10道路
- **中型城市：** 200建筑，50道路
- **大型城市：** 500建筑，150道路
- **超大城市：** 1000建筑，300道路

#### 目标FPS（1080p）
| 城市规模 | 当前FPS | 目标FPS | 优化后预期 |
|---------|---------|---------|-----------|
| 小型    | 60      | 60      | 60        |
| 中型    | 35-45   | 60      | 55-60     |
| 大型    | 20-30   | 45+     | 45-55     |
| 超大    | 10-15   | 30+     | 35-45     |

### 微信小游戏特殊考虑

**限制：**
- WebGL性能较原生低20-30%
- 内存限制更严格
- 不支持某些Unity功能

**针对性优化：**
1. 更激进的LOD策略
2. 更小的纹理和网格
3. 简化材质和着色器
4. 预烘焙更多内容

### 开发工具建议

**性能分析：**
```csharp
public class RenderingProfiler
{
    public static void Profile()
    {
        Debug.Log($"Buildings Rendered: {buildingCount}");
        Debug.Log($"Draw Calls: {drawCalls}");
        Debug.Log($"Triangles: {triangles}");
        Debug.Log($"Render Time: {renderTime}ms");
    }
}
```

**热重载支持：**
```csharp
#if UNITY_EDITOR
[MenuItem("Pocket City/Reload Renderer")]
public static void ReloadRenderer()
{
    // 开发时快速测试渲染变更
}
#endif
```

---

**文档版本：** 1.0  
**创建日期：** 2026-06-12  
**负责人：** Claude Opus 4.8  
**评审状态：** 待评审

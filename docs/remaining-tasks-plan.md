# 待优化任务规划

## 当前状态

### ✅ 已完成（2/4）
1. **优化CitySimulationCore性能** - 完成 ✅
2. **优化地图渲染性能** - 完成 ✅

### 📋 待完成（2/4）
3. **增强顾问系统智能度** - 待实施
4. **改进建筑程序化生成** - 待实施

---

## 任务 #3: 增强顾问系统智能度

### 当前状况
项目已实现11个顾问系统：
- `RISK_FORECAST_ADVISOR` - 风险预测
- `BUDGET_BREAKDOWN_ADVISOR` - 预算拆解
- `DISTRICT_PRIORITY_ADVISOR` - 片区优先级
- `ROAD_HIERARCHY_ADVISOR` - 道路层级
- `COMMUTE_CORRIDOR_ADVISOR` - 通勤走廊
- `SERVICE_GAP_ADVISOR` - 服务短板
- `ECONOMIC_SPECIALIZATION_ADVISOR` - 经济专精
- `GROWTH_BOTTLENECK_ADVISOR` - 成长瓶颈
- `HOUSING_AFFORDABILITY_ADVISOR` - 住房负担
- `BUILDING_UPGRADE_READINESS_ADVISOR` - 建筑升级准备度
- `DEMAND_DRIVER_ANALYSIS` - 需求驱动分析

### 优化目标
1. **智能优先级排序** - 根据城市当前状况动态调整顾问建议优先级
2. **上下文感知** - 考虑玩家最近操作和游戏进度
3. **减少信息过载** - 洞察优先栈(ObjectiveInsightParts)更智能展示
4. **提高建议质量** - 更具体、可操作的建议

### 实施建议

#### 3.1 创建智能优先级评分系统
```csharp
public class AdvisorPriorityScorer
{
    // 评分因子
    private float urgencyWeight = 0.4f;      // 紧急程度
    private float impactWeight = 0.3f;       // 影响范围
    private float actionabilityWeight = 0.2f; // 可操作性
    private float noveltyWeight = 0.1f;      // 新鲜度（避免重复）

    public float CalculatePriority(AdvisorInsight insight, CityMetrics metrics)
    {
        var urgency = CalculateUrgency(insight, metrics);
        var impact = CalculateImpact(insight, metrics);
        var actionability = CalculateActionability(insight, metrics);
        var novelty = CalculateNovelty(insight);
        
        return urgency * urgencyWeight 
             + impact * impactWeight 
             + actionability * actionabilityWeight 
             + novelty * noveltyWeight;
    }
}
```

#### 3.2 实现上下文感知系统
```csharp
public class AdvisorContextTracker
{
    private Queue<string> recentPlayerActions;
    private Dictionary<string, float> advisorLastShownTime;
    
    // 根据玩家最近行为调整建议
    public bool ShouldShowAdvisor(string advisorType, CityMetrics metrics)
    {
        // 例如：刚建造了学校，提高教育相关顾问优先级
        if (recentPlayerActions.Contains("build_school"))
        {
            if (advisorType == "SERVICE_GAP_ADVISOR" || 
                advisorType == "EDUCATION_CAPACITY_ADVISOR")
            {
                return true;
            }
        }
        
        // 避免频繁显示同一顾问
        var timeSinceLastShown = Time.time - advisorLastShownTime[advisorType];
        return timeSinceLastShown > MinDisplayInterval;
    }
}
```

#### 3.3 改进建议文案生成
```csharp
public class SmartAdvisorTextGenerator
{
    // 生成更具体的建议
    public string GenerateActionableAdvice(AdvisorType type, CityMetrics metrics)
    {
        switch (type)
        {
            case AdvisorType.ServiceGap:
                // 从 "补充服务覆盖" 改进为 "北部片区缺少诊所，建议在(15,20)附近建造"
                var gap = FindLargestServiceGap(metrics);
                return $"{gap.District}片区缺少{gap.ServiceType}，建议在{gap.BestLocation}附近建造";
                
            case AdvisorType.RoadHierarchy:
                // 从 "升级主干道" 改进为 "东西向主路拥堵严重，建议升级(10,5)-(20,5)路段"
                var bottleneck = FindWorstBottleneck(metrics);
                return $"{bottleneck.Direction}向主路拥堵严重，建议升级{bottleneck.Route}路段";
        }
    }
}
```

### 工作量估算
- **时间：** 3-5天
- **复杂度：** 中等
- **影响范围：** `CitySimulationCore.cs`, `CityHudViewModel.cs`
- **文件数：** 2-3个新文件，2-3个修改文件

---

## 任务 #4: 改进建筑程序化生成

### 当前状况
- `BUILDING_VISUAL_PREFAB_LIBRARY` 已实现基础程序化外观
- 38种建筑类型都有fallback
- 使用低多边形程序外观

### 优化目标
1. **增加视觉变体** - 同类建筑有不同外观
2. **更多细节层次** - 窗户、阳台、屋顶装饰
3. **程序化纹理** - 动态生成建筑材质
4. **性能优化** - 网格合并和实例化

### 实施建议

#### 4.1 建筑变体系统
```csharp
public class ProceduralBuildingVariant
{
    // 为同类建筑生成多个变体
    public Mesh GenerateVariant(BuildingType type, int seed)
    {
        Random.InitState(seed);
        
        // 变体参数
        var height = BaseHeight(type) * Random.Range(0.9f, 1.1f);
        var width = BaseWidth(type) * Random.Range(0.95f, 1.05f);
        var roofType = Random.Range(0, 3); // 平顶、尖顶、圆顶
        var windowPattern = Random.Range(0, 5);
        
        return BuildMesh(height, width, roofType, windowPattern);
    }
}
```

#### 4.2 细节分层系统
```csharp
public class BuildingDetailLayers
{
    // LOD 0: 完整细节
    private Mesh HighDetail(BuildingConfig config)
    {
        var mesh = new Mesh();
        AddWalls(mesh);
        AddWindows(mesh, 4); // 4层窗户
        AddBalconies(mesh);
        AddRoofDetails(mesh);
        AddDoorway(mesh);
        return mesh;
    }
    
    // LOD 1: 中等细节
    private Mesh MediumDetail(BuildingConfig config)
    {
        var mesh = new Mesh();
        AddWalls(mesh);
        AddWindows(mesh, 2); // 2层窗户
        AddSimpleRoof(mesh);
        return mesh;
    }
    
    // LOD 2: 低细节
    private Mesh LowDetail(BuildingConfig config)
    {
        // 简单方块
        return CreateCube(config.Size);
    }
}
```

#### 4.3 程序化材质生成
```csharp
public class ProceduralBuildingMaterial
{
    // 动态生成建筑材质
    public Material GenerateMaterial(BuildingType type, int seed)
    {
        var mat = new Material(baseShader);
        
        // 根据类型选择颜色范围
        var colorRange = GetColorRange(type);
        mat.color = Random.ColorHSV(
            colorRange.hueMin, colorRange.hueMax,
            colorRange.satMin, colorRange.satMax,
            colorRange.valMin, colorRange.valMax
        );
        
        // 生成程序化纹理
        var texture = GenerateProceduralTexture(type, seed);
        mat.mainTexture = texture;
        
        return mat;
    }
    
    private Texture2D GenerateProceduralTexture(BuildingType type, int seed)
{
        // 生成砖块、窗户等纹理
        var tex = new Texture2D(256, 256);
        // ... 程序化纹理生成逻辑
        return tex;
    }
}
```

#### 4.4 性能优化：网格批处理
```csharp
public class BuildingBatcher
{
    // 合并相同材质的建筑网格
    public void BatchBuildings()
    {
        var buildingsByMaterial = GroupBuildingsByMaterial();
        
        foreach (var group in buildingsByMaterial)
        {
            var combines = new CombineInstance[group.Count];
            for (int i = 0; i < group.Count; i++)
            {
                combines[i].mesh = group[i].mesh;
                combines[i].transform = group[i].transform.localToWorldMatrix;
            }
            
            var batched = new Mesh();
            batched.CombineMeshes(combines);
            
            // 创建批处理对象
            CreateBatchedObject(batched, group.material);
        }
    }
}
```

### 工作量估算
- **时间：** 5-7天
- **复杂度：** 中高
- **影响范围：** 新建多个生成器类，修改渲染器集成
- **文件数：** 5-6个新文件，1-2个修改文件

---

## 推荐实施顺序

### 优先级评估

| 任务 | 用户体验提升 | 技术难度 | 工作量 | 推荐顺序 |
|-----|----------|---------|--------|---------|
| #3 顾问系统 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | 中 | **1** |
| #4 建筑生成 | ⭐⭐⭐ | ⭐⭐⭐⭐ | 中高 | **2** |

### 建议时间表

**本周（6月12-18日）**
- 任务#3: 增强顾问系统智能度
  - 实现智能优先级评分
  - 添加上下文感知
  - 改进建议文案

**下周（6月19-25日）**
- 任务#4: 改进建筑程序化生成
  - 建筑变体系统
  - 细节分层
  - 程序化材质

---

## 后续优化方向

完成当前4个任务后，可以考虑：

1. **音效系统** - 环境音效、UI反馈音
2. **动画系统** - 建筑建造动画、车流动画
3. **UI/UX优化** - 改进HUD布局和交互
4. **AI玩家** - 自动城市规划AI
5. **多人模式** - 城市对比和竞赛
6. **教程系统** - 新手引导和提示

---

**文档创建时间：** 2026-06-12  
**更新频率：** 每完成一个任务后更新  
**负责人：** 开发团队

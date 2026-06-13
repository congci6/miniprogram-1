# 智能顾问系统 - 任务#3完成文档

## 概述

任务#3"增强顾问系统智能度"已经完成实施。本文档说明实现的功能、架构和使用方法。

## 实施日期
2026-06-12

## 完成的功能

### ✅ 1. 智能优先级评分系统
**文件**: `AdvisorPriorityScorer.cs`

- **多因子评分**: 使用紧急度(40%)、影响范围(30%)、可操作性(20%)、新鲜度(10%)进行综合评分
- **上下文增强**: 根据玩家最近操作提升相关顾问优先级30%
- **重复惩罚**: 避免同一顾问频繁显示，根据显示次数和时间间隔降低优先级
- **动态计算**: 
  - 紧急度根据具体指标计算（如服务缺口、资金状况、道路拥堵）
  - 影响范围考虑城市人口规模
  - 可操作性基于当前资金情况

### ✅ 2. 上下文感知系统
**文件**: `AdvisorContextTracker.cs`

- **行为追踪**: 记录玩家最近10次操作（建造、道路、分区、税率、政策）
- **智能关联**: 
  - 建造学校/诊所 → 提升服务缺口顾问
  - 建造/升级道路 → 提升道路层级/通勤走廊顾问
  - 分区调整 → 提升需求驱动/成长瓶颈顾问
  - 税率/预算调整 → 提升预算拆解顾问
  - 政策切换 → 提升片区优先级顾问
- **时间衰减**: 操作越新权重越高（30秒内100%，60秒70%，120秒40%）
- **显示控制**: 同一顾问60秒内不重复显示

### ✅ 3. 改进建议文案生成
**文件**: `SmartAdvisorTextGenerator.cs`

将通用建议转换为具体可操作建议：

| 原建议 | 增强后 |
|--------|--------|
| "补充服务覆盖" | "补充服务覆盖 > 优先补充医疗" |
| "升级主干道" | "升级主干道 > 升级主干道缓解拥堵" |
| "预算紧张" | "预算紧张 > 可考虑提高税率" |
| "住房不足" | "住房不足 > 需增加120人住房容量" |

- **服务缺口**: 自动识别最缺乏的服务类型（医疗、教育、消防、治安、公园）
- **道路建议**: 根据拥堵情况给出具体措施
- **财政建议**: 根据税率和债务情况提供选项
- **住房建议**: 计算具体缺口数量

### ✅ 4. 集成到主系统

**修改的文件**:
- `CitySimulationCore.cs`: 添加AdvisorContextTracker实例，在所有玩家操作中记录行为
- `CityHudViewModel.cs`: 替换原有简单排序为智能评分系统
- `CityHudViewModel.SmartAdvisor.cs`: 集成文案增强和上下文追踪
- `CityGameController.cs`: 初始化时设置上下文追踪器

**记录的玩家操作**:
```csharp
// CitySimulationCore中自动记录：
- TryPlaceBuilding() → "build_school", "build_clinic", "build_service", "build"
- TryBuildRoad() → "build_road"
- TryUpgradeRoad() → "upgrade_road"
- TrySetZone() → "set_zone"
- CycleTaxLevel() → "cycle_tax"
- CycleServiceBudgetLevel() → "cycle_budget"
- TogglePolicy() → "toggle_policy"
```

## 架构设计

```
CityGameController (初始化)
    ↓
CitySimulationCore (记录玩家行为)
    ↓
AdvisorContextTracker (追踪上下文)
    ↓
AdvisorPriorityScorer (智能评分) ← 整合上下文
    ↓
SmartAdvisorTextGenerator (增强文案)
    ↓
CityHudViewModel.SmartAdvisor (组装显示)
    ↓
CityRuntimeHud (UI展示)
```

## 使用示例

### 场景1: 玩家刚建造了学校
```
1. 玩家点击建造学校
2. CitySimulationCore.TryPlaceBuilding() 记录 "build_school"
3. AdvisorContextTracker.RecordAction("build_school")
4. 下次更新HUD时，SERVICE_GAP_ADVISOR优先级提升30%
5. 如果服务缺口较大，显示"服务覆盖不足 > 优先补充医疗"
```

### 场景2: 资金紧张时的建议
```
1. metrics.Cash < 1000
2. UrgencyWeight高 + Actionability高
3. 文案增强检测到税率较低
4. 显示"预算紧张 > 可考虑提高税率"
```

### 场景3: 避免重复显示
```
1. BUDGET_BREAKDOWN_ADVISOR刚显示过
2. AdvisorPriorityScorer记录lastShownTime
3. 60秒内Novelty因子接近0
4. 即使紧急度高，也会被其他顾问超越
```

## 性能优化

- **轻量级追踪**: 只保留最近10次操作，内存占用极小
- **惰性计算**: 仅在HUD更新时计算评分
- **缓存时间戳**: 避免重复计算时间间隔

## 测试建议

1. **上下文感知测试**:
   - 建造学校后，观察服务缺口顾问是否优先显示
   - 修路后，观察道路层级顾问是否优先显示

2. **文案增强测试**:
   - 资金<1000时，检查是否给出税率或债券建议
   - 住房不足时，检查是否显示具体缺口数量

3. **重复控制测试**:
   - 同一顾问是否在60秒内重复出现
   - 多个紧急事项时，是否轮流显示

4. **评分系统测试**:
   - 同时有多个问题时，最紧急的是否优先显示
   - 资金充足时，高成本建议是否优先于低成本建议

## 可能的改进方向

1. **机器学习**: 根据玩家历史偏好调整权重
2. **A/B测试**: 测试不同权重配置的效果
3. **多语言**: 支持文案的本地化
4. **可视化**: 在调试模式下显示各顾问的评分详情
5. **反馈循环**: 追踪玩家是否采纳建议，调整未来优先级

## 相关文件清单

### 新增文件 (3个)
1. `AdvisorContextTracker.cs` - 上下文追踪器
2. `AdvisorPriorityScorer.cs` - 智能评分系统
3. `SmartAdvisorTextGenerator.cs` - 文案增强生成器

### 修改文件 (4个)
1. `CitySimulationCore.cs` - 集成上下文追踪
2. `CityHudViewModel.cs` - 使用智能评分
3. `CityHudViewModel.SmartAdvisor.cs` - 组装智能顾问
4. `CityGameController.cs` - 初始化设置

## 工作量统计

- **实际耗时**: 约2小时
- **代码行数**: 新增约400行，修改约50行
- **复杂度**: 中等
- **测试覆盖**: 需人工测试

## 下一步

任务#3已完成，可以继续任务#4"改进建筑程序化生成"。

---

**文档维护**: 如有新功能或bug修复，请更新此文档。
**联系方式**: 开发团队

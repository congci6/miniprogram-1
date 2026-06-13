# Unity UI 与美术方向

## 设计目标
小游戏第一屏应直接进入可玩的城市规划界面，而不是落地页。整体气质偏清晰、轻量、规划感，避免过重的写实城市和复杂菜单。

## 首屏布局
- 主视图：低多边形等距城市地图，占据大部分屏幕。
- 顶栏：人口、现金、幸福度、净收入、财政信用/行政效率/债务压力/债券本金、日期。
- 左侧工具栏：道路、道路升级、分区、建筑、服务、拆除、图层。
- 右侧检查器：当前目标、`OBJECTIVE_ACTION_ADVICE` “建议：...”行动提示、`ALERT_PRIORITY_DIGEST` 警报摘要、`RISK_FORECAST_ADVISOR` 风险预测文案、`BUDGET_BREAKDOWN_ADVISOR` 预算拆解/财政顾问文案、`DISTRICT_PRIORITY_ADVISOR` 片区/系统优先级顾问文案、`ROAD_HIERARCHY_ADVISOR` 道路层级/瓶颈升级顾问文案、`COMMUTE_CORRIDOR_ADVISOR` 通勤走廊顾问文案、`HOUSING_AFFORDABILITY_ADVISOR` 住房负担/宜居迁入顾问文案、`ECONOMIC_SPECIALIZATION_ADVISOR` 经济专精顾问文案、`SERVICE_GAP_ADVISOR` 服务短板建议、`CITY_EVENT_DIGEST` 事件摘要、`DEMAND_DRIVER_ANALYSIS` 需求洞察、工具按钮、建筑“选址诊断”、分区适宜度、缓冲风险、优质片区目标、暂停/倍速、税率、服务预算、存读档和确认反馈。
- 政策效果反馈复用右侧检查器：`PolicyImpactPreview` 用紧凑 delta 列表显示本次政策切换的启用/关闭，以及月收支、拥堵、停车压力、步行可达性、事故风险、雨洪韧性/内涝风险和政策积压变化。
- 目标行动建议复用当前目标/里程碑面板：在原目标 hint 后追加一行以内的“建议：...”短句，由当前未完成里程碑 id 和城市指标生成；均衡服务、交通、财政、医疗、教育、警务和消防等目标应给出可执行方向，但不新增按钮、弹窗或底部状态格。
- 警报摘要复用右侧警报栏：`ALERT_PRIORITY_DIGEST` 按严重度排序并最多显示少量最关键告警，尾部用 `+N` 表示还有更多；现金、赤字、水电、污水、雨洪、医疗、消防、警务、灾害、交通和服务缺口等风险优先，底层 `Metrics.Alerts` 完整列表不被裁剪。
- 风险预测顾问复用现有 HUD 文案行：`RISK_FORECAST_ADVISOR` 显示 `ForecastRisk`、`ForecastFocus`、`ForecastAction` 和 `CashRunwayDays`，用于提前提示现金续航、财政、基础设施、服务或交通风险；它不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 预算拆解顾问复用现有 HUD 文案行：`BUDGET_BREAKDOWN_ADVISOR` 显示 `BudgetStress`、`BudgetFocus`、`BudgetDriver` 和 `BudgetAction`，把现金/赤字、债务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收等财政压力压缩成主因和短建议；它不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 片区优先级顾问复用现有目标/警报文案行：`DISTRICT_PRIORITY_ADVISOR` 显示 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver` 和 `DistrictPriorityAction`，核心实现名可用 `DistrictPriorityAdvisor` 或 `ComputeDistrictPriority`，把交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境等压缩成当前最需要治理的优先级和短建议；它只在优先级偏高或有风险时出现，不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 道路层级顾问复用现有目标/警报文案行：`ROAD_HIERARCHY_ADVISOR` 显示 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver` 和 `RoadHierarchyAction`，核心实现名可用 `RoadHierarchyAdvisor` 或 `ComputeRoadHierarchyAdvice`，把主干道不足、断头路、路网连通不足、路口延误、道路瓶颈、拥堵、公交候车/运力、停车压力、事故/养护等压缩成当前最该处理的交通层级问题和短建议；它只在压力偏高或有风险时出现，不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 通勤走廊顾问复用现有目标/警报文案行：`COMMUTE_CORRIDOR_ADVISOR` 通过 `CommuteCorridorText` 显示“通勤:压 ... -> ...”类短句，说明当前移动问题来自住岗平衡、通勤效率、汽车依赖、公交通勤、停车搜索、路网连通、货运满载或外部连接中的哪一类；它作为 `ObjectiveInsightParts` 候选进入优先栈，不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 住房负担/宜居迁入顾问复用现有目标/警报文案行：`HOUSING_AFFORDABILITY_ADVISOR` 通过 `HousingAffordabilityText` 显示“住房:压 ... -> ...”类短句，说明当前迁入与居住负担问题来自租金压力、住宅容量/人口缺口、住宅分区或混合/高密供给、平均地价/税率、公交覆盖、服务公平、宜居/生活压力、住岗平衡或保障住房政策中的哪一类；它作为 `ObjectiveInsightParts` 候选进入优先栈，不新增按钮、弹窗、工具项、底部状态格、建筑数量、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- 经济专精顾问复用现有目标/警报文案行：`ECONOMIC_SPECIALIZATION_ADVISOR` 通过 `EconomicSpecializationText` 显示“经济:专... -> ...”类短句，说明当前最适合推进资源工业、物流供应链、办公创新、旅游会展或混合商业中的哪条经济线；它作为 `ObjectiveInsightParts` 候选进入优先栈，不新增按钮、弹窗、工具项、底部状态格、建筑数量、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- 城市事件摘要复用现有 HUD 文案行：`CITY_EVENT_DIGEST` 显示 `RecentEvents` / `EventDigest` 的近期建造、政策、存读档和系统事件，可由 `BuildEventDigestText` 或同义方法压缩成短句；它不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 需求驱动分析复用现有 HUD 文案行：`DEMAND_DRIVER_ANALYSIS` 显示 `DemandFocus`、`DemandDriver`、`DemandAction` 和 `DemandUrgency`，解释最高需求及下一步动作；它不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 服务短板建议复用现有目标/警报文案行：`SERVICE_GAP_ADVISOR` 显示 `ServiceGapAdvisorFocus`、`ServiceGapAdvisorDriver` 和 `ServiceGapAdvisorAction`，把诊所/学校/消防/警务/公园覆盖，以及教育、健康、安全、火灾风险压缩成当前最该补齐的服务短板；它作为 `ObjectiveInsightParts` 候选进入优先栈，不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 成长瓶颈顾问复用现有目标/警报文案行：`GROWTH_BOTTLENECK_ADVISOR` 显示 `GrowthBottleneckScore`、`GrowthBottleneckFocus`、`GrowthBottleneckDriver` 和 `GrowthBottleneckAction`，把住房、财政、通勤、服务、公用设施、就业、供应链和宜居问题压缩成当前最卡增长的一条建议；它不新增按钮、弹窗、工具项或底部状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 建筑升级准备度顾问复用现有目标/警报文案行：`BUILDING_UPGRADE_READINESS_ADVISOR` 通过 `BuildingUpgradeReadinessText` 显示“升级:候/阻 ... -> ...”类短句，说明住宅/商业/办公/工业当前升级候选数、阻塞数、主要焦点、驱动原因和行动建议；它复用单栋建筑升级逻辑，不新增按钮、弹窗、工具项、底部状态格、建筑数量、workers、TS/Vite、WebGL2 或 SharedArrayBuffer。
- 洞察优先栈复用右侧目标/警报文案行：`HUD_INSIGHT_PRIORITY_STACK` / `ObjectiveInsightParts` 不新增功能按钮、不增加 HUD 状态格，而是把 `RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`SERVICE_GAP_ADVISOR`、`GROWTH_BOTTLENECK_ADVISOR`、`BUILDING_UPGRADE_READINESS_ADVISOR`、`DEMAND_DRIVER_ANALYSIS`、`CITY_EVENT_DIGEST` 作为候选 insight；`ObjectiveHint` 永远保持第一优先级，其他 insight 按风险、压力和事件重要性排序或限量显示少量最高优先级条目，降低横屏右侧拥挤，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 底部：两行状态网格，显示住宅、商业、混合用地、办公、工业、服务、基础设施需求，以及居住成本、宜居/生活压力、治安/警务响应/案件积压、人才、创新、用工、路网/道路瓶颈/路口延误、道路安全/事故风险/养护覆盖、步行、通勤/汽车依赖/停车压力、环境、健康/医疗响应/病患积压、灾备/灾害风险、应急响应、火灾风险/消防保障、生命关怀/死亡压力、吸引力、游客/外部连接、用地、商品/资源适配/本地供给/铁路导入/仓储稳定、运维/服务均衡/服务不足人口/主要缺口、通信/邮政/企业效率和服务覆盖状态；财政信用保留在顶栏，避免底部过载。

## 图层按钮
- Normal：普通城市视图。
- Traffic：道路负载、拥堵、断头路、路网连通性、路口延误和道路瓶颈。
- Pollution：污染与噪声。
- Zoning：住宅、商业、混合用地、办公、工业、公共服务、基础设施分区。
- Services：公园覆盖、医疗覆盖、医疗容量/响应压力、应急避难/灾备覆盖、`memorial_garden` 生命关怀覆盖、`DeathcareAccess` 关怀热力、教育覆盖、消防覆盖、`FireProtectionAccess` 消防保障热力、警务覆盖、`police_precinct` 警务容量/响应压力、邮政覆盖半径与公共服务容量压力。
- Transit：公交站、轨道交通站、城际枢纽覆盖、可达性、外部连接和运力压力。
- Waste：回收处理站和垃圾发电厂覆盖、容量压力与清洁压力。
- Logistics：货运站覆盖、资源加工园、配送中心、货运铁路站、铁路导入、`ResourcePotential`、`ResourceSpecialization`、`IndustrialSpecialization`、本地供给、仓储缓冲、供应链稳定、商业/工业货流可达性和货运容量压力。
- Communications：通信枢纽覆盖、通信容量压力、研发园区配套和企业效率。
- RoadSafety：道路养护覆盖、事故风险和道路安全。
- LandValue：地价热力。
- Utilities：供电、供水、污水处理、可靠性和容量压力。
- Stormwater：雨水花园覆盖、雨洪韧性和内涝风险。

## 已提供的 Unity UI 接口
- `CityHudViewModel.FromMetrics`：把 `CityMetrics` 转为顶栏、需求条、`ALERT_PRIORITY_DIGEST` 警报摘要、`RISK_FORECAST_ADVISOR` 风险预测、`BUDGET_BREAKDOWN_ADVISOR` 预算拆解、`DISTRICT_PRIORITY_ADVISOR` 片区/系统优先级、`ROAD_HIERARCHY_ADVISOR` 道路层级/瓶颈升级顾问、`COMMUTE_CORRIDOR_ADVISOR` 通勤走廊顾问、`HOUSING_AFFORDABILITY_ADVISOR` 住房负担/宜居迁入顾问、`ECONOMIC_SPECIALIZATION_ADVISOR` 经济专精顾问、`SERVICE_GAP_ADVISOR` 服务短板建议、`BUILDING_UPGRADE_READINESS_ADVISOR` 建筑升级准备度、`CITY_EVENT_DIGEST` 事件摘要、`DEMAND_DRIVER_ANALYSIS` 需求洞察、目标面板数据和 `OBJECTIVE_ACTION_ADVICE` 行动建议。
- `COMMUTE_CORRIDOR_ADVISOR`：通过 `CityHudViewModel.FromMetrics` 读取 `CommuteCorridorScore`、`CommuteCorridorFocus`、`CommuteCorridorDriver` 和 `CommuteCorridorAction`，生成 `CommuteCorridorText` 并作为 `ObjectiveInsightParts` 候选显示。
- `HOUSING_AFFORDABILITY_ADVISOR`：通过 `CityHudViewModel.FromMetrics` 读取 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver` 和 `HousingAffordabilityAction`，生成 `HousingAffordabilityText` 并作为 `ObjectiveInsightParts` 候选显示。
- `ECONOMIC_SPECIALIZATION_ADVISOR`：通过 `CityHudViewModel.FromMetrics` 读取 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver` 和 `EconomicSpecializationAction`，生成 `EconomicSpecializationText` 并作为 `ObjectiveInsightParts` 候选显示。
- `GROWTH_BOTTLENECK_ADVISOR`：通过 `CityHudViewModel.FromMetrics` 进入 `ObjectiveInsightParts`，只在增长瓶颈分数或关键风险足够高时显示一条紧凑建议。
- `BUILDING_UPGRADE_READINESS_ADVISOR`：通过 `CityHudViewModel.FromMetrics` 读取 `BuildingUpgradeReadinessScore`、`BuildingUpgradeReadyCount`、`BuildingUpgradeBlockedCount`、`BuildingUpgradeReadinessFocus`、`BuildingUpgradeReadinessDriver` 和 `BuildingUpgradeReadinessAction`，生成 `BuildingUpgradeReadinessText` 并作为 `ObjectiveInsightParts` 候选显示。
- `CityHudViewModel.OverlayColor`：根据当前 `OverlayMode` 和 `TileData` 计算热力图颜色。
- `CityGameController.HudSnapshot`：供 Unity UI 直接读取。
- `CityGameController.GetOverlayColor`：供 tile renderer 或 mesh overlay 直接调用。
- `CityRuntimeHud`：运行时自动生成横屏 HUD、图层按钮和建造工具按钮。
- `CityInteractionController`：处理鼠标/触控输入，支持拖拽铺路、拖拽分区、点击建造和点击拆除。
- `CityCameraController`：处理相机平移、滚轮缩放、双指缩放和地图边界限制。
- `CitySaveController`：处理手动保存、读取、删除和自动存档；微信环境优先走 storage，编辑器回退到 `PlayerPrefs`。
- `CityMapRenderer`：用顶点色地形网格、道路方块、建筑方块和 overlay 热力图形成可玩的临时视觉层。
- `BUILDING_VISUAL_PREFAB_LIBRARY`：Unity 渲染层按 `ModelKey`/建筑类型生成低多边形程序外观，38 个建筑都有 fallback；它只替换建筑视觉表现，不新增建筑数量、不修改 `miniprogram/game.json`、不使用 worker。
- 建筑等级会通过方块高度表现；正式 prefab 替换时也应保留 1/2/3 级的高度或细节差异。
- HUD 现在包含当前工具状态、当前目标行动建议、暂停/倍速、税率、城市政策、存读档状态和建造/分区预览、建筑“选址诊断”、适宜度与错误反馈，可直接显示现金不足、分区不匹配、道路不可铺设等结果。
- HUD 的城市政策按钮点击后应在同一预览区显示 `PolicyImpactPreview`，作为即时反馈，不新增单独按钮、弹窗或底部状态格。

## 原型场景
运行 Unity 菜单 `Pocket City/Create Prototype Scene` 后，会生成一个可直接 Play 的低保真原型。它不是最终美术，但已经具备完整 UI 接线：
- 顶栏：日期、人口、现金、月净收支、财政信用/行政效率/债务压力/债券本金、幸福度、评分。
- 左侧：图层切换。
- 右侧：目标、`OBJECTIVE_ACTION_ADVICE` “建议：...”行动提示、`ALERT_PRIORITY_DIGEST` 警报摘要、`RISK_FORECAST_ADVISOR` 风险预测、`BUDGET_BREAKDOWN_ADVISOR` 预算拆解、`DISTRICT_PRIORITY_ADVISOR` 片区/系统优先级、`ROAD_HIERARCHY_ADVISOR` 道路层级/瓶颈升级顾问、`COMMUTE_CORRIDOR_ADVISOR` 通勤走廊顾问、`HOUSING_AFFORDABILITY_ADVISOR` 住房负担/宜居迁入顾问、`ECONOMIC_SPECIALIZATION_ADVISOR` 经济专精顾问、`SERVICE_GAP_ADVISOR` 服务短板建议、`CITY_EVENT_DIGEST` 事件摘要、`DEMAND_DRIVER_ANALYSIS` 需求洞察、当前工具、建造预览、“选址诊断”、铺路、道路升级、七类分区、三十八类建筑、拆除、暂停、倍速、税率、服务预算、债券、保存、读取、绿色规范、公交优先、增长补贴、保障住房、交通安全行动、完整街道、信号优化、拥堵收费和停车费工具。
- 右侧警报摘要最多显示少量关键告警，使用紧凑单行或短列表；当完整 `Metrics.Alerts` 数量更多时，以 `+N` 收尾，避免挤压三十八类建筑按钮和底部 33 项状态。
- 右侧风险预测使用短标签或一行建议展示 `ForecastRisk`、`ForecastFocus`、`ForecastAction` 和 `CashRunwayDays`；风险文案可放在目标/警报附近，但不能新增按钮、弹窗或底部状态格。
- 右侧预算拆解使用短标签或一行建议展示 `BudgetStress`、`BudgetFocus`、`BudgetDriver` 和 `BudgetAction`；预算文案可放在目标/警报/财政信息附近，优先显示“压力：维护费；行动：紧缩服务或扩税基”这类短句，不能新增按钮、弹窗或底部状态格。
- 右侧片区优先级使用短标签或一行建议展示 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver` 和 `DistrictPriorityAction`；只在优先级偏高或有风险时显示，优先显示“优先：交通瓶颈；行动：升级主干/补公交”这类短句，不能新增按钮、弹窗或底部状态格。
- 右侧道路层级顾问使用短标签或一行建议展示 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver` 和 `RoadHierarchyAction`；只在压力偏高或有风险时显示，优先显示“道路：主干不足；行动：升级主干”或“道路：断头路；行动：打通连接”这类短句，不能新增按钮、弹窗或底部状态格。
- 右侧住房负担/宜居迁入顾问使用短标签或一行建议展示 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver` 和 `HousingAffordabilityAction`；优先显示“住房：租金压力；行动：补公寓/混合用地”或“住房：宜居压力；行动：补服务/公交”这类短句，不能新增按钮、弹窗、底部状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不能修改 `miniprogram/game.json`。
- 右侧事件摘要使用短标签或一行文本展示 `RecentEvents` / `EventDigest`，放在目标/警报附近；事件文案可压缩最近 1-3 条，但不能新增按钮、弹窗或底部状态格。
- 右侧需求洞察使用短标签或一行文本展示 `DemandFocus`、`DemandDriver`、`DemandAction` 和 `DemandUrgency`，放在目标/警报附近；需求文案应解释最高需求和下一步动作，但不能新增按钮、弹窗或底部状态格。
- 右侧政策效果反馈应使用两到三行紧凑文本或小型 delta 列表；优先展示启用/关闭、月收支、拥堵、停车、步行、安全、雨洪和政策积压，避免挤压九项政策按钮和三十八类建筑按钮。
- 底部：两行最多 17 列状态网格，显示住宅/商业/混合用地/办公/工业需求、居住成本、宜居度/生活压力、治安压力/警务响应/案件积压、人才/高等教育/生产率、创新能力/企业效率、用工缺口、路网连通性/断头路/道路瓶颈/路口延误、道路安全/事故风险/养护覆盖、步行可达性、通勤效率/汽车依赖/停车压力/停车满载率、环境质量/噪声压力、公共健康/健康风险/医疗满载/医疗响应/病患积压、灾备/灾害风险、应急响应、火灾风险/消防保障/消防满载/消防响应、生命关怀覆盖/满载率/死亡压力、吸引力、游客/外部连接、用地效率/空置分区/用地冲突、商品平衡/资源适配/本地供给/铁路导入/仓储稳定、运维状态/服务负载/服务公平/服务不足人口/主要缺口、水电可靠性/满载率/污水满载率/内涝风险、公园覆盖、医疗覆盖、教育覆盖、消防覆盖、公交覆盖/满载率、货运覆盖/满载率、通信覆盖/满载率/邮政覆盖/邮政满载率/企业效率和回收覆盖/满载率/稳定度。

## 视觉资产清单
后续可用 `codex-image-2` 生成以下 2D 参考图或贴图：
- 横屏 UI mockup，一张 16:9。
- 低多边形城市建筑图标：住宅、公寓、商铺、混合街区、办公楼、研发园区、工坊、资源加工园、配送中心、公园、城市广场、会展中心、市政厅、诊所、区域医院、应急避难中心、纪念花园、学校、学院、消防站、警务站、警署、通信枢纽、邮政局、道路养护站、停车楼、雨水花园、公交站、轨道交通站、城际枢纽、货运站、货运铁路站、电站、太阳能阵列、水塔、污水站、垃圾发电厂、回收站。
- 七类分区色板和图层热力图色板。
- 微信小游戏加载页背景。

## 当前生成资产
当前优先采用 Unity Editor 生成的轻量资产，原因是首包更小、可重复生成、不会阻塞玩法验证：
- `Pocket City/Create Visual Assets` 生成材质、分区色板、热力图色板、建筑图标图集和加载页背景。
- `Pocket City/Create Prototype Scene` 会自动调用视觉资产生成器，并将材质绑定到 `CityMapRenderer`。
- 后续接入 `codex-image-2` 时，优先替换 `building-icons.png` 和 `loading-background.png`，材质色板继续保留为 fallback。

## Unity 落地建议
- 建筑先用简单 prefab 和纯色材质搭出可玩版本，再逐步替换贴图。
- UI 使用 UGUI 或 UI Toolkit 均可；先保证横屏触控面积和信息密度。
- “选址诊断”放在建造预览信息内，用两行以内的短句呈现 `SiteDiagnosis`，不要新增独立工具按钮、状态格或教学弹窗；当诊断较长时优先换行或截断，避免挤压三十八类建筑按钮。
- `PolicyImpactPreview` 放在同一右侧预览信息区，用短标签和正负号表达 delta；长列表在窄屏优先折行或隐藏次要项，不改变 38/48/33 数量口径。
- `OBJECTIVE_ACTION_ADVICE` 放在当前目标 hint 后面，使用“建议：补医疗容量”这类短句，不做按钮样式，不占底部状态格；长建议优先压缩为目标动作加对象，例如“建议：补主要缺口：邮政”。
- `ALERT_PRIORITY_DIGEST` 放在右侧警报栏内，只做视图层排序与数量压缩；不要新增警报按钮、过滤器、弹窗或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `RISK_FORECAST_ADVISOR` 放在目标/警报/顶部财政信息附近，使用“风险：现金 18 天；行动：控预算”这类短句；不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `BUDGET_BREAKDOWN_ADVISOR` 放在目标/警报/顶部财政信息附近，使用“预算：公交维护高；行动：补客运容量”这类短句；字段口径为 `BudgetStress`、`BudgetFocus`、`BudgetDriver`、`BudgetAction`，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `DISTRICT_PRIORITY_ADVISOR` 放在目标/警报附近，使用“优先：服务缺口；行动：补医疗/公交”这类短句；字段口径为 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver`、`DistrictPriorityAction`，只在优先级偏高或有风险时显示，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `ROAD_HIERARCHY_ADVISOR` 放在目标/警报附近，使用“道路：路口延误；行动：优化信号”这类短句；字段口径为 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver`、`RoadHierarchyAction`，只在压力偏高或有风险时显示，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `HOUSING_AFFORDABILITY_ADVISOR` 放在目标/警报附近，使用“住房：租金高 -> 补公寓/保障住房”这类短句；字段口径为 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver`、`HousingAffordabilityAction`，输入来自 `RentPressure`、住宅容量/人口缺口、住宅分区/混合/高密供给、地价/税率、公交、服务公平、宜居/生活压力、住岗平衡和 `AffordableHousing` 政策，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `ECONOMIC_SPECIALIZATION_ADVISOR` 放在目标/警报附近，使用“经济:专办公创新 -> 补研发/高教/通信”或“经济:专物流供应链 -> 补配送/货运容量”这类短句；字段口径为 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver`、`EconomicSpecializationAction`，输入来自 `BusinessEfficiency`、`InnovationCapacity`、`OfficeJobs`、`WorkforceSkill`、`AdvancedEducationCoverage`、`IndustrialSpecialization`、`ResourceSpecialization`、`LocalGoodsSupply`、`GoodsBalance`、`SupplyChainStability`、`LogisticsCoverage`/`LogisticsUtilization`、`Attractiveness`、`Visitors`、`TourismIncome`、`MixedUseBuildings` 和 `RegionalConnectivity`；不要新增按钮、弹窗、工具项、HUD 状态格、建筑数量、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不要修改 `miniprogram/game.json`。
- `CITY_EVENT_DIGEST` 放在目标/警报附近，使用“事件：建造诊所；政策：公交优先”这类短句；不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `DEMAND_DRIVER_ANALYSIS` 放在目标/警报附近，使用“需求：住宅/居住成本高 -> 补公寓”这类短句；不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `SERVICE_GAP_ADVISOR` 放在目标/警报附近，使用“服务短板：医疗；行动：补诊所”这类短句；字段口径为 `ServiceGapAdvisorFocus`、`ServiceGapAdvisorDriver`、`ServiceGapAdvisorAction`，输入来自 clinic/school/fire/police/park 覆盖与 education/health/safety/fire risk 等现有指标，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- `HUD_INSIGHT_PRIORITY_STACK` / `ObjectiveInsightParts` 放在右侧目标/警报文案区域内部，先显示 `ObjectiveHint`，再显示少量由风险、预算压力、片区优先级、道路层级、通勤走廊、住房负担、经济专精、服务短板、建筑升级准备度、需求驱动和城市事件摘要筛出的最高优先级 insight；不要让 advisor 文案同时铺满右侧，不要新增按钮、弹窗、工具项或 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 底部状态项超过一行时应使用稳定网格或分页，不回退为单行挤压布局。
- 图层用材质颜色或 tile overlay 表示，不依赖复杂后处理。
- 所有按钮使用图标加短标签，长文案放到右侧检查器。

# 技术方案

## 架构定位
项目已经切换为 Unity-first。`unity/` 是唯一活跃游戏工程；`legacy/typescript-prototype/` 仅作为迁移参考，不再构建或发布 TS 版本。

```text
Unity Scene / UI
  -> CityGameController
  -> CityInteractionController / CityCameraController / CitySaveController
  -> CitySimulationCore
  -> CityGridCore
  -> CityConfig / BuildingDefinition
  -> WeChatMiniGameBridge
```

## 分层约束
- `PocketCity.Core`：纯数据类型、建筑定义、分区、地形、城市指标和预览结果；建筑预览结果包含 `BuildingSiteScore` 与 `SiteDiagnosis`，用于展示中文“选址诊断”。
- `PolicyImpactPreview` 属于预览结果口径，用于记录城市政策切换前后的即时差值，包括本次动作启用/关闭、月收支、拥堵、停车压力、步行可达性、事故风险、雨洪韧性/内涝风险和政策积压等字段；它只服务右侧预览面板，不新增按钮或底部状态格。
- `OBJECTIVE_ACTION_ADVICE` 属于当前目标/里程碑面板文案口径：它基于当前未完成里程碑 id 和 `CityMetrics` 生成一条短“建议：...”行动提示，追加在原目标 hint 后；它不改变里程碑定义、存档结构、按钮数量或底部状态格。
- `ALERT_PRIORITY_DIGEST` 属于 HUD 视图层口径：它从 `Metrics.Alerts` 读取完整告警列表，按严重度排序并为右侧警报栏生成少量最关键告警和 `+N` 溢出提示；它不得裁剪、改写或替换底层 `Metrics.Alerts`，也不改变存档结构、按钮数量、底部状态格、38/48/33 口径或 `miniprogram/game.json`。
- `RISK_FORECAST_ADVISOR` 属于近期风险预测文案口径：它读取现金续航、月净收支、债务压力、水电/污水/雨洪、公共服务容量、交通瓶颈和高优先级告警，输出 `ForecastRisk`、`ForecastFocus`、`ForecastAction` 与 `CashRunwayDays`；核心实现名可用 `RiskForecastAdvisor` 或 `ComputeForecastRisk`。它只复用现有目标、警报、预览或顶部财政信息行，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `BUDGET_BREAKDOWN_ADVISOR` 属于预算压力拆解/财政顾问文案口径：它读取现金/赤字、债务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收等现有指标，输出 `BudgetStress`、`BudgetFocus`、`BudgetDriver` 与 `BudgetAction`；核心实现名可用 `BudgetBreakdownAdvisor` 或 `ComputeBudgetBreakdown`。它只复用现有目标/警报/财政文案区域给出主要财政压力来源和短行动建议，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `DISTRICT_PRIORITY_ADVISOR` 属于片区/系统优先级顾问文案口径：它读取交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境等现有指标，输出 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver` 与 `DistrictPriorityAction`；核心实现名可用 `DistrictPriorityAdvisor` 或 `ComputeDistrictPriority`。它只在优先级偏高或有风险时复用现有目标/警报文案区域给出当前最需要治理的片区或系统优先级和短行动建议，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `ROAD_HIERARCHY_ADVISOR` 属于道路层级/瓶颈升级顾问文案口径：它读取主干道数量/占比、断头路、路网连通性、`IntersectionDelay`、`RoadBottleneckPressure`、拥堵、公交候车/运力、停车压力、事故风险和道路养护覆盖等现有道路/交通指标，输出 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver` 与 `RoadHierarchyAction`；核心实现名可用 `RoadHierarchyAdvisor` 或 `ComputeRoadHierarchyAdvice`。它只在压力偏高或有风险时复用现有目标/警报文案区域给出当前最该处理的交通层级问题和短行动建议，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `COMMUTE_CORRIDOR_ADVISOR` 属于通勤走廊顾问文案口径：它读取住岗平衡、`CommuteEfficiency`、`CarDependency`、公交覆盖/利用率/可靠性/候车压力、停车压力/覆盖/利用率、路网连通、道路瓶颈、路口延误、货运覆盖/利用率、供应链稳定和 `RegionalConnectivity` 等现有移动指标，输出 `CommuteCorridorScore`、`CommuteCorridorFocus`、`CommuteCorridorDriver` 与 `CommuteCorridorAction`；核心实现名可用 `CommuteCorridorAdvisor` 或 `ComputeCommuteCorridorAdvice`。HUD 适配层生成 `CommuteCorridorText` 并进入 `ObjectiveInsightParts` 候选，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `CITY_EVENT_DIGEST` 属于近期事件摘要文案口径：它读取 `RecentEvents` / `EventDigest`，事件写入入口可用 `AddCityEvent`、`PushCityEvent` 或同义名，展示文本可用 `BuildEventDigestText` 或同义名。它只复用现有目标/警报文案区域，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `DEMAND_DRIVER_ANALYSIS` 属于需求解释文案口径：它读取七类需求、居住成本、商品供给、通勤、人才、物流、服务缺口和基础设施容量等指标，输出 `DemandFocus`、`DemandDriver`、`DemandAction` 与 `DemandUrgency`；核心实现名可用 `AnalyzeDemandDrivers` 或 `ComputeDemandInsight`。它只复用现有目标/警报/需求文案区域，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `SERVICE_GAP_ADVISOR` 属于服务短板建议文案口径：它读取 clinic/school/fire/police/park 覆盖，以及 education、health、safety、fire risk 等既有服务与风险指标，输出 `ServiceGapAdvisorScore`、`ServiceGapAdvisorFocus`、`ServiceGapAdvisorDriver` 与 `ServiceGapAdvisorAction`；核心实现名可用 `ServiceGapAdvisor` 或 `ComputeServiceGapAdvisor`。它只复用右侧目标/警报文案区域给出当前最该补齐的服务短板和短行动建议，并进入 `ObjectiveInsightParts` 候选，不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 或 `miniprogram/game.json`。
- `BUILDING_UPGRADE_READINESS_ADVISOR` 属于建筑升级准备度文案口径：它复用单栋建筑自然升级逻辑，按住宅/商业/办公/工业的年龄门槛、升级分、地价、公交、接路、服务覆盖、物流、教育/高教、劳动力、污染/噪音等条件判断升级机会或阻塞，输出 `BuildingUpgradeReadinessScore`、`BuildingUpgradeReadyCount`、`BuildingUpgradeBlockedCount`、`BuildingUpgradeReadinessFocus`、`BuildingUpgradeReadinessDriver` 与 `BuildingUpgradeReadinessAction`；HUD 适配层生成 `BuildingUpgradeReadinessText` 并进入 `ObjectiveInsightParts` 候选，短句可采用“升级:候/阻 ... -> ...”格式。它不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer。
- `HOUSING_AFFORDABILITY_ADVISOR` 属于住房负担/宜居迁入顾问文案口径：它读取 `RentPressure`、住宅容量与人口缺口、`ResidentialZoneTiles`、`MixedUse`、`HighDensityResidentialBuildings`、`AverageLandValue`、`TaxLevel`、`TransitCoverage`、`ServiceEquity`、`LivingCondition`、`LivingPressure`、`JobsHousingBalance` 和 `AffordableHousing` 政策状态等既有指标，输出 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver` 与 `HousingAffordabilityAction`；核心实现名可用 `HousingAffordabilityAdvisor` 或 `ComputeHousingAffordabilityAdvice`。HUD 适配层生成 `HousingAffordabilityText` 并进入 `ObjectiveInsightParts` 候选，短句可采用“住房:压 ... -> ...”格式。它不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- `ECONOMIC_SPECIALIZATION_ADVISOR` 属于经济专精顾问文案口径：它读取 `BusinessEfficiency`、`InnovationCapacity`、`OfficeJobs`、`WorkforceSkill`、`AdvancedEducationCoverage`、`IndustrialSpecialization`、`ResourceSpecialization`、`LocalGoodsSupply`、`GoodsBalance`、`SupplyChainStability`、`LogisticsCoverage`/`LogisticsUtilization`、`Attractiveness`、`Visitors`、`TourismIncome`、`MixedUseBuildings` 和 `RegionalConnectivity` 等既有经济、产业、物流、旅游和混合商业指标，输出 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver` 与 `EconomicSpecializationAction`；核心实现名可用 `EconomicSpecializationAdvisor` 或 `ComputeEconomicSpecializationAdvice`。HUD 适配层生成 `EconomicSpecializationText` 并进入 `ObjectiveInsightParts` 候选，短句可采用“经济:专... -> ...”格式，说明当前最适合推进资源工业、物流供应链、办公创新、旅游会展或混合商业哪条经济线。它不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- `HUD_INSIGHT_PRIORITY_STACK` / `ObjectiveInsightParts` 属于 HUD 视图层口径：它只为右侧目标/警报文案选择和排序少量 insight，不新增功能按钮、不增加 HUD 状态格，也不改变底层顾问数据。候选来自 `RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`SERVICE_GAP_ADVISOR`、`BUILDING_UPGRADE_READINESS_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`DEMAND_DRIVER_ANALYSIS`、`CITY_EVENT_DIGEST` 等现有顾问输出；`ObjectiveHint` 固定第一优先级，其余 insight 按风险、压力和事件重要性排序或限量显示，降低横屏右侧拥挤，不改变 38/48/33 或 `miniprogram/game.json`。
- `WECHAT_SAFE_LIFECYCLE_FEEDBACK` 属于微信平台安全反馈口径：微信环境下切后台/暂停时触发安全自动保存，关键城市命令和保存结果走安全触觉反馈；Editor 下回退到 `PlayerPrefs` 与无触觉 fallback。它只复用 `CitySaveController` 和 `WeChatMiniGameBridge`，不新增 worker，也不修改 `miniprogram/game.json`。
- `TILE_INSPECTOR_OVERLAY_LEGEND` 属于 HUD/交互增强口径：当前 `OverlayMode` 应提供图例，悬停或点击地块时右侧检查器显示分区、建筑/道路、当前图层数值和诊断摘要。它只读取已有地块、道路、建筑、图层和诊断数据，不新增建筑、按钮、政策、worker、存档字段或 `miniprogram/game.json` 配置。
- `PocketCity.Simulation`：地图、分区、道路、路口延误/`IntersectionDelay`、道路瓶颈/`RoadBottleneckPressure`、道路养护、事故风险、道路安全、财政信用、行政效率、外部连接、债务压力、市政债券、建筑、预算、需求、发展品质、用地冲突、停车压力/覆盖/容量、雨洪韧性/内涝风险、医疗容量/`HealthLoad`/`HealthCapacity`/`HealthUtilization`/`MedicalResponse`/`PatientBacklog`、教育容量/`EducationLoad`/`EducationCapacity`/`EducationUtilization`/`StudentBacklog`/`LearningPipeline`、消防韧性/`FireRisk`/`FireProtection`/`FireLoad`/`FireCapacity`/`FireUtilization`/`FireResponse`、生命关怀/`DeathcareCoverage`/`DeathcareLoad`/`DeathcareCapacity`/`DeathcareUtilization`/`MortalityPressure`、警务响应/`SecurityLoad`/`SecurityCapacity`/`SecurityUtilization`/`PoliceResponse`/`CaseBacklog`、公交可靠性/`TransitReliability`/`TransitWaitPressure`/`ComputeTransitWaitPressure`、服务公平/`UnderservedResidents`/主要服务缺口来源、宜居度/`LivingCondition`/`LivingPressure`、灾备/灾害风险、资源潜力/资源适配/产业专精、货运铁路导入、仓储缓冲/供应链稳定、通信覆盖、邮政服务、企业效率、创新能力、城市吸引力/游客经济/会展客流、里程碑和指标重算。
- `PocketCity.Simulation` 也负责九项城市政策效果：绿色规范、公交优先、增长补贴、保障住房、交通安全行动、完整街道、信号优化、拥堵收费和 `CityPolicy.ParkingFees`（中文 UI：停车费/停车收费）。
- `PocketCity.Runtime`：Unity `MonoBehaviour` 入口、HUD、输入命令转发、相机、税率/政策按钮、存档和微信平台桥。
- `Assets/Editor/PocketCity`：Unity Editor 工具，用来生成默认 `CityConfig`。
- `Assets/Plugins/WebGL/WeChatBridge.jslib`：Unity WebGL 到微信小游戏 JS 环境的最小桥接。

## 城市模拟
- `CityGridCore` 维护 64x64 网格、地形、分区、道路、建筑占地、交通、污染、噪声、地价、停车可达性、雨洪可达性、邮政 `MailAccess`、生命关怀 `DeathcareAccess`、资源潜力和各类服务可达性。
- `CitySimulationCore` 按日推进人口和建筑年龄，每 30 天结算预算。
- 道路有普通路和主干道两级；主干道由已有道路升级而来，容量约为普通路 2 倍，维护费更高，并带来更高沿线噪声。
- 路网连通性由断头路、交叉口、主干道数量和建筑接路率计算，会影响通勤效率、城市评分、HUD、告警和“连通路网”里程碑。
- 道路养护站按半径覆盖道路；道路养护覆盖、维护状态、拥堵、断头路、交叉口、主干道、应急响应和步行可达性共同形成事故风险与道路安全，事故风险会增加额外道路负载并驱动道路安全告警。
- `ComputeDebtPressure` / `ComputeFiscalHealth` / `ComputeAdministrationEfficiency` 会根据现金、月净收支、月支出、债券本金和行政容量形成财政信用与行政效率；`AdministrationLoad`、`AdministrationCapacity`、`AdministrationUtilization` 和 `PolicyBacklog` 会把人口与启用政策数转成行政满载和政策积压，影响政策成本、幸福度、城市评分和服务需求，并进入 `administration_capacity`、“行政容量不足”和“政策积压偏高”口径。债务压力会影响幸福度、城市评分、住宅/商业/工业/办公/混合需求和财政告警，`PolicyMonthlyExpense` 会按行政效率与政策积压调整正向政策执行成本。
- 未接入道路的建筑只有 20% 效率，但仍消耗水电。
- `PreviewBuilding` 在校验现金、地形、占地、道路和分区匹配的同时计算 `BuildingSiteScore` / `SiteDiagnosis`；诊断按建筑类型读取地价、污染/噪声、道路接入、推荐分区/适宜度、公交/物流/通信/邮政/停车/雨洪/服务可达性等条件，输出 1-2 行中文建议。该结果只进入建造预览和右侧检查器，不改变建筑配置、工具按钮、存档结构或底部 33 个 HUD 状态格。
- 每日推进时，住宅/商业/混合用地/办公/工业需求超过阈值后会尝试在对应已分区、可放置、接入道路且适宜度达标的空地上自然开发建筑；自动开发只收取少量接入补贴，并写入存档。
- `ZoneSuitabilityForRect` / `ZoneSuitabilityForTile` 会按地价、服务、公交通勤、污染噪声、治安、物流和废弃物可达性评估住宅/商业/混合用地/办公/工业适宜度；拖拽分区预览显示百分比，自然开发会过滤低适宜度并优先高适宜度地块。
- 建筑选址诊断复用分区适宜度和已有可达性热力：住宅/公寓偏好服务、公园、公交、低污染噪声和合理地价；商业/混合偏好高地价、公交、停车、服务和客流；办公/研发偏好教育、高等教育、通信、公交、治安和水电可靠；工业/资源偏好工业分区、货运、丘陵/资源潜力、低敏感邻接和废弃物可达；物流设施偏好货运需求、道路接入和低冲突；通信/邮政/公共服务设施偏好服务缺口和覆盖盲区；停车、雨洪、水电/污水/回收设施优先解释容量缺口、服务半径、污染/噪声或内涝风险。
- `ComputeLandUseConflict` 会按相邻分区、污染噪声和敏感用地权重计算用地冲突；拖拽分区预览显示缓冲风险，工业/设施贴住宅、混合用地或办公会提高冲突，公共服务区会提供轻量缓冲。冲突会影响幸福度、城市评分、住宅/商业/工业/办公/混合需求和服务需求，并驱动“用地冲突偏高”告警与“功能缓冲”里程碑。
- `ComputeDevelopmentQuality` 会按已开发建筑的分区适配、接路状态、建筑等级和权重计算发展品质；品质会影响幸福度、城市评分、住宅/商业/工业/办公/混合需求和服务需求，并驱动“片区品质偏低”告警与“优质片区”里程碑。
- 增长型分区会统计已开发面积、空置面积和用地效率；紧凑开发给城市评分少量加成，空置分区过多会形成评分惩罚和规划告警。
- 居住成本压力高且公寓楼已解锁时，住宅自然开发会额外加入公寓楼候选；公寓候选偏好地价、公交和服务较好的 2x3 住宅地块。
- 水电短缺会降低建筑效率并触发幸福度惩罚；水电容量、负载、满载率和可靠性会进入 HUD、告警和韧性目标。太阳能阵列提供零污染供电，并在中期水电吃紧时触发清洁电力告警和“清洁电力”目标。污水处理站提供排水处理容量，污水过载会提高污染、噪声、健康风险和基础设施需求，并进入 HUD、告警和“水环境”目标。雨水花园提供雨洪容量，人口/岗位/道路/开发强度/工业活动/地形暴露会形成雨洪负荷，容量不足会推高内涝风险并进入 HUD、告警和“雨洪韧性”目标。
- 服务建筑按类型覆盖城市：口袋公园提供公园覆盖，城市广场和会展中心提供地标吸引力，市政厅提供行政效率，城际枢纽提供外部连接，社区诊所和区域医院提供医疗覆盖，应急避难中心提供灾备容量，纪念花园提供生命关怀覆盖，社区学校和社区学院提供教育覆盖与教育容量，社区学院额外提供高等教育覆盖，社区消防站提供消防覆盖，社区警务站提供警务覆盖，警署提供中后期警务容量与响应；教育容量不新增建筑，基础服务合成综合服务覆盖并提高地价/幸福度。
- 医疗、教育、消防、警务和生命关怀会形成公共服务负载、容量和利用率；服务利用率超过容量时，有效分类覆盖按可靠性下降，并提高公共服务需求和容量不足告警。医疗专项会通过 `HealthLoad`、`HealthCapacity`、`HealthUtilization`、`MedicalResponse` 和 `PatientBacklog` 表达诊疗容量、响应和病患积压，教育专项会通过 `EducationLoad`、`EducationCapacity`、`EducationUtilization`、`StudentBacklog` 和 `LearningPipeline` 表达学位压力、入学积压和学习通道，警务专项还会通过 `SecurityLoad`、`SecurityCapacity`、`SecurityUtilization`、`PoliceResponse` 和 `CaseBacklog` 表达执法容量、响应和案件积压。
- `ResidentialServiceScore` / `ComputeServiceEquity` 会按住宅片区的公园、医疗、教育、公交、消防、警务、回收、通信、邮政和生命关怀可达性计算服务公平；`SERVICE_EQUITY_GAP_SOURCES` 按住宅敏感建筑容量加权统计这些覆盖缺失，`ComputeUnderservedResidents` 用容量加权缺口估算服务不足人口。HUD 运维/服务均衡项会显示服务不足人口与主要服务缺口来源；服务公平低会增加服务需求、压低幸福度、城市评分、住宅/商业/办公/混合需求，并驱动“片区服务不均”告警与“均衡服务”里程碑。
- `CityHudViewModel.FromMetrics` 生成右侧警报栏时应用 `ALERT_PRIORITY_DIGEST`：现金不足、负现金、月度赤字、财政信用/债务风险、水电与污水过载、雨洪与内涝、医疗/消防/警务容量或响应、灾备/灾害、交通拥堵/瓶颈/事故、公共服务容量和服务公平缺口等同级优先；展示列表达到上限后保留 `+N`，但 `CityMetrics.Alerts` 继续提供完整列表给测试、日志或后续 UI。
- `RISK_FORECAST_ADVISOR` 可在模拟重算后基于同一份 `CityMetrics` 生成近期风险摘要：`CashRunwayDays` 由现金、月净收支和月支出估算现金续航，`ForecastRisk` 表示整体风险等级，`ForecastFocus` 选择最需要处理的风险域，`ForecastAction` 给出一句短行动建议。风险域优先覆盖现金/财政、基础设施容量、公共服务容量、灾害/健康、交通瓶颈和高优先级告警；该摘要只进入 HUD 文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `BUDGET_BREAKDOWN_ADVISOR` 可在预算和指标重算后基于同一份 `CityMetrics` 生成预算压力拆解：`BudgetStress` 表示财政压力等级，`BudgetFocus` 选择最主要预算压力来源，`BudgetDriver` 解释该来源来自赤字、债务服务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收中的哪一组指标，`BudgetAction` 给出一句短行动建议。该摘要只进入 HUD 目标/警报/财政文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `DISTRICT_PRIORITY_ADVISOR` 可在指标重算后基于同一份 `CityMetrics` 生成片区/系统优先级摘要：`DistrictPriorityScore` 表示治理优先级，`DistrictPriorityFocus` 选择最需要处理的片区或系统域，`DistrictPriorityDriver` 解释它来自交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境中的哪一组指标，`DistrictPriorityAction` 给出一句短行动建议。该摘要只在优先级偏高或有风险时进入 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `ROAD_HIERARCHY_ADVISOR` 可在指标重算后基于同一份 `CityMetrics` 生成道路层级/瓶颈升级摘要：`RoadHierarchyPressure` 表示道路层级压力，`RoadHierarchyFocus` 选择当前最该处理的交通层级问题，`RoadHierarchyDriver` 解释它来自主干道不足、断头路、路网连通不足、路口延误、道路瓶颈、拥堵、公交候车/运力、停车压力、事故/养护中的哪一组指标，`RoadHierarchyAction` 给出一句短行动建议。该摘要只在压力偏高或有风险时进入 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `COMMUTE_CORRIDOR_ADVISOR` 可在指标重算后基于同一份 `CityMetrics` 生成通勤走廊摘要：`CommuteCorridorScore` 表示移动压力或机会优先级，`CommuteCorridorFocus` 选择当前最该处理的走廊焦点，`CommuteCorridorDriver` 解释它来自住岗平衡、通勤效率、汽车依赖、公交通勤、停车搜索、路网连通、货运满载或外部连接中的哪一组指标，`CommuteCorridorAction` 给出一句短行动建议。该摘要只进入右侧 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `CITY_EVENT_DIGEST` 可在模拟层通过 `AddCityEvent` / `PushCityEvent` 记录最近建造、政策、存读档和关键系统事件，并限制 `RecentEvents` / `EventDigest` 数量；HUD 侧通过 `BuildEventDigestText` 或同义方法生成一行短摘要，放在目标/警报附近，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `DEMAND_DRIVER_ANALYSIS` 在需求计算完成后运行：从住宅、商业、混合、办公、工业、服务和设施需求中选择最高项，用当前指标解释驱动因素，并生成短行动建议。该摘要只进入 HUD 文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `SERVICE_GAP_ADVISOR` 可在服务覆盖与风险指标重算后基于同一份 `CityMetrics` 生成服务短板摘要：`ServiceGapAdvisorScore` 表示短板紧迫度，`ServiceGapAdvisorFocus` 选择诊所/医疗、学校/教育、消防、警务、公园或安全中的主要短板，`ServiceGapAdvisorDriver` 解释它来自覆盖不足、教育压力、健康风险、安全压力或火灾风险，`ServiceGapAdvisorAction` 给出一句短行动建议。该摘要只进入右侧 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `BUILDING_UPGRADE_READINESS_ADVISOR` 可在建筑升级分重算后基于同一份 `CityMetrics` 生成升级准备度摘要：`BuildingUpgradeReadinessScore` 表示整体升级机会，`BuildingUpgradeReadyCount` 与 `BuildingUpgradeBlockedCount` 表示候选/阻塞建筑数量，`BuildingUpgradeReadinessFocus` 选择住宅、商业、办公或工业中的主要升级焦点，`BuildingUpgradeReadinessDriver` 解释主要来自年龄、地价、公交、接路、服务、物流、教育/高教、劳动力、污染或噪音，`BuildingUpgradeReadinessAction` 给出一句短行动建议。该摘要只进入右侧 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `HOUSING_AFFORDABILITY_ADVISOR` 可在居住成本、住房供给和宜居指标重算后基于同一份 `CityMetrics` 生成住房负担摘要：`HousingAffordabilityScore` 表示住房负担与迁入稳定性优先级，`HousingAffordabilityFocus` 选择租金压力、住宅缺口、缺少高密地块、地价/税率、公交/服务公平、宜居压力、住岗错配或保障住房政策中的主要焦点，`HousingAffordabilityDriver` 解释驱动原因，`HousingAffordabilityAction` 给出一句短行动建议。该摘要只进入右侧 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `ECONOMIC_SPECIALIZATION_ADVISOR` 可在经济、产业、物流、旅游和混合商业指标重算后基于同一份 `CityMetrics` 生成经济专精摘要：`EconomicSpecializationScore` 表示专精推进优先级，`EconomicSpecializationFocus` 选择资源工业、物流供应链、办公创新、旅游会展或混合商业中的主线，`EconomicSpecializationDriver` 解释驱动原因来自企业效率、创新、高教/人才、产业/资源专精、商品供需、供应链、物流满载、吸引力/游客/旅游收入、混合建筑或区域连接，`EconomicSpecializationAction` 给出一句短行动建议。该摘要只进入右侧 HUD 目标/警报文案，不写入建筑配置、按钮配置、底部状态格或小游戏配置。
- `HUD_INSIGHT_PRIORITY_STACK` / `ObjectiveInsightParts` 在 `CityHudViewModel.FromMetrics` 或同等 HUD 适配层汇总上述 advisor 输出，只生成右侧目标/警报文案的展示栈：先保留 `ObjectiveHint`，再从风险预测、预算拆解、片区优先级、道路层级、通勤走廊、服务短板、建筑升级准备度、住房负担、经济专精、需求驱动和城市事件摘要中选出少量最高优先级条目。该栈只影响展示顺序和展示数量，不写入存档、不改模拟指标、不新增按钮配置、底部状态格或小游戏配置。
- 维护状态由现金缓冲、服务预算、服务利用率、水电利用率、拥堵、道路养护覆盖和城市规模计算；维护状态会折损服务可靠性，并影响幸福度、城市评分、服务/基础设施需求、告警和“城市运维”里程碑。
- 应急响应由医疗覆盖、医疗响应、消防覆盖、警务覆盖、警务响应、服务可靠性、路网连通性、拥堵、断头路、服务利用率和未接路建筑共同计算；响应会影响治安压力、公共健康、健康风险、幸福度、城市评分、服务需求、告警和“应急响应”里程碑。灾备由接路应急避难容量、应急响应、雨洪韧性、水电可靠性、路网连通性和维护状态计算；灾害风险由内涝风险、健康风险、事故风险、拥堵和公用设施可靠性推高，并由灾备压低，进入 HUD、健康、幸福度、告警和“灾害准备”里程碑。
- 教育覆盖和学习通道会提高商业/办公/工业需求、就业税收质量、劳动力素质和建筑升级评分；高等教育覆盖会进一步提高劳动力素质、创新能力、生产率、办公需求、城市评分和中后期建筑成长，入学积压会压低人才成长并推高服务需求。
- 混合用地需求由住宅需求、商业需求、平均地价、公交覆盖、服务覆盖、警务覆盖、税率和政策共同计算；混合街区同时计入住容、岗位、住宅服务覆盖和建筑成长。
- 办公需求由人口、教育覆盖、高等教育覆盖、创新能力、平均地价、公交覆盖、警务覆盖、税率/政策和现有办公岗位共同计算；办公岗位达到目标后完成“知识经济”里程碑，研发园区与创新能力达标后完成“创新高地”里程碑。
- 城市广场作为地标服务建筑，接入道路后同时提高公园覆盖和城市吸引力；会展中心作为大型地标服务建筑，接入道路后提高城市吸引力、游客和会展旅游收入。吸引力由地标、公园、服务、公交、治安、地价和混合街区提高，由污染、拥堵和犯罪压力压低。
- 游客数由城市吸引力、人口、岗位和地标数量计算，旅游收入叠加大型地标收益后进入月度预算和净收支。
- 商品需求由人口、商业/混合商业岗位、游客和混合街区计算；商品供给由工业岗位、外部连接、资源加工园和货运铁路站产生，并受货运覆盖、货运满载率、水电可靠性、劳动力素质和资源适配影响。资源加工园写入 `LocalGoodsSupply`、`ResourcePotential`、`ResourceSpecialization` 和 `IndustrialSpecialization`：`ResourcePotential` 从丘陵、工业地块和货运可达性汇总，`ResourceSpecialization` 把资源潜力经水电、货运和人才折算成本地供给，`IndustrialSpecialization` 把资源适配转化为工业需求、税收质量和城市评分收益。货运铁路站写入 `FreightImportSupply`，配送中心写入 `GoodsStorage` 和 `SupplyChainStability`，并通过仓储缓冲在短缺时补足部分可用供给；商品短缺或本地资源适配不足会压低商业需求、幸福度和城市评分，同时推高工业补供需求；供应链稳定、资源适配和平衡良好会带来少量需求、税收和城市评分收益。
- 劳动力素质由教育覆盖、高等教育覆盖、办公岗位、研发园区、就业规模、升级建筑和地价提高，由污染和犯罪压力压低；用工缺口由岗位数超过可就业人口时产生，并由更高人才水平缓解。
- 创新能力由接路研发园区、高等教育覆盖、通信覆盖/满载率、劳动力素质、水电可靠性和办公岗位计算；它会提高企业效率、生产率奖金、税收质量、办公/工业/商业/混合需求和城市评分，并驱动“缺少研发园区”“研发配套不足”告警。
- 生产率奖金由就业人口、劳动力素质、高等教育覆盖、货运覆盖、企业效率、创新能力和办公岗位计算，进入月度预算；用工缺口会压低幸福度、城市评分、办公/商业/工业/混合需求，并推高住宅需求。
- 住岗平衡由可就业人口和岗位数差距计算；通勤效率由公交覆盖、公交可靠性、候车压力、路网连通性、住岗平衡、混合街区、主干道、拥堵、道路瓶颈和未接路建筑共同计算；汽车依赖由通勤效率、公交覆盖、公交可靠性、候车压力、混合街区、拥堵和住岗平衡共同计算。
- 通勤效率会提高城市评分和住宅/商业/办公/工业/混合需求；汽车依赖会压低幸福度、城市评分和部分发展需求。
- `ComputeParkingPressure` 会按汽车依赖、商业/办公出行、会展客流、路网、主干道、公交、混合街区、用地效率、邻里停车楼覆盖/容量、停车收费和拥堵计算停车压力；停车收费在人口 >= 140 且道路 >= 8 时形成停车收费收入，并在公交覆盖、路网连通和停车覆盖足够时轻微降低汽车依赖与停车压力；压力高时会通过 `ParkingSearchRoadLoad` 增加找车位绕行，压低幸福度、城市吸引力、商业/办公/混合需求，并驱动“停车压力偏高”告警、“停车设施不足/满载”告警、“会展交通承压”告警、“停车收费阻力”告警、“低车依赖”、“停车调度”和“停车收费”里程碑。
- 步行可达性由路网连通性、公交覆盖、综合服务、公园覆盖、用地效率、混合街区、汽车依赖和拥堵共同计算；步行可达性会影响幸福度、城市评分、住宅/商业/办公/混合需求、服务需求、告警和“步行城市”里程碑。
- 环境质量由公园覆盖、回收覆盖、污水处理可靠性、雨洪韧性、公交覆盖、污染、噪声、内涝风险和汽车依赖计算；噪声压力由建筑/道路噪声、拥堵、汽车依赖、公交覆盖和公园覆盖计算。
- 环境质量和噪声压力会影响幸福度、城市评分、住宅/商业/办公/混合需求和公共服务需求。
- 公共健康由医疗覆盖、医疗响应、灾备、生命关怀、环境质量、回收覆盖、污水处理可靠性、雨洪韧性、水电可靠性、污染、噪声压力和内涝风险计算；健康风险由公共健康、灾备、污染、噪声压力、水电短缺、污水过载、内涝风险、病患积压和死亡压力计算。
- 公共健康和健康风险会影响迁入速度、幸福度、城市评分、住宅/商业/办公/混合需求和公共服务需求。
- `ComputeLivingCondition` 会综合服务覆盖、服务公平、公园、教育、生命关怀、公交覆盖、候车压力、通勤效率、步行、居住成本、治安、环境、公共健康、健康风险、噪声、道路瓶颈、停车压力和水电可靠性生成 `LivingCondition`；`ComputeLivingPressure` 会把低宜居、居住成本、治安、健康风险、噪声、道路瓶颈、候车压力和服务不均合成为 `LivingPressure`。人口达到 160 后宜居度低于 45 触发“宜居度偏低”，人口达到 220 后生活压力高于 60 触发“生活压力偏高”；达成人口 250、宜居度 65 且生活压力不高于 35 可完成 `livable_district`。
- 社区诊所和区域医院按服务图层覆盖医疗敏感建筑。`HealthCapacityForBuildings` / `HealthBuildingCapacity` 汇总医疗容量，`HealthUtilization` 计算医疗满载率，`ComputeMedicalResponse` 合成医疗覆盖、容量可靠性、路网、拥堵、断头路和应急响应，`ComputePatientBacklog` 生成病患积压；`HealthLoad`、`HealthCapacity`、`HealthUtilization`、`MedicalResponse` 和 `PatientBacklog` 进入 HUD、服务需求、公共健康、健康风险、告警和 `healthcare_capacity` 里程碑。容量不足触发“医疗容量不足”，响应偏低触发“医疗响应偏低”，病患积压偏高触发“病患积压偏高”。
- 社区学校和社区学院按服务图层覆盖教育敏感建筑并形成学位容量；`EducationCapacityForBuildings` / `EducationBuildingCapacity` 汇总学校与社区学院容量，`EducationLoad` 汇总人口、岗位、办公和工业带来的学习需求，`EducationUtilization` 计算学位满载率，`ComputeStudentBacklog` 生成入学积压，`ComputeLearningPipeline` 合成教育覆盖、高等教育、容量可靠性和服务可靠性；`EducationLoad`、`EducationCapacity`、`EducationUtilization`、`StudentBacklog` 和 `LearningPipeline` 进入 HUD、服务需求、劳动力素质、生产率、商业/办公/工业需求、告警和 `education_capacity` 里程碑。容量不足触发“教育容量不足”，积压偏高触发“入学积压偏高”，学习通道偏弱触发“学习通道偏弱”。
- 消防覆盖按建筑风险权重统计，工业、污染、噪声和高岗位建筑风险更高；覆盖不足会压低幸福度、评分和工业需求。消防韧性由 `ConnectedFireBuildings` 收集接路消防站，`FireRiskForBuilding` 汇总建筑风险，`FireCapacityForBuildings` / `FireBuildingCapacity` 汇总消防容量，`ApplyFireProtectionTileAccess` 写入 `FireProtectionAccess`，`ComputeFireRisk` 与 `ComputeFireResponse` 生成 `FireRisk`、`FireProtection`、`FireLoad`、`FireCapacity`、`FireUtilization` 和 `FireResponse`；缺口提示“缺少消防覆盖”“消防容量不足”“火灾风险偏高”，达标后完成 `fire_resilience` 里程碑。
- 犯罪压力由失业、居住成本、拥堵、警务覆盖、警务响应和案件积压共同计算；压力过高会压低幸福度、城市评分、住宅/商业需求，并提高公共服务需求。
- 公交站、轨道交通站和城际枢纽按半径覆盖住宅容量和岗位，并提供公交运力容量；本轮不新增客运建筑类型。轨道交通站解锁更晚、成本和维护更高，但覆盖半径与容量更大；城际枢纽额外提供外部连接。覆盖内乘客负载超过容量时，`TransitReliability` 降低，有效公交覆盖会按可靠性打折，溢出的出行重新推高道路负载和拥堵，并写入公共交通热力图；`ComputeTransitWaitPressure` 使用原始公交覆盖作为启用门槛，并综合有效覆盖折损、`TransitUtilization`、`TransitReliability`、拥堵、路网连通性和服务可靠性生成 `TransitWaitPressure`。人口达到 120 后，候车压力会压低通勤效率、幸福度和城市评分，推高汽车依赖与服务需求；可靠性偏低触发“公交可靠性偏低”，候车压力偏高触发“公交候车压力偏高”，达标后完成 `transit_reliability`。中后期过载且未建轨道交通站会触发“缺少轨道交通”告警。
- 货运站按半径覆盖商业和工业建筑，并提供货运容量；配送中心和货运铁路站作为中后期物流设施，分别提供仓储缓冲/供应链稳定和更高货运容量/铁路导入，且货运铁路站不计入客运外部连接。覆盖内商业/工业减少道路负载、提高建筑税收质量，并写入货运热力图。资源加工园和配送中心使用货运图层预览；资源加工园不新增建筑，选址在丘陵、工业地块和货运可达区域时提高 `ResourcePotential`，再把货运覆盖、水电可靠性和劳动力素质折算成本地商品供给、资源适配与产业专精。货运负载超过容量时，有效货运覆盖按可靠性下降，溢出的货流重新推高道路负载；本地资源适配不足、仓储调度或铁路导入不足会触发对应物流告警。
- 通信枢纽按半径覆盖住宅、商业、办公、混合用地和工业活动，并提供通信容量；研发园区选择通信图层作为预览图层。覆盖内建筑减少少量交通压力，提高企业效率、生产率奖金、创新能力和税收质量，并写入通信热力图。通信负载超过容量时，有效通信覆盖按可靠性下降，触发通信容量告警并压低商业/办公需求。
- `post_office` 邮政局按半径覆盖住宅、商业、办公、混合用地、工业和地标建筑的邮件需求。`ConnectedMailBuildings` 收集已接路邮政建筑，`MailWeightForBuilding` 计算需求权重，`MailCapacityForBuildings` 汇总 `MailBuildingCapacity`，`ApplyMailTileAccess` 写入 `MailAccess`，`IsMailBuilding` 和 `IsMailSensitiveBuilding` 区分邮政设施与邮件敏感建筑；`MailCoverage`、`MailLoad`、`MailCapacity`、`MailUtilization` 和 `MailReliability` 进入 HUD、服务需求、税收质量、少量交通减压、告警和 `mail_service` 里程碑。覆盖不足触发“缺少邮政服务”，容量不足触发“邮政容量不足”，可靠性或覆盖偏低触发“邮件配送受阻”。
- `memorial_garden` 纪念花园按服务图层覆盖生命关怀敏感建筑。`ConnectedDeathcareBuildings` 收集已接路生命关怀建筑，`DeathcareWeightForBuilding` 计算需求权重，`DeathcareCapacityForBuildings` / `DeathcareBuildingCapacity` 汇总生命关怀容量，`ApplyDeathcareTileAccess` 写入 `DeathcareAccess`，`IsDeathcareBuilding` 和 `IsDeathcareSensitiveBuilding` 区分生命关怀设施与敏感建筑；`ComputeMortalityPressure` 生成 `DeathcareCoverage`、`DeathcareLoad`、`DeathcareCapacity`、`DeathcareUtilization` 和 `MortalityPressure`，并进入 HUD、服务需求、公共健康、健康风险、告警和 `deathcare_ready` 里程碑。覆盖不足触发“缺少生命关怀”，容量不足触发“生命关怀容量不足”，死亡压力偏高触发“死亡压力偏高”。
- `police_precinct` 警署按服务图层补足中后期警务容量。`SecurityCapacityForBuildings` / `SecurityBuildingCapacity` 汇总警务容量，`SecurityUtilization` 计算警务满载率，`ComputePoliceResponse` 合成路网、拥堵、覆盖和容量可靠性，`ComputeCaseBacklog` 生成案件积压；`SecurityLoad`、`SecurityCapacity`、`SecurityUtilization`、`PoliceResponse` 和 `CaseBacklog` 进入 HUD、服务需求、治安压力、告警和 `police_readiness` 里程碑。容量不足触发“警务容量不足”，响应偏低触发“警务响应偏低”，案件积压偏高触发“案件积压偏高”。
- 道路养护站按半径写入道路养护热力图；覆盖率受服务预算修正，并进入 HUD、维护状态、事故风险、道路安全、幸福度、评分和需求。
- 邻里停车楼按半径覆盖住宅、商业、办公、混合用地和工业停车需求，提供停车容量并写入停车热力图；覆盖内建筑减少少量交通生成，容量不足时有效覆盖下降并触发停车设施告警。
- 雨水花园按半径写入雨洪热力图并提供雨洪容量；公园覆盖和完整街道可降低雨洪负荷，容量不足或内涝风险偏高会触发雨洪告警并影响环境、健康、幸福度和评分。
- 回收处理站和垃圾发电厂按半径覆盖建筑垃圾负荷，并提供回收容量；垃圾发电厂额外提供供电，但增加污染、噪声、用水、维护费和交通压力；垃圾负荷超过容量时，回收可靠性下降、有效覆盖受限，并增加污染、噪声、设施需求和幸福度惩罚，同时写入 HUD、告警、里程碑和回收热力图。
- 住宅、商业、混合用地、办公和工业建筑按年龄、地价、公交可达性、接路状态、单栋区位品质和全城发展品质自然升级，升级后提高容量/岗位/税值/维护和可视高度；办公楼额外受教育覆盖、劳动力素质和警务覆盖推动。
- 居住成本压力由住房占用率、平均地价、税率、服务覆盖、公交覆盖和住宅余量计算；压力过高会降低幸福度、迁入速度和城市评分，并在 HUD 底部显示。
- `HOUSING_AFFORDABILITY_ADVISOR` 不改变居住成本公式、自然开发规则或政策效果，只在指标重算后解释住房负担来源；它可以把租金压力、住宅容量缺口、高密/混合供给、地价/税率、公交与服务公平、宜居/生活压力、住岗平衡和保障住房政策状态组合成一个右侧 insight，但不得新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2、SharedArrayBuffer 或 `miniprogram/game.json` 字段。
- `ECONOMIC_SPECIALIZATION_ADVISOR` 不改变经济、需求、旅游、商品、物流、自然开发或里程碑公式，只在指标重算后解释当前最值得放大的经济线；它可以把企业效率、创新、高教/人才、办公岗位、产业/资源专精、本地供给、商品平衡、供应链稳定、物流覆盖/满载、吸引力、游客、旅游收入、混合商业和区域连接组合成一个右侧 insight，但不得新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2、SharedArrayBuffer 或 `miniprogram/game.json` 字段。
- 道路负载来自住宅容量、岗位和建筑交通生成值，负载超过分级道路容量会推高拥堵。`ComputeIntersectionDelay` 会把交叉口密度、断头路、拥堵、主干道和路网连通性合成路口延误，`ComputeRoadBottleneckPressure` 会把拥堵、连通缺口、断头路、复杂交叉口和延误合成为道路瓶颈；瓶颈会回灌少量拥堵，压低通勤效率、幸福度和城市评分，并推高服务需求。信号优化和拥堵收费通过 `PolicyAdjustedIntersectionDelay` 缓解延误。
- 预算由居民税、岗位税、建筑税值、生产率奖金、企业效率奖金、创新税收奖金、行政税收质量奖金、旅游收入和停车收费收入组成，按当前税率倍率计算后，扣除建筑维护费、道路维护费、债务服务费和政策净支出；外部连接会提高游客与少量外部商品供给，行政效率会降低正向政策成本，拥堵收费和停车收费可让政策项形成收入，市政债券会提供一次性现金并在月结中逐步偿还本金。
- 税率有低/标准/高三档，分别影响税收倍率、幸福度和住宅/商业/混合用地/办公/工业需求，并随存档保存。
- 市政服务预算有紧缩/标准/加码三档，按 80%/100%/125% 调整公共服务、医疗响应、教育容量/学习通道、应急避难、生命关怀、警务响应、基础设施、污水、雨洪、公交、货运、资源加工、仓储、货运铁路、通信、邮政、道路养护、停车、回收和垃圾发电输出，同时按同档位调整对应建筑维护开支；紧缩会推高服务需求和低覆盖告警，加码会改善服务效率但更容易造成赤字。
- 需求分为住宅、商业、混合用地、办公、工业、服务和基础设施七类。
- 当前目标/里程碑面板先显示原有目标 hint，再追加 `OBJECTIVE_ACTION_ADVICE` 生成的“建议：...”短提示。建议生成只读取当前未完成里程碑 id 和城市指标：`balanced_service`/均衡服务优先显示主要服务缺口来源；交通类目标根据断头路、主干道数量、公交覆盖/可靠性/候车压力提示打通断头路、升级主干或补公交；财政类目标根据现金缓冲、月净收支、税基、债务压力和债务服务提示控预算、扩税基或处理债务；医疗、教育、警务、消防等专项目标根据容量利用率、积压、覆盖和响应提示补对应容量、覆盖或响应。该流程只改变目标面板文案，不新增按钮、不增加 33 个底部状态格，也不改变 38 类建筑或 48 个工具按钮。
- 风险预测顾问在现有目标/警报/预览或顶部财政文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以与 `ALERT_PRIORITY_DIGEST` 共用风险排序输入，但不得裁剪 `Metrics.Alerts`，也不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 预算拆解顾问在现有目标/警报/顶部财政文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以复用财政信用、月净收支、债务压力、服务预算、维护费、公共服务容量和基础设施容量等指标，但不得新增 HUD 控件、不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 片区优先级顾问在现有目标/警报文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以复用交通瓶颈、服务公平、居住成本、财政、基础设施、公共安全、供应链和宜居环境等指标，但只在优先级偏高或有风险时显示，不得新增 HUD 控件、不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 道路层级顾问在现有目标/警报文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以复用道路层级、连通性、路口延误、瓶颈、拥堵、公交候车/运力、停车、事故和养护等指标，但只在压力偏高或有风险时显示，不得新增 HUD 控件、不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 住房负担/宜居迁入顾问在现有目标/警报文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以复用租金压力、住宅容量/人口缺口、高密住宅/混合用地供给、地价/税率、公交、服务公平、宜居/生活压力、住岗平衡和保障住房政策等指标，但不得新增 HUD 控件、不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 城市事件摘要在现有目标/警报文案中展示，不作为新的里程碑、工具按钮或底部状态项；它可以复用 `ALERT_PRIORITY_DIGEST` 附近的紧凑文本样式，但不得裁剪 `Metrics.Alerts`，也不得改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 需求驱动分析在现有目标/警报/需求文案中展示，不作为新的里程碑、工具按钮或底部状态项；它只解释七类需求的当前最高压力、驱动原因和下一步建议，不改变 38 类建筑、48 个工具按钮、33 个底部状态格或 `miniprogram/game.json`。
- 里程碑覆盖路网、连通路网、`traffic_flow` 交通流线、道路养护、安全道路、财政信用、偿债纪律、市政中心、区域门户、人口、分区生长、紧凑用地、优质片区、功能缓冲、高密住区、混合核心、知识经济、创新高地、城市吸引力、会展客流、商品市场、本地供给、`specialized_industry` 产业专精、供应链缓冲、铁路货运、人才城市、高等教育、`education_capacity` 学位容量、步行城市、顺畅通勤、低车依赖、停车调度、完整街道、信号优化、拥堵收费、停车收费、绿色宜居、`livable_district` 宜居街区、健康城市、区域医疗中心、`healthcare_capacity` 医疗容量、灾害准备、基础设施平衡、水电韧性、清洁电力、水环境、雨洪韧性、城市运维、服务圈、公共服务容量、均衡服务、应急响应、消防网络、`fire_resilience` 消防韧性、`deathcare_ready` 生命关怀、平安街区、`police_readiness` 警务响应、公交运力、`transit_reliability` 公交可靠性、轨道骨架、货运循环、货运运力、清洁街区、回收容量、资源回收能源和财政健康。
- 模拟可暂停，或以 1x/2x/4x 倍速推进。
- 城市政策会改变污染、噪声、道路容量、交叉口拥堵、路口延误、道路瓶颈、事故风险、道路安全、步行可达性、汽车依赖、停车压力、雨洪负荷、人口增长、公交压力、居住成本、需求和月度政策收支；停车收费会在人口与道路规模达标后增加政策收入，公交替代不足且停车压力仍高时触发“停车收费阻力”；行政效率会影响政策净支出。
- 切换政策时，模拟层应以切换前快照和切换后重算结果生成 `PolicyImpactPreview`，供 Runtime HUD 在右侧预览面板显示启用/关闭与关键 delta；该流程不得改变九项政策按钮数量、38 类建筑数量、48 个工具按钮数量或 33 个底部状态格数量。
- `CitySaveData` 记录版本、日期、现金、债券本金、税率、服务预算、解锁项、分级道路、分区、建筑、自动开发标记和启用政策；读取后重算服务覆盖、行政效率、外部连接、拥堵、污染、地价、资源潜力、资源适配、产业专精、需求、税收、预算开支、债务服务和政策收支。

## Unity 数据
`CityConfig` 是 ScriptableObject，包含地图、经济、预算周期、道路成本、分区成本、幸福度惩罚和建筑配置。默认建筑包括住宅舱、公寓楼、街角商铺、混合街区、共享办公楼、研发园区、制造工坊、资源加工园、口袋公园、城市广场、会展中心、市政厅、社区诊所、区域医院、应急避难中心、纪念花园、社区学校、社区学院、社区消防站、社区警务站、警署、通信枢纽、道路养护站、邻里停车楼、雨水花园、街区公交站、轨道交通站、城际枢纽、货运站、配送中心、货运铁路站、微型电站、太阳能阵列、净水塔、污水处理站、垃圾发电厂和回收处理站。选址诊断和目标行动建议都不新增默认建筑定义，生成器和 verify 仍以 38 个基础建筑为准。

## 微信入口
`miniprogram/` 是导出目录。当前占位 `game.js` 只提示 Unity 构建未生成；正式版本必须用 Unity/团结微信小游戏转换产物覆盖。

## 平台桥
`WeChatMiniGameBridge` 在 WebGL 构建中调用 `Assets/Plugins/WebGL/WeChatBridge.jslib`。当前桥接包含分享、震动、`wx.setStorageSync`、`wx.getStorageSync` 和 `wx.removeStorageSync`；编辑器环境使用 `PlayerPrefs` fallback，便于本地 Play Mode 验证。

## 当前限制
当前环境未检测到可用的 Unity/Unity Hub 命令，无法在当前环境执行 Unity Editor 编译。下一次在 Unity 中打开项目后，需要完成 Console 编译检查、默认配置生成和微信小游戏转换导出。

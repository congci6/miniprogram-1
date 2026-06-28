# 口袋城市规划师 微信小游戏版

这是一个非 Unity 的微信小游戏工程。当前上线路径使用 TypeScript 共享模拟核心，并为微信小游戏生成 Canvas 2D runtime；`unity/` 只保留为历史迁移参考，不再作为当前架构目标。

## 当前状态
- `browser/` 是当前活跃开发工程，使用 Phaser + TypeScript + Vite 做浏览器调试版本。
- `browser/src/wechat/main.ts` 是微信小游戏 Canvas 2D 入口，不依赖 DOM、Phaser 或 Unity。
- `miniprogram/game.js` 由 `npm run build:wechat` 生成，包含可启动的非 Unity Canvas runtime，不再是 Unity 占位提示。
- `unity/` 和旧文档中的 Unity 内容只作为历史实现参考；后续功能应优先迁移到 `browser/src` 的共享模拟与微信 runtime。

## 已有玩法核心

当前非 Unity runtime 已具备：等距城市网格、确定性水域/丘陵地形、住宅/商业/工业分区、程序化分区建筑标记、道路铺设、道路升级、道路容量、功能缓冲/用地冲突、用地效率/紧凑开发、清理工具、现金消耗、人口增长、道路覆盖、公共服务建筑、公园/医疗/教育覆盖、污染、拥堵、幸福度、城市评分、城市等级经验与解锁、地块检查、材料仓库、工厂生产队列、离线生产结算、城市订单交付、住宅材料升级、城市目标奖励、目标行动建议、微信本地存档、安全生命周期反馈和横屏 Canvas HUD。下面的大量系统来自历史 Unity 方案和产品规划，尚未全部迁移到当前微信 Canvas runtime。
- 网格地图、地形、水面和山地限制。
- 道路铺设、普通路/主干道升级、道路容量、道路负载、拥堵、断头路、交叉口、路网连通性、`IntersectionDelay` 路口延误和 `RoadBottleneckPressure` 道路瓶颈指标。
- 38 类住宅、商业、混合用地、办公、工业、服务和基础设施建筑；中后期可解锁高容量公寓楼、研发园区、资源加工园、配送中心、货运铁路站、市政厅、区域医院、`emergency_shelter` 应急避难中心、`memorial_garden` 纪念花园、`police_precinct` 警署、通信枢纽、`post_office` 邮政局、道路养护站、邻里停车楼、雨水花园、太阳能阵列、垃圾发电厂、会展中心和城际枢纽，缓解住房紧张、建立创新能力、补强本地资源适配、建立仓储缓冲、打开铁路货运、建立行政效率与容量、补强医疗覆盖、提高灾备、提供生命关怀、提升警务响应、提升企业效率、补足邮件配送、降低事故风险、治理停车压力并提升雨洪/清洁电力/资源回收/地标客流/外部连接韧性。
- 教育容量本轮不新增建筑，继续由社区学校和社区学院承担；两类教育设施会从覆盖扩展为学位容量、入学积压和学习通道口径。
- 行政容量本轮不新增建筑，继续由既有市政厅承担；市政厅会从行政效率扩展为 `AdministrationLoad`、`AdministrationCapacity`、`AdministrationUtilization` 和 `PolicyBacklog`，接入 HUD 顶栏、政策成本、幸福度、城市评分、服务需求、告警和 `administration_capacity` 里程碑。
- 公交可靠性本轮不新增建筑，继续由街区公交站、轨道交通站和城际枢纽承担；三类客运设施会从公交覆盖/运力扩展为 `TransitReliability`、`TransitWaitPressure` 和 `ComputeTransitWaitPressure` 口径，接入通勤效率、汽车依赖、幸福度、城市评分、服务需求、告警和 `transit_reliability` 里程碑，可靠性偏低或候车压力偏高时提示“公交可靠性偏低”“公交候车压力偏高”。
- 住宅/商业/混合用地/办公/工业/公共服务/基础设施分区。
- 需求驱动的分区自然开发：空置住宅/商业/混合用地/办公/工业分区会在接路、现金和分区适宜度允许时自动长出建筑。
- 分区适宜度：住宅/商业/混合用地/办公/工业会按地价、服务、公交通勤、污染噪声、治安和物流条件评价，拖拽分区预览会显示适宜度。
- 建筑选址诊断：建筑预览会显示 `BuildingSiteScore` / `SiteDiagnosis` 和中文“选址诊断”，按建筑类型解释地价、污染/噪声、道路接入、推荐分区/适宜度，以及公交、物流、通信、邮政、停车、雨洪和服务可达性带来的优势或短板；该增强不新增建筑、不新增工具按钮，38/48/33 数量口径不变。
- 建筑视觉 prefab 库：`BUILDING_VISUAL_PREFAB_LIBRARY` 只属于 Unity 渲染层，按 `ModelKey`/建筑类型生成低多边形程序外观；38 个建筑都有 fallback，不新增建筑数量、不修改 `miniprogram/game.json`，也不使用 worker。
- 成长瓶颈顾问：`GROWTH_BOTTLENECK_ADVISOR` 只复用现有城市指标，输出 `GrowthBottleneckScore`、`GrowthBottleneckFocus`、`GrowthBottleneckDriver` 和 `GrowthBottleneckAction`；它把住房、财政、通勤、服务、公用设施、就业、供应链和宜居问题压缩成一条右侧目标洞察，不新增按钮、建筑、worker 或 HUD 状态格。
- 建筑升级准备度顾问：`BUILDING_UPGRADE_READINESS_ADVISOR` 复用单栋建筑自然升级逻辑，按住宅/商业/办公/工业的年龄门槛、升级分、地价、公交、接路、服务覆盖、物流、教育/高教、劳动力、污染/噪音等判断升级机会或阻塞；输出 `BuildingUpgradeReadinessScore`、`BuildingUpgradeReadyCount`、`BuildingUpgradeBlockedCount`、`BuildingUpgradeReadinessFocus`、`BuildingUpgradeReadinessDriver` 和 `BuildingUpgradeReadinessAction`，由 HUD 的 `BuildingUpgradeReadinessText` 进入右侧 `ObjectiveInsightParts` / insight stack，显示为“升级:候/阻 ... -> ...”类短句；不新增建筑数量、按钮、底部 HUD 统计槽、workers、TS、Vite、WebGL2 或 SharedArrayBuffer。
- 住房负担/宜居迁入顾问：`HOUSING_AFFORDABILITY_ADVISOR` 复用 `RentPressure`、住宅容量/人口缺口、住宅分区与混合用地/高密住宅建筑、平均地价/税率、公交覆盖、服务公平、`LivingCondition`/`LivingPressure`、住岗平衡和“保障住房”政策，输出 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver` 与 `HousingAffordabilityAction`；HUD 生成 `HousingAffordabilityText` 进入 `ObjectiveInsightParts`，显示为“住房: ... -> ...”类短句；不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- 经济专精顾问：`ECONOMIC_SPECIALIZATION_ADVISOR` 复用 `BusinessEfficiency`、`InnovationCapacity`、`OfficeJobs`、`WorkforceSkill`、`AdvancedEducationCoverage`、`IndustrialSpecialization`、`ResourceSpecialization`、`LocalGoodsSupply`、`GoodsBalance`、`SupplyChainStability`、`LogisticsCoverage`/`LogisticsUtilization`、`Attractiveness`、`Visitors`、`TourismIncome`、`MixedUseBuildings` 和 `RegionalConnectivity` 等既有指标，输出 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver` 与 `EconomicSpecializationAction`；HUD 生成 `EconomicSpecializationText` 进入 `ObjectiveInsightParts`，显示为“经济:专... -> ...”类短句，用于说明当前最适合推进资源工业、物流供应链、办公创新、旅游会展或混合商业中的哪条经济线；不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- 地块检查器与图层图例：`TILE_INSPECTOR_OVERLAY_LEGEND` 只增强 HUD/交互；当前图层应显示简短图例，玩家点击或悬停地块时，右侧检查器显示该地块的分区、建筑/道路、当前图层数值和诊断摘要；不新增建筑、按钮、政策或 worker，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 政策效果反馈：点击任一城市政策按钮后，右侧预览面板会显示 `PolicyImpactPreview`，标明本次切换是启用还是关闭，并即时列出月收支、拥堵、停车压力、步行可达性、事故风险、雨洪韧性/内涝风险、政策积压等关键 delta；该反馈复用既有九项政策按钮，不新增按钮，也不改变 38/48/33 数量口径。
- 目标行动建议：`OBJECTIVE_ACTION_ADVICE` 会在当前目标/里程碑面板的原目标 hint 后追加简短“建议：...”行动提示，由当前未完成里程碑 id 和城市指标生成；均衡服务会提示补主要服务缺口来源，交通目标提示打通断头路、升级主干或补公交，财政目标提示控预算、扩税基或处理债务，医疗/教育/警务/消防等专项目标提示补对应容量或响应；该能力不新增按钮、不增加 HUD 状态格，也不改变 38/48/33 数量口径。
- 警报优先摘要：`ALERT_PRIORITY_DIGEST` 只在 HUD 视图层整理右侧警报栏，按严重度排序并最多显示少量最关键告警，末尾用 `+N` 表示还有更多；底层 `Metrics.Alerts` 仍保留完整告警列表。优先级倾向现金、赤字、水电、污水、雨洪、医疗、消防、警务、灾害、交通和服务缺口等高风险事项；该能力不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 风险预测顾问：`RISK_FORECAST_ADVISOR` 复用现有 HUD 文案行、右侧警报/目标提示和顶部财政信息，输出 `ForecastRisk`、`ForecastFocus`、`ForecastAction` 与 `CashRunwayDays`；核心实现名可用 `RiskForecastAdvisor` 或 `ComputeForecastRisk`。该能力用于提示现金续航、财政、基础设施、服务和交通的近期风险，不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 预算拆解顾问：`BUDGET_BREAKDOWN_ADVISOR` 作为预算压力拆解/财政顾问文案口径，输出 `BudgetStress`、`BudgetFocus`、`BudgetDriver` 与 `BudgetAction`；核心实现名可用 `BudgetBreakdownAdvisor` 或 `ComputeBudgetBreakdown`。它根据现金/赤字、债务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收等现有指标判断主要财政压力来源，并给出一条短行动建议；HUD 只复用现有目标/警报/财政文案区域，不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 片区优先级顾问：`DISTRICT_PRIORITY_ADVISOR` 作为片区/系统优先级顾问口径，输出 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver` 与 `DistrictPriorityAction`；核心实现名可用 `DistrictPriorityAdvisor` 或 `ComputeDistrictPriority`。它基于现有指标选择当前最需要治理的片区或系统优先级，覆盖交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境等，并给出一条短行动建议；HUD 只在优先级偏高或有风险时复用现有目标/警报文案区域显示，不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 道路层级顾问：`ROAD_HIERARCHY_ADVISOR` 作为道路层级/瓶颈升级顾问口径，输出 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver` 与 `RoadHierarchyAction`；核心实现名可用 `RoadHierarchyAdvisor` 或 `ComputeRoadHierarchyAdvice`。它基于现有道路/交通指标选择当前最该处理的交通层级问题，覆盖主干道不足、断头路、路网连通不足、路口延误、道路瓶颈、拥堵、公交候车/运力、停车压力、事故/养护等，并给出一条短行动建议；HUD 只在压力偏高或有风险时复用现有目标/警报文案区域显示，不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 通勤走廊顾问：`COMMUTE_CORRIDOR_ADVISOR` 复用既有通勤、公交、停车、道路、货运和外部连接指标，输出 `CommuteCorridorScore`、`CommuteCorridorFocus`、`CommuteCorridorDriver` 与 `CommuteCorridorAction`；它把住岗平衡、通勤效率、汽车依赖、公交覆盖/可靠性/候车压力、停车压力、路网连通、道路瓶颈、货运满载和区域连接压缩成一条右侧 `CommuteCorridorText` 洞察，作为 `ObjectiveInsightParts` 候选显示，不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer。
- 城市事件摘要：`CITY_EVENT_DIGEST` 汇总 `RecentEvents` / `EventDigest`，由 `AddCityEvent`、`PushCityEvent` 和 `BuildEventDigestText`（或同义实现名）形成短文本，复用现有 HUD 目标/警报文案区域展示近期操作和系统事件；该能力不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 需求驱动分析：`DEMAND_DRIVER_ANALYSIS` 读取 `DemandFocus`、`DemandDriver`、`DemandAction` 与 `DemandUrgency`，解释当前最高需求来自住房、商品、通勤、人才、物流、服务缺口或基础设施容量等哪类压力，并复用现有 HUD 目标/警报/需求文案给出下一步行动；该能力不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 服务短板顾问：`SERVICE_GAP_ADVISOR` 作为右侧目标区的服务短板建议口径，输出 `ServiceGapAdvisorScore`、`ServiceGapAdvisorFocus`、`ServiceGapAdvisorDriver` 与 `ServiceGapAdvisorAction`；核心实现名可用 `ServiceGapAdvisor` 或 `ComputeServiceGapAdvisor`。它基于既有 clinic/school/fire/police/park 覆盖，以及 education、health、safety、fire risk 等服务与风险指标，选择当前最该补齐的医疗、教育、消防、警务、公园或安全短板，并给出一条短行动建议；HUD 只复用右侧目标/警报文案区域，并进入 `ObjectiveInsightParts` 优先栈，不新增按钮、不增加 HUD 状态格，不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 洞察优先栈：`HUD_INSIGHT_PRIORITY_STACK` / `ObjectiveInsightParts` 是右侧目标/警报文案的洞察优先栈，不是新功能按钮，也不增加 HUD 状态格；它把 `RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`SERVICE_GAP_ADVISOR`、`BUILDING_UPGRADE_READINESS_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`DEMAND_DRIVER_ANALYSIS`、`CITY_EVENT_DIGEST` 等现有顾问信息作为候选，`ObjectiveHint` 保持第一优先级，其余 insight 按风险、压力和事件重要性排序并限量显示少量最高优先级条目，降低横屏右侧拥挤；不改变 38/48/33，也不修改 `miniprogram/game.json`。
- 微信安全生命周期反馈：`WECHAT_SAFE_LIFECYCLE_FEEDBACK` 已迁移到当前非 Unity 微信 Canvas runtime；切后台/暂停时通过 `onHide` 安全自动保存，回到前台时通过 `onShow` 安全读档并结算离线进度，关键城市命令和保存成功/失败使用包裹异常的 `vibrateShort` 触觉反馈；当微信 storage 或触觉 API 不可用时只显示短状态并继续当前城市，不新增 worker，也不修改 `miniprogram/game.json`。
- 功能缓冲：`FUNCTIONAL_BUFFER_LAND_USE_CONFLICT` 已迁移到当前非 Unity Canvas runtime；已开发工业贴近住宅、商业或非公园服务会形成用地冲突压力，公园可作为缓冲减免，冲突会影响幸福度、评分、住宅/商业/工业需求、告警、风险/卡点/片区优先级洞察、地块检查诊断和“功能缓冲”目标；不新增建筑、按钮、worker，也不修改 `miniprogram/game.json`。
- 发展品质：`DEVELOPMENT_QUALITY_DISTRICT` 已迁移到当前非 Unity Canvas runtime；已开发建筑会按区位适配、接路、服务覆盖、污染/功能缓冲和建筑成熟度形成发展品质，影响幸福度、评分、需求、告警、风险/卡点/片区优先级洞察、地块检查诊断和“优质片区”目标；建筑年龄随日推进并进入存档，不新增建筑、按钮、worker，也不修改 `miniprogram/game.json`。
- 用地效率：`LAND_USE_EFFICIENCY_COMPACT_DEVELOPMENT` 已迁移到当前非 Unity Canvas runtime；住宅/商业/工业分区会统计已开发面积、空置面积和开发率，紧凑开发会提高城市评分，过量空置分区会触发规划告警、风险/卡点/片区优先级洞察和“紧凑用地”目标；不新增建筑、按钮、worker，也不修改 `miniprogram/game.json`。
- 高密住宅自然开发：`NATURAL_HIGH_DENSITY_RESIDENTIAL` 已迁移到当前非 Unity Canvas runtime；居住压力或住宅需求偏高时，成熟、接路、地价与发展品质达标且已解锁的住宅会逐日自然成长到 2/3 级，提高住房容量、触发住宅升级目标和近期事件；不新增按钮、worker，也不修改 `miniprogram/game.json`。
- 混合用地：`NATURAL_MIXED_USE_CORE` 已迁移到当前非 Unity Canvas runtime；高地价、高服务覆盖、住宅与商业需求都较强时，成熟住宅贴近商业会自然融合为混合核心，同时提供住房与岗位，接入经济专精顾问、服务覆盖、用地冲突、地块检查、浏览器/微信渲染和“混合核心”目标；不新增按钮、worker，也不修改 `miniprogram/game.json`。
- 办公与知识经济：教育、地价、公交、治安和研发园区会推高办公需求，办公岗位进入“知识经济”里程碑，研发园区与高教/通信配套会进入“创新高地”里程碑。
- 城市吸引力与游客经济：城市广场和会展中心会提高地标吸引力，城际枢纽会提高外部连接，吸引游客并带来旅游收入；会展客流会额外推高停车与交通压力。
- 劳动力素质与用工缺口：教育、办公岗位、研发能力和建筑成长会提升人才水平，人才水平进入生产率奖金、岗位需求和城市评分。
- 教育容量：社区学校和社区学院会形成 `EducationLoad`、`EducationCapacity`、`EducationUtilization`、`StudentBacklog` 和 `LearningPipeline`；这些指标接入 HUD、服务需求、商业/办公/工业需求、人才水平和建筑成长，容量不足、入学积压偏高或学习通道偏弱时提示“教育容量不足”“入学积压偏高”“学习通道偏弱”，并进入 `education_capacity` 里程碑口径。
- 通信覆盖与企业效率：通信枢纽覆盖住宅、商业、办公、混合用地和工业活动；研发园区需要高等教育、通信覆盖和水电可靠性支撑创新能力；覆盖不足或容量过载会降低企业效率、税收质量、商业/办公需求和城市评分。
- 邮政服务：`post_office` 邮政局会覆盖住宅、商业、办公、混合用地、工业和地标建筑的邮件需求，形成 `MailCoverage`、`MailLoad`、`MailCapacity`、`MailUtilization`、`MailReliability` 和 `MailAccess`；覆盖不足、容量过载或配送可靠性偏低会触发“缺少邮政服务”“邮政容量不足”“邮件配送受阻”，并进入 `mail_service` 里程碑口径。
- 消防韧性：社区消防站会从现有消防覆盖扩展出 `FireRisk`、`FireProtection`、`FireLoad`、`FireCapacity`、`FireUtilization` 和 `FireResponse`；服务图层使用 `FireProtectionAccess` 表达消防保障热力，容量不足、覆盖缺口或火灾风险偏高时提示“缺少消防覆盖”“消防容量不足”“火灾风险偏高”，并进入 `fire_resilience` 里程碑口径。
- 医疗容量：社区诊所和区域医院会从医疗覆盖扩展出 `HealthLoad`、`HealthCapacity`、`HealthUtilization`、`MedicalResponse` 和 `PatientBacklog`；容量不足、响应偏低或病患积压偏高时提示“医疗容量不足”“医疗响应偏低”“病患积压偏高”，并进入 `healthcare_capacity` 里程碑口径。
- 生命关怀：`memorial_garden` 纪念花园会接入服务图层，形成 `DeathcareCoverage`、`DeathcareLoad`、`DeathcareCapacity`、`DeathcareUtilization`、`MortalityPressure` 和 `DeathcareAccess`；覆盖不足、容量过载或死亡压力偏高时提示“缺少生命关怀”“生命关怀容量不足”“死亡压力偏高”，并进入 `deathcare_ready` 里程碑口径。
- 警务响应：`police_precinct` 警署会在社区警务站之上补强治安执法容量，形成 `SecurityLoad`、`SecurityCapacity`、`SecurityUtilization`、`PoliceResponse` 和 `CaseBacklog`；容量不足、响应偏低或案件积压偏高时提示“警务容量不足”“警务响应偏低”“案件积压偏高”，并进入 `police_readiness` 里程碑口径。
- 道路养护、瓶颈与安全：道路养护站覆盖道路网络，养护不足、拥堵、断头路和高风险交叉口会推高事故风险；交叉口密度、断头路、拥堵和主干道骨架会形成路口延误与道路瓶颈，信号优化和拥堵收费可缓解。道路安全、瓶颈和延误会影响幸福度、城市评分、需求、告警和“道路养护/安全道路/交通流线”里程碑。
- 财政信用与债务压力：现金缓冲、月净收支、市政支出、市政债券和行政效率会形成财政信用；赤字、现金不足、负现金和债务本金会推高债务压力，行政效率会改善税收质量并降低政策执行成本，行政满载或政策积压会反向影响幸福度、城市评分、发展需求、服务需求、告警和“财政信用”/“偿债纪律”/`administration_capacity` 里程碑。
- 商品供需与资源适配：工业岗位、外部连接、资源加工园和货运铁路站形成商品供给，配送中心形成仓储缓冲和供应链稳定度，商业、居民和游客形成消费需求；`ResourcePotential` 会从丘陵地形、工业地块和货运可达性估算本地资源潜力，资源加工园把潜力转化为 `ResourceSpecialization` 和本地供给，`IndustrialSpecialization` 再把资源适配转化为工业成长、税收、幸福度和城市评分收益。
- 通勤效率与汽车依赖：住岗平衡、公交覆盖、公交可靠性、低候车压力、城际连接、混合街区、主干道和路网连通性会改善通勤，拥堵、道路瓶颈、公交候车压力和断路建筑会拉高汽车依赖。
- 停车压力：高汽车依赖、商业/办公出行和拥堵会推高找车位压力，公交、路网连通、混合街区、紧凑用地和邻里停车楼会缓解；停车楼还提供覆盖热力、容量和满载率，压力过高会增加绕行交通并压低幸福度、吸引力和商业/办公需求。
- 雨洪韧性：人口、岗位、道路、已开发分区、工业活动和地形暴露会形成雨洪负荷；公园、完整街道和雨水花园会降低内涝风险，风险过高会压低环境质量、公共健康、幸福度和城市评分。
- 步行可达性：连通路网、公交、服务、公园、紧凑用地、混合街区和完整街道政策会形成可步行街区；信号优化、拥堵收费和停车收费可降低拥堵、路口延误、道路瓶颈或停车绕行，并间接改善通勤与可达性。
- 环境质量与噪声压力：污染、噪声、汽车依赖、污水过载和内涝风险会压低宜居度，公园、回收、污水处理、公交、绿色规范、完整街道和雨洪韧性设施能改善环境。
- 公共健康与健康风险：医疗覆盖（社区诊所和区域医院）、医疗响应、灾备、环境质量、回收、污水处理、水电可靠性、雨洪韧性和生命关怀会提高公共健康，污染、噪声、设施短缺、内涝风险、病患积压和死亡压力会带来健康风险。
- 公共服务容量：医疗、教育、消防、警务、应急避难和生命关怀服务会形成服务负载和容量；区域医院能提供更大医疗半径与容量，医疗容量会转化为 `MedicalResponse` 并压低 `PatientBacklog`，学校和社区学院会提供 `EducationCapacity` 并压低 `StudentBacklog`、提高 `LearningPipeline`，应急避难中心能补强灾备，纪念花园会补足生命关怀容量，警署会补强警务容量与响应效率，容量不足会降低有效覆盖并触发扩建压力。
- 服务公平：住宅片区会按公园、医疗、教育、公交、消防、警务、回收、通信、邮政和生命关怀可达性形成均衡度；服务缺口来源按住宅敏感建筑容量加权估算，HUD 会显示服务不足人口与主要服务缺口来源；服务分布不均会压低幸福度、评分和发展需求，并触发“片区服务不均”告警。
- 宜居度：`LivingCondition` 会把服务覆盖与公平、公园、教育、生命关怀、公交、通勤、步行、环境、公共健康和水电可靠性合成为居民生活条件；`LivingPressure` 汇总居住成本、治安、健康风险、噪声、道路瓶颈、候车压力和服务不均，接入 HUD、幸福度、城市评分、住宅/混合/服务需求、告警和 `livable_district` 里程碑。
- 应急响应与灾备：医疗、消防、警务服务、路网连通和低拥堵会提高响应效率；医疗容量会转化为 `MedicalResponse` 并压低 `PatientBacklog`，警署会把警务容量转化为 `PoliceResponse` 并压低 `CaseBacklog`，应急避难中心接入 Services overlay，会把避难容量、应急响应、雨洪韧性、水电可靠性、路网连通和维护状态转化为 `DisasterPreparedness`（灾备），降低 `DisasterRisk`（灾害风险）；断头路、服务过载和未接路建筑会拖慢响应。
- 城市运维：现金缓冲、服务预算、服务负载、水电负载和拥堵会形成维护状态；维护不足会折损服务可靠性并推高扩建压力。
- 供电、供水、水电可靠性/满载率、污水处理负载/容量/满载率/可靠性、雨洪负载/容量/满载率/韧性、内涝风险、污染、噪声、地价、路网连通性、断头路、路口延误、道路瓶颈、道路养护、事故风险、道路安全、步行可达性、财政信用、行政效率、行政负载/容量/满载率、政策积压、债务压力、债券本金/偿债额、用地效率、空置分区、发展品质、用地冲突、就业、办公岗位、劳动力素质、高等教育覆盖、教育负载/容量/满载率、入学积压、学习通道、创新能力、用工缺口、生产率奖金、商品供需、`ResourcePotential`、`ResourceSpecialization`、`IndustrialSpecialization`、本地供给、铁路导入、仓储容量、供应链稳定、住岗平衡、通勤效率、汽车依赖、停车压力、停车覆盖/容量/满载率、通信覆盖/容量/满载率、邮政覆盖/容量/满载率/可靠性、`HealthLoad`、`HealthCapacity`、`HealthUtilization`、`MedicalResponse`、`PatientBacklog`、`EducationLoad`、`EducationCapacity`、`EducationUtilization`、`StudentBacklog`、`LearningPipeline`、`FireRisk`、`FireProtection`、`FireLoad`、`FireCapacity`、`FireUtilization`、`FireResponse`、`DeathcareCoverage`、`DeathcareLoad`、`DeathcareCapacity`、`DeathcareUtilization`、`MortalityPressure`、`SecurityLoad`、`SecurityCapacity`、`SecurityUtilization`、`PoliceResponse`、`CaseBacklog`、`LivingCondition`、`LivingPressure`、环境质量、噪声压力、公共健康、健康风险、应急响应、灾备、灾害风险、城市维护状态、服务公平、服务不足人口、主要服务缺口来源、居住成本、治安压力、城市吸引力、游客、旅游收入、会展客流目标、外部连接、公共服务容量、公园覆盖、医疗覆盖、教育覆盖、消防覆盖、生命关怀覆盖、警务响应、货运覆盖/运力、回收覆盖/容量/满载率和幸福度。
- 公交站、轨道交通站和城际枢纽覆盖、公交运力容量、公交可靠性、候车压力、外部连接、满载率、公共交通热力图、交通减压和公交覆盖/运力/可靠性/轨道骨架/区域门户里程碑。
- 公交可靠性里程碑：`transit_reliability` 会检查公交覆盖、`TransitReliability` 和 `TransitWaitPressure`；不新增建筑，只引导玩家用既有公交站、轨道交通站和城际枢纽补足可靠客运网络。
- 货运站、配送中心和货运铁路站覆盖、货运运力容量、满载率、仓储缓冲、供应链稳定、铁路导入、资源加工园本地供给、资源适配、产业专精、商业/工业货流减压、工业需求提升和货运覆盖/运力/本地供给/产业专精/供应链缓冲/铁路货运里程碑。
- 连通路网与 `traffic_flow` 目标会鼓励减少断头路、形成交叉路网和主干道骨架，并把道路瓶颈与路口延误压低到可控水平。
- 商品市场里程碑：商品供给达到消费需求的 90%；本地供给里程碑要求建成资源加工园并让商品平衡达到 95%；`specialized_industry`（产业专精）里程碑要求资源加工园吃到丘陵/工业地块/货运可达性的资源适配并形成足够 `IndustrialSpecialization`；供应链缓冲里程碑要求建成接路配送中心并让供应链稳定达到 65。
- 灾害准备里程碑：建成接路应急避难中心，并让灾备达到 65。
- 消防韧性里程碑：`fire_resilience` 会检查消防保障、火灾风险、消防容量利用率和响应效率。
- 医疗容量里程碑：`healthcare_capacity` 会检查医疗容量利用率、医疗响应和病患积压。
- 学位容量里程碑：`education_capacity` 会检查教育覆盖、`EducationUtilization`、`StudentBacklog` 和 `LearningPipeline`。
- 生命关怀里程碑：`deathcare_ready` 会检查生命关怀覆盖、容量利用率和死亡压力。
- 警务响应里程碑：`police_readiness` 会检查警务容量、警务响应和案件积压；警务站/警署覆盖、犯罪压力、治安告警和平安街区里程碑会共同驱动治安服务扩建。
- 宜居街区里程碑：`livable_district` 会在人口 250 后检查宜居度达到 65 且生活压力不高于 35。
- 行政容量里程碑：`administration_capacity` 会检查行政效率、`AdministrationUtilization` 和 `PolicyBacklog`；市政厅不新增建筑类型，只通过既有市政厅容量承接人口与政策数量增长。
- 回收站和垃圾发电厂覆盖、垃圾负荷、处理容量、满载率、清洁街区/回收容量/资源回收能源里程碑和回收覆盖热力图；垃圾发电厂会用更高污染、噪声、用水和维护费换取回收容量与供电。
- 建筑自然升级：住宅、商业、混合用地、办公和工业会根据地价、公交、接路、发展品质和年龄成长到 2/3 级；建筑升级准备度顾问只解释这些既有机会/阻塞，不改变升级规则或建筑数量。
- 居住成本压力：住房紧张、地价过高和高税率会抬高成本，服务、公交和住宅余量可以缓解。
- 每日模拟推进、每 30 天预算结算、税收、生产率奖金、旅游收入、维护费、道路维护费、债务服务费和现金流。
- 低/标准/高税率档位，影响税收、幸福度和住宅/商业/混合用地/办公/工业需求。
- 市政服务预算有紧缩/标准/加码三档，影响服务、水电、污水、雨洪、回收、公交、货运、通信、邮政、生命关怀、道路养护、停车设施和行政效率/容量，以及公共建筑维护开支。
- 污水处理站会提供排水处理容量，过载时推高污染、噪声、水环境风险、健康风险和基础设施需求。
- 雨水花园会提供雨洪容量和覆盖热力，容量不足或内涝风险偏高时会触发告警并推高基础设施需求。
- 社区学院会提供高等教育覆盖并补强教育容量；学校和社区学院共同提高劳动力素质、办公需求、生产率奖金和中后期建筑成长，并触发高等教育与 `education_capacity` 目标。
- 水电韧性、水环境、清洁电力、资源回收能源和雨洪韧性目标会检查水电可靠性、污水处理可靠性、太阳能阵列、垃圾发电厂、雨洪韧性及对应满载率，帮助玩家提前扩建电站、太阳能、水塔、污水处理站、回收设施和雨水花园。
- 紧凑用地目标会鼓励先消化已划分地块，再继续外扩新区。
- 里程碑目标和城市等级；当前目标面板会保留原目标 hint，并追加 `OBJECTIVE_ACTION_ADVICE` 的“建议：...”短提示来引导下一步操作。
- 拆除预览与退款。
- 建筑预览、失败原因与选址诊断。
- 政策效果反馈：城市政策切换后在右侧预览显示本次启用/关闭和关键指标 delta。
- 暂停、1x/2x/4x 模拟倍速和横屏相机拖拽/缩放。
- 本地存档、读档、自动存档，以及微信小游戏 storage 桥接。
- 九项城市政策：绿色规范、公交优先、增长补贴、保障住房、交通安全行动、完整街道、信号优化、拥堵收费、停车收费，影响污染、交通、人口增长、居住成本、道路安全、步行可达性、雨洪负荷、交叉口拥堵、汽车依赖、停车压力和预算收支；停车收费在人口与道路规模达标后形成停车收费收入，在公交覆盖、路网和停车覆盖足够时轻微降低汽车依赖与停车压力，公交替代不足且停车压力仍高时触发“停车收费阻力”；行政效率/容量较高时会降低正向政策执行成本，行政满载会推高政策积压。
- 原型场景生成器：自动创建摄像机、方向光、地图渲染、HUD、工具按钮和点击/拖拽交互。
- 视觉资源工厂：自动生成材质、分区色板、热力图色板、建筑图标图集、研发园区/资源加工园/配送中心/货运铁路/城市广场/会展中心/市政厅/医院/应急避难（`IconShape.Shelter`）/通信/道路养护/停车/轨道交通/城际枢纽/太阳能/垃圾发电图标和加载页背景。

## 开发入口
1. 浏览器调试：`cd browser && npm run dev`。
2. 浏览器生产构建：`cd browser && npm run build`。
3. 微信小游戏构建：`npm run build:wechat`，输出到 `miniprogram/game.js`。
4. 微信开发者工具打开 `miniprogram/`，使用横屏小游戏模式预览与真机调试。

## 本地校验
```bash
npm run verify
```

当前非 Unity Canvas runtime 还包含玩家可控税率管理、暂停/倍速控制、九项城市政策与政策效果预览、行政容量与政策积压里程碑、功能缓冲/用地冲突目标、用地效率/紧凑开发目标、HUD 洞察优先栈、R/C/I 分区需求信号、需求驱动分析、风险预测顾问、预算拆解顾问、成长瓶颈顾问、片区优先级顾问、住房负担顾问、建筑升级准备度顾问、经济专精顾问、服务短板顾问、道路层级顾问、通勤走廊顾问、地块检查器与图层图例、告警优先摘要、需求驱动自然开发、地图视角操作、安全生命周期反馈和近期事件摘要：浏览器 HUD 和微信 Canvas 管理面板都提供低税、标准和高税档位、暂停/1x/2x/4x 时间档位，以及绿色规范、公交优先、增长补贴、保障住房、交通安全行动、完整街道、信号优化、拥堵收费和停车收费政策；政策切换会即时预览月收支、拥堵、停车、步行、事故、雨洪、内涝和政策积压 delta，并影响现金流、行政效率、幸福度、评分、需求、污染、拥堵、停车压力、步行可达性、道路安全与雨洪风险；浏览器管理面板和微信 Canvas 管理面板会把目标、风险、预算、行政容量、功能缓冲、用地效率、成长卡点、片区优先级、服务、道路、通勤、升级、住房、经济、需求、告警和近期事件压缩为少量最高优先级洞察，微信 Canvas 侧栏继续显示三类需求值、压缩后的需求驱动、住房/混合摘要、经济专精摘要、功能缓冲摘要、用地效率摘要、道路/通勤摘要，并在选中地块时显示图层数值与诊断；现金、污染、用地冲突、空置分区、道路、行政满载、服务、停车、安全、雨洪和政策积压等告警会按优先级压缩成少量摘要并保留完整告警列表；接路空置分区会随需求逐日长出住宅、商业或工业建筑，成熟且品质达标的住宅会在居住压力下自然升到高密等级，成熟核心区也会在高地价与住宅/商业双需求下自然形成混合建筑；浏览器支持右键/中键拖拽与滚轮缩放，微信 Canvas 支持查看模式单指平移和双指缩放；微信切后台会安全自动保存，回到前台会安全读档并结算离线进度，storage 或触觉 API 不可用时继续当前城市；最近操作、生产完成、目标完成、政策切换和自动开发会写入 HUD/Canvas 事件摘要，并随城市状态一起存档恢复。

`npm run verify` 会保留历史源码静态校验，并额外构建非 Unity 微信小游戏入口，确保 `miniprogram/game.js` 不是 Unity 占位文件。

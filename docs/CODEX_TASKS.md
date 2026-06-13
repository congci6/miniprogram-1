# Codex 任务记录

## 当前方向
继续推进 Unity 架构的微信小游戏城市规划玩法。不再恢复或维护 TS 运行版。宜居度/生活压力已落地：`LivingCondition`、`LivingPressure`、`ComputeLivingCondition`、`ComputeLivingPressure`、`livable_district` 里程碑，以及“宜居度偏低”“生活压力偏高”告警。本轮新增建筑预览里的 `BuildingSiteScore` / `SiteDiagnosis` / 中文“选址诊断”说明，用 1-2 行解释当前建筑为什么适合或不适合该地块；不新增建筑或工具按钮，建筑数/工具按钮数/HUD 状态数为 38/48/33。
本轮政策效果反馈使用 `PolicyImpactPreview`：点击任一既有城市政策按钮后，右侧预览面板显示本次切换为启用/关闭，并即时列出月收支、拥堵、停车压力、步行可达性、事故风险、雨洪韧性/内涝风险、政策积压等关键 delta；不新增按钮、不修改 `miniprogram/game.json`，建筑数/工具按钮数/HUD 状态数继续保持 38/48/33。
本轮 `SERVICE_EQUITY_GAP_SOURCES` 口径要求 HUD 显示服务不足人口与主要服务缺口来源；缺口来源从住宅敏感建筑的公园/医疗/教育/公交/消防/警务/回收/通信/邮政/生命关怀覆盖缺失按容量加权估算，不新增建筑、工具按钮或 HUD 状态格。
本轮 `OBJECTIVE_ACTION_ADVICE` 口径要求当前目标/里程碑面板在原目标 hint 后追加简短“建议：...”行动提示；建议由当前未完成里程碑 id 和城市指标生成，例如均衡服务提示补主要服务缺口来源，交通目标提示打通断头路、升级主干或补公交，财政目标提示控预算、扩税基或处理债务，医疗/教育/警务/消防等专项目标提示补对应容量或响应；不新增按钮、不增加 HUD 状态格，38/48/33 数量保持不变。
本轮 `ALERT_PRIORITY_DIGEST` 口径要求右侧警报栏不再无限拼接全部告警，而是在 HUD 视图层按严重度排序并最多显示少量最关键告警，末尾用 `+N` 表示剩余数量；底层 `Metrics.Alerts` 仍保留完整告警列表。排序倾向现金、赤字、水电、污水、雨洪、医疗、消防、警务、灾害、交通和服务缺口等高风险事项；不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `RISK_FORECAST_ADVISOR` 口径为即将落地的近期风险预测准备 verify marker 和文档说明：核心需提供 `ForecastRisk`、`ForecastFocus`、`ForecastAction`、`CashRunwayDays`，实现名可用 `RiskForecastAdvisor` 或 `ComputeForecastRisk`；HUD 只复用现有目标、警报、预览或顶部财政文案行提示现金续航、财政、基础设施、服务和交通风险，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `BUDGET_BREAKDOWN_ADVISOR` 口径为即将落地的预算压力拆解/财政顾问准备文档和 QA 口径：核心需提供 `BudgetStress`、`BudgetFocus`、`BudgetDriver`、`BudgetAction`，实现名可用 `BudgetBreakdownAdvisor` 或 `ComputeBudgetBreakdown`；它根据现金/赤字、债务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收等现有指标判断主要财政压力来源并给出短行动建议。HUD 只复用现有目标/警报/财政文案区域，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `DISTRICT_PRIORITY_ADVISOR` 口径为即将落地的片区/系统优先级顾问准备文档、QA 和 UI 口径：核心需提供 `DistrictPriorityScore`、`DistrictPriorityFocus`、`DistrictPriorityDriver`、`DistrictPriorityAction`，实现名可用 `DistrictPriorityAdvisor` 或 `ComputeDistrictPriority`；它基于现有指标选择当前最需要治理的片区或系统优先级，覆盖交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境等，并给出短行动建议。HUD 只在优先级偏高或有风险时复用现有目标/警报文案区域，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `ROAD_HIERARCHY_ADVISOR` 口径为即将落地的道路层级/瓶颈升级顾问准备文档、QA 和 UI 口径：核心需提供 `RoadHierarchyPressure`、`RoadHierarchyFocus`、`RoadHierarchyDriver`、`RoadHierarchyAction`，实现名可用 `RoadHierarchyAdvisor` 或 `ComputeRoadHierarchyAdvice`；它基于现有道路/交通指标选择当前最该处理的交通层级问题，覆盖主干道不足、断头路、路网连通不足、路口延误、道路瓶颈、拥堵、公交候车/运力、停车压力、事故/养护等，并给出短行动建议。HUD 只在压力偏高或有风险时复用现有目标/警报文案区域，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。

本轮 `COMMUTE_CORRIDOR_ADVISOR` 已落地为通勤走廊/移动链顾问：核心提供 `CommuteCorridorScore`、`CommuteCorridorFocus`、`CommuteCorridorDriver`、`CommuteCorridorAction`，实现名为 `CommuteCorridorAdvisor` / `ComputeCommuteCorridorAdvice`；它复用住岗平衡、通勤效率、汽车依赖、公交覆盖/可靠性/候车压力、停车压力、路网连通、道路瓶颈、货运满载和区域连接，生成 `CommuteCorridorText` 并作为 `ObjectiveInsightParts` 候选进入右侧 insight stack。该能力不新增建筑、按钮、底部 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `HOUSING_AFFORDABILITY_ADVISOR` 口径为住房负担/宜居迁入顾问准备文档、QA 和 UI 口径：核心需提供 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver`、`HousingAffordabilityAction`，实现名可用 `HousingAffordabilityAdvisor` 或 `ComputeHousingAffordabilityAdvice`；它复用 `RentPressure`、HousingCapacity/Population 缺口、`ResidentialZoneTiles`、`MixedUse`、`HighDensityResidentialBuildings`、`AverageLandValue`/`TaxLevel`、`TransitCoverage`、`ServiceEquity`、`LivingCondition`/`LivingPressure`、`JobsHousingBalance` 和 `AffordableHousing` 政策，生成 `HousingAffordabilityText` 并作为 `ObjectiveInsightParts` 候选进入右侧 insight stack。该能力不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `ECONOMIC_SPECIALIZATION_ADVISOR` 口径为经济专精顾问准备文档、QA 和 UI 口径：核心需提供 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver`、`EconomicSpecializationAction`，实现名可用 `EconomicSpecializationAdvisor` 或 `ComputeEconomicSpecializationAdvice`；它复用 `BusinessEfficiency`、`InnovationCapacity`、`OfficeJobs`、`WorkforceSkill`、`AdvancedEducationCoverage`、`IndustrialSpecialization`、`ResourceSpecialization`、`LocalGoodsSupply`、`GoodsBalance`、`SupplyChainStability`、`LogisticsCoverage`/`LogisticsUtilization`、`Attractiveness`、`Visitors`、`TourismIncome`、`MixedUseBuildings` 和 `RegionalConnectivity`，生成 `EconomicSpecializationText` 并作为 `ObjectiveInsightParts` 候选进入右侧 insight stack。该能力不新增建筑、按钮、底部 HUD 状态格、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `CITY_EVENT_DIGEST` 口径为近期城市事件摘要准备 verify marker 和文档说明：核心需保留 `CITY_EVENT_DIGEST`、`RecentEvents` / `EventDigest`、`AddCityEvent`、`PushCityEvent` 和 `BuildEventDigestText`（可兼容同义实现名）；HUD 只复用现有目标/警报文案区域展示近期操作、政策、存读档和系统事件，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `DEMAND_DRIVER_ANALYSIS` 口径要求核心提供 `DemandFocus`、`DemandDriver`、`DemandAction`、`DemandUrgency` 和 `AnalyzeDemandDrivers` / `ComputeDemandInsight` marker；HUD 只复用现有目标/警报/需求文案区域解释最高需求、驱动原因和下一步行动，不新增按钮、不增加 HUD 状态格，不修改 `miniprogram/game.json`，38/48/33 数量保持不变。
本轮 `HUD_INSIGHT_PRIORITY_STACK` 方向只补文档/QA/UI 口径：它是右侧目标/警报文案的洞察优先栈，不新增功能按钮、不增加 HUD 状态格；它把 `RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`DEMAND_DRIVER_ANALYSIS`、`CITY_EVENT_DIGEST` 等现有顾问信息作为候选，`ObjectiveHint` 保持第一优先级，其余 insight 按风险、压力和事件重要性排序或限量显示少量最高优先级条目，降低横屏右侧拥挤；不修改 `miniprogram/game.json`，38/48/33 数量保持不变。

## 已完成
- Unity-only 项目结构。
- C# 城市模拟核心：道路、路网连通性、交通瓶颈/路口延误、道路养护、事故风险、道路安全、财政信用/行政效率/外部连接/债务压力/市政债券、步行可达性、应急响应/灾备/灾害风险、城市运维、建筑、分区、预算、人口、需求、服务、拥堵、污染、地价、用地效率、发展品质、用地冲突、居住成本、混合用地、办公/知识经济/创新经济、城市吸引力/游客经济、会展客流、商品供需/本地资源供给/资源适配/产业专精/铁路导入/仓储缓冲/供应链稳定、水电韧性、清洁电力、垃圾发电、污水处理、雨洪韧性/内涝风险、通信覆盖/企业效率、邮政服务、医疗容量/响应、生命关怀/死亡压力、教育/高等教育/教育容量/学位压力/学习通道、警务容量/响应、劳动力素质/用工缺口、公交可靠性/候车压力、通勤效率/汽车依赖、停车压力/停车容量、服务公平/服务不足人口/主要服务缺口来源、环境质量/噪声压力、公共健康/健康风险、物流运力/仓储/货运铁路、幸福度和里程碑。
- Unity Runtime 控制器接口：建造、铺路、分区、拆除、图层切换和 tile 查询。
- Runtime HUD：顶栏、需求条、警报、图层按钮、常用工具按钮和当前目标/里程碑行动建议。
- 点击/触控交互：拖拽铺路、拖拽分区、点击建造、点击拆除。
- 相机控制：右键/中键拖拽平移、滚轮缩放、双指缩放和边界限制。
- 临时地图渲染：顶点色地形、道路方块、建筑方块和图层热力覆盖。
- 暂停、倍速、手动保存、读取和自动存档。
- 税率系统：低/标准/高税率已接入税收、幸福度、住宅/商业/混合用地/办公/工业需求、HUD、告警和存档。
- 城市政策：绿色规范、公交优先、增长补贴、保障住房、交通安全行动、完整街道、信号优化、拥堵收费、停车收费，已接入预算、污染、道路容量、交叉口拥堵、道路安全、步行可达性、汽车依赖、停车压力、雨洪负荷、居住成本、需求、人口增长、HUD 和存档；停车收费在人口 >= 140 且道路 >= 8 时带来停车收费收入，公交覆盖、路网和停车覆盖足够时轻微降低汽车依赖与停车压力，公交替代不足且停车压力仍高时触发“停车收费阻力”；行政效率会降低正向政策执行成本。
- 政策效果反馈：`PolicyImpactPreview` 已定义为右侧预览口径，政策按钮切换后展示启用/关闭与即时指标 delta，覆盖财政、拥堵、停车、步行、安全、雨洪和政策积压，不新增城市政策按钮。
- 目标行动建议：`OBJECTIVE_ACTION_ADVICE` 作为目标/里程碑面板文案口径，在原目标 hint 后追加“建议：...”短提示；服务、交通、财政、医疗、教育、警务和消防等目标会按当前指标给出下一步行动方向，不新增按钮或 HUD 状态格。
- 警报优先摘要：`ALERT_PRIORITY_DIGEST` 作为右侧警报栏的 HUD 视图层摘要口径，只排序和截断展示文本，不裁剪 `Metrics.Alerts` 完整列表；现金/赤字/水电/污水/雨洪/医疗/消防/警务/灾害/交通/服务缺口等优先浮到前面，溢出用 `+N` 表示，不新增按钮、HUD 状态格或小游戏配置。
- 预算拆解顾问口径：`BUDGET_BREAKDOWN_ADVISOR` 作为目标/警报/财政文案区域的预算压力拆解口径，汇总 `BudgetStress` / `BudgetFocus` / `BudgetDriver` / `BudgetAction`，把现金/赤字、债务、政策执行、建筑维护、公共服务容量、水电/污水/雨洪、公交/货运/通信/邮政、道路维护/停车/回收等现有指标压缩成主要财政压力来源和短行动建议；不新增按钮、HUD 状态格或小游戏配置，不改变 38/48/33。
- 片区优先级顾问口径：`DISTRICT_PRIORITY_ADVISOR` 作为目标/警报文案区域的片区/系统优先级顾问口径，汇总 `DistrictPriorityScore` / `DistrictPriorityFocus` / `DistrictPriorityDriver` / `DistrictPriorityAction`，把交通瓶颈、服务公平/服务缺口、住房/居住成本、财政/预算压力、水电污水雨洪、公共安全/消防警务医疗、商品物流/供应链、宜居/环境等现有指标压缩成当前最需要治理的优先级和短行动建议；仅在优先级偏高或有风险时显示，不新增按钮、HUD 状态格或小游戏配置，不改变 38/48/33。
- 道路层级顾问口径：`ROAD_HIERARCHY_ADVISOR` 作为目标/警报文案区域的道路层级/瓶颈升级顾问口径，汇总 `RoadHierarchyPressure` / `RoadHierarchyFocus` / `RoadHierarchyDriver` / `RoadHierarchyAction`，把主干道不足、断头路、路网连通不足、路口延误、道路瓶颈、拥堵、公交候车/运力、停车压力、事故/养护等现有道路和交通指标压缩成当前最该处理的交通层级问题和短行动建议；仅在压力偏高或有风险时显示，不新增按钮、HUD 状态格或小游戏配置，不改变 38/48/33。
- 住房负担/宜居迁入顾问口径：`HOUSING_AFFORDABILITY_ADVISOR` 作为目标/警报文案区域的住房顾问口径，汇总 `HousingAffordabilityScore` / `HousingAffordabilityFocus` / `HousingAffordabilityDriver` / `HousingAffordabilityAction`，把租金压力、住房容量/人口缺口、住宅分区与混合/高密供给、地价/税率、公交、服务公平、宜居/生活压力、住岗平衡和保障住房政策压缩成当前最影响迁入稳定性的短行动建议；生成 `HousingAffordabilityText` 并进入 `ObjectiveInsightParts`，不新增建筑、按钮、HUD 状态格、workers、TS/Vite、WebGL2、SharedArrayBuffer 或小游戏配置，不改变 38/48/33。
- 经济专精顾问口径：`ECONOMIC_SPECIALIZATION_ADVISOR` 作为目标/警报文案区域的经济顾问口径，汇总 `EconomicSpecializationScore` / `EconomicSpecializationFocus` / `EconomicSpecializationDriver` / `EconomicSpecializationAction`，把企业效率、创新能力、办公岗位、人才/高教、产业/资源专精、本地供给、商品平衡、供应链稳定、物流覆盖/满载、城市吸引力、游客、旅游收入、混合商业和区域连接压缩成当前最适合推进的资源工业、物流供应链、办公创新、旅游会展或混合商业经济线；生成 `EconomicSpecializationText` 并进入 `ObjectiveInsightParts`，显示为“经济:专... -> ...”类短句，不新增建筑、按钮、HUD 状态格、workers、TS/Vite、WebGL2、SharedArrayBuffer 或小游戏配置，不改变 38/48/33。
- 城市事件摘要：`CITY_EVENT_DIGEST` 作为目标/警报附近的 HUD 文案口径，汇总 `RecentEvents` / `EventDigest` 的近期事件；事件写入入口可用 `AddCityEvent` / `PushCityEvent`，展示文本可用 `BuildEventDigestText` 或同义名，不新增按钮、HUD 状态格或小游戏配置。
- 需求驱动分析：`DEMAND_DRIVER_ANALYSIS` 作为需求条解释口径，汇总 `DemandFocus` / `DemandDriver` / `DemandAction` / `DemandUrgency`，把最高需求解释成住房、商品、通勤、人才、物流、服务或设施短板，并给出下一步行动建议；不新增按钮、HUD 状态格或小游戏配置。
- 洞察优先栈口径：`HUD_INSIGHT_PRIORITY_STACK` 作为右侧目标/警报文案的候选排序口径，`ObjectiveHint` 固定第一优先级，`RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`DEMAND_DRIVER_ANALYSIS`、`CITY_EVENT_DIGEST` 只作为候选 insight 进入少量限量展示；文档/QA/UI 口径已补，不代表已完成 Unity Editor、真机或微信开发者工具验证，不新增按钮、HUD 状态格或小游戏配置，不改变 38/48/33。
- 公共交通：街区公交站、轨道交通站和城际枢纽生成公交覆盖热力和运力容量，覆盖内建筑降低道路负载，城际枢纽额外提供外部连接；本轮不新增建筑，三类客运设施扩展为 `TransitReliability`、`TransitWaitPressure` 和 `ComputeTransitWaitPressure` 口径。满载过高会降低有效覆盖、压低公交可靠性、推高候车压力并触发告警；候车压力用原始公交覆盖启用，人口达到 120 后进入通勤、幸福度、城市评分和服务需求；中后期公交过载且未建轨道交通站会提示“缺少轨道交通”；公交覆盖率、满载率、可靠性、候车压力、外部连接、“公交可靠性偏低”“公交候车压力偏高”、`transit_reliability`、“轨道骨架”和“区域门户”进入 HUD、需求、幸福度、城市评分、服务需求和里程碑。
- 货运物流：货运站生成货运覆盖热力和运力容量，覆盖内商业/工业降低道路负载并提升税收质量、需求、建筑成长；资源加工园使用丘陵、工业地块和货运可达性形成 `ResourcePotential`，再结合水电可靠性和人才水平形成 `ResourceSpecialization`、本地供给和 `IndustrialSpecialization`；配送中心使用货运覆盖、水电可靠性和货运满载率形成仓储缓冲与供应链稳定；货运铁路站提供更高物流容量和铁路导入，不计入客运外部连接；满载过高会降低有效覆盖、推高道路负载，并进入 HUD、告警、货运循环、货运运力、本地供给、产业专精、供应链缓冲和铁路货运里程碑。
- 通信服务：通信枢纽生成通信覆盖热力和容量，覆盖内住宅、商业、办公、混合用地和工业活动降低少量交通压力，并提高企业效率、生产率奖金、税收质量、需求、告警、智慧商务和通信容量里程碑。
- 邮政服务：`post_office` 邮政局口径覆盖住宅、商业、办公、混合用地、工业和地标建筑的邮件需求；`ConnectedMailBuildings`、`MailCapacityForBuildings`、`MailBuildingCapacity`、`MailWeightForBuilding`、`ApplyMailTileAccess`、`IsMailBuilding`、`IsMailSensitiveBuilding` 进入 verify marker；`MailCoverage`、`MailLoad`、`MailCapacity`、`MailUtilization`、`MailReliability`、`MailAccess`、`mail_service` 和三类邮政告警进入文档口径。
- 医疗容量/响应已落地：社区诊所和区域医院提供医疗容量；`HealthLoad`、`HealthCapacity`、`HealthUtilization`、`MedicalResponse`、`PatientBacklog` 已接入 HUD、服务需求、公共健康/健康风险、三类医疗告警和 `healthcare_capacity`。
- 教育容量/学位压力已落地：社区学校和社区学院提供教育容量；`EducationLoad`、`EducationCapacity`、`EducationUtilization`、`StudentBacklog`、`LearningPipeline` 已接入 HUD、需求、人才、告警和 `education_capacity`。
- 生命关怀/死亡护理已落地：`memorial_garden` 生命纪念花园提供生命关怀覆盖和容量；`DeathcareCoverage`、`DeathcareLoad`、`DeathcareCapacity`、`DeathcareUtilization`、`MortalityPressure`、`DeathcareAccess` 已接入 HUD、服务公平、服务需求、公共健康、三类生命关怀告警和 `deathcare_ready`。
- 警务容量/响应已落地：社区警务站和 `police_precinct` 警务分局提供覆盖与执法容量；`SecurityLoad`、`SecurityCapacity`、`SecurityUtilization`、`PoliceResponse`、`CaseBacklog` 已接入 HUD、服务需求、犯罪压力、三类警务告警和 `police_readiness`。
- 道路养护与安全：道路养护站生成路安热力，事故风险和道路安全已接入 HUD、道路负载、维护状态、幸福度、需求、评分、告警、道路养护和安全道路里程碑。
- 财政信用：现金缓冲、月净收支、月支出、市政债券本金、行政效率和债务压力已接入顶部 HUD、幸福度、需求、评分、告警、财政信用和偿债纪律里程碑。
- 市政厅/行政容量：市政厅已接入建筑配置、HUD、服务图层、税收质量、政策成本、告警和“市政中心”里程碑；本轮扩展 `AdministrationLoad`、`AdministrationCapacity`、`AdministrationUtilization`、`PolicyBacklog`、`ComputePolicyBacklog`、`administration_capacity`、“行政容量不足”和“政策积压偏高”，不新增建筑，建筑数/工具按钮数保持 38/48。
- 道路分级：普通道路可升级为主干道，主干道提高容量、提高维护费和沿线噪声，并进入存档、HUD 工具和里程碑。
- 路网连通性：断头路、交叉口、主干道和建筑接路率已接入 HUD、通勤效率、城市评分、告警和“连通路网”里程碑。
- 交通瓶颈/路口延误：`IntersectionDelay` 由交叉口密度、断头路、拥堵、主干道和路网连通性计算，`RoadBottleneckPressure` 进一步汇总拥堵、连通缺口、断头路、交叉口和延误；瓶颈会回灌少量拥堵、压低通勤效率/幸福度/城市评分并推高服务需求，HUD 路网项显示“瓶/延”，信号优化和拥堵收费可降低延误，并进入“道路瓶颈偏高”“路口延误偏高”和 `traffic_flow`。
- 步行可达性：连通路网、公交、服务、公园、紧凑用地、混合街区、汽车依赖和拥堵已接入 HUD、幸福度、需求、评分、告警和“步行城市”里程碑。
- 建筑成长：住宅、商业、混合用地、办公和工业会根据年龄、地价、公交可达性、接路状态、发展品质和类型加成自然升级，等级影响容量、岗位、税值、维护和建筑高度。
- 分类服务：口袋公园提供公园覆盖，社区诊所和区域医院提供医疗覆盖，社区学校提供教育覆盖；服务图层、HUD、幸福度、需求、告警、税收质量和里程碑已使用分类覆盖。
- 公共服务容量：医疗、教育、消防、警务、应急避难和生命关怀已接入服务负载、容量和利用率；过载会降低有效覆盖，并进入 HUD、需求、告警和里程碑，医疗、生命关怀、教育和警务都有专项响应/积压压力。
- 服务公平：住宅片区按公园、医疗、教育、公交、消防、警务、回收、通信、邮政和生命关怀可达性形成服务公平，`DeathcareAccess` 已计入地块服务公平；服务不足人口和主要服务缺口来源按住宅敏感建筑容量加权估算，已接入 HUD、幸福度、需求、评分、告警和“均衡服务”里程碑。
- 宜居度/生活压力：`LivingCondition` 综合服务覆盖与公平、公园、教育、生命关怀、公交、通勤、步行、环境、公共健康和水电可靠性；`LivingPressure` 汇总居住成本、治安、健康风险、噪声、道路瓶颈、候车压力和服务不均，已接入 HUD、幸福度、评分、住宅/混合/服务需求、告警和 `livable_district`。
- 城市运维：现金缓冲、服务预算、服务负载、水电负载、雨洪压力、拥堵和城市规模已接入 HUD、服务可靠性、幸福度、需求、评分、告警和“城市运维”里程碑。
- 应急响应与灾备：医疗覆盖、医疗响应、消防、警务覆盖、警务响应、服务可靠性、路网连通、拥堵、断头路和未接路建筑已接入 HUD、治安、健康、评分、服务需求、告警和“应急响应”里程碑；应急避难中心、雨洪、水电、路网和维护状态已接入灾备/灾害风险、告警和“灾害准备”里程碑。
- 安全服务：社区消防站提供消防覆盖，按建筑风险权重影响幸福度、评分、服务需求、工业需求、告警和消防网络里程碑。
- 治安服务：社区警务站和 `police_precinct` 警务分局提供警务覆盖、执法容量和响应，犯罪压力受失业、居住成本、拥堵、警务覆盖、警务响应和案件积压影响，并进入 HUD、告警、需求、评分、平安街区和 `police_readiness` 里程碑。
- 回收覆盖：回收处理站和垃圾发电厂生成回收热力，垃圾负荷、容量、满载率和可靠性进入 HUD、告警、设施需求、污染、幸福度、清洁街区、回收容量和资源回收能源里程碑。
- 混合用地：混合分区、混合街区、混合需求、混合核心里程碑已接入 HUD、自动开发、图层色板和存档重算。
- 办公/知识经济/创新经济：办公分区、共享办公楼、研发园区、办公需求、办公岗位、创新能力、“知识经济”和“创新高地”里程碑已接入 HUD、自动开发、图层色板和存档重算。
- 城市吸引力/游客经济：城市广场、会展中心、吸引力、游客、旅游收入、地标停车需求、“城市吸引力”和“会展客流”里程碑已接入 HUD、预算、默认配置和图标图集。
- 商品供需：工业岗位、资源加工园本地供给、资源适配、产业专精、配送中心仓储缓冲、供应链稳定、货运铁路站铁路导入、外部连接、商业/居民/游客消费、货运可靠性、商品平衡、“商品市场”、“本地供给”、`specialized_industry`、“供应链缓冲”和“铁路货运”里程碑已接入 HUD、需求、税收、幸福度、评分和告警口径；商品 HUD 显示资源适配，告警提示本地资源适配不足。
- 劳动力素质/用工缺口：教育覆盖、高等教育覆盖、教育容量、学习通道、办公岗位、研发能力、升级建筑、岗位缺口、生产率奖金、“人才城市”和 `education_capacity` 里程碑已接入 HUD、预算、需求、评分和告警。
- 通勤效率/汽车依赖：住岗平衡、公交覆盖、公交可靠性、候车压力、混合街区、主干道、拥堵和断路建筑已接入 HUD、需求、评分、幸福度、告警、“顺畅通勤”和 `transit_reliability` 里程碑。
- 完整街道政策：以道路容量和维护成本为代价，降低接路建筑车流、汽车依赖、停车压力、雨洪负荷、噪声和事故风险，提高步行可达性、道路安全与混合街区需求，并进入“完整街道”里程碑。
- 信号优化政策：按交叉口数量和路网连通性降低拥堵，减少事故风险、提高道路安全并增加商业/办公/混合/工业需求；拥堵仍高且交叉口密集时触发“信号优化过载”告警，并进入“信号优化”里程碑。
- 拥堵收费政策：在人口和道路规模达标后形成政策收入，降低拥堵、汽车依赖和停车压力，但公交替代不足且汽车依赖仍高时触发“拥堵收费阻力”告警，并进入“拥堵收费”里程碑。
- `CityPolicy.ParkingFees`（中文 UI：停车费/停车收费）：在人口 >= 140 且道路 >= 8 时形成停车收费收入，在公交覆盖、路网和停车覆盖足够时轻微降低汽车依赖与停车压力；公交替代不足且停车压力仍高时触发“停车收费阻力”告警，并进入“停车收费”里程碑。
- 停车压力/容量：汽车依赖、岗位/商业/办公出行和拥堵会推高停车压力；公交、连通路网、混合街区、紧凑用地、停车收费和邻里停车楼可缓解。停车楼提供覆盖热力和容量，满载时降低有效覆盖，已接入 HUD、道路负载、幸福度、吸引力、需求、告警、“低车依赖”、“停车调度”和“停车收费”里程碑。
- 雨洪韧性/内涝风险：雨水花园、公园和完整街道可降低雨洪压力；雨洪负载、容量、满载率、韧性和内涝风险已接入 HUD、Stormwater 图层、告警、环境质量、公共健康、幸福度、评分、基础设施需求和“雨洪韧性”里程碑。
- 环境质量/噪声压力：公园、回收、污水处理、雨洪韧性、公交、绿色规范、污染、噪声、内涝风险和汽车依赖已接入 HUD、需求、评分、幸福度、告警和“绿色宜居”里程碑。
- 公共健康/健康风险：医疗覆盖、区域医院、医疗响应、病患积压、生命关怀、死亡压力、环境质量、回收覆盖、污水处理、雨洪韧性、水电可靠性、污染、内涝风险和噪声压力已接入 HUD、人口迁入、需求、评分、幸福度、告警、“健康城市”“区域医疗中心”、`healthcare_capacity` 和 `deathcare_ready` 里程碑。
- 水电韧性：供电/供水容量、负载、可靠性和满载率已接入 HUD、Utilities 图层、告警和“水电韧性”里程碑；太阳能阵列作为中期零污染供电设施接入清洁电力告警和“清洁电力”里程碑，垃圾发电厂作为中后期资源回收能源设施接入回收容量、供电、污染和“资源回收能源”里程碑。
- 污水处理：污水处理站、污水负载、容量、可靠性和满载率已接入 HUD、Utilities 图层、告警、污染、健康、基础设施需求和“水环境”里程碑。
- 高等教育/教育容量：社区学院、高等教育覆盖、学校/学院容量、入学积压、学习通道、人才加成、生产率、办公需求、建筑成长、告警、“高等教育”和 `education_capacity` 里程碑已接入。
- 分区自然开发：住宅/商业/混合用地/办公/工业分区会按需求、道路接入、现金状态和适宜度自动长出建筑，自动建筑可存档并进入“分区生长”里程碑。
- 分区适宜度：拖拽预览显示适宜度，自然开发过滤低适宜度地块，规划告警改为缺少适宜地块。
- 建筑选址诊断：建造预览会显示 `BuildingSiteScore` 和 `SiteDiagnosis`，按建筑类型结合地价、污染/噪声、公交/物流/通信/邮政/停车/雨洪/服务可达性、道路接入、推荐分区与适宜度生成“选址诊断”中文建议；该功能只解释选址，不新增建筑、工具按钮或 HUD 状态格。
- 功能缓冲：拖拽分区预览显示缓冲风险，用地冲突会影响幸福度、评分、需求、服务压力、告警和“功能缓冲”里程碑。
- 发展品质：已开发建筑按分区适配、接路、等级和区位条件汇总发展品质，影响幸福度、评分、需求、服务压力、告警和“优质片区”里程碑。
- 用地效率：增长型分区已接入已开发面积、空置分区、城市评分、HUD、告警和“紧凑用地”里程碑。
- 高密自然开发：居住成本和住宅需求偏高时，已解锁公寓楼会加入住宅分区自动开发，并进入“高密住区”里程碑。
- 一键原型场景：`Pocket City/Create Prototype Scene`。
- 默认 `CityConfig` 生成器 verify 预期保持 38 个基础建筑定义；教育容量、选址诊断、目标行动建议、`RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`CITY_EVENT_DIGEST`、`DEMAND_DRIVER_ANALYSIS` 和 `HUD_INSIGHT_PRIORITY_STACK` 不新增建筑，并且 Runtime HUD 工具按钮数继续保持 48、底部状态数继续保持 33。
- 微信小游戏桥接：分享、震动和 storage 存读档 fallback。
- 微信小游戏占位入口，移除 `workers` 配置。

## 下一步
- 下一步优先做 UI 拥挤检查：在后续 Unity Editor / 微信横屏预览中检查 `HUD_INSIGHT_PRIORITY_STACK` 是否将 `ObjectiveHint` 固定为第一优先级，并将 `RISK_FORECAST_ADVISOR`、`BUDGET_BREAKDOWN_ADVISOR`、`DISTRICT_PRIORITY_ADVISOR`、`ROAD_HIERARCHY_ADVISOR`、`COMMUTE_CORRIDOR_ADVISOR`、`HOUSING_AFFORDABILITY_ADVISOR`、`ECONOMIC_SPECIALIZATION_ADVISOR`、`CITY_EVENT_DIGEST` 和 `DEMAND_DRIVER_ANALYSIS` 限量为少量最高优先级 insight，避免挤压右侧目标/警报区域、工具按钮或底部 33 项状态网格；本轮文档不声称已完成 Unity Editor、真机或微信开发者工具验证。
- 继续接入微信小游戏转换 SDK 导出 `miniprogram/`，保持 `game.json` 无 `workers`。
- 在 Unity 中打开 `PocketCityPrototype.unity`，修复 Console 中任何真实编译问题。
- 替换正式 UI mockup、建筑图标、加载页资产和建筑 prefab。
- 接入微信小游戏转换 SDK 并导出 `miniprogram/`。
- 用微信开发者工具和真机验证性能、存档、分享、震动与触控交互。

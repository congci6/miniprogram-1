# Unity 架构迁移方案

## 当前结论
项目已经切换为 Unity-first。`unity/` 是唯一活跃工程；旧 TypeScript + Three.js 原型归档到 `legacy/typescript-prototype/`，只作迁移参考。

## 模块职责
```text
Unity Scene / Prefabs / UI
  -> CityGameController
  -> CityInteractionController / CityCameraController / CitySaveController
  -> CitySimulationCore
  -> CityGridCore
  -> CityConfig
  -> WeChatMiniGameBridge
```

- `CitySimulationCore`：纯玩法核心，负责道路分级、路网连通性、路口延误/`IntersectionDelay`、道路瓶颈/`RoadBottleneckPressure`、交叉口信号优化、道路养护、事故风险、道路安全、财政信用/行政效率/外部连接/债务压力/市政债券、步行可达性、应急响应/灾备/灾害风险、城市运维、分区、分区适宜度、用地冲突、发展品质、分区自然开发、用地效率、高密住宅开发、混合用地、办公/知识经济/创新经济、城市吸引力/游客经济/会展客流、商品供需/`ResourcePotential`/`ResourceSpecialization`/`IndustrialSpecialization`/本地资源供给/铁路导入/仓储缓冲/供应链稳定、水电韧性、清洁电力、污水处理、雨洪韧性/内涝风险、通信覆盖/企业效率、邮政服务/`MailCoverage`/`MailUtilization`/`mail_service`、医疗容量/`HealthLoad`/`HealthCapacity`/`HealthUtilization`/`MedicalResponse`/`PatientBacklog`/`healthcare_capacity`、教育容量/`EducationLoad`/`EducationCapacity`/`EducationUtilization`/`StudentBacklog`/`LearningPipeline`/`education_capacity`、消防韧性/`FireRisk`/`FireProtection`/`FireLoad`/`FireCapacity`/`FireUtilization`/`FireResponse`/`fire_resilience`、生命关怀/`DeathcareCoverage`/`DeathcareUtilization`/`MortalityPressure`/`deathcare_ready`、警务响应/`SecurityLoad`/`SecurityCapacity`/`SecurityUtilization`/`PoliceResponse`/`CaseBacklog`/`police_readiness`、教育/高等教育覆盖、劳动力素质/用工缺口、公交可靠性/`TransitReliability`/`TransitWaitPressure`/`transit_reliability`、住岗平衡/通勤效率/汽车依赖、停车压力/覆盖/容量/停车收费收入、服务公平、`LivingCondition`/`LivingPressure` 宜居度与生活压力、环境质量/噪声压力、公共健康/健康风险、建筑成长、经济、人口、分类服务、安全/消防覆盖、警务/治安压力、公共交通/轨道交通/城际枢纽、货运物流/仓储/货运铁路/运力、回收覆盖/容量、垃圾发电、居住成本、拥堵、预算、九项城市政策、`traffic_flow`、`livable_district`、`specialized_industry` 等里程碑和解锁。
- 行政容量不新增建筑，由既有市政厅形成 `AdministrationLoad`、`AdministrationCapacity`、`AdministrationUtilization` 和 `PolicyBacklog`；这些指标接入 HUD 顶栏、政策成本、幸福度、城市评分、服务需求、告警和 `administration_capacity` 里程碑。
- 公交可靠性不新增建筑，由街区公交站、轨道交通站和城际枢纽形成 `TransitReliability`、`TransitWaitPressure` 和 `ComputeTransitWaitPressure`；这些指标接入 HUD 公交项、通勤效率、汽车依赖、幸福度、城市评分、服务需求、告警和 `transit_reliability` 里程碑。
- `GROWTH_BOTTLENECK_ADVISOR` 不新增建筑或 UI 控件，由 `CitySimulationCore` 复用住房、财政、通勤、服务、公用设施、就业、供应链和宜居指标生成 `GrowthBottleneckScore`、`GrowthBottleneckFocus`、`GrowthBottleneckDriver` 与 `GrowthBottleneckAction`，再由 `CityHudViewModel` 作为 `ObjectiveInsightParts` 候选显示。
- `COMMUTE_CORRIDOR_ADVISOR` 不新增建筑或 UI 控件，由 `CitySimulationCore` 复用住岗平衡、通勤效率、汽车依赖、公交覆盖/可靠性/候车压力、停车压力、路网连通、道路瓶颈、货运满载和区域连接等移动指标，生成 `CommuteCorridorScore`、`CommuteCorridorFocus`、`CommuteCorridorDriver` 与 `CommuteCorridorAction`；`CityHudViewModel` 只把它压缩为 `CommuteCorridorText` 并作为 `ObjectiveInsightParts` 候选显示。
- `BUILDING_UPGRADE_READINESS_ADVISOR` 不新增建筑或 UI 控件，由 `CitySimulationCore` 复用单栋建筑升级逻辑，按住宅/商业/办公/工业的年龄门槛、升级分、地价、公交、接路、服务覆盖、物流、教育/高教、劳动力、污染/噪音等判断升级机会或阻塞，生成 `BuildingUpgradeReadinessScore`、`BuildingUpgradeReadyCount`、`BuildingUpgradeBlockedCount`、`BuildingUpgradeReadinessFocus`、`BuildingUpgradeReadinessDriver` 与 `BuildingUpgradeReadinessAction`；`CityHudViewModel` 只把它压缩为 `BuildingUpgradeReadinessText` 并作为 `ObjectiveInsightParts` 候选显示，不新增按钮、底部 HUD 统计槽、workers、TS/Vite、WebGL2 或 SharedArrayBuffer。
- `HOUSING_AFFORDABILITY_ADVISOR` 不新增建筑或 UI 控件，由 `CitySimulationCore` 复用 `RentPressure`、住宅容量/人口缺口、住宅分区、混合用地、高密住宅建筑、平均地价、税率、公交覆盖、服务公平、`LivingCondition`、`LivingPressure`、住岗平衡和“保障住房”政策，生成 `HousingAffordabilityScore`、`HousingAffordabilityFocus`、`HousingAffordabilityDriver` 与 `HousingAffordabilityAction`；`CityHudViewModel` 只把它压缩为 `HousingAffordabilityText` 并作为 `ObjectiveInsightParts` 候选显示，不新增按钮、底部 HUD 统计槽、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- `ECONOMIC_SPECIALIZATION_ADVISOR` 不新增建筑或 UI 控件，由 `CitySimulationCore` 复用 `BusinessEfficiency`、`InnovationCapacity`、`OfficeJobs`、`WorkforceSkill`、`AdvancedEducationCoverage`、`IndustrialSpecialization`、`ResourceSpecialization`、`LocalGoodsSupply`、`GoodsBalance`、`SupplyChainStability`、`LogisticsCoverage`/`LogisticsUtilization`、`Attractiveness`、`Visitors`、`TourismIncome`、`MixedUseBuildings` 和 `RegionalConnectivity` 等既有指标，生成 `EconomicSpecializationScore`、`EconomicSpecializationFocus`、`EconomicSpecializationDriver` 与 `EconomicSpecializationAction`；`CityHudViewModel` 只把它压缩为 `EconomicSpecializationText` 并作为 `ObjectiveInsightParts` 候选显示，短句为“经济:专... -> ...”类，不新增按钮、底部 HUD 统计槽、workers、TS/Vite、WebGL2 或 SharedArrayBuffer，也不修改 `miniprogram/game.json`。
- `CityGridCore`：地图核心，维护地形、道路、分区、建筑占用、道路养护/停车/邮政/教育/生命关怀/警务服务可达性和图层数据。
- `CityGameController`：Unity 入口，暴露建造、铺路、分区、拆除、图层切换、暂停倍速、税率、服务预算、债券、城市政策、存档和指标读取接口。
- `CityInteractionController`：输入层，负责点击建造、点击拆除、拖拽铺路和拖拽分区。
- `CityCameraController`：相机层，负责鼠标/触控平移缩放和地图边界限制。
- `CitySaveController`：存档层，负责手动保存、读取、删除和自动存档。
- `CityConfig`：ScriptableObject 数据资产，承载地图、经济、建筑和平衡数值。
- `DefaultCityConfigFactory`：Editor 菜单，生成可运行默认配置。
- `WeChatMiniGameBridge`：微信平台桥，封装分享、震动、storage、切后台/暂停自动保存触发和安全触觉反馈等平台调用。

## 玩法方向
目标是做适合微信小游戏体量的“口袋城市规划”体验：保留城市建设游戏中最有反馈感的路网、分区、服务覆盖、预算和里程碑，而不是复制大型 PC 城市模拟的完整复杂度。

核心循环：
1. 铺设道路形成连通开发骨架，减少断头路，并把关键走廊升级为主干道。
2. 划分住宅、商业、混合用地、办公、工业、公共服务和基础设施分区。
3. 让接路且适宜度达标的住宅/商业/混合用地/办公/工业分区按需求自然开发，持续提升发展品质、控制用地冲突并减少空置分区，同时手动放置公园、市政厅、诊所、区域医院、纪念花园、学校、消防、道路养护等关键服务和基础设施建筑。
4. 用公交站覆盖住宅和就业核心，后期用轨道交通站和城际枢纽承接更高客流，缓解通勤道路负载、提高公交可靠性、压低候车压力并补足外部连接。
5. 用连通路网、混合街区、公园和生活服务形成步行可达街区，减少汽车依赖。
6. 用现金缓冲、服务预算、道路养护和低过载维持城市运维，避免维护不足拖垮服务可靠性。
7. 让住宅片区均衡获得公园、医疗、教育、公交、消防、警务、邮政、生命关怀和回收可达性，避免全城覆盖不错但局部片区长期缺服务。
8. 用 `LivingCondition` 和 `LivingPressure` 把房租、服务公平、公交等待、道路瓶颈、环境、健康、治安和步行性汇总成玩家能在 HUD 里直接读懂的居民生活质量。
9. 用医疗、教育、消防、警务、警署、应急避难、纪念花园、道路养护和连通道路降低事故风险并维持应急响应/学位供给/灾备/生命关怀/警务响应；医疗系统会用 `HealthCapacity`、`MedicalResponse` 和 `PatientBacklog` 达成 `healthcare_capacity`，教育系统会用 `EducationCapacity`、`StudentBacklog` 和 `LearningPipeline` 达成 `education_capacity`，消防韧性会用消防覆盖、容量和路网响应控制 `FireRisk`，达成 `fire_resilience`，生命关怀会用 `DeathcareAccess`、容量和死亡压力达成 `deathcare_ready`，警务系统会用 `SecurityCapacity`、`PoliceResponse` 和 `CaseBacklog` 达成 `police_readiness`，避免服务过载导致教育、治安、健康、火灾、死亡、灾害风险和道路安全恶化。
10. 用货运站覆盖商业和工业货流，并补足货运容量，降低货车压力并提升工业发展质量。
11. 用配送中心建立仓储缓冲和供应链稳定，避免商品市场被短期缺口拖垮。
12. 用货运铁路站承接后期外部货物导入，给商品市场提供铁路导入，但不把它当作客运外部连接。
13. 用资源加工园吃到丘陵、工业地块和货运可达性的 `ResourcePotential`，再把水电可靠性和人才水平转化为 `ResourceSpecialization`、本地商品供给和 `IndustrialSpecialization`，减少对外部连接的依赖。
14. 通过工业、资源适配、产业专精、商业、货运、仓储、货运铁路、外部连接和游客消费维持商品市场平衡，再用服务、教育、地价、公交、货运、通信、邮政、研发园区和治安可达性培育建筑升级、混合街区、办公岗位与创新能力。
15. 用城市广场、会展中心、公园、服务、公交、外部连接和低污染街区提高城市吸引力，获得游客与旅游收入，并用公交和停车设施承接会展客流。
16. 通过教育覆盖、教育容量、高等教育、研发园区、办公岗位和建筑成长提高劳动力素质，避免岗位扩张后出现用工缺口；学校和社区学院共同控制 `EducationUtilization` 与 `StudentBacklog`，社区学院与研发园区支撑办公需求、创新能力、生产率奖金和中后期建筑成长。
17. 通过住岗平衡、公交/轨道交通/城际枢纽可靠性、低候车压力、混合街区、完整街道、信号优化、拥堵收费和主干道提高通勤效率，降低汽车依赖。
18. 用公交、连通路网、紧凑用地、混合街区、完整街道、拥堵收费、停车收费和邻里停车楼降低停车压力，减少找车位绕行对拥堵和商业吸引力的拖累，并在人口与道路规模达标后获得停车收费收入。
19. 用公园、回收、污水处理、公交、绿色规范、完整街道和雨洪韧性改善环境质量，压低污染、噪声和内涝风险。
20. 用医疗覆盖、医疗容量、医疗响应、环境、回收、可靠水电、清洁电力和雨洪韧性降低健康风险，保持人口迁入。
21. 用垃圾发电厂把后期垃圾负荷转成回收容量和供电，同时承担污染、噪声、用水、交通和维护成本。
22. 观察拥堵、事故风险、道路安全、污染、地价、幸福度、财政信用、行政效率、债券本金和现金流。
23. 调整低/标准/高税率，在收入、幸福度和需求之间取舍。
24. 启用绿色规范、公交优先、增长补贴、完整街道、信号优化、拥堵收费或停车收费，在预算、道路容量、交叉口拥堵、道路安全、雨洪韧性、汽车依赖、停车压力和成长速度之间取舍；停车收费需要公交覆盖和停车覆盖承接，否则会出现阻力告警。
25. 用暂停/倍速管理节奏，保存城市进度。
26. 解锁更高阶服务、`post_office` 邮政局、`memorial_garden` 纪念花园、`police_precinct` 警署、轨道交通、城际枢纽、配送中心、货运铁路和清洁基础设施，并扩展新区；中后期通过 `specialized_industry`、`mail_service`、`transit_reliability`、`healthcare_capacity`、`education_capacity`、`deathcare_ready`、`police_readiness` 和 `livable_district` 里程碑确认城市已经形成可持续的本地资源产业链、可靠邮件配送网络、可靠公交网络、医疗响应、学位供给、生命关怀服务、警务响应能力与宜居街区。

## 微信小游戏导出注意
- `miniprogram/game.json` 不使用 `workers` 字段，避免微信小游戏校验报错。
- WebGL 产物必须通过 Unity/团结微信小游戏转换 SDK 生成。
- 需要关注首包体积、纹理压缩、WASM 加载提示、弱网重试和横屏适配。
- 微信平台能力只通过 `WeChatMiniGameBridge` 接入，玩法核心不得直接依赖 `wx`。
- 存档在微信环境使用 `wx.setStorageSync` / `wx.getStorageSync`，编辑器环境回退到 `PlayerPrefs`。
- `WECHAT_SAFE_LIFECYCLE_FEEDBACK` 只复用现有 `CitySaveController` 和 `WeChatMiniGameBridge`：微信环境切后台/暂停自动保存，关键城市命令和保存结果使用安全触觉反馈；Editor 下回退到 `PlayerPrefs` 与无触觉 fallback，不新增 worker，也不改 `miniprogram/game.json`。

## 本地验证限制
当前环境未检测到可用的 Unity/Unity Hub 命令，因此本仓库目前只能做结构与源码静态校验。Unity Console 编译、场景运行、真机性能和微信开发者工具预览需要在具备 Unity 环境的机器上完成。

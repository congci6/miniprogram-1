import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';

const modeArg = process.argv.find((arg) => arg.startsWith('--mode='));
const verifyMode = modeArg ? modeArg.slice('--mode='.length) : (process.env.VERIFY_UNITY_MODE || 'scaffold');
assert(['scaffold', 'exported'].includes(verifyMode), `Unknown verify mode: ${verifyMode}. Expected scaffold or exported.`);

const requiredFiles = [
  'unity/Assets/Scripts/PocketCity/Core/CityTypes.cs',
  'unity/Assets/Scripts/PocketCity/Core/CityConfig.cs',
  'unity/Assets/Scripts/PocketCity/Simulation/CityGridCore.cs',
  'unity/Assets/Scripts/PocketCity/Simulation/CitySimulationCore.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityGameController.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityCameraController.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityHudViewModel.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityInteractionController.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityMapRenderer.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CitySaveController.cs',
  'unity/Assets/Editor/PocketCity/PrototypeSceneFactory.cs',
  'unity/Assets/Editor/PocketCity/VisualAssetFactory.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityRuntimeHud.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/WeChatMiniGameBridge.cs',
  'unity/Assets/Plugins/WebGL/WeChatBridge.jslib',
  'unity/Assets/Editor/PocketCity/DefaultCityConfigFactory.cs',
  'unity/Packages/manifest.json',
  'unity/ProjectSettings/EditorBuildSettings.asset',
  'unity/ProjectSettings/ProjectVersion.txt',
  'unity/Assets/Shaders/PocketCityVertexColorTransparent.shader',
  'unity/Assets/Scenes/PocketCityPrototype.unity',
  'docs/UNITY_ARCHITECTURE.md',
  'docs/UNITY_UI_ART_DIRECTION.md',
  'docs/LOW_POLY_ISOMETRIC_REFERENCE_UI.md',
  'miniprogram/game.js',
  'miniprogram/game.json',
];

const retiredFiles = [
  'src',
  'index.html',
  'tsconfig.json',
  'vite.config.ts',
  'vitest.config.ts',
  'package-lock.json',
];

const buildingIds = [
  'residential_pod',
  'apartment_block',
  'market_corner',
  'mixed_use_block',
  'office_studio',
  'research_campus',
  'maker_yard',
  'resource_processor',
  'pocket_park',
  'city_plaza',
  'convention_center',
  'city_hall',
  'micro_power',
  'solar_farm',
  'water_tower',
  'water_reclaimer',
  'waste_to_energy_plant',
  'health_post',
  'district_hospital',
  'emergency_shelter',
  'memorial_garden',
  'bus_hub',
  'metro_station',
  'intercity_terminal',
  'cargo_depot',
  'distribution_center',
  'freight_rail_terminal',
  'primary_school',
  'community_college',
  'fire_station',
  'police_kiosk',
  'police_precinct',
  'telecom_hub',
  'post_office',
  'road_maintenance_depot',
  'parking_garage',
  'rain_garden',
  'recycling_yard',
];

const resourceSpecializationMarkers = [
  'ResourceSpecialization',
  'ResourcePotential',
  'IndustrialSpecialization',
  'ComputeResourceSpecialization',
  'ResourceSpecializationForBuildings',
  'ResourcePotentialForBuilding',
  'TerrainResourcePotentialForRect',
  'specialized_industry',
  '\\u8d44\\u6e90\\u9002\\u914d',
  '\\u672c\\u5730\\u8d44\\u6e90\\u9002\\u914d\\u4e0d\\u8db3',
];

const mailServiceMarkers = [
  'post_office',
  'mail_service',
  'MailCoverage',
  'MailLoad',
  'MailCapacity',
  'MailUtilization',
  'MailReliability',
  'MailAccess',
  'ConnectedMailBuildings',
  'MailCapacityForBuildings',
  'MailBuildingCapacity',
  'MailWeightForBuilding',
  'ApplyMailTileAccess',
  'IsMailBuilding',
  'IsMailSensitiveBuilding',
  '\\u7f3a\\u5c11\\u90ae\\u653f\\u670d\\u52a1',
  '\\u90ae\\u653f\\u5bb9\\u91cf\\u4e0d\\u8db3',
  '\\u90ae\\u4ef6\\u914d\\u9001\\u53d7\\u963b',
];

const fireResilienceCoreMarkers = [
  'FireRisk',
  'FireProtection',
  'FireLoad',
  'FireCapacity',
  'FireUtilization',
  'FireResponse',
  'FireProtectionAccess',
  'ConnectedFireBuildings',
  'FireCapacityForBuildings',
  'FireBuildingCapacity',
  'FireRiskForBuilding',
  'ApplyFireProtectionTileAccess',
  'ComputeFireRisk',
  'ComputeFireResponse',
  '缺少消防覆盖',
  '消防容量不足',
  '火灾风险偏高',
  'fire_resilience',
];

const fireResilienceTypeMarkers = [
  'FireRisk',
  'FireProtection',
  'FireLoad',
  'FireCapacity',
  'FireUtilization',
  'FireResponse',
  'FireProtectionAccess',
];

const fireResilienceHudMarkers = [
  'FireRisk',
  'FireProtection',
  'FireUtilization',
  'FireResponse',
  'FireProtectionAccess',
];

const medicalCapacityCoreMarkers = [
  'HealthLoad',
  'HealthCapacity',
  'HealthUtilization',
  'MedicalResponse',
  'PatientBacklog',
  'HealthCapacityForBuildings',
  'HealthBuildingCapacity',
  'HealthUtilization',
  'ComputeMedicalResponse',
  'ComputePatientBacklog',
  '医疗容量不足',
  '医疗响应偏低',
  '病患积压偏高',
  'healthcare_capacity',
];

const medicalCapacityTypeMarkers = [
  'HealthLoad',
  'HealthCapacity',
  'HealthUtilization',
  'MedicalResponse',
  'PatientBacklog',
];

const medicalCapacityHudMarkers = [
  'HealthUtilization',
  'MedicalResponse',
  'PatientBacklog',
];

const educationCapacityCoreMarkers = [
  'EducationLoad',
  'EducationCapacity',
  'EducationUtilization',
  'StudentBacklog',
  'LearningPipeline',
  'EducationCapacityForBuildings',
  'EducationBuildingCapacity',
  'ComputeStudentBacklog',
  'ComputeLearningPipeline',
  ['教育容量不足', '学位容量不足'],
  '入学积压偏高',
  ['学习通道偏弱', '学习通道薄弱'],
  'education_capacity',
];

const educationCapacityTypeMarkers = [
  'EducationLoad',
  'EducationCapacity',
  'EducationUtilization',
  'StudentBacklog',
  'LearningPipeline',
];

const educationCapacityHudMarkers = [
  'EducationUtilization',
  'StudentBacklog',
  'LearningPipeline',
];

const transitReliabilityCoreMarkers = [
  'TransitReliability',
  'TransitWaitPressure',
  'ComputeTransitWaitPressure',
  'rawTransitCoverage',
  'effectiveCoverageDrop',
  'transitImpactWaitPressure',
  'transit_reliability',
  '公交可靠性偏低',
  '公交候车压力偏高',
];

const transitReliabilityTypeMarkers = [
  'TransitReliability',
  'TransitWaitPressure',
];

const transitReliabilityHudMarkers = [
  'TransitReliability',
  'TransitWaitPressure',
];

const trafficFlowCoreMarkers = [
  'IntersectionDelay',
  'RoadBottleneckPressure',
  'ComputeIntersectionDelay',
  'ComputeRoadBottleneckPressure',
  'PolicyAdjustedIntersectionDelay',
  'roadBottleneckPressure / 8',
  'roadBottleneckPressure / 5',
  'traffic_flow',
  '道路瓶颈偏高',
  '路口延误偏高',
];

const trafficFlowTypeMarkers = [
  'IntersectionDelay',
  'RoadBottleneckPressure',
];

const trafficFlowHudMarkers = [
  'IntersectionDelay',
  'RoadBottleneckPressure',
];

const livingConditionCoreMarkers = [
  'LivingCondition',
  'LivingPressure',
  'ComputeLivingCondition',
  'ComputeLivingPressure',
  'LivingConditionPenalty',
  'LivingConditionBonus',
  'livingCondition / 14',
  'livingPressure / 5',
  'livable_district',
  '宜居度偏低',
  '生活压力偏高',
];

const livingConditionTypeMarkers = [
  'LivingCondition',
  'LivingPressure',
];

const livingConditionHudMarkers = [
  'LivingCondition',
  'LivingPressure',
  'living',
  '宜居',
];

const deathcareCoreMarkers = [
  'memorial_garden',
  'DeathcareCoverage',
  'DeathcareLoad',
  'DeathcareCapacity',
  'DeathcareUtilization',
  'MortalityPressure',
  'DeathcareAccess',
  'ConnectedDeathcareBuildings',
  'DeathcareCapacityForBuildings',
  'DeathcareBuildingCapacity',
  'DeathcareWeightForBuilding',
  'ApplyDeathcareTileAccess',
  'IsDeathcareBuilding',
  'IsDeathcareSensitiveBuilding',
  'ComputeMortalityPressure',
  '缺少生命关怀',
  '生命关怀容量不足',
  '死亡压力偏高',
  'deathcare_ready',
];

const deathcareTypeMarkers = [
  'DeathcareAccess',
  'DeathcareCoverage',
  'DeathcareLoad',
  'DeathcareCapacity',
  'DeathcareUtilization',
  'MortalityPressure',
];

const deathcareHudMarkers = [
  'DeathcareCoverage',
  'DeathcareUtilization',
  'MortalityPressure',
  'DeathcareAccess',
];

const policeEnforcementCoreMarkers = [
  'police_precinct',
  'SecurityLoad',
  'SecurityCapacity',
  'SecurityUtilization',
  'PoliceResponse',
  'CaseBacklog',
  'SecurityCapacityForBuildings',
  'SecurityBuildingCapacity',
  'SecurityUtilization',
  'ComputePoliceResponse',
  'ComputeCaseBacklog',
  '警务容量不足',
  '警务响应偏低',
  '案件积压偏高',
  'police_readiness',
];

const policeEnforcementTypeMarkers = [
  'SecurityLoad',
  'SecurityCapacity',
  'SecurityUtilization',
  'PoliceResponse',
  'CaseBacklog',
];

const policeEnforcementHudMarkers = [
  'SecurityUtilization',
  'PoliceResponse',
  'CaseBacklog',
];

const serviceEquityGapSourceCoreMarkers = [
  'ServiceGapPressure',
  'ServiceGapFocus',
  'AddServiceGap',
  'ComputeServiceGapPressure',
  'ServiceGapFocusLabel',
  '服务缺口',
];

const serviceEquityGapSourceTypeMarkers = [
  'ServiceGapPressure',
  'ServiceGapFocus',
];

const serviceEquityGapSourceHudMarkers = [
  'UnderservedResidents',
  'ServiceGapFocus',
];

const objectiveActionAdviceCoreMarkers = [
  'ObjectiveHintWithAdvice',
  'ObjectiveAdviceFor',
  '建议：',
  'balanced_services',
  'ServiceGapFocus',
  'transit_reliability',
];

const objectiveActionAdviceHudMarkers = [
  'ObjectiveHint',
  'metrics.ActiveObjective.Hint',
  'snapshot.ObjectiveHint',
];

const alertPriorityDigestHudMarkers = [
  'AddPrioritizedAlerts',
  'AlertPriority',
  ['AlertPriorityDigestLimit', 'AlertDigestLimit', 'MaxPrioritizedAlerts'],
  'snapshot.Alerts',
  ['target.Add("+"', 'target.Add($"+', 'snapshot.Alerts.Add("+"', 'snapshot.Alerts.Add($"+'],
];

const riskForecastAdvisorCoreMarkers = [
  'RISK_FORECAST_ADVISOR',
  'ForecastRisk',
  'ForecastFocus',
  'ForecastAction',
  'CashRunwayDays',
  ['RiskForecastAdvisor', 'ComputeForecastRisk'],
];

const riskForecastAdvisorTypeMarkers = [
  'ForecastRisk',
  'ForecastFocus',
  'ForecastAction',
  'CashRunwayDays',
];

const riskForecastAdvisorHudMarkers = [
  'ForecastRisk',
  'ForecastFocus',
  'ForecastAction',
  'CashRunwayDays',
];

const cityEventDigestCoreMarkers = [
  'CITY_EVENT_DIGEST',
  ['AddCityEvent', 'RecordCityEvent'],
  ['PushCityEvent', 'AppendCityEvent'],
];

const cityEventDigestTypeMarkers = [
  ['RecentEvents', 'EventDigest'],
];

const cityEventDigestHudMarkers = [
  ['RecentEvents', 'EventDigest', 'EventDigestText'],
];

const cityEventDigestPresentationMarkers = [
  'BuildEventDigestText',
  ['BuildEventDigestText', 'BuildCityEventDigestText', 'BuildRecentEventText', 'FormatEventDigestText'],
];

const demandDriverAnalysisCoreMarkers = [
  'DEMAND_DRIVER_ANALYSIS',
  'DemandFocus',
  'DemandDriver',
  'DemandAction',
  'DemandUrgency',
  ['AnalyzeDemandDrivers', 'ComputeDemandInsight'],
];

const demandDriverAnalysisTypeMarkers = [
  'DemandFocus',
  'DemandDriver',
  'DemandAction',
  'DemandUrgency',
];

const demandDriverAnalysisHudMarkers = [
  'DemandFocus',
  'DemandDriver',
  'DemandAction',
  'DemandUrgency',
];

const budgetBreakdownAdvisorCoreMarkers = [
  'BUDGET_BREAKDOWN_ADVISOR',
  'BudgetStress',
  'BudgetFocus',
  'BudgetDriver',
  'BudgetAction',
  ['BudgetBreakdownAdvisor', 'ComputeBudgetBreakdown'],
];

const budgetBreakdownAdvisorTypeMarkers = [
  'BudgetStress',
  'BudgetFocus',
  'BudgetDriver',
  'BudgetAction',
];

const budgetBreakdownAdvisorHudMarkers = [
  'BudgetStress',
  'BudgetFocus',
  'BudgetDriver',
  'BudgetAction',
  'BudgetInsightText',
];

const budgetBreakdownAdvisorRuntimeHudMarkers = [
  'BudgetInsightText',
  'BuildObjectiveHintText',
];

const districtPriorityAdvisorCoreMarkers = [
  'DISTRICT_PRIORITY_ADVISOR',
  'DistrictPriorityScore',
  'DistrictPriorityFocus',
  'DistrictPriorityDriver',
  'DistrictPriorityAction',
  ['DistrictPriorityAdvisor', 'ComputeDistrictPriority'],
];

const districtPriorityAdvisorTypeMarkers = [
  'DistrictPriorityScore',
  'DistrictPriorityFocus',
  'DistrictPriorityDriver',
  'DistrictPriorityAction',
];

const districtPriorityAdvisorHudMarkers = [
  'DistrictPriorityScore',
  'DistrictPriorityFocus',
  'DistrictPriorityDriver',
  'DistrictPriorityAction',
  'DistrictPriorityText',
];

const districtPriorityAdvisorRuntimeHudMarkers = [
  'DistrictPriorityText',
  'BuildObjectiveHintText',
];

const serviceGapAdvisorCoreMarkers = [
  'SERVICE_GAP_ADVISOR',
  'ServiceGapAdvisorScore',
  'ServiceGapAdvisorFocus',
  'ServiceGapAdvisorDriver',
  'ServiceGapAdvisorAction',
  'ServiceGapPressure',
  'ServiceGapFocus',
  ['ServiceGapAdvisor', 'ComputeServiceGapAdvisor', 'ComputeServiceGapAdvice'],
];

const serviceGapAdvisorTypeMarkers = [
  'ServiceGapAdvisorScore',
  'ServiceGapAdvisorFocus',
  'ServiceGapAdvisorDriver',
  'ServiceGapAdvisorAction',
];

const serviceGapAdvisorHudMarkers = [
  'ServiceGapAdvisorScore',
  'ServiceGapAdvisorFocus',
  'ServiceGapAdvisorDriver',
  'ServiceGapAdvisorAction',
  'ServiceGapText',
  ['BuildServiceGapInsightText', 'BuildServiceGapText'],
];

const serviceGapAdvisorRuntimeHudMarkers = [
  'ServiceGapText',
  'BuildObjectiveHintText',
];

const growthBottleneckAdvisorCoreMarkers = [
  'GROWTH_BOTTLENECK_ADVISOR',
  'GrowthBottleneckScore',
  'GrowthBottleneckFocus',
  'GrowthBottleneckDriver',
  'GrowthBottleneckAction',
  ['GrowthBottleneckAdvisor', 'ComputeGrowthBottleneckAdvice'],
  'AddGrowthBottleneckCandidate',
];

const growthBottleneckAdvisorTypeMarkers = [
  'GrowthBottleneckScore',
  'GrowthBottleneckFocus',
  'GrowthBottleneckDriver',
  'GrowthBottleneckAction',
];

const growthBottleneckAdvisorHudMarkers = [
  'GrowthBottleneckScore',
  'GrowthBottleneckFocus',
  'GrowthBottleneckDriver',
  'GrowthBottleneckAction',
  'GrowthBottleneckText',
  ['BuildGrowthBottleneckText', 'BuildGrowthBottleneckInsightText'],
  'ShouldShowGrowthBottleneck',
];

const growthBottleneckAdvisorRuntimeHudMarkers = [
  'GrowthBottleneckText',
  'BuildObjectiveHintText',
];

const commuteCorridorAdvisorCoreMarkers = [
  'COMMUTE_CORRIDOR_ADVISOR',
  'CommuteCorridorScore',
  'CommuteCorridorFocus',
  'CommuteCorridorDriver',
  'CommuteCorridorAction',
  ['CommuteCorridorAdvisor', 'ComputeCommuteCorridorAdvice'],
  'AddCommuteCorridorCandidate',
  'CommuteEfficiency',
  'CarDependency',
  'TransitWaitPressure',
  'ParkingPressure',
  'LogisticsUtilization',
  'RegionalConnectivity',
];

const commuteCorridorAdvisorTypeMarkers = [
  'CommuteCorridorScore',
  'CommuteCorridorFocus',
  'CommuteCorridorDriver',
  'CommuteCorridorAction',
];

const commuteCorridorAdvisorHudMarkers = [
  'CommuteCorridorScore',
  'CommuteCorridorFocus',
  'CommuteCorridorDriver',
  'CommuteCorridorAction',
  'CommuteCorridorText',
  ['BuildCommuteCorridorText', 'BuildCommuteCorridorInsightText'],
  'ShouldShowCommuteCorridor',
];

const commuteCorridorAdvisorRuntimeHudMarkers = [
  'CommuteCorridorText',
  'BuildObjectiveHintText',
];

const economicSpecializationAdvisorCoreMarkers = [
  'ECONOMIC_SPECIALIZATION_ADVISOR',
  'EconomicSpecializationScore',
  'EconomicSpecializationFocus',
  'EconomicSpecializationDriver',
  'EconomicSpecializationAction',
  ['EconomicSpecializationAdvisor', 'ComputeEconomicSpecializationAdvice'],
  'AddEconomicSpecializationCandidate',
  'BusinessEfficiency',
  'InnovationCapacity',
  'OfficeJobs',
  'WorkforceSkill',
  'AdvancedEducationCoverage',
  'IndustrialSpecialization',
  'ResourceSpecialization',
  'LocalGoodsSupply',
  'GoodsBalance',
  'SupplyChainStability',
  'LogisticsCoverage',
  'LogisticsUtilization',
  'Attractiveness',
  'Visitors',
  'TourismIncome',
  'MixedUseBuildings',
  'RegionalConnectivity',
];

const economicSpecializationAdvisorTypeMarkers = [
  'EconomicSpecializationScore',
  'EconomicSpecializationFocus',
  'EconomicSpecializationDriver',
  'EconomicSpecializationAction',
];

const economicSpecializationAdvisorHudMarkers = [
  'EconomicSpecializationScore',
  'EconomicSpecializationFocus',
  'EconomicSpecializationDriver',
  'EconomicSpecializationAction',
  'EconomicSpecializationText',
  ['BuildEconomicSpecializationText', 'BuildEconomicSpecializationInsightText'],
  'ShouldShowEconomicSpecialization',
];

const economicSpecializationAdvisorRuntimeHudMarkers = [
  'EconomicSpecializationText',
  'BuildObjectiveHintText',
];

const buildingUpgradeReadinessAdvisorCoreMarkers = [
  'BUILDING_UPGRADE_READINESS_ADVISOR',
  'BuildingUpgradeReadinessScore',
  'BuildingUpgradeReadyCount',
  'BuildingUpgradeBlockedCount',
  'BuildingUpgradeReadinessFocus',
  'BuildingUpgradeReadinessDriver',
  'BuildingUpgradeReadinessAction',
  ['BuildingUpgradeReadinessAdvisor', 'ComputeBuildingUpgradeReadiness'],
  'BuildingUpgradeScore',
  'RequiredScoreForNextLevel',
  'RequiredAgeForNextLevel',
  'IsUpgradeableBuilding',
  'BuildingUpgradeBlocker',
  'AddBuildingUpgradeCandidate',
];

const buildingUpgradeReadinessAdvisorTypeMarkers = [
  'BuildingUpgradeReadinessScore',
  'BuildingUpgradeReadyCount',
  'BuildingUpgradeBlockedCount',
  'BuildingUpgradeReadinessFocus',
  'BuildingUpgradeReadinessDriver',
  'BuildingUpgradeReadinessAction',
];

const buildingUpgradeReadinessAdvisorHudMarkers = [
  'BuildingUpgradeReadinessScore',
  'BuildingUpgradeReadyCount',
  'BuildingUpgradeBlockedCount',
  'BuildingUpgradeReadinessFocus',
  'BuildingUpgradeReadinessDriver',
  'BuildingUpgradeReadinessAction',
  'BuildingUpgradeReadinessText',
  ['BuildBuildingUpgradeReadinessText', 'BuildBuildingUpgradeReadinessInsightText'],
  'ShouldShowBuildingUpgradeReadiness',
];

const buildingUpgradeReadinessAdvisorRuntimeHudMarkers = [
  'BuildingUpgradeReadinessText',
  'BuildObjectiveHintText',
];

const infrastructureResilienceAdvisorCoreMarkers = [
  'INFRASTRUCTURE_RESILIENCE_ADVISOR',
  'InfrastructureResilienceScore',
  'InfrastructureResilienceFocus',
  'InfrastructureResilienceDriver',
  'InfrastructureResilienceAction',
  ['InfrastructureResilienceAdvisor', 'ComputeInfrastructureResilienceAdvice'],
  'AddInfrastructureResilienceCandidate',
  'RoadMaintenanceCoverage',
  'UtilityReliability',
  'WastewaterReliability',
  'StormwaterResilience',
  'FloodRisk',
  'EmergencyResponse',
  'DisasterPreparedness',
  'DisasterRisk',
  'MaintenanceCondition',
];

const infrastructureResilienceAdvisorTypeMarkers = [
  'InfrastructureResilienceScore',
  'InfrastructureResilienceFocus',
  'InfrastructureResilienceDriver',
  'InfrastructureResilienceAction',
];

const infrastructureResilienceAdvisorHudMarkers = [
  'InfrastructureResilienceScore',
  'InfrastructureResilienceFocus',
  'InfrastructureResilienceDriver',
  'InfrastructureResilienceAction',
  'InfrastructureResilienceText',
  'BuildInfrastructureResilienceText',
  'ShouldShowInfrastructureResilience',
];

const infrastructureResilienceAdvisorRuntimeHudMarkers = [
  'InfrastructureResilienceText',
  'INFRASTRUCTURE_RESILIENCE_TOOL_RECOMMENDATIONS',
  'InfrastructureRoadToolScore',
  'InfrastructureToolRecommendationScore',
  'InfrastructureToolDriverLabel',
  'InfrastructureFocusHasAny',
  'BuildObjectiveHintText',
];

const housingAffordabilityAdvisorCoreMarkers = [
  'HOUSING_AFFORDABILITY_ADVISOR',
  'HousingAffordabilityScore',
  'HousingAffordabilityFocus',
  'HousingAffordabilityDriver',
  'HousingAffordabilityAction',
  ['HousingAffordabilityAdvisor', 'ComputeHousingAffordabilityAdvice'],
  'AddHousingAffordabilityCandidate',
  'RentPressure',
  'HousingCapacity',
  'Population',
  'ResidentialZoneTiles',
  'HighDensityResidentialBuildings',
  'JobsHousingBalance',
  'LivingCondition',
  'LivingPressure',
  'TransitCoverage',
  'ServiceEquity',
  'AffordableHousing',
];

const housingAffordabilityAdvisorTypeMarkers = [
  'HousingAffordabilityScore',
  'HousingAffordabilityFocus',
  'HousingAffordabilityDriver',
  'HousingAffordabilityAction',
];

const housingAffordabilityAdvisorHudMarkers = [
  'HousingAffordabilityScore',
  'HousingAffordabilityFocus',
  'HousingAffordabilityDriver',
  'HousingAffordabilityAction',
  'HousingAffordabilityText',
  ['BuildHousingAffordabilityText', 'BuildHousingAffordabilityInsightText'],
  'ShouldShowHousingAffordability',
];

const housingAffordabilityAdvisorRuntimeHudMarkers = [
  'HousingAffordabilityText',
  'BuildObjectiveHintText',
];

const tileInspectorOverlayLegendRuntimeHudMarkers = [
  'TILE_INSPECTOR_OVERLAY_LEGEND',
  ['TileInspectorText', 'SelectedTileText', 'TileReadoutText'],
  ['OverlayLegendText', 'OverlayLegend'],
  ['BuildTileInspectorText', 'BuildSelectedTileText', 'BuildTileReadoutText'],
  ['BuildOverlayLegendText', 'BuildOverlayLegend', 'LegendForOverlay'],
  'controller.GetTile',
  'controller.OverlayMode',
  'TileData',
  'Terrain',
  'Zone',
  'RoadId',
  'BuildingId',
  'Traffic',
  'Pollution',
  'Noise',
  'LandValue',
  'TransitAccess',
  'LogisticsAccess',
  'WasteAccess',
  'CommunicationAccess',
  'MailAccess',
  'RoadMaintenanceAccess',
  'ParkingAccess',
  'StormwaterAccess',
  ['\\u4f4e', '低'],
  ['\\u4e2d', '中'],
  ['\\u9ad8', '高'],
];

const actionableTileDiagnosisRuntimeHudMarkers = [
  'CITY_ACTIONABLE_TILE_DIAGNOSIS',
  'TILE_OVERLAY_SHORT_GAP_LABELS',
  'BuildTileActionDiagnosis',
  'TileHasUse',
  'ServiceWeaknessLabel',
  'CommunicationWeaknessLabel',
  'TrafficStressLabel',
  'UtilityOverlayValueText',
  '\\u8bca\\u65ad:\\u9053\\u8def\\u6ee1\\u8f7d',
  '\\u8bca\\u65ad:\\u670d\\u52a1\\u7a7a\\u767d',
  '\\u8bca\\u65ad:\\u7f3a\\u516c\\u4ea4',
  '\\u8bca\\u65ad:\\u96e8\\u6d2a\\u8584\\u5f31',
];

const buildingVisualPrefabLibraryMarkers = [
  'BUILDING_VISUAL_PREFAB_LIBRARY',
  'ModelKeyVisualCatalog',
  'CreateBuildingVisual',
  'FallbackCubeVisual',
  'MaterialForDefinition',
  'controller.GetBuildingDefinition',
  'BuildingDefinition',
  'ModelKey',
  'AddPart',
  'residential',
  'commercial',
  'mixed_use',
  'office',
  'industrial',
  'clinic',
  'school',
  'transit',
  'communications',
  'parking',
  'waste_to_energy',
  'landmark',
];

const hudInsightPriorityStackRuntimeHudMarkers = [
  ['BuildInsightPriorityStack', 'BuildSmartInsightPriorityStack', 'BuildObjectiveInsightStack', 'BuildObjectiveInsights'],
  ['InsightPriority', 'ObjectiveInsightPriority', 'AddInsightPriority'],
  ['MaxObjectiveInsights', 'ObjectiveInsightLimit', 'MaxObjectiveHintInsights'],
  'ObjectiveInsightParts',
  'ForecastText',
  'BudgetInsightText',
  'DistrictPriorityText',
  'RoadHierarchyText',
  'CommuteCorridorText',
  'EconomicSpecializationText',
  'ServiceGapText',
  'GrowthBottleneckText',
  'BuildingUpgradeReadinessText',
  'HousingAffordabilityText',
  'DemandInsightText',
  'RecentEventText',
];

const objectiveActionAdviceRuntimeHudMarkers = [
  'ObjectiveHint',
  'snapshot.ObjectiveHint',
];

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function escapedUnicode(marker) {
  return marker.replace(/[^\x00-\x7F]/g, (ch) => `\\u${ch.charCodeAt(0).toString(16).padStart(4, '0')}`);
}

function includesMarker(source, marker) {
  return source.includes(marker) || source.includes(escapedUnicode(marker));
}

function walkFiles(root, suffix) {
  const files = [];
  for (const entry of readdirSync(root)) {
    const fullPath = `${root}/${entry}`;
    if (statSync(fullPath).isDirectory()) {
      files.push(...walkFiles(fullPath, suffix));
    } else if (fullPath.endsWith(suffix)) {
      files.push(fullPath);
    }
  }

  return files;
}

function assertNoForbiddenRuntimeMarkers() {
  const forbiddenMarkers = [
    '"workers"',
    'workers',
    'texImage3D',
    'WebGL2RenderingContext',
    'webgl2',
    'SharedArrayBuffer',
    'createImageBitmap',
    'new Worker',
    'Worker(',
  ];
  const files = [
    ...walkFiles('miniprogram', ''),
    ...walkFiles('unity/Assets', ''),
  ];

  for (const file of files) {
    const source = readFileSync(file, 'utf8');
    for (const marker of forbiddenMarkers) {
      assert(!source.includes(marker), `Forbidden mini game runtime marker "${marker}" found in ${file}`);
    }
  }
}

function assertNoBrokenCSharpStrings(file) {
  const source = readFileSync(file, 'utf8');
  assert(!source.includes('\uFFFD'), `C# file contains replacement characters: ${file}`);

  let inString = false;
  let escaped = false;
  let verbatim = false;
  let line = 1;
  let startLine = 1;

  for (let i = 0; i < source.length; i += 1) {
    const ch = source[i];
    if (ch === '\n') {
      assert(!inString || verbatim, `C# string crosses a newline in ${file}, starting line ${startLine}`);
      line += 1;
      escaped = false;
      continue;
    }

    if (!inString) {
      assert(!(ch === '\\' && source[i + 1] === 'n'), `C# file contains a literal \\n outside a string: ${file}, line ${line}`);
      if (ch === '"') {
        inString = true;
        escaped = false;
        verbatim = source[i - 1] === '@' || (source[i - 1] === '$' && source[i - 2] === '@');
        startLine = line;
      }
      continue;
    }

    if (verbatim) {
      if (ch === '"' && source[i + 1] === '"') {
        i += 1;
      } else if (ch === '"') {
        inString = false;
        verbatim = false;
      }
      continue;
    }

    if (escaped) {
      escaped = false;
    } else if (ch === '\\') {
      escaped = true;
    } else if (ch === '"') {
      inString = false;
    }
  }

  assert(!inString, `C# file has an unterminated string: ${file}, starting line ${startLine}`);
}

for (const file of requiredFiles) {
  assert(existsSync(file), `Missing required Unity-first file: ${file}`);
}

for (const file of retiredFiles) {
  assert(!existsSync(file), `TypeScript runtime artifact is still active: ${file}`);
}

for (const file of walkFiles('unity/Assets', '.cs')) {
  assertNoBrokenCSharpStrings(file);
}

assertNoForbiddenRuntimeMarkers();

const packageJson = JSON.parse(readFileSync('package.json', 'utf8'));
assert(!packageJson.dependencies, 'Root package.json must not declare TypeScript runtime dependencies.');
assert(!packageJson.devDependencies, 'Root package.json must not declare Vite/Vitest dev dependencies.');

const gameJson = JSON.parse(readFileSync('miniprogram/game.json', 'utf8'));
assert(!Object.prototype.hasOwnProperty.call(gameJson, 'workers'), 'miniprogram/game.json must not contain workers.');
assert(gameJson.deviceOrientation === 'landscape', 'Unity mini game placeholder must stay landscape.');

const gameJs = readFileSync('miniprogram/game.js', 'utf8');
assert(gameJs.trim().length > 0, 'miniprogram/game.js must not be empty.');
if (verifyMode === 'scaffold') {
  assert(gameJs.includes('UNITY_BUILD_PENDING'), 'miniprogram/game.js should be the Unity build placeholder in scaffold mode.');
} else {
  assert(!gameJs.includes('UNITY_BUILD_PENDING'), 'miniprogram/game.js must be replaced by exported Unity output in exported mode.');
  assert(!gameJs.includes('Unity build pending'), 'miniprogram/game.js must not contain the placeholder modal in exported mode.');
}

const factory = readFileSync('unity/Assets/Editor/PocketCity/DefaultCityConfigFactory.cs', 'utf8');
const expectedBuildingCount = 38;
const expectedDemandStatCount = 33;
const expectedTopStatCount = 8;
const expectedOverlayButtonCount = 14;
const expectedToolButtonCount = 48;
const expectedControlButtonCount = 7;
const expectedPolicyButtonCount = 9;
assert(buildingIds.length === expectedBuildingCount, `Unity scaffold expected ${expectedBuildingCount} building ids, found ${buildingIds.length}.`);
for (const id of buildingIds) {
  assert(factory.includes(`Id = "${id}"`), `Default CityConfig factory missing building id: ${id}`);
}
assert((factory.match(/config\.Buildings\.Add\(new BuildingDefinition/g) || []).length === expectedBuildingCount, `Default CityConfig factory should define ${expectedBuildingCount} buildings.`);

for (const marker of ['emergency_shelter', 'ModelKey = "shelter"', '\\u5e94\\u6025\\u907f\\u96be\\u4e2d\\u5fc3']) {
  assert(factory.includes(marker), `Default CityConfig factory missing shelter marker: ${marker}`);
}

const core = readFileSync('unity/Assets/Scripts/PocketCity/Simulation/CitySimulationCore.cs', 'utf8');
for (const marker of ['TryBuildRoad', 'PreviewRoadUpgrade', 'TryUpgradeRoad', 'RoadTier.Arterial', 'RoadCapacityForTier', 'RoadUpkeepForTier', 'ArterialRoadUpgradeCost', 'ArterialRoadTiles', 'RoadConnectivity', 'DeadEndRoadTiles', 'IntersectionRoadTiles', 'ComputeRoadConnectivity', 'connected_grid', '路网连通性偏低', 'Walkability', 'ComputeWalkability', 'walkable_city', '步行可达性偏低', 'EmergencyResponse', 'ComputeEmergencyResponse', 'response_ready', '应急响应偏低', 'MaintenanceCondition', 'ComputeMaintenanceCondition', 'ApplyMaintenanceCondition', 'maintenance_ready', '城市维护状态偏低', 'ZoneSuitabilityForRect', 'ZoneSuitabilityForTile', 'MinZoneSuitabilityForAutoDevelopment', '缺少适宜地块', '适宜度', 'ZoneConflictRiskForRect', 'ComputeLandUseConflict', 'LandUseConflictForTile', 'LandUseConflictPenalty', 'LandUseBufferBonus', 'zoning_buffer', '用地冲突偏高', '缓冲风险', 'PreviewBuilding', 'BuildingSiteScore', 'SiteDiagnosis', 'AverageSiteValue', '选址诊断', 'PreviewZone', 'TrySetZone', 'TryDemolishAt', 'CreateSaveData', 'Version = 6', 'ApplySaveData', 'CycleTaxLevel', 'TaxRatePercent', 'TaxDemandModifier', 'TogglePolicy', 'PolicyMonthlyExpense', 'PolicyRentPressureRelief', 'CityPolicy.AffordableHousing', 'CityServiceBudgetLevel', 'CycleServiceBudgetLevel', 'IssueMunicipalBond', 'BondPrincipal', 'BondPayment', 'ComputeBondPayment', 'MunicipalBondCash', 'debt_service_control', '\\u503a\\u52a1\\u670d\\u52a1\\u8fc7\\u9ad8', 'ServiceBudgetPercent', 'BudgetAdjustedServiceValue', 'ServiceBudgetHappinessModifier', 'service_budget_balance', 'UtilityLoad', 'UtilityCapacity', 'UtilityUtilization', 'UtilityReliability', 'utility_resilience', 'renewable_power', '\\u7f3a\\u5c11\\u6e05\\u6d01\\u7535\\u529b', 'solar_farm', '水电负荷过高', 'ServiceLoad', 'ServiceCapacity', 'ServiceUtilization', 'ServiceEquity', 'UnderservedResidents', 'ResidentialServiceScore', 'ComputeServiceEquity', 'ComputeUnderservedResidents', 'ServiceEquityPenalty', 'ServiceEquityBonus', 'balanced_services', '片区服务不均', 'PublicServiceCapacityForBuildings', 'PublicServiceLoad', 'ServiceReliability', 'ApplyServiceReliability', 'PublicServiceBuildingCapacity', 'service_capacity', '公共服务容量不足', 'ZoneType.Office', 'Metrics.Demand.Office', 'OfficeJobs', 'OfficeZoneTiles', 'IsOfficeBuilding', 'knowledge_economy', 'office_studio', 'ZoneType.MixedUse', 'Metrics.Demand.MixedUse', 'MixedUseBuildings', 'MixedUseZoneTiles', 'IsMixedUseBuilding', 'mixed_core', 'mixed_use_block', 'Attractiveness', 'Visitors', 'TourismIncome', 'LandmarkBuildings', 'ConnectedAttractionBuildings', 'IsAttractionBuilding', 'ComputeAttractiveness', 'ComputeVisitors', 'city_attraction', 'GoodsSupply', 'GoodsDemand', 'GoodsBalance', 'ComputeGoodsDemand', 'ComputeGoodsSupply', 'ComputeGoodsBalance', 'GoodsShortagePenalty', 'GoodsMarketBonus', 'goods_market', '商品供应不足', 'WorkforceSkill', 'LaborShortage', 'ProductivityBonus', 'ComputeWorkforceSkill', 'ComputeLaborShortage', 'ComputeProductivityBonus', 'BusinessEfficiency', 'ComputeBusinessEfficiency', 'BusinessEfficiencyTaxBonus', 'talent_pool', 'JobsHousingBalance', 'CommuteEfficiency', 'CarDependency', 'ParkingPressure', 'ComputeJobsHousingBalance', 'ComputeCommuteEfficiency', 'ComputeCarDependency', 'ComputeParkingPressure', 'ParkingSearchRoadLoad', 'ParkingHappinessPenalty', 'ParkingAccessBonus', 'ParkingAccessPenalty', 'low_car_core', '停车压力偏高', 'CommuteHappinessPenalty', 'smooth_commute', 'EnvironmentQuality', 'NoiseStress', 'ComputeEnvironmentQuality', 'ComputeNoiseStress', 'EnvironmentHappinessPenalty', 'green_city', 'PublicHealth', 'HealthRisk', 'ComputePublicHealth', 'ComputeHealthRisk', 'HealthHappinessPenalty', 'healthy_city', 'plaza', 'TryAutoDevelopZones', 'FindAutoDevelopmentSite', 'AutoDevelopmentCandidate', 'AutoDevelopmentGrant', 'HighDensityResidentialDemand', 'HighDensityResidentialBuildings', 'DevelopedZoneTiles', 'LandUseEfficiency', 'IdleZoneTiles', 'DevelopmentQuality', 'ComputeDevelopmentQuality', 'DevelopmentQualityForBuilding', 'DevelopmentQualityBonus', 'DevelopmentQualityPenalty', 'quality_blocks', '片区品质偏低', 'ComputeLandUseEfficiency', 'IdleZonePenalty', 'CompactLandUseBonus', 'IsGrowthZoneBuilding', 'compact_city', '空置分区过多', 'density_core', 'ZonedDevelopmentBuildings', 'ConnectedParkBuildings', 'ConnectedHealthBuildings', 'ConnectedEducationBuildings', 'ConnectedSafetyBuildings', 'ConnectedSecurityBuildings', 'ParkCoverage', 'HealthCoverage', 'EducationCoverage', 'SafetyCoverage', 'SecurityCoverage', 'CrimePressure', 'ComputeCrimePressure', 'CrimeHappinessPenalty', 'ConnectedTransitBuildings', 'TransitCoverage', 'TransitLoad', 'TransitCapacity', 'TransitUtilization', 'TransitCapacityForBuildings', 'TransitReliability', 'TransitOverloadRoadLoad', 'TransitBuildingCapacity', 'metro_station', 'metro_network', 'CountBuildingsById', '\\u7f3a\\u5c11\\u8f68\\u9053\\u4ea4\\u901a', '\\u516c\\u4ea4\\u8fd0\\u529b +', 'transit_capacity', '公交运力不足', 'ConnectedLogisticsBuildings', 'LogisticsCoverage', 'LogisticsLoad', 'LogisticsCapacity', 'LogisticsUtilization', 'LogisticsCapacityForBuildings', 'LogisticsReliability', 'LogisticsOverloadRoadLoad', 'LogisticsBuildingCapacity', 'freight_capacity', '货运运力不足', 'ConnectedCommunicationBuildings', 'CommunicationCoverage', 'CommunicationLoad', 'CommunicationCapacity', 'CommunicationUtilization', 'CommunicationCapacityForBuildings', 'CommunicationReliability', 'CommunicationBuildingCapacity', 'CommunicationWeightForBuilding', 'ApplyCommunicationTileAccess', 'IsCommunicationBuilding', 'IsCommunicationSensitiveBuilding', 'connected_business', 'communication_capacity', '通信覆盖不足', '通信容量不足', '企业效率偏低', 'ConnectedWasteBuildings', 'WasteCoverage', 'WasteLoad', 'WasteCapacity', 'ApplyParkTileAccess', 'ApplyHealthTileAccess', 'ApplyEducationTileAccess', 'ApplySafetyTileAccess', 'ApplySecurityTileAccess', 'ApplyTransitTileAccess', 'ApplyLogisticsTileAccess', 'ApplyWasteTileAccess', 'SafetyWeightForBuilding', 'SafetyRiskPenalty', 'SecurityWeightForBuilding', 'LogisticsWeightForBuilding', 'WasteShortfallPollution', 'EducationTaxBonus', 'UpdateBuildingLevels', 'BuildingUpgradeScore', 'siteQuality', 'Metrics.DevelopmentQuality / 20', 'LevelScaledOutput', 'UpgradedBuildings', 'NetIncome', 'CityMilestone', 'secure_blocks', 'AverageLandValue', 'ComputeRentPressure', 'RentHappinessPenalty']) {
  assert(core.includes(marker), `Unity simulation core missing marker: ${marker}`);
}

for (const marker of ['emergency_shelter', 'shelter', 'DisasterPreparedness', 'DisasterRisk', 'ConnectedShelterBuildings', 'DisasterPreparednessCapacityForBuildings', 'ComputeDisasterPreparedness', 'ComputeDisasterRisk', 'DisasterPreparednessBuildingCapacity', 'IsShelterBuilding', 'DisasterRiskHappinessPenalty', 'disaster_preparedness', '\\u7f3a\\u5c11\\u5e94\\u6025\\u907f\\u96be', '\\u57ce\\u5e02\\u707e\\u5bb3\\u98ce\\u9669\\u504f\\u9ad8', '\\u707e\\u5bb3\\u51c6\\u5907', '\\u5efa\\u6210 1 \\u5ea7\\u63a5\\u8def\\u5e94\\u6025\\u907f\\u96be\\u4e2d\\u5fc3\\u4e14\\u707e\\u5907\\u8fbe\\u5230 65', '\\u707e\\u5907 +']) {
  assert(core.includes(marker), `Unity simulation core missing disaster preparedness marker: ${marker}`);
}

for (const marker of ['RoadMaintenanceCoverage', 'AccidentRisk', 'RoadSafety', 'ConnectedRoadMaintenanceBuildings', 'RoadMaintenanceCoverageForRoads', 'RoadMaintenanceWeightForRoad', 'ComputeAccidentRisk', 'AccidentRoadLoad', 'ComputeRoadSafety', 'ApplyRoadMaintenanceTileAccess', 'IsRoadCoveredByService', 'IsRoadMaintenanceBuilding', 'road_care', 'safe_roads', '\u9053\u8def\u517b\u62a4\u4e0d\u8db3', '\u9053\u8def\u4e8b\u6545\u98ce\u9669\u504f\u9ad8', '\u9053\u8def\u5b89\u5168\u504f\u4f4e']) {
  assert(core.includes(marker), `Unity simulation core missing road safety marker: ${marker}`);
}

for (const marker of ['FiscalHealth', 'DebtPressure', 'BondPrincipal', 'BondPayment', 'ComputeDebtPressure', 'ComputeFiscalHealth', 'fiscal_credit', 'debt_service_control', '\\u8d22\\u653f\\u4fe1\\u7528\\u504f\\u4f4e', '\\u503a\\u52a1\\u538b\\u529b\\u504f\\u9ad8', '\\u503a\\u52a1\\u670d\\u52a1\\u8fc7\\u9ad8', '\\u73b0\\u91d1\\u7f13\\u51b2\\u4e0d\\u8db3']) {
  assert(core.includes(marker), `Unity simulation core missing fiscal marker: ${marker}`);
}

for (const marker of ['AdministrationEfficiency', 'AdministrationLoad', 'AdministrationCapacity', 'AdministrationUtilization', 'PolicyBacklog', 'ConnectedAdministrationBuildings', 'AdministrationCapacityForBuildings', 'AdministrationBuildingCapacity', 'AdministrationLoad(', 'AdministrationUtilization(', 'ComputeAdministrationEfficiency', 'ComputePolicyBacklog', 'AdministrationAdjustedPolicyExpense', 'AdministrationTaxBonus', 'AdministrationFiscalBonus', 'AdministrationServiceDemandRelief', 'IsAdministrationBuilding', 'city_hall', 'civic_administration', 'administration_capacity', '\\u884c\\u653f\\u6548\\u7387\\u504f\\u4f4e', '\\u653f\\u7b56\\u6267\\u884c\\u8fc7\\u8f7d', '行政容量不足', '政策积压偏高']) {
  assert(includesMarker(core, marker), `Unity simulation core missing administration marker: ${marker}`);
}

for (const marker of ['RegionalConnectivity', 'ConnectedRegionalConnectionBuildings', 'RegionalConnectionCapacityForBuildings', 'RegionalConnectionBuildingCapacity', 'ComputeRegionalConnectivity', 'RegionalTourismBonus', 'IsRegionalConnectionBuilding', 'intercity_terminal', 'regional_gateway', '\\u5916\\u90e8\\u8fde\\u63a5\\u4e0d\\u8db3']) {
  assert(core.includes(marker), `Unity simulation core missing regional connection marker: ${marker}`);
}

for (const marker of ['waste_to_energy_plant', 'waste_to_energy', '\\u7f3a\\u5c11\\u5783\\u573e\\u53d1\\u7535', '\\u8d44\\u6e90\\u56de\\u6536\\u80fd\\u6e90', '\\u56de\\u6536\\u5bb9\\u91cf +']) {
  assert(core.includes(marker), `Unity simulation core missing waste-to-energy marker: ${marker}`);
}

for (const marker of ['convention_center', 'landmark', 'AttractionParkingDemandForBuildings', 'LandmarkTourismIncomeForBuildings', 'convention_draw', '\\u7f3a\\u5c11\\u4f1a\\u5c55\\u5730\\u6807', '\\u4f1a\\u5c55\\u4ea4\\u901a\\u627f\\u538b', '\\u5438\\u5f15\\u529b +']) {
  assert(core.includes(marker), `Unity simulation core missing convention center marker: ${marker}`);
}

for (const marker of ['research_campus', 'innovation', 'InnovationCapacity', 'ConnectedInnovationBuildings', 'InnovationBaseForBuildings', 'ComputeInnovationCapacity', 'InnovationTaxBonus', 'innovation_district', '\\u7f3a\\u5c11\\u7814\\u53d1\\u56ed\\u533a', '\\u7814\\u53d1\\u914d\\u5957\\u4e0d\\u8db3', '\\u521b\\u65b0\\u80fd\\u529b +']) {
  assert(core.includes(marker), `Unity simulation core missing innovation marker: ${marker}`);
}

for (const marker of ['resource_processor', 'resource', 'LocalGoodsSupply', 'ConnectedResourceBuildings', 'ComputeLocalGoodsSupply', 'ResourceBuildingSupply', 'local_supply', '\\u7f3a\\u5c11\\u672c\\u5730\\u8d44\\u6e90', '\\u8d44\\u6e90\\u7269\\u6d41\\u4e0d\\u8db3', '\\u672c\\u5730\\u4f9b\\u7ed9 +']) {
  assert(core.includes(marker), `Unity simulation core missing resource supply marker: ${marker}`);
}

for (const marker of resourceSpecializationMarkers) {
  assert(core.includes(marker), `Unity simulation core missing resource specialization marker: ${marker}`);
}

for (const marker of mailServiceMarkers) {
  assert(core.includes(marker), `Unity simulation core missing mail service marker: ${marker}`);
}

for (const marker of fireResilienceCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing fire resilience marker: ${marker}`);
}

for (const marker of medicalCapacityCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing medical capacity marker: ${marker}`);
}

for (const marker of educationCapacityCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing education capacity marker: ${markerOptions.join(' / ')}`);
}

for (const marker of transitReliabilityCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing transit reliability marker: ${marker}`);
}

for (const marker of trafficFlowCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing traffic flow marker: ${marker}`);
}

for (const marker of livingConditionCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing living condition marker: ${marker}`);
}

for (const marker of deathcareCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing deathcare marker: ${marker}`);
}

for (const marker of policeEnforcementCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing police enforcement marker: ${marker}`);
}

for (const marker of serviceEquityGapSourceCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing service equity gap source marker: ${marker}`);
}

for (const marker of objectiveActionAdviceCoreMarkers) {
  assert(includesMarker(core, marker), `Unity simulation core missing objective action advice marker: ${marker}`);
}

for (const marker of riskForecastAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing risk forecast advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of cityEventDigestCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing city event digest marker: ${markerOptions.join(' / ')}`);
}

for (const marker of demandDriverAnalysisCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing demand driver analysis marker: ${markerOptions.join(' / ')}`);
}

for (const marker of budgetBreakdownAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing budget breakdown advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of districtPriorityAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing district priority advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of serviceGapAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing service gap advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of growthBottleneckAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing growth bottleneck advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of commuteCorridorAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing commute corridor advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of economicSpecializationAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing economic specialization advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of buildingUpgradeReadinessAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing building upgrade readiness advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of infrastructureResilienceAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing infrastructure resilience advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of housingAffordabilityAdvisorCoreMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(core, option)), `Unity simulation core missing housing affordability advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of ['distribution_center', 'warehouse', 'GoodsStorage', 'SupplyChainStability', 'ConnectedWarehouseBuildings', 'ComputeGoodsStorage', 'ComputeSupplyChainStability', 'ApplyGoodsStorageBuffer', 'WarehouseStorageCapacity', 'IsWarehouseBuilding', 'supply_chain_buffer', '\\u7f3a\\u5c11\\u914d\\u9001\\u4e2d\\u5fc3', '\\u4ed3\\u50a8\\u8c03\\u5ea6\\u53d7\\u963b', '\\u4ed3\\u50a8 +']) {
  assert(core.includes(marker), `Unity simulation core missing warehouse buffer marker: ${marker}`);
}

for (const marker of ['freight_rail_terminal', 'freight_rail', 'FreightImportSupply', 'ConnectedFreightRailBuildings', 'ComputeFreightImportSupply', 'FreightRailImportSupply', 'IsFreightRailBuilding', 'rail_freight_gateway', '\\u7f3a\\u5c11\\u8d27\\u8fd0\\u94c1\\u8def', '\\u94c1\\u8def\\u8d27\\u8fd0\\u53d7\\u963b', '\\u94c1\\u8def\\u5bfc\\u5165 +']) {
  assert(core.includes(marker), `Unity simulation core missing freight rail marker: ${marker}`);
}

for (const marker of ['district_hospital', 'regional_healthcare', '\\u7f3a\\u5c11\\u533a\\u57df\\u533b\\u9662']) {
  assert(core.includes(marker), `Unity simulation core missing regional hospital marker: ${marker}`);
}

for (const marker of ['CityPolicy.TrafficSafetyCampaign', 'PolicyAccidentRiskRelief', 'PolicyRoadSafetyBonus']) {
  assert(core.includes(marker), `Unity simulation core missing traffic safety policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.CompleteStreets', 'PolicyWalkabilityBonus', 'PolicyAdjustedCarDependency', 'PolicyAdjustedParkingPressure', 'PolicyAdjustedRoadCapacity', 'complete_streets', '\\u5b8c\\u6574\\u8857\\u9053\\u62e5\\u5835']) {
  assert(core.includes(marker), `Unity simulation core missing complete streets policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.SignalOptimization', 'PolicyAdjustedCongestion', 'PolicySignalCongestionRelief', 'PolicySignalAccidentRelief', 'PolicySignalRoadSafetyBonus', 'signal_optimization', '\\u4fe1\\u53f7\\u4f18\\u5316\\u8fc7\\u8f7d']) {
  assert(core.includes(marker), `Unity simulation core missing signal optimization policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.CongestionPricing', 'PolicyCongestionPricingRelief', 'PolicyCongestionPricingCarRelief', 'PolicyCongestionPricingParkingRelief', 'PolicyCongestionChargeRevenue', 'congestion_pricing', '\\u62e5\\u5835\\u6536\\u8d39\\u963b\\u529b']) {
  assert(core.includes(marker), `Unity simulation core missing congestion pricing policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.ParkingFees', 'PolicyParkingFeeRevenue', 'PolicyParkingFeeCarRelief', 'PolicyParkingFeePressureRelief', 'parking_fees', '\\u505c\\u8f66\\u6536\\u8d39\\u963b\\u529b']) {
  assert(core.includes(marker), `Unity simulation core missing parking fees policy marker: ${marker}`);
}

for (const marker of ['WasteUtilization', 'WasteReliability', 'waste_capacity', '\\u56de\\u6536\\u5bb9\\u91cf\\u4e0d\\u8db3']) {
  assert(core.includes(marker), `Unity simulation core missing waste capacity marker: ${marker}`);
}

for (const marker of ['ConnectedWastewaterBuildings', 'WastewaterLoad', 'WastewaterCapacity', 'WastewaterUtilization', 'WastewaterReliability', 'WastewaterCapacityForBuildings', 'WastewaterBuildingCapacity', 'WastewaterShortfallPollution', 'IsWastewaterBuilding', 'water_sanitation', '\\u6c61\\u6c34\\u5904\\u7406\\u8fc7\\u8f7d', '\\u6c34\\u73af\\u5883\\u98ce\\u9669\\u504f\\u9ad8']) {
  assert(core.includes(marker), `Unity simulation core missing wastewater marker: ${marker}`);
}

for (const marker of ['ConnectedAdvancedEducationBuildings', 'AdvancedEducationCoverage', 'AdvancedEducationWeightForBuilding', 'IsAdvancedEducationBuilding', 'higher_education', '\\u9ad8\\u7b49\\u6559\\u80b2\\u4e0d\\u8db3']) {
  assert(core.includes(marker), `Unity simulation core missing advanced education marker: ${marker}`);
}

for (const marker of ['ConnectedParkingBuildings', 'ParkingCoverage', 'ParkingLoad', 'ParkingCapacity', 'ParkingUtilization', 'ParkingWeightForBuilding', 'ParkingBuildingCapacity', 'ApplyParkingTileAccess', 'IsParkingBuilding', 'parking_relief', '\\u505c\\u8f66\\u8bbe\\u65bd\\u4e0d\\u8db3', '\\u505c\\u8f66\\u8bbe\\u65bd\\u6ee1\\u8f7d']) {
  assert(core.includes(marker), `Unity simulation core missing parking marker: ${marker}`);
}

for (const marker of ['ConnectedStormwaterBuildings', 'StormwaterLoad', 'StormwaterCapacity', 'StormwaterUtilization', 'StormwaterResilience', 'FloodRisk', 'StormwaterCapacityForBuildings', 'StormwaterBuildingCapacity', 'ApplyStormwaterTileAccess', 'IsStormwaterBuilding', 'StormwaterTerrainExposure', 'stormwater_ready', '\\u96e8\\u6d2a\\u5bb9\\u91cf\\u4e0d\\u8db3', '\\u5185\\u6d9d\\u98ce\\u9669\\u504f\\u9ad8']) {
  assert(core.includes(marker), `Unity simulation core missing stormwater marker: ${marker}`);
}

const types = readFileSync('unity/Assets/Scripts/PocketCity/Core/CityTypes.cs', 'utf8');
const cityPolicyEnumMatch = types.match(/public enum CityPolicy\s*\{([\s\S]*?)\}/);
assert(cityPolicyEnumMatch, 'Unity core types missing CityPolicy enum.');
const cityPolicyNames = cityPolicyEnumMatch[1]
  .split(',')
  .map((entry) => entry.trim())
  .filter(Boolean);
assert(JSON.stringify(cityPolicyNames) === JSON.stringify([
  'GreenCode',
  'TransitPriority',
  'GrowthGrants',
  'AffordableHousing',
  'TrafficSafetyCampaign',
  'CompleteStreets',
  'SignalOptimization',
  'CongestionPricing',
  'ParkingFees',
]), 'Unity CityPolicy enum must stay aligned with 9 runtime policy buttons.');
for (const marker of ['CitySaveData', 'SavedBuilding', 'SavedZoneTile', 'SavedRoadSegment', 'RoadTier', 'RoadSegments', 'RoadConnectivity', 'DeadEndRoadTiles', 'IntersectionRoadTiles', 'Walkability', 'EmergencyResponse', 'MaintenanceCondition', 'CityPolicy', 'AffordableHousing', 'CityServiceBudgetLevel', 'ServiceBudgetLevel', 'ServiceBudgetPercent', 'ServiceBudgetExpense', 'UtilityLoad', 'UtilityCapacity', 'UtilityUtilization', 'UtilityReliability', 'ServiceLoad', 'ServiceCapacity', 'ServiceUtilization', 'ServiceEquity', 'UnderservedResidents', 'Office', 'OfficeJobs', 'OfficeZoneTiles', 'MixedUse', 'MixedUseBuildings', 'MixedUseZoneTiles', 'Attractiveness', 'Visitors', 'TourismIncome', 'GoodsSupply', 'LocalGoodsSupply', 'FreightImportSupply', 'GoodsStorage', 'SupplyChainStability', 'GoodsDemand', 'GoodsBalance', 'LandmarkBuildings', 'WorkforceSkill', 'LaborShortage', 'ProductivityBonus', 'BusinessEfficiency', 'JobsHousingBalance', 'CommuteEfficiency', 'CarDependency', 'ParkingPressure', 'ParkingCoverage', 'ParkingLoad', 'ParkingCapacity', 'ParkingUtilization', 'ParkingAccess', 'StormwaterAccess', 'StormwaterLoad', 'StormwaterCapacity', 'StormwaterUtilization', 'StormwaterResilience', 'FloodRisk', 'EnvironmentQuality', 'NoiseStress', 'PublicHealth', 'HealthRisk', 'CityTaxLevel', 'TaxRatePercent', 'TaxLevel', 'PolicyExpense', 'ActivePolicies', 'SiteScore', 'SiteDiagnosis', 'ParkCoverage', 'HealthCoverage', 'EducationCoverage', 'SafetyCoverage', 'SecurityCoverage', 'ParkAccess', 'HealthAccess', 'EducationAccess', 'SafetyAccess', 'SecurityAccess', 'TransitCoverage', 'TransitLoad', 'TransitCapacity', 'TransitUtilization', 'TransitAccess', 'LogisticsCoverage', 'LogisticsLoad', 'LogisticsCapacity', 'LogisticsUtilization', 'LogisticsAccess', 'WasteCoverage', 'WasteLoad', 'WasteCapacity', 'WasteAccess', 'CommunicationAccess', 'CommunicationCoverage', 'CommunicationLoad', 'CommunicationCapacity', 'CommunicationUtilization', 'MailAccess', 'MailCoverage', 'MailLoad', 'MailCapacity', 'MailUtilization', 'MailReliability', 'CrimePressure', 'ArterialRoadTiles', 'ZonedDevelopmentBuildings', 'HighDensityResidentialBuildings', 'DevelopedZoneTiles', 'LandUseEfficiency', 'IdleZoneTiles', 'DevelopmentQuality', 'LandUseConflict', 'AutoDeveloped', 'UpgradedBuildings', 'MaxBuildingLevel', 'Level', 'RentPressure', 'Logistics', 'Communications', 'Parking', 'Stormwater', 'OverlayMode']) {
  assert(types.includes(marker), `Unity core types missing save marker: ${marker}`);
}

for (const marker of ['DisasterPreparedness', 'DisasterRisk']) {
  assert(types.includes(marker), `Unity core types missing disaster preparedness marker: ${marker}`);
}

for (const marker of ['RoadMaintenanceAccess', 'RoadMaintenanceCoverage', 'AccidentRisk', 'RoadSafety']) {
  assert(types.includes(marker), `Unity core types missing road safety marker: ${marker}`);
}

for (const marker of fireResilienceTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing fire resilience marker: ${marker}`);
}

for (const marker of medicalCapacityTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing medical capacity marker: ${marker}`);
}

for (const marker of educationCapacityTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing education capacity marker: ${marker}`);
}

for (const marker of transitReliabilityTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing transit reliability marker: ${marker}`);
}

for (const marker of trafficFlowTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing traffic flow marker: ${marker}`);
}

for (const marker of livingConditionTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing living condition marker: ${marker}`);
}

for (const marker of deathcareTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing deathcare marker: ${marker}`);
}

for (const marker of policeEnforcementTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing police enforcement marker: ${marker}`);
}

for (const marker of serviceEquityGapSourceTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing service equity gap source marker: ${marker}`);
}

for (const marker of riskForecastAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing risk forecast advisor marker: ${marker}`);
}

for (const marker of cityEventDigestTypeMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => types.includes(option)), `Unity core types missing city event digest marker: ${markerOptions.join(' / ')}`);
}

for (const marker of demandDriverAnalysisTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing demand driver analysis marker: ${marker}`);
}

for (const marker of budgetBreakdownAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing budget breakdown advisor marker: ${marker}`);
}

for (const marker of districtPriorityAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing district priority advisor marker: ${marker}`);
}

for (const marker of serviceGapAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing service gap advisor marker: ${marker}`);
}

for (const marker of growthBottleneckAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing growth bottleneck advisor marker: ${marker}`);
}

for (const marker of commuteCorridorAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing commute corridor advisor marker: ${marker}`);
}

for (const marker of economicSpecializationAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing economic specialization advisor marker: ${marker}`);
}

for (const marker of buildingUpgradeReadinessAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing building upgrade readiness advisor marker: ${marker}`);
}

for (const marker of infrastructureResilienceAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing infrastructure resilience advisor marker: ${marker}`);
}

for (const marker of housingAffordabilityAdvisorTypeMarkers) {
  assert(types.includes(marker), `Unity core types missing housing affordability advisor marker: ${marker}`);
}

for (const marker of ['FiscalHealth', 'DebtPressure', 'BondPrincipal', 'BondPayment']) {
  assert(types.includes(marker), `Unity core types missing fiscal marker: ${marker}`);
}

for (const marker of ['AdministrationEfficiency', 'AdministrationLoad', 'AdministrationCapacity', 'AdministrationUtilization', 'PolicyBacklog']) {
  assert(types.includes(marker), `Unity core types missing administration marker: ${marker}`);
}

for (const marker of ['RegionalConnectivity']) {
  assert(types.includes(marker), `Unity core types missing regional connection marker: ${marker}`);
}

for (const marker of ['ResourceSpecialization', 'ResourcePotential', 'IndustrialSpecialization']) {
  assert(types.includes(marker), `Unity core types missing resource specialization marker: ${marker}`);
}

for (const marker of ['WasteUtilization', 'WasteReliability']) {
  assert(types.includes(marker), `Unity core types missing waste capacity marker: ${marker}`);
}

for (const marker of ['WastewaterLoad', 'WastewaterCapacity', 'WastewaterUtilization', 'WastewaterReliability']) {
  assert(types.includes(marker), `Unity core types missing wastewater marker: ${marker}`);
}

for (const marker of ['AdvancedEducationCoverage']) {
  assert(types.includes(marker), `Unity core types missing advanced education marker: ${marker}`);
}

for (const marker of ['TrafficSafetyCampaign']) {
  assert(types.includes(marker), `Unity core types missing traffic safety policy marker: ${marker}`);
}

for (const marker of ['CompleteStreets']) {
  assert(types.includes(marker), `Unity core types missing complete streets policy marker: ${marker}`);
}

for (const marker of ['SignalOptimization']) {
  assert(types.includes(marker), `Unity core types missing signal optimization policy marker: ${marker}`);
}

for (const marker of ['CongestionPricing']) {
  assert(types.includes(marker), `Unity core types missing congestion pricing policy marker: ${marker}`);
}

for (const marker of ['ParkingFees']) {
  assert(types.includes(marker), `Unity core types missing parking fees policy marker: ${marker}`);
}

const hud = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityHudViewModel.cs', 'utf8');
assert((hud.match(/snapshot\.DemandStats\.Add/g) || []).length === expectedDemandStatCount, `Unity HUD view model should expose ${expectedDemandStatCount} demand stats.`);
for (const marker of ['LocalGoodsSupply', 'FreightImportSupply', 'SupplyChainStability', '\\u672c', '\\u94c1', '\\u4ed3']) {
  assert(hud.includes(marker), `Unity HUD view model missing resource supply marker: ${marker}`);
}

for (const marker of ['HudStat', 'CityHudSnapshot', 'OverlayColor', 'NORMAL_VIEW_UNBUILT_ZONE_PADS', 'IsUnbuiltZonedTile', 'NormalViewZoneColor', 'OverlayMode.Normal', 'OverlayMode.Traffic', 'OverlayMode.Zoning', 'OverlayMode.Services', 'OverlayMode.Transit', 'OverlayMode.Logistics', 'OverlayMode.Waste', 'OverlayMode.Utilities', 'demand.Office', 'ZoneType.Office', 'demand.MixedUse', 'ZoneType.MixedUse', 'Attractiveness', 'Visitors', 'TourismIncome', 'LandUseEfficiency', 'IdleZoneTiles', 'LandUseConflict', '用地', 'RoadConnectivity', 'DeadEndRoadTiles', '路网', 'Walkability', '步行', 'EmergencyResponse', '响应', 'MaintenanceCondition', 'ServiceEquity', '运维', 'GoodsBalance', '商品', 'UtilityReliability', 'UtilityUtilization', '\\u6c34\\u7535', 'WorkforceSkill', 'LaborShortage', 'ProductivityBonus', 'CommuteEfficiency', 'CarDependency', 'ParkingPressure', 'EnvironmentQuality', 'NoiseStress', 'PublicHealth', 'HealthRisk', 'ServiceUtilization', 'ParkCoverage', 'HealthCoverage', 'EducationCoverage', 'SafetyCoverage', 'SafetyAccess', 'SecurityAccess', 'TransitCoverage', 'TransitUtilization', 'LogisticsCoverage', 'LogisticsUtilization', 'LogisticsAccess', 'WasteCoverage', 'RentPressure', 'CrimePressure']) {
  assert(hud.includes(marker), `Unity HUD view model missing marker: ${marker}`);
}

for (const marker of ['DisasterPreparedness', 'DisasterRisk', 'disaster', '\\u707e\\u5907', '\\u9669']) {
  assert(hud.includes(marker), `Unity HUD view model missing disaster preparedness marker: ${marker}`);
}

for (const marker of ['OverlayMode.Communications', 'CommunicationCoverage', 'CommunicationUtilization', 'CommunicationAccess', 'BusinessEfficiency', 'communication']) {
  assert(hud.includes(marker), `Unity HUD view model missing communication marker: ${marker}`);
}

for (const marker of ['MailCoverage', 'MailUtilization', 'MailAccess', '\\u90ae', '\\u90ae\\u6ee1']) {
  assert(hud.includes(marker), `Unity HUD view model missing mail service marker: ${marker}`);
}

for (const marker of ['OverlayMode.RoadSafety', 'RoadMaintenanceAccess', 'RoadMaintenanceCoverage', 'AccidentRisk', 'RoadSafety', 'road_safety']) {
  assert(hud.includes(marker), `Unity HUD view model missing road safety marker: ${marker}`);
}

for (const marker of fireResilienceHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing fire resilience marker: ${marker}`);
}

for (const marker of medicalCapacityHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing medical capacity marker: ${marker}`);
}

for (const marker of educationCapacityHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing education capacity marker: ${marker}`);
}

for (const marker of transitReliabilityHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing transit reliability marker: ${marker}`);
}

for (const marker of trafficFlowHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing traffic flow marker: ${marker}`);
}

for (const marker of livingConditionHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing living condition marker: ${marker}`);
}

for (const marker of deathcareHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing deathcare marker: ${marker}`);
}

for (const marker of policeEnforcementHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing police enforcement marker: ${marker}`);
}

for (const marker of serviceEquityGapSourceHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing service equity gap source marker: ${marker}`);
}

for (const marker of objectiveActionAdviceHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing objective action advice marker: ${marker}`);
}

for (const marker of alertPriorityDigestHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing alert priority digest marker: ${markerOptions.join(' / ')}`);
}

for (const marker of riskForecastAdvisorHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing risk forecast advisor marker: ${marker}`);
}

for (const marker of cityEventDigestHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing city event digest marker: ${markerOptions.join(' / ')}`);
}

for (const marker of demandDriverAnalysisHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing demand driver analysis marker: ${marker}`);
}

for (const marker of budgetBreakdownAdvisorHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing budget breakdown advisor marker: ${marker}`);
}

for (const marker of districtPriorityAdvisorHudMarkers) {
  assert(hud.includes(marker), `Unity HUD view model missing district priority advisor marker: ${marker}`);
}

for (const marker of serviceGapAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing service gap advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of growthBottleneckAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing growth bottleneck advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of commuteCorridorAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing commute corridor advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of economicSpecializationAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing economic specialization advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of buildingUpgradeReadinessAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing building upgrade readiness advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of infrastructureResilienceAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing infrastructure resilience advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of housingAffordabilityAdvisorHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hud.includes(option)), `Unity HUD view model missing housing affordability advisor marker: ${markerOptions.join(' / ')}`);
}

for (const marker of ['FiscalHealth', 'DebtPressure', 'BondPrincipal', 'fiscal']) {
  assert(hud.includes(marker), `Unity HUD view model missing fiscal marker: ${marker}`);
}

for (const marker of ['AdministrationEfficiency', 'AdministrationLoad', 'AdministrationCapacity', 'AdministrationUtilization', 'PolicyBacklog', 'administration']) {
  assert(hud.includes(marker), `Unity HUD view model missing administration marker: ${marker}`);
}

for (const marker of ['RegionalConnectivity']) {
  assert(hud.includes(marker), `Unity HUD view model missing regional connection marker: ${marker}`);
}

for (const marker of ['WasteUtilization', 'WasteReliability']) {
  assert(hud.includes(marker), `Unity HUD view model missing waste capacity marker: ${marker}`);
}

for (const marker of ['WastewaterUtilization', 'WastewaterReliability']) {
  assert(hud.includes(marker), `Unity HUD view model missing wastewater marker: ${marker}`);
}

for (const marker of ['AdvancedEducationCoverage']) {
  assert(hud.includes(marker), `Unity HUD view model missing advanced education marker: ${marker}`);
}

for (const marker of ['OverlayMode.Parking', 'ParkingAccess', 'ParkingUtilization']) {
  assert(hud.includes(marker), `Unity HUD view model missing parking marker: ${marker}`);
}

for (const marker of ['OverlayMode.Stormwater', 'StormwaterAccess', 'StormwaterUtilization', 'FloodRisk']) {
  assert(hud.includes(marker), `Unity HUD view model missing stormwater marker: ${marker}`);
}

const controller = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityGameController.cs', 'utf8');
for (const marker of ['HudSnapshot', 'GetOverlayColor', 'PreviewRoadUpgrade', 'ConfirmRoadUpgrade', 'PreviewZone', 'ConfirmZone', 'PreviewDemolish', 'ConfirmDemolish', 'ExportSaveJson', 'ImportSaveJson', 'CycleSimulationSpeed', 'TogglePause', 'CycleTaxLevel', 'ServiceBudgetLevel', 'CycleServiceBudgetLevel', 'IssueMunicipalBond', 'TogglePolicy', 'IsPolicyActive', 'CommandFeedbackVersion', 'LastCommandSucceeded', 'LastCommandFeedbackText', 'lastCommandFeedbackText', 'BuildCommandFeedbackText', 'COMMAND_FEEDBACK_PULSE', 'COMMAND_FEEDBACK_DETAIL_SUMMARY', 'PolicyImpactPreview', 'BuildPolicyImpactPreview', 'MANAGEMENT_COMMAND_IMPACT_PREVIEW', 'BuildManagementImpactPreview', 'BuildManagementBlockedPreview', 'TaxLevelLabel', 'ServiceBudgetLabel', '\\u57ce\\u5e02\\u7ba1\\u7406\\u53cd\\u9988', '\\u653f\\u7b56\\u6548\\u679c\\u53cd\\u9988']) {
  assert(controller.includes(marker), `Unity controller missing marker: ${marker}`);
}
assert(/lastCommandSucceeded\s*=\s*success;[\s\S]{0,160}lastCommandFeedbackText\s*=\s*BuildCommandFeedbackText[\s\S]{0,160}commandFeedbackVersion\s*\+=\s*1;[\s\S]{0,260}platformBridge\s*==\s*null/.test(controller), 'Unity controller should publish command feedback before the optional platform bridge check.');

const camera = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityCameraController.cs', 'utf8');
for (const marker of ['CityCameraController', 'HandleMouseDrag', 'HandleMouseZoom', 'HandleTouchZoom', 'SetMapSize', 'MINIMAP_CAMERA_CONTROLS', 'ZoomIn', 'ZoomOut', 'FrameMap', 'AdjustZoom']) {
  assert(camera.includes(marker), `Unity camera controller missing marker: ${marker}`);
}

const mapRenderer = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityMapRenderer.cs', 'utf8');
for (const marker of ['CityMapRenderer', 'RebuildAll', 'BuildTileMesh', 'GetOverlayColor', ['CreatePrimitive', 'CreateCube', 'GetCubeMesh'], 'BuildingVisualSignature', 'RoadVisualSignature', 'RoadTier.Arterial', 'mixedUseMaterial', 'officeMaterial', 'ZoneType.MixedUse', 'ZoneType.Office', 'BuildingLevel']) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => mapRenderer.includes(option)), `Unity map renderer missing marker: ${markerOptions.join(' / ')}`);
}

for (const marker of buildingVisualPrefabLibraryMarkers) {
  assert(mapRenderer.includes(marker), `Unity map renderer missing building visual prefab library marker: ${marker}`);
}

for (const marker of ['LOW_POLY_ISOMETRIC_REFERENCE_UI', 'LOW_POLY_TERRAIN_SHADE_PATCHES', 'LOW_POLY_SHORELINE_DETAILS', 'LOW_POLY_WATER_SURFACE_RIPPLES', 'LowPolyWaterRippleDash', 'LowPolyWaterSpark', 'AddWaterSurfaceDetail', 'FRESH_SHORELINE_TREE_VARIATION', 'CITY_PLANNING_ZONE_PARCEL_CUES', 'LowPolyZoneParcelEdge', 'LowPolyZoneParcelStake', 'AddZoneParcelCue', 'IsUnbuiltZonedSceneryTile', 'CITY_DISTRICT_ZONE_SKIRTS', 'ZoneSkirtFront', 'ZoneSkirtSide', 'ZoneParcelCornerTick', 'AddBuildingZoneSkirt', 'CITY_SKYLINE_FACADE_DETAILS', 'CITY_SKYLINE_ROAD_DETAILS', 'CITY_DISTRICT_IDENTITY_DETAILS', 'CITY_NODE_TRANSIT_IDENTITY', 'TransitTransferPavers', 'TransitNodePylon', 'TransitStopCanopy', 'CITY_NODE_LANDMARK_IDENTITY', 'LandmarkPlazaAxis', 'LandmarkCrownGlint', 'LandmarkBeaconSpire', 'ISOMETRIC_VISIBLE_FACADE_BANDS', 'CITY_SKYLINE_ROOF_RIMS_AND_GREENROOFS', 'RebuildDecorations', 'LowPolyTreeCanopy', 'LowPolyTreeCanopyHighlight', 'LowPolyWaterGlint', 'LowPolyRock', 'LowPolyShorelineBand', 'LowPolyShorelineReed', 'LowPolyBuildingFootprintShadow', 'SkylineWindowBandFront', 'SkylineWindowBandSide', 'SkylineRooftopUnit', 'SkylineRoofAccent', 'SkylineRoofFrontRim', 'SkylineRoofSideRim', 'RooftopGreenPatch', 'RooftopSolarPatch', 'AddSkylineRoofDetails', 'StorefrontAwning', 'PlatformGuideStripe', 'PublicEntrySteps', 'LoadingApron', 'AddSkylineFacadeDetails', 'AddDistrictIdentityDetails', 'IsLandscapeModel', 'IsUtilityModel', 'windowMaterial', 'buildingFootprintMaterial', 'RoadCenterMark', 'RoadCrosswalkStripe', 'ArterialLaneEdge', 'CITY_SKYLINE_ROAD_FLOW_CHEVRONS', 'CITY_SKYLINE_ROAD_CURB_READABILITY', 'RoadFlowChevron', 'RoadCurbEdge', 'RoadTerminalCap', 'AddRoadFlowChevrons', 'AddRoadChevronMark', 'AddRoadCurbEdges', 'AddRoadIntersectionCrosswalks', 'AddArterialLaneEdges', 'AddCrosswalkSet', 'AddRoadDetailMark', 'RoadConnectionCount', 'RebuildLockedRegionGuide', 'LockedRegionDashedOutline', 'DecorationHash', 'FacetedTileColor', 'LowPolyCornerShade', 'IsShorelineSceneryTile', 'shoreMaterial', 'ShiftColor']) {
  assert(mapRenderer.includes(marker), `Unity map renderer missing low poly isometric reference marker: ${marker}`);
}
assert(/Terrain\s*==\s*TerrainType\.Water[\s\S]{0,420}AddWaterSurfaceDetail\(new GridPos\(x,\s*y\),\s*hash\)[\s\S]{0,180}continue;/.test(mapRenderer), 'Unity map renderer should keep water detail inside the water decoration branch.');
assert(/modelKey\s*==\s*"transit"[\s\S]{0,620}TransitTransferPavers[\s\S]{0,320}TransitNodePylon[\s\S]{0,320}TransitStopCanopy[\s\S]{0,180}return;/.test(mapRenderer), 'Unity map renderer should keep transit node identity details in the transit identity branch.');
assert(/modelKey\s*==\s*"landmark"[\s\S]{0,420}LandmarkPlazaAxis[\s\S]{0,320}LandmarkCrownGlint[\s\S]{0,320}LandmarkBeaconSpire[\s\S]{0,180}return;/.test(mapRenderer), 'Unity map renderer should keep landmark identity details in the landmark identity branch.');

for (const marker of ['CITY_SKYLINES_STYLE_DIAGNOSTICS', 'RebuildPlanningSignals', 'TrafficPulseMarker', 'NORMAL_VIEW_TRAFFIC_RIBBONS', 'NormalTrafficRibbon', 'RebuildNormalTrafficRibbons', 'AddTrafficLoadRibbon', 'TrafficLoadPercent', 'ServiceGapPin', 'NeedsCoverageSignal', 'LAYER_GAP_PIN_SIGNALS', 'NORMAL_VIEW_CITY_ISSUE_BADGES', 'RebuildCityIssueBadges', 'AddCityIssueBadge', 'CityIssueSeverity', 'CityIssueUsesTrafficMaterial', 'CityIssueBadgePost', 'CityIssueBadgeCap', 'CoverageSignalHeight', 'IsParkingSensitiveUse', 'IsPollutionSensitiveUse', 'IsUtilityStress', 'IsStormwaterStress', 'LandValueSignalThreshold', 'AddRoadCenterMark', 'HasRoadAt', 'UNITY_HOVER_DRAG_PREVIEW_GHOST', 'ShowBuildingPlacementPreview', 'ShowRoadPlacementPreview', 'ShowZonePlacementPreview', 'ClearPlacementPreview', 'PlacementPreviewSignature']) {
  assert(mapRenderer.includes(marker), `Unity map diagnostics layer missing marker: ${marker}`);
}

const interaction = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityInteractionController.cs', 'utf8');
for (const marker of ['CityInteractionController', 'SelectRoadTool', 'SelectRoadUpgradeTool', 'SelectZoneTool', 'SelectBuildingTool', 'OverlayForBuilding', 'TryScreenToGrid', 'ConfirmRoad', 'ConfirmRoadUpgrade', 'SetOverlay', 'OverlayMode.Zoning', 'OverlayMode.Logistics', 'OverlayMode.Waste']) {
  assert(interaction.includes(marker), `Unity interaction controller missing marker: ${marker}`);
}

for (const marker of ['UNITY_HOVER_DRAG_PREVIEW_GHOST', 'UpdateHoverPreview', 'HoverPreviewSignature', 'ResetHoverPreview', 'ShowBuildingPlacementPreview', 'ShowRoadPlacementPreview', 'ShowZonePlacementPreview', 'ShowSingleTilePlacementPreview', 'PreviewBuilding', 'PreviewRoad', 'PreviewZone']) {
  assert(interaction.includes(marker), `Unity interaction controller missing hover placement preview marker: ${marker}`);
}

for (const marker of ['resource_processor', 'OverlayMode.Logistics']) {
  assert(interaction.includes(marker), `Unity interaction controller missing resource supply marker: ${marker}`);
}

for (const marker of ['distribution_center', 'OverlayMode.Logistics']) {
  assert(interaction.includes(marker), `Unity interaction controller missing warehouse marker: ${marker}`);
}

for (const marker of ['freight_rail_terminal', 'OverlayMode.Logistics']) {
  assert(interaction.includes(marker), `Unity interaction controller missing freight rail marker: ${marker}`);
}

for (const marker of ['OverlayMode.Communications', 'telecom_hub']) {
  assert(interaction.includes(marker), `Unity interaction controller missing communication marker: ${marker}`);
}

for (const marker of ['metro_station', 'intercity_terminal']) {
  assert(interaction.includes(marker), `Unity interaction controller missing metro marker: ${marker}`);
}

for (const marker of ['OverlayMode.RoadSafety', 'road_maintenance_depot']) {
  assert(interaction.includes(marker), `Unity interaction controller missing road safety marker: ${marker}`);
}

for (const marker of ['water_reclaimer']) {
  assert(interaction.includes(marker), `Unity interaction controller missing wastewater marker: ${marker}`);
}

for (const marker of ['solar_farm']) {
  assert(interaction.includes(marker), `Unity interaction controller missing solar marker: ${marker}`);
}

for (const marker of ['community_college']) {
  assert(interaction.includes(marker), `Unity interaction controller missing advanced education marker: ${marker}`);
}

for (const marker of ['OverlayMode.Parking', 'parking_garage']) {
  assert(interaction.includes(marker), `Unity interaction controller missing parking marker: ${marker}`);
}

for (const marker of ['OverlayMode.Stormwater', 'rain_garden']) {
  assert(interaction.includes(marker), `Unity interaction controller missing stormwater marker: ${marker}`);
}

for (const marker of ['OverlayMode.Waste', 'waste_to_energy_plant']) {
  assert(interaction.includes(marker), `Unity interaction controller missing waste-to-energy marker: ${marker}`);
}

for (const marker of ['OverlayMode.Services', 'convention_center']) {
  assert(interaction.includes(marker), `Unity interaction controller missing convention center marker: ${marker}`);
}

for (const marker of ['OverlayMode.Communications', 'research_campus']) {
  assert(interaction.includes(marker), `Unity interaction controller missing innovation marker: ${marker}`);
}

for (const marker of ['city_hall', 'district_hospital']) {
  assert(interaction.includes(marker), `Unity interaction controller missing service building marker: ${marker}`);
}

for (const marker of ['OverlayMode.Services', 'emergency_shelter']) {
  assert(interaction.includes(marker), `Unity interaction controller missing shelter service marker: ${marker}`);
}

for (const marker of ['OverlayMode.Services', 'memorial_garden']) {
  assert(interaction.includes(marker), `Unity interaction controller missing deathcare service marker: ${marker}`);
}

for (const marker of ['OverlayMode.Services', 'police_precinct']) {
  assert(interaction.includes(marker), `Unity interaction controller missing police precinct service marker: ${marker}`);
}

const runtimeHud = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CityRuntimeHud.cs', 'utf8');
const demandStatLoopMatches = runtimeHud.match(new RegExp(`for\\s*\\(var i = 0;\\s*i < ${expectedDemandStatCount};\\s*i \\+= 1\\)[\\s\\S]{0,360}demandTexts\\.Add`, 'g')) || [];
const topStatLoopMatches = runtimeHud.match(new RegExp(`for\\s*\\(var i = 0;\\s*i < ${expectedTopStatCount};\\s*i \\+= 1\\)[\\s\\S]{0,360}topTexts\\.Add`, 'g')) || [];
assert(demandStatLoopMatches.length === 1, `Unity runtime HUD should allocate exactly one ${expectedDemandStatCount}-slot demand stat loop.`);
assert(topStatLoopMatches.length === 1, `Unity runtime HUD should allocate exactly one ${expectedTopStatCount}-slot top stat loop.`);
assert((runtimeHud.match(/^\s*AddOverlayButton\(toolbar\.transform,/gm) || []).length === expectedOverlayButtonCount, `Unity runtime HUD should define ${expectedOverlayButtonCount} overlay buttons.`);
assert((runtimeHud.match(/^\s*AddToolButton\(toolGrid\.transform,/gm) || []).length === expectedToolButtonCount, `Unity runtime HUD should define ${expectedToolButtonCount} tool buttons.`);
assert((runtimeHud.match(/^\s*AddControlButton\(toolGrid\.transform,/gm) || []).length === expectedControlButtonCount, `Unity runtime HUD should define ${expectedControlButtonCount} control buttons.`);
assert((runtimeHud.match(/^\s*AddPolicyButton\(toolGrid\.transform,/gm) || []).length === expectedPolicyButtonCount, `Unity runtime HUD should define ${expectedPolicyButtonCount} policy buttons.`);
const roadHierarchyAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of ['ROAD_HIERARCHY_ADVISOR', 'RoadHierarchyPressure', 'RoadHierarchyFocus', 'RoadHierarchyDriver', 'RoadHierarchyAction']) {
  assert(includesMarker(roadHierarchyAdvisorSource, marker), `Unity road hierarchy advisor missing marker: ${marker}`);
}
assert(['RoadHierarchyAdvisor', 'ComputeRoadHierarchyAdvice'].some((marker) => includesMarker(roadHierarchyAdvisorSource, marker)), 'Unity road hierarchy advisor missing method marker: RoadHierarchyAdvisor / ComputeRoadHierarchyAdvice');
for (const marker of ['resource_processor', '\\u8d44\\u6e90', 'Build Tool Dock']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing resource supply marker: ${marker}`);
}

const cityEventDigestPresentationSource = `${core}\n${hud}\n${runtimeHud}`;
for (const marker of cityEventDigestPresentationMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(cityEventDigestPresentationSource, option)), `Unity HUD/runtime missing city event digest text marker: ${markerOptions.join(' / ')}`);
}

for (const marker of ['distribution_center', '\\u4ed3\\u50a8', 'Build Tool Dock']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing warehouse marker: ${marker}`);
}

for (const marker of ['freight_rail_terminal', '\\u94c1\\u8d27', 'Build Tool Dock']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing freight rail marker: ${marker}`);
}

for (const marker of ['CityRuntimeHud', 'CanvasScaler', 'GridLayoutGroup', 'GridLayoutGroup.Constraint.FixedRowCount', 'Demand Bar', 'REFERENCE_IMAGE_CITY_DEMAND_PANEL', 'REFERENCE_IMAGE_DEMAND_PILL_GRID_DENSITY', 'REFERENCE_IMAGE_BOTTOM_BUILD_TOOL_DOCK', 'DEMAND_WARNING_BACKPLATES', 'CreateDemandStatTile', 'SetDemandStatBackplate', 'DemandStatBackplateColor', 'new Vector2(56f, 22f)', 'new Vector2(56f, 18f)', 'i < 33', 'OverlayMode.Traffic', 'OverlayMode.Transit', 'OverlayMode.Logistics', 'OverlayMode.Waste', 'OverlayMode.Communications', 'OverlayMode.Parking', 'OverlayMode.Stormwater', 'SetOverlay', 'HudSnapshot', 'AddToolButton', 'AddControlButton', 'AddPolicyButton', 'BuildToolStatusText', 'BuildPreviewText', 'TaxStatusText', 'BudgetStatusText', 'PolicyStatusText', 'CycleTaxLevel', 'CycleServiceBudgetLevel', 'IssueMunicipalBond', '\\u503a\\u5238', 'SaveGame', 'LoadGame', 'SelectRoadUpgradeTool', 'SelectBuildingTool', 'ZoneType.MixedUse', 'ZoneType.Office', 'AffordableHousing', 'apartment_block', 'mixed_use_block', 'office_studio', 'research_campus', 'city_plaza', 'convention_center', 'city_hall', 'cargo_depot', 'primary_school', 'community_college', 'fire_station', 'police_kiosk', 'telecom_hub', 'post_office', 'metro_station', 'intercity_terminal', 'parking_garage', 'rain_garden', 'solar_farm', 'water_reclaimer', 'waste_to_energy_plant', 'recycling_yard']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing marker: ${marker}`);
}

for (const marker of ['LOW_POLY_ISOMETRIC_REFERENCE_UI', 'LIGHT_CITY_HUD_SURFACES', 'REFERENCE_IMAGE_RESOURCE_CARD', 'REFERENCE_IMAGE_RESOURCE_OBJECTIVE_PROGRESS', 'BuildResourceObjectiveProgressBar', 'RefreshResourceObjectiveProgress', 'resourceObjectiveProgressFill', 'resourceObjectiveProgressText', 'REFERENCE_IMAGE_TOP_RESOURCE_CAPSULES', 'REFERENCE_IMAGE_TOP_CAPSULE_COMPACT_TEXT', 'REFERENCE_IMAGE_RIGHT_MILESTONE_CARD', 'REFERENCE_IMAGE_CITY_TITLE', 'Mini Map Zoom', 'Mini Map Controls', 'DYNAMIC_MINIMAP_SAMPLER', 'MINIMAP_SELECTED_CELL_BLEND_TINT', 'MINIMAP_SELECTED_CELL_OUTLINE', 'MINIMAP_SELECTED_ISSUE_SEVERITY_OUTLINE', 'MiniMapSelectedIssueOutlineColor', 'miniMapCells', 'miniMapCellOutlines', 'Outline', 'MiniMapColumns', 'MiniMapRows', 'BuildMiniMapCells', 'RefreshMiniMap', 'MiniMapTileColor', 'MiniMapIssueSeverity', 'ZoneMiniMapColor', 'SampleMiniMapAxisForTile', 'AnchorTopLeft', 'AnchorTopRight', 'AnchorBottomRight', 'new Color32(65, 169, 184, 245)']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing low poly reference marker: ${marker}`);
}

for (const marker of ['CityCameraController cameraController', 'Camera.main.GetComponent<CityCameraController>()', 'HorizontalLayoutGroup', 'AddMiniMapControlButton', 'MiniMapButton ', 'cameraController.ZoomOut()', 'cameraController.FrameMap()', 'cameraController.ZoomIn()']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing minimap camera control marker: ${marker}`);
}
assert((runtimeHud.match(/^\s*AddMiniMapControlButton\(controls\.transform,/gm) || []).length === 3, 'Unity runtime HUD should define 3 minimap camera control buttons.');
assert(/AddMiniMapControlButton\(controls\.transform,\s*"-"[\s\S]{0,180}cameraController\.ZoomOut\(\)/.test(runtimeHud), 'Unity runtime HUD missing minimap zoom-out button binding.');
assert(/AddMiniMapControlButton\(controls\.transform,\s*"0"[\s\S]{0,180}cameraController\.FrameMap\(\)/.test(runtimeHud), 'Unity runtime HUD missing minimap reset/frame button binding.');
assert(/AddMiniMapControlButton\(controls\.transform,\s*"\+"[\s\S]{0,180}cameraController\.ZoomIn\(\)/.test(runtimeHud), 'Unity runtime HUD missing minimap zoom-in button binding.');

for (const marker of ['CITY_SKYLINES_STYLE_DIAGNOSTICS', 'CITY_PULSE_KPI_STRIP', 'CITY_PULSE_PRIMARY_DRIVER_LABEL', 'PrimaryPulseDriverLabel', 'ACTION_PREVIEW_COMPACT_DIAGNOSIS', 'COMMAND_FEEDBACK_PULSE', 'COMMAND_FEEDBACK_DETAIL_SUMMARY', 'RefreshCommandFeedbackPulse', 'BuildCommandFeedbackPulseText', 'CommandFeedbackPreviewColor', 'CommandFeedbackVersion', 'LastCommandSucceeded', 'LastCommandFeedbackText', 'commandFeedbackText', 'CITY_TOOL_RECOMMENDATION_REASON_LINE', 'BuildToolRecommendationHint', 'ToolRecommendationDriverLabel', 'ToolBindingLabel', 'CITY_DEMAND_TOOL_RECOMMENDATIONS', 'RIGHT_SIDE_MILESTONE_TASK_CARDS', 'Milestone Task Cards', 'milestoneTaskText', 'BuildObjectiveCardText', 'BuildMilestoneTaskCardText', 'AppendMilestoneCardPart', 'CountCompletedMilestones', 'FirstObjectiveHintLine', 'BuildCityPulseText', 'FirstPreviewDetailLine', 'CompactPreviewLine', 'City Pulse', 'CashRunwayStatus', 'RoadBottleneckPressure', 'ServiceGapPressure', 'ToolIdleColor', 'StrongestToolRecommendationScore', 'IsDemandRecommendedTool', 'DemandAwareToolColor', 'ToolRecommendationScore', 'DemandForZone', 'BlendToolRecommendationColor', 'metrics.Demand.Residential', 'metrics.Demand.Commercial', 'metrics.Demand.Utility', 'IsTransitOrLogisticsTool', 'snapshot.ObjectiveTitle', 'snapshot.ObjectiveProgress', 'snapshot.ObjectiveRequired', 'snapshot.ObjectiveInsightParts']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing diagnostics marker: ${marker}`);
}
assert(/seenCommandFeedbackVersion\s*==\s*controller\.CommandFeedbackVersion/.test(runtimeHud), 'Unity runtime HUD should suppress duplicate command feedback pulses.');
assert(/commandFeedbackPulseTimer\s*=\s*0\.65f/.test(runtimeHud), 'Unity runtime HUD should show a short command feedback pulse.');
assert(/commandFeedbackText\s*=\s*controller\.LastCommandFeedbackText/.test(runtimeHud), 'Unity runtime HUD should cache the latest command feedback detail text.');
assert(/ToolStatusWithLegend[\s\S]*BuildToolRecommendationHint/.test(runtimeHud), 'Unity runtime HUD should include the tool recommendation reason in the status legend.');
assert(/CreateText\(sidePanel\.transform,\s*"Milestone Task Cards"/.test(runtimeHud), 'Unity runtime HUD should render a separate milestone task card in the right panel.');
assert(/BuildMilestoneTaskCardText\(snapshot,\s*controller\.Metrics\)/.test(runtimeHud), 'Unity runtime HUD should refresh milestone task cards from the live metrics snapshot.');
assert(/BuildMilestoneTaskCardText[\s\S]*MILESTONE_CARD_RECENT_EVENT_BEACON[\s\S]*snapshot\.RecentEventText/.test(runtimeHud), 'Unity runtime HUD should surface recent events inside the milestone card.');
assert(/RefreshMiniMap[\s\S]*MiniMapSelectedIssueOutlineColor/.test(runtimeHud), 'Unity runtime HUD should color selected minimap outlines by issue severity.');

for (const marker of objectiveActionAdviceRuntimeHudMarkers) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing objective action advice marker: ${marker}`);
}

for (const marker of budgetBreakdownAdvisorRuntimeHudMarkers) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing budget breakdown advisor marker: ${marker}`);
}

for (const marker of districtPriorityAdvisorRuntimeHudMarkers) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing district priority advisor marker: ${marker}`);
}

for (const marker of serviceGapAdvisorRuntimeHudMarkers) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing service gap advisor marker: ${marker}`);
}

const growthBottleneckAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of growthBottleneckAdvisorRuntimeHudMarkers) {
  assert(includesMarker(growthBottleneckAdvisorSource, marker), `Unity HUD/runtime missing growth bottleneck advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*GrowthBottleneck[\s\S]*metrics\.GrowthBottleneckScore/.test(hud), 'Unity HUD insight stack should prioritize growth bottleneck text by score.');

const commuteCorridorAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of commuteCorridorAdvisorRuntimeHudMarkers) {
  assert(includesMarker(commuteCorridorAdvisorSource, marker), `Unity HUD/runtime missing commute corridor advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*CommuteCorridor[\s\S]*metrics\.CommuteCorridorScore/.test(hud), 'Unity HUD insight stack should prioritize commute corridor text by score.');

const economicSpecializationAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of economicSpecializationAdvisorRuntimeHudMarkers) {
  assert(includesMarker(economicSpecializationAdvisorSource, marker), `Unity HUD/runtime missing economic specialization advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*EconomicSpecialization[\s\S]*metrics\.EconomicSpecializationScore/.test(hud), 'Unity HUD insight stack should prioritize economic specialization text by score.');

const buildingUpgradeReadinessAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of buildingUpgradeReadinessAdvisorRuntimeHudMarkers) {
  assert(includesMarker(buildingUpgradeReadinessAdvisorSource, marker), `Unity HUD/runtime missing building upgrade readiness advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*BuildingUpgradeReadiness[\s\S]*metrics\.BuildingUpgradeReadinessScore/.test(hud), 'Unity HUD insight stack should prioritize building upgrade readiness text by score.');

const infrastructureResilienceAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of infrastructureResilienceAdvisorRuntimeHudMarkers) {
  assert(includesMarker(infrastructureResilienceAdvisorSource, marker), `Unity HUD/runtime missing infrastructure resilience advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*InfrastructureResilience[\s\S]*metrics\.InfrastructureResilienceScore/.test(hud), 'Unity HUD insight stack should prioritize infrastructure resilience text by score.');

const housingAffordabilityAdvisorSource = `${types}\n${core}\n${hud}\n${runtimeHud}`;
for (const marker of housingAffordabilityAdvisorRuntimeHudMarkers) {
  assert(includesMarker(housingAffordabilityAdvisorSource, marker), `Unity HUD/runtime missing housing affordability advisor marker: ${marker}`);
}
assert(/AddInsightPriority\([\s\S]*HousingAffordability[\s\S]*metrics\.HousingAffordabilityScore/.test(hud), 'Unity HUD insight stack should prioritize housing affordability text by score.');

for (const marker of tileInspectorOverlayLegendRuntimeHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => includesMarker(runtimeHud, option)), `Unity runtime HUD missing tile inspector / overlay legend marker: ${markerOptions.join(' / ')}`);
}

for (const marker of actionableTileDiagnosisRuntimeHudMarkers) {
  assert(includesMarker(runtimeHud, marker), `Unity runtime HUD missing actionable tile diagnosis marker: ${marker}`);
}

const hudInsightPriorityStackSource = `${hud}\n${runtimeHud}`;
for (const marker of hudInsightPriorityStackRuntimeHudMarkers) {
  const markerOptions = Array.isArray(marker) ? marker : [marker];
  assert(markerOptions.some((option) => hudInsightPriorityStackSource.includes(option)), `Unity HUD/runtime missing HUD insight priority stack marker: ${markerOptions.join(' / ')}`);
}
assert(/AddInsightPriority\([\s\S]*BudgetInsight[\s\S]*metrics\.BudgetStress/.test(hud) || /AddInsightPriority\([\s\S]*BUDGET_BREAKDOWN_ADVISOR/.test(hud), 'Unity HUD insight stack should prioritize budget insight text by budget stress.');
assert(/AddInsightPriority\([\s\S]*DemandInsight[\s\S]*metrics\.DemandUrgency/.test(hud) || /AddInsightPriority\([\s\S]*DEMAND_DRIVER_ANALYSIS/.test(hud), 'Unity HUD insight stack should prioritize demand insight text by demand urgency.');

for (const marker of ['SiteDiagnosis', 'ACTION_PREVIEW_COMPACT_DIAGNOSIS', 'FirstPreviewDetailLine', 'CompactPreviewLine']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing site diagnosis preview marker: ${marker}`);
}

for (const marker of ['emergency_shelter', '\\u907f\\u96be']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing shelter marker: ${marker}`);
}

for (const marker of ['memorial_garden']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing deathcare marker: ${marker}`);
}

for (const marker of ['police_precinct']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing police precinct marker: ${marker}`);
}

for (const marker of ['new Vector2(56f, 18f)', 'Build Tool Dock', 'i < 8', 'OverlayMode.RoadSafety', 'road_maintenance_depot']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing road safety marker: ${marker}`);
}

for (const marker of ['CityPolicy.TrafficSafetyCampaign']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing traffic safety policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.CompleteStreets']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing complete streets policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.SignalOptimization']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing signal optimization policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.CongestionPricing', '\\u62e5\\u5835\\u8d39', '\\u6536\\u652f']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing congestion pricing policy marker: ${marker}`);
}

for (const marker of ['CityPolicy.ParkingFees', '\\u505c\\u8f66\\u8d39']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing parking fees policy marker: ${marker}`);
}

for (const marker of ['district_hospital']) {
  assert(runtimeHud.includes(marker), `Unity runtime HUD missing regional hospital marker: ${marker}`);
}

const lowPolyDoc = readFileSync('docs/LOW_POLY_ISOMETRIC_REFERENCE_UI.md', 'utf8');
for (const marker of ['LOW_POLY_ISOMETRIC_REFERENCE_UI', 'RoadCenterMark', 'LockedRegionDashedOutline', 'Mini Map Zoom', '33 demand stats', '48 tool buttons']) {
  assert(lowPolyDoc.includes(marker), `Low poly isometric reference doc missing marker: ${marker}`);
}

const save = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/CitySaveController.cs', 'utf8');
for (const marker of ['CitySaveController', 'SaveGame', 'LoadGame', 'DeleteSave', 'SetStorageString', 'GetStorageString']) {
  assert(save.includes(marker), `Unity save controller missing marker: ${marker}`);
}

for (const marker of ['WECHAT_SAFE_LIFECYCLE_FEEDBACK', 'OnApplicationPause', 'OnApplicationFocus', 'AutoSaveOnApplicationPause', 'RequestLifecycleAutoSave', 'TryLoadOnStartup', 'loadOnStartup', 'startupLoadAttempted', 'autoSaveTimer = Mathf.Max(5f, autoSaveInterval)', 'LifecycleSaveCooldownSeconds', 'SaveGame(true)', 'LoadGame(true)', 'VibrateSuccess', 'VibrateWarning', 'PlaySaveFeedback', 'LastStorageStatus', 'RefreshStorageStatus', 'GetStorageStatusString']) {
  assert(save.includes(marker), `Unity save controller missing WeChat safe lifecycle marker: ${marker}`);
}
assert(/OnApplicationPause[\s\S]*RequestLifecycleAutoSave/.test(save), 'Unity save controller lifecycle pause should request lifecycle auto save.');
assert(/OnApplicationFocus[\s\S]*RequestLifecycleAutoSave/.test(save), 'Unity save controller lifecycle focus loss should request lifecycle auto save.');

const sceneFactory = readFileSync('unity/Assets/Editor/PocketCity/PrototypeSceneFactory.cs', 'utf8');
for (const marker of ['Create Prototype Scene', 'VisualAssetFactory.CreateVisualAssets', 'CityMapRenderer', 'MixedUse.mat', 'Office.mat', 'CityRuntimeHud', 'CityInteractionController', 'CitySaveController', 'CityCameraController', 'AssignObject(hud, "cameraController", cameraController)', 'WeChatMiniGameBridge', 'EventSystem', 'EditorSceneManager.SaveScene']) {
  assert(sceneFactory.includes(marker), `Unity prototype scene factory missing marker: ${marker}`);
}

for (const marker of ['RoadLine.mat', 'Roof.mat', 'TreeTrunk.mat', 'TreeCanopy.mat', 'Rock.mat', 'LockedArea.mat', 'TrafficPulse.mat', 'ServiceNeed.mat', 'PreviewOk.mat', 'PreviewBlocked.mat', 'new Vector3(-42f, 48f, -42f)', 'new Color32(195, 229, 239, 255)']) {
  assert(sceneFactory.includes(marker), `Unity prototype scene factory missing low poly isometric reference marker: ${marker}`);
}

const visualFactory = readFileSync('unity/Assets/Editor/PocketCity/VisualAssetFactory.cs', 'utf8');
for (const marker of ['Create Visual Assets', 'CreateBuildingIconAtlas', 'CreateLoadingBackground', 'zone-palette.png', 'heat-palette.png', 'building-icons.png', 'loading-background.png', 'MixedUse.mat', 'Office.mat', 'IconShape.Book', 'IconShape.Shield', 'IconShape.Office', 'IconShape.MixedUse', 'IconShape.Plaza', 'new Texture2D(1024, 640', 'IconShape.WastePower', 'IconShape.Convention', 'IconShape.Research', 'IconShape.Mail']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing marker: ${marker}`);
}

for (const marker of ['RoadLine.mat', 'Roof.mat', 'TreeTrunk.mat', 'TreeCanopy.mat', 'Rock.mat', 'LockedArea.mat', 'TrafficPulse.mat', 'ServiceNeed.mat', 'PreviewOk.mat', 'PreviewBlocked.mat', 'new Color32(195, 229, 239, 255)', 'new Color32(134, 207, 142, 255)']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing low poly isometric reference marker: ${marker}`);
}

const prototypeScene = readFileSync('unity/Assets/Scenes/PocketCityPrototype.unity', 'utf8');
for (const marker of ['Pocket City Game', 'City Map Renderer', 'Main Camera', 'Sun Light', 'EventSystem']) {
  assert(prototypeScene.includes(marker), `Unity prototype scene missing playable demo object: ${marker}`);
}

const editorBuildSettings = readFileSync('unity/ProjectSettings/EditorBuildSettings.asset', 'utf8');
for (const marker of ['enabled: 1', 'path: Assets/Scenes/PocketCityPrototype.unity']) {
  assert(editorBuildSettings.includes(marker), `Unity build settings missing prototype demo scene marker: ${marker}`);
}

for (const marker of ['IconShape.Resource']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing resource marker: ${marker}`);
}

for (const marker of ['IconShape.Warehouse']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing warehouse marker: ${marker}`);
}

for (const marker of ['IconShape.FreightRail']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing freight rail marker: ${marker}`);
}

for (const marker of ['IconShape.Signal']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing communication marker: ${marker}`);
}

for (const marker of ['IconShape.Wrench']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing road safety marker: ${marker}`);
}

for (const marker of ['IconShape.Parking']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing parking marker: ${marker}`);
}

for (const marker of ['IconShape.RainGarden']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing stormwater marker: ${marker}`);
}

for (const marker of ['IconShape.Metro']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing metro marker: ${marker}`);
}

for (const marker of ['IconShape.Solar']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing solar marker: ${marker}`);
}

for (const marker of ['IconShape.Hospital']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing regional hospital marker: ${marker}`);
}

for (const marker of ['IconShape.CityHall']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing administration marker: ${marker}`);
}

for (const marker of ['IconShape.Terminal']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing regional connection marker: ${marker}`);
}

for (const marker of ['IconShape.Shelter']) {
  assert(visualFactory.includes(marker), `Unity visual asset factory missing shelter marker: ${marker}`);
}

const bridge = readFileSync('unity/Assets/Scripts/PocketCity/Runtime/WeChatMiniGameBridge.cs', 'utf8');
for (const marker of ['SetStorageString', 'GetStorageString', 'DeleteStorageKey']) {
  assert(bridge.includes(marker), `Unity WeChat bridge missing storage marker: ${marker}`);
}

for (const marker of ['WECHAT_SAFE_LIFECYCLE_FEEDBACK', 'TrySetStorageString', 'TryGetStorageString', 'TryDeleteStorageKey', 'VibrateSuccess', 'VibrateWarning', 'VibrateSafe', 'LastPlatformStatus', 'GetStorageStatusString', 'Storage save failed', 'Vibrate failed', 'WxRegisterLifecycleCallbacks', 'OnWeChatHide', 'OnWeChatShow', 'Lifecycle resumed: WeChat show', 'RequestLifecycleAutoSave']) {
  assert(bridge.includes(marker), `Unity WeChat bridge missing safe lifecycle feedback marker: ${marker}`);
}
assert(!/OnWeChatShow[\s\S]{0,120}RequestLifecycleSave/.test(bridge), 'Unity WeChat bridge should not save on WeChat show.');
for (const marker of ['WeChatMiniGameBridge', 'platformBridge', 'PlayCityCommandFeedback', 'VibrateSuccess', 'VibrateWarning', 'ConfirmBuilding', 'ConfirmRoad', 'ConfirmRoadUpgrade', 'ConfirmZone', 'ConfirmDemolish']) {
  assert(controller.includes(marker), `Unity game controller missing command feedback marker: ${marker}`);
}

const jslib = readFileSync('unity/Assets/Plugins/WebGL/WeChatBridge.jslib', 'utf8');
for (const marker of ['WxSetStorageString', 'WxGetStorageString', 'WxDeleteStorageKey', 'wx.setStorageSync', 'return 1', 'return 0', 'stringToNewUTF8']) {
  assert(jslib.includes(marker), `WeChat jslib missing storage marker: ${marker}`);
}

for (const marker of ['WxVibrateShort', 'wx.vibrateShort', "feedbackType = 'light'", "feedbackType = 'medium'", "feedbackType = 'heavy'", "reason === 'success'", "reason === 'warning'", 'WxRegisterLifecycleCallbacks', 'wx.onHide', 'wx.onShow', 'SendMessage', 'OnWeChatHide', 'OnWeChatShow', 'WxVibrateShort failed', 'WxSetStorageString failed', 'WxGetStorageString failed', 'WxDeleteStorageKey failed', 'WxGetStorageStatusString', 'wx.getStorageInfoSync', 'localStorage.setItem', 'localStorage.getItem', 'localStorage.removeItem', 'try {', 'catch (error)', 'console.warn']) {
  assert(jslib.includes(marker), `WeChat jslib missing safe lifecycle feedback marker: ${marker}`);
}

assert(runtimeHud.includes('rootImage.raycastTarget = false'), 'Runtime HUD root must not block map input with a transparent raycast target.');
assert(controller.includes('var importedSimulation = new CitySimulationCore(config)') && controller.includes('simulation = importedSimulation'), 'City save import should be transactional and only replace simulation after successful import.');
for (const marker of ['Enum.IsDefined(typeof(CityTaxLevel)', 'Enum.IsDefined(typeof(ZoneType)', 'Enum.IsDefined(typeof(RoadTier)', 'Enum.IsDefined(typeof(CityPolicy)', 'save.Version < 6 || save.LockedExpansionUnlocked']) {
  assert(core.includes(marker), `Unity simulation import missing save sanitization marker: ${marker}`);
}

console.log(`Unity-only ${verifyMode} verification passed.`);

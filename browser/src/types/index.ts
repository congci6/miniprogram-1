// ===== Basic type definitions migrated from Unity PocketCity.Core =====
export enum BuildingCategory {
  Residential, Commercial, Industrial, Utility, Service,
  Decoration, Park, Office, MixedUse,
}
export enum OverlayMode {
  Normal, Traffic, Pollution, Zoning, Services, Transit,
  LandValue, Waste, Logistics, Utilities, Communications,
  RoadSafety, Parking, Stormwater,
}
export enum ZoneType {
  None, Residential, Commercial, Industrial, Civic,
  Utility, Office, MixedUse,
}
export enum TerrainType { Plain, Water, Hill }
export enum CityPolicy {
  GreenCode, TransitPriority, GrowthGrants, AffordableHousing,
  TrafficSafetyCampaign, CompleteStreets, SignalOptimization,
  CongestionPricing, ParkingFees,
}
export enum CityTaxLevel { Low, Normal, High }
export type CityTimeScale = 0 | 1 | 2 | 4;
export enum ServiceBudgetLevel { Lean, Standard, Boosted }
export enum RoadTier { Local, Arterial }
export enum BuildingRotation { None = 0, North, East, South, West }
export type PlanningTool =
  | 'inspect'
  | 'road'
  | 'residential'
  | 'commercial'
  | 'industrial'
  | 'park'
  | 'clinic'
  | 'school'
  | 'erase';
export type ServiceBuildingId = 'community_park' | 'community_clinic' | 'community_school';
export type MaterialId = 'wood' | 'metal' | 'plastic';
export type CityMaterialInventory = Record<MaterialId, number>;
export type MaterialCost = Partial<Record<MaterialId, number>>;
export interface ProductionJob {
  id: string; materialId: MaterialId; label: string;
  remainingDays: number; totalDays: number;
}
export interface CityOrder {
  id: string; title: string; required: MaterialCost; rewardCash: number;
}
export interface CityObjective {
  id: string; title: string; description: string;
  advice: string; rewardCash: number; rewardExperience: number; completed: boolean;
}
export interface CityTileInspection {
  title: string; terrain: string; zone: string; road: string; building: string;
  overlayLabel: string; overlayValue: string; diagnosis: string; legend: string;
}
export interface CityPolicyImpactPreview {
  policy: CityPolicy; label: string; nextEnabled: boolean; summary: string; deltas: string[];
}
export interface CityPolicyState {
  policy: CityPolicy; label: string; shortLabel: string; enabled: boolean; preview: CityPolicyImpactPreview;
}
export interface CityInsight {
  id: string; label: string; text: string; priority: number;
}
export type CityUnlockActionId = 'roadUpgrade' | 'residentialLevel2' | 'residentialLevel3';
export interface CityUnlockEntry {
  label: string; unlockLevel: number; unlocked: boolean;
}
export interface CityUnlockState {
  materials: Record<MaterialId, CityUnlockEntry>;
  services: Record<ServiceBuildingId, CityUnlockEntry>;
  actions: Record<CityUnlockActionId, CityUnlockEntry>;
}
export interface GridPos { x: number; y: number }
export interface BuildingDefinition {
  id: string; name: string; category: BuildingCategory;
  cost: number; upkeep: number; size: number;
  capacity: number; jobs: number; serviceRadius: number;
  serviceValue: number; powerOutput: number; waterOutput: number;
  powerUse: number; waterUse: number; pollution: number;
  trafficGeneration: number; preferredZone: ZoneType;
  unlockMinPopulation: number; unlockMinCityScore: number;
}
export interface CityMetrics {
  day: number; population: number; cash: number;
  happiness: number; cityScore: number;
  cityLevel: number; cityExperience: number; nextLevelExperience: number;
  cityLevelName: string; taxLevel: CityTaxLevel; taxRatePercent: number;
  residentialDemand: number; commercialDemand: number; industrialDemand: number;
  demandAdvice: string;
  demandFocus: string; demandDriver: string; demandAction: string; demandUrgency: number;
  forecastRisk: number; forecastFocus: string; forecastAction: string; cashRunwayDays: number;
  budgetStress: number; budgetFocus: string; budgetDriver: string; budgetAction: string;
  growthBottleneckScore: number; growthBottleneckFocus: string; growthBottleneckDriver: string; growthBottleneckAction: string;
  economicSpecializationScore: number; economicSpecializationFocus: string; economicSpecializationDriver: string; economicSpecializationAction: string;
  districtPriorityScore: number; districtPriorityFocus: string; districtPriorityDriver: string; districtPriorityAction: string;
  housingAffordabilityScore: number; housingAffordabilityFocus: string; housingAffordabilityDriver: string; housingAffordabilityAction: string;
  buildingUpgradeReadinessScore: number; buildingUpgradeReadyCount: number; buildingUpgradeBlockedCount: number;
  buildingUpgradeReadinessFocus: string; buildingUpgradeReadinessDriver: string; buildingUpgradeReadinessAction: string;
  serviceGapAdvisorScore: number; serviceGapAdvisorFocus: string; serviceGapAdvisorDriver: string; serviceGapAdvisorAction: string;
  roadHierarchyPressure: number; roadHierarchyFocus: string; roadHierarchyDriver: string; roadHierarchyAction: string;
  commuteCorridorScore: number; commuteCorridorFocus: string; commuteCorridorDriver: string; commuteCorridorAction: string;
  congestion: number; pollution: number; crime: number;
  healthCoverage: number; educationCoverage: number;
  safetyCoverage: number; securityCoverage: number;
  parkCoverage: number; transitCoverage: number;
  roadCoverage: number; serviceGapPressure: number;
  parkingPressure: number; walkability: number; accidentRisk: number;
  stormwaterResilience: number; floodRisk: number; policyBacklog: number;
  administrationLoad: number; administrationCapacity: number;
  administrationUtilization: number; administrationEfficiency: number;
  functionalBufferScore: number; landUseConflictPressure: number; landUseConflictCount: number;
  functionalBufferFocus: string; functionalBufferDriver: string; functionalBufferAction: string;
  landUseEfficiencyScore: number; vacantZoneTiles: number; developedZoneRatio: number;
  landUseEfficiencyFocus: string; landUseEfficiencyDriver: string; landUseEfficiencyAction: string;
  developmentQualityScore: number; lowQualityBuildingCount: number;
  developmentQualityFocus: string; developmentQualityDriver: string; developmentQualityAction: string;
  landValue: number; rentPressure: number;
  attractiveness: number; visitors: number; tourismIncome: number;
  housingCapacity: number; buildingCount: number; mixedUseBuildings: number; officeBuildings: number; officeJobs: number;
  unlockedBuildingIds: string[]; alerts: string[]; alertDigest: string;
  recentEvents: string[];
}

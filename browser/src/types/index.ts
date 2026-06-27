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
export enum ServiceBudgetLevel { Lean, Standard, Boosted }
export enum RoadTier { Local, Arterial }
export enum BuildingRotation { None = 0, North, East, South, West }
export type PlanningTool =
  | 'inspect'
  | 'road'
  | 'residential'
  | 'commercial'
  | 'industrial'
  | 'erase';
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
  cityLevelName: string; taxRatePercent: number;
  congestion: number; pollution: number; crime: number;
  healthCoverage: number; educationCoverage: number;
  safetyCoverage: number; securityCoverage: number;
  parkCoverage: number; transitCoverage: number;
  roadCoverage: number; serviceGapPressure: number;
  landValue: number; rentPressure: number;
  housingCapacity: number; buildingCount: number;
  unlockedBuildingIds: string[]; alerts: string[];
}

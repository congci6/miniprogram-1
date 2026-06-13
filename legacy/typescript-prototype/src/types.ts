export type GridPos = { x: number; y: number };

export type GridRect = { x: number; y: number; w: number; h: number };

export type TerrainType = 'plain' | 'water' | 'hill';

export type ZoneType = 'none' | 'residential' | 'commercial' | 'industrial';

export type OverlayMode = 'normal' | 'traffic' | 'pollution';

export type BuildingCategory =
  | 'residential'
  | 'commercial'
  | 'industrial'
  | 'utility'
  | 'service';

export type Tile = {
  terrain: TerrainType;
  zone: ZoneType;
  roadId?: string;
  buildingId?: string;
  pollution: number;
  landValue: number;
};

export type BuildingConfig = {
  id: string;
  name: string;
  category: BuildingCategory;
  size: { w: number; h: number };
  cost: number;
  upkeep: number;
  capacity?: number;
  jobs?: number;
  powerUse?: number;
  powerOutput?: number;
  waterUse?: number;
  waterOutput?: number;
  pollution?: number;
  serviceRadius?: number;
  unlock?: {
    minPopulation?: number;
    minCityScore?: number;
  };
  modelKey: string;
};

export type PlacedBuilding = {
  id: string;
  configId: string;
  pos: GridPos;
  size: { w: number; h: number };
  connectedRoadId?: string;
};

export type RoadNode = {
  id: string;
  pos: GridPos;
  load: number;
  capacity: number;
};

export type DemandMetrics = {
  residential: number;
  commercial: number;
  industrial: number;
};

export type CityObjective = {
  title: string;
  hint: string;
  progress: number;
  required: number;
  done: boolean;
};

export type CityMetrics = {
  population: number;
  cash: number;
  happiness: number;
  housingCapacity: number;
  jobs: number;
  powerSupply: number;
  powerDemand: number;
  waterSupply: number;
  waterDemand: number;
  congestion: number;
  pollution: number;
  serviceCoverage: number;
  demand: DemandMetrics;
  cityScore: number;
  cityLevelName: string;
  alerts: string[];
  roadTiles: number;
  buildingCount: number;
  connectedBuildings: number;
  disconnectedBuildings: number;
  unlockedBuildingIds: string[];
  activeObjective: CityObjective;
};

export type GameCommand =
  | { type: 'BUILD_ROAD'; from: GridPos; to: GridPos }
  | { type: 'PLACE_BUILDING'; buildingId: string; pos: GridPos }
  | { type: 'DEMOLISH'; pos: GridPos }
  | { type: 'SET_ZONE'; zone: ZoneType; area: GridRect };

export type CommandResult = {
  ok: boolean;
  message: string;
  cost?: number;
};

export type SerializedCityState = {
  width: number;
  height: number;
  tiles: Tile[];
  buildings: PlacedBuilding[];
  roads: RoadNode[];
  metrics: CityMetrics;
  elapsedSeconds: number;
  taxRate: number;
  nextId: number;
};

export type SaveGame = {
  version: number;
  createdAt: number;
  updatedAt: number;
  city: SerializedCityState;
};

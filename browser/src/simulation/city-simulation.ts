import { CityGrid } from './grid';
import {
  CityMaterialInventory,
  CityMetrics,
  CityInsight,
  CityObjective,
  CityOrder,
  CityPolicy,
  CityPolicyImpactPreview,
  CityPolicyState,
  CityTileInspection,
  CityTaxLevel,
  CityUnlockState,
  MaterialCost,
  MaterialId,
  PlanningTool,
  ProductionJob,
  ServiceBuildingId,
  TerrainType,
  ZoneType,
} from '@/types/index';

interface GridStats {
  roads: number;
  upgradedRoads: number;
  roadCapacity: number;
  zonedTiles: number;
  developedZoneTiles: number;
  vacantZoneTiles: number;
  housingCapacity: number;
  jobs: number;
  pollution: number;
  plannedResidentialTiles: number;
  industrialTiles: number;
  residentialTiles: number;
  upgradedResidentialTiles: number;
  serviceBuildings: number;
  parkCoveredResidentialTiles: number;
  healthCoveredResidentialTiles: number;
  educationCoveredResidentialTiles: number;
  landUseConflictPressure: number;
  landUseConflictCount: number;
}

interface DemandAnalysis {
  residential: number;
  commercial: number;
  industrial: number;
  advice: string;
  focus: string;
  driver: string;
  action: string;
  urgency: number;
}

interface RiskForecast {
  risk: number;
  focus: string;
  action: string;
  cashRunwayDays: number;
}

interface MonthlyBudget {
  income: number;
  roadCost: number;
  zoningCost: number;
  populationCost: number;
  pollutionCost: number;
  policyNet: number;
  policyBacklogCost: number;
  expenses: number;
  net: number;
}

interface PolicyEffect {
  monthlyNet: number;
  congestion: number;
  pollution: number;
  residentialDemand: number;
  commercialDemand: number;
  industrialDemand: number;
  happiness: number;
  rentPressure: number;
  parkingPressure: number;
  walkability: number;
  accidentRisk: number;
  stormwaterResilience: number;
  floodRisk: number;
}

interface PolicyPreviewMetrics {
  monthlyNet: number;
  congestion: number;
  parkingPressure: number;
  walkability: number;
  accidentRisk: number;
  stormwaterResilience: number;
  floodRisk: number;
  policyBacklog: number;
}

interface AdministrationState {
  load: number;
  capacity: number;
  utilization: number;
  efficiency: number;
  policyBacklog: number;
}

interface FunctionalBufferAdvisor {
  score: number;
  pressure: number;
  conflictCount: number;
  focus: string;
  driver: string;
  action: string;
}

interface LandUseEfficiencyAdvisor {
  score: number;
  pressure: number;
  vacantZoneTiles: number;
  developedZoneRatio: number;
  focus: string;
  driver: string;
  action: string;
}

interface PolicyDefinition {
  label: string;
  shortLabel: string;
  effect: PolicyEffect;
}

interface BudgetBreakdownAdvisor {
  stress: number;
  focus: string;
  driver: string;
  action: string;
}

interface GrowthBottleneckAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

interface EconomicSpecializationAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

interface DistrictPriorityAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

interface HousingAffordabilityAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

interface BuildingUpgradeReadinessAdvisor {
  score: number;
  readyCount: number;
  blockedCount: number;
  focus: string;
  driver: string;
  action: string;
}

interface ServiceGapAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

interface RoadHierarchyAdvisor {
  pressure: number;
  focus: string;
  driver: string;
  action: string;
}

interface CommuteCorridorAdvisor {
  score: number;
  focus: string;
  driver: string;
  action: string;
}

export interface PlanningActionResult {
  changed: boolean;
  message: string;
}

export interface CityOfflineProgressResult {
  daysElapsed: number;
  materialsProduced: CityMaterialInventory;
  storageBlocked: boolean;
  capped: boolean;
}

interface TileSnapshot {
  x: number;
  y: number;
  zone: ZoneType;
  roadId: string;
  buildingId: string;
}

export interface CitySimulationLegacySnapshot {
  version: 1;
  metrics: CityMetrics;
  tiles: TileSnapshot[];
}

export interface CitySimulationSnapshotV2 {
  version: 2;
  metrics: CityMetrics;
  materials: CityMaterialInventory;
  productionQueue: ProductionJob[];
  orders: CityOrder[];
  completedOrders: number;
  completedObjectiveIds?: string[];
  activePolicies?: CityPolicy[];
  nextProductionId: number;
  nextOrderId: number;
  tiles: TileSnapshot[];
}

export interface CitySimulationSnapshot extends Omit<CitySimulationSnapshotV2, 'version'> {
  version: 3;
  savedAtMs: number;
}

export type CitySimulationSaveData = CitySimulationLegacySnapshot | CitySimulationSnapshotV2 | CitySimulationSnapshot;

const ZONE_STATS: Partial<Record<ZoneType, { housing: number; jobs: number; pollution: number; label: string }>> = {
  [ZoneType.Residential]: { housing: 24, jobs: 0, pollution: 1, label: '住宅区' },
  [ZoneType.Commercial]: { housing: 0, jobs: 18, pollution: 2, label: '商业区' },
  [ZoneType.Industrial]: { housing: 0, jobs: 28, pollution: 7, label: '工业区' },
};

const INSPECTION_ZONE_LABELS: Record<ZoneType, string> = {
  [ZoneType.None]: '未规划',
  [ZoneType.Residential]: '住宅区',
  [ZoneType.Commercial]: '商业区',
  [ZoneType.Industrial]: '工业区',
  [ZoneType.Civic]: '市政区',
  [ZoneType.Utility]: '设施区',
  [ZoneType.Office]: '办公区',
  [ZoneType.MixedUse]: '混合区',
};

const INSPECTION_TERRAIN_LABELS: Record<TerrainType, string> = {
  [TerrainType.Plain]: '平地',
  [TerrainType.Water]: '水域',
  [TerrainType.Hill]: '丘陵',
};

const TILE_INSPECTION_LEGEND = '图例: 绿住宅 蓝商业 橙工业 黑道路 粉服务 黄选中';

const MATERIAL_LABELS: Record<MaterialId, string> = {
  wood: '木材',
  metal: '金属',
  plastic: '塑料',
};

const SERVICE_LABELS: Record<ServiceBuildingId, string> = {
  community_park: '社区公园',
  community_clinic: '社区诊所',
  community_school: '社区学校',
};

interface ServiceBuildingDefinition {
  label: string;
  cashCost: number;
  unlockLevel: number;
  radius: number;
  jobs: number;
  pollution: number;
  parkValue: number;
  healthValue: number;
  educationValue: number;
}

const SERVICE_BUILDINGS: Record<ServiceBuildingId, ServiceBuildingDefinition> = {
  community_park: {
    label: '社区公园',
    cashCost: 420,
    unlockLevel: 1,
    radius: 3,
    jobs: 2,
    pollution: -1,
    parkValue: 1,
    healthValue: 0,
    educationValue: 0,
  },
  community_clinic: {
    label: '社区诊所',
    cashCost: 620,
    unlockLevel: 2,
    radius: 4,
    jobs: 10,
    pollution: 0,
    parkValue: 0,
    healthValue: 1,
    educationValue: 0,
  },
  community_school: {
    label: '社区学校',
    cashCost: 680,
    unlockLevel: 3,
    radius: 4,
    jobs: 12,
    pollution: 1,
    parkValue: 0,
    healthValue: 0,
    educationValue: 1,
  },
};

const PRODUCTION_RECIPES: Record<MaterialId, { label: string; days: number; cashCost: number; unlockLevel: number }> = {
  wood: { label: '木材', days: 2, cashCost: 20, unlockLevel: 1 },
  metal: { label: '金属', days: 3, cashCost: 35, unlockLevel: 2 },
  plastic: { label: '塑料', days: 4, cashCost: 55, unlockLevel: 3 },
};

const ORDER_TEMPLATES: Array<{ title: string; required: MaterialCost; rewardCash: number }> = [
  { title: '邻里建材订单', required: { wood: 2, metal: 1 }, rewardCash: 520 },
  { title: '商业街补货', required: { wood: 1, plastic: 1 }, rewardCash: 430 },
  { title: '施工队急需材料', required: { metal: 2, plastic: 1 }, rewardCash: 720 },
  { title: '社区翻新计划', required: { wood: 3 }, rewardCash: 360 },
];

const RESIDENTIAL_UPGRADE_COSTS: Record<number, MaterialCost> = {
  2: { wood: 2, metal: 1 },
  3: { wood: 3, metal: 2, plastic: 1 },
};

const RESIDENTIAL_CAPACITY_BY_LEVEL: Record<number, number> = {
  1: 24,
  2: 42,
  3: 64,
};

interface CityObjectiveDefinition {
  id: string;
  title: string;
  description: string;
  rewardCash: number;
  rewardExperience: number;
  isMet: (simulation: CitySimulation, stats: GridStats) => boolean;
}

const OBJECTIVE_DEFINITIONS: CityObjectiveDefinition[] = [
  {
    id: 'first-road',
    title: '接通第一条路',
    description: '修建 1 段道路，给街区留下通行骨架',
    rewardCash: 180,
    rewardExperience: 20,
    isMet: (_simulation, stats) => stats.roads >= 1,
  },
  {
    id: 'first-neighborhood',
    title: '形成第一片社区',
    description: '规划 2 个住宅地块，打开人口增长',
    rewardCash: 260,
    rewardExperience: 35,
    isMet: (_simulation, stats) => stats.plannedResidentialTiles >= 2,
  },
  {
    id: 'start-factory',
    title: '启动材料生产',
    description: '排产任意一种材料，建立订单供给',
    rewardCash: 320,
    rewardExperience: 30,
    isMet: (simulation) => simulation.productionQueue.length > 0 || simulation.getStorageUsed() > 0 || simulation.completedOrders > 0,
  },
  {
    id: 'first-arterial',
    title: '升级第一条主干道',
    description: '把任意道路升级为主干道，提高通行容量',
    rewardCash: 540,
    rewardExperience: 45,
    isMet: (_simulation, stats) => stats.upgradedRoads >= 1,
  },
  {
    id: 'first-delivery',
    title: '完成第一笔订单',
    description: '交付 1 个城市订单，回收建设现金',
    rewardCash: 520,
    rewardExperience: 55,
    isMet: (simulation) => simulation.completedOrders >= 1,
  },
  {
    id: 'upgrade-home',
    title: '升级一处住宅',
    description: '把任意住宅升级到 2 级，提升住房容量',
    rewardCash: 640,
    rewardExperience: 70,
    isMet: (_simulation, stats) => stats.upgradedResidentialTiles >= 1,
  },
  {
    id: 'first-service',
    title: '建设第一座公共服务',
    description: '建成公园、诊所或学校中的任意一座',
    rewardCash: 520,
    rewardExperience: 50,
    isMet: (_simulation, stats) => stats.serviceBuildings >= 1,
  },
  {
    id: 'balanced-services',
    title: '完善基础服务覆盖',
    description: '让公园、医疗、教育覆盖率都达到 50%',
    rewardCash: 960,
    rewardExperience: 120,
    isMet: (simulation) => simulation.metrics.parkCoverage >= 50
      && simulation.metrics.healthCoverage >= 50
      && simulation.metrics.educationCoverage >= 50,
  },
  {
    id: 'administration-capacity',
    title: '稳住行政容量',
    description: '启用 2 项政策，并保持行政利用率与政策积压可控',
    rewardCash: 820,
    rewardExperience: 90,
    isMet: (simulation) => {
      const enabledPolicies = simulation.getPolicyStates().filter((policy) => policy.enabled).length;
      return enabledPolicies >= 2
        && simulation.metrics.administrationEfficiency >= 70
        && simulation.metrics.administrationUtilization <= 90
        && simulation.metrics.policyBacklog <= 35;
    },
  },
  {
    id: 'functional-buffer',
    title: '建立功能缓冲',
    description: '让住宅和工业保持间距，避免贴脸污染冲突',
    rewardCash: 760,
    rewardExperience: 85,
    isMet: (simulation, stats) => stats.residentialTiles >= 2
      && stats.industrialTiles >= 1
      && simulation.metrics.landUseConflictPressure <= 20
      && simulation.metrics.functionalBufferScore >= 75,
  },
  {
    id: 'compact-development',
    title: '推进紧凑用地',
    description: '先消化已划分地块，再继续外扩新区',
    rewardCash: 840,
    rewardExperience: 95,
    isMet: (simulation, stats) => stats.zonedTiles >= 6
      && simulation.metrics.developedZoneRatio >= 70
      && simulation.metrics.vacantZoneTiles <= 3
      && simulation.metrics.landUseEfficiencyScore >= 70,
  },
];

const ZONE_COST = 120;
const ROAD_COST = 180;
const ROAD_UPGRADE_COST = 360;
const ERASE_COST = 20;
const STORAGE_CAPACITY = 30;
const MAX_RESIDENTIAL_LEVEL = 3;
const MAX_RECENT_EVENTS = 5;
const ROAD_UPGRADE_UNLOCK_LEVEL = 2;
const RESIDENTIAL_UPGRADE_UNLOCK_LEVELS: Record<number, number> = {
  2: 2,
  3: 3,
};
const OFFLINE_MS_PER_DAY = 60_000;
const MAX_OFFLINE_DAYS = 72;
const ROAD_CAPACITY: Record<string, number> = {
  local: 1,
  arterial: 3,
};
const ROAD_LABELS: Record<string, string> = {
  local: '普通道路',
  arterial: '主干道',
};
const ACTION_EXPERIENCE = {
  road: 8,
  zone: 5,
  production: 3,
  order: 45,
  residentialUpgrade: 60,
  service: 40,
  roadUpgrade: 35,
};
const CITY_LEVEL_EXPERIENCE = [0, 80, 220, 460, 800, 1250, 1800, 2500, 3400, 4600];
const CITY_LEVEL_NAMES = [
  '新生街区',
  '起步城区',
  '成长街区',
  '活力城区',
  '繁荣城区',
  '区域中心',
  '都会核心',
  '卓越都会',
  '理想城市',
  '未来都会',
];
const ZERO_POLICY_EFFECT: PolicyEffect = {
  monthlyNet: 0,
  congestion: 0,
  pollution: 0,
  residentialDemand: 0,
  commercialDemand: 0,
  industrialDemand: 0,
  happiness: 0,
  rentPressure: 0,
  parkingPressure: 0,
  walkability: 0,
  accidentRisk: 0,
  stormwaterResilience: 0,
  floodRisk: 0,
};
const POLICY_ORDER: CityPolicy[] = [
  CityPolicy.GreenCode,
  CityPolicy.TransitPriority,
  CityPolicy.GrowthGrants,
  CityPolicy.AffordableHousing,
  CityPolicy.TrafficSafetyCampaign,
  CityPolicy.CompleteStreets,
  CityPolicy.SignalOptimization,
  CityPolicy.CongestionPricing,
  CityPolicy.ParkingFees,
];
const POLICY_DEFINITIONS: Record<CityPolicy, PolicyDefinition> = {
  [CityPolicy.GreenCode]: {
    label: '绿色规范',
    shortLabel: '绿色',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -62, pollution: -9, stormwaterResilience: 10, floodRisk: -8, industrialDemand: -3 },
  },
  [CityPolicy.TransitPriority]: {
    label: '公交优先',
    shortLabel: '公交',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -86, congestion: -8, parkingPressure: -7, walkability: 9, commercialDemand: 3 },
  },
  [CityPolicy.GrowthGrants]: {
    label: '增长补贴',
    shortLabel: '补贴',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -118, residentialDemand: 7, commercialDemand: 5, industrialDemand: 4, happiness: 2 },
  },
  [CityPolicy.AffordableHousing]: {
    label: '保障住房',
    shortLabel: '保障',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -74, residentialDemand: 8, happiness: 4, rentPressure: -10 },
  },
  [CityPolicy.TrafficSafetyCampaign]: {
    label: '交通安全行动',
    shortLabel: '安全',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -46, accidentRisk: -13, happiness: 1 },
  },
  [CityPolicy.CompleteStreets]: {
    label: '完整街道',
    shortLabel: '完整',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -78, congestion: -4, parkingPressure: -4, walkability: 14, accidentRisk: -7, stormwaterResilience: 4 },
  },
  [CityPolicy.SignalOptimization]: {
    label: '信号优化',
    shortLabel: '信号',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: -42, congestion: -10, accidentRisk: -4, commercialDemand: 2 },
  },
  [CityPolicy.CongestionPricing]: {
    label: '拥堵收费',
    shortLabel: '拥堵',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: 82, congestion: -9, parkingPressure: -3, walkability: 3, happiness: -2 },
  },
  [CityPolicy.ParkingFees]: {
    label: '停车收费',
    shortLabel: '停车',
    effect: { ...ZERO_POLICY_EFFECT, monthlyNet: 68, parkingPressure: -10, congestion: -3, walkability: 2, happiness: -1 },
  },
};

export class CitySimulation {
  readonly grid: CityGrid;
  metrics: CityMetrics;
  readonly materials: CityMaterialInventory = { wood: 0, metal: 0, plastic: 0 };
  readonly productionQueue: ProductionJob[] = [];
  readonly orders: CityOrder[] = [];
  completedOrders = 0;
  private readonly completedObjectiveIds = new Set<string>();
  private dayAccumulator = 0;
  private taxLevel: CityTaxLevel = CityTaxLevel.Normal;
  private activePolicies: CityPolicy[] = [];
  private nextProductionId = 1;
  private nextOrderId = 1;

  constructor(w: number, h: number) {
    this.grid = new CityGrid(w, h);
    this.metrics = this.createInitialMetrics();
    this.ensureOrders();
    this.computeMetrics();
  }

  private createInitialMetrics(): CityMetrics {
    return {
      day: 1, population: 0, cash: 50000, happiness: 50,
      cityScore: 50, cityLevel: 1, cityExperience: 0,
      nextLevelExperience: CITY_LEVEL_EXPERIENCE[1], cityLevelName: CITY_LEVEL_NAMES[0],
      taxLevel: CityTaxLevel.Normal, taxRatePercent: 9, congestion: 0, pollution: 0, crime: 0,
      residentialDemand: 0, commercialDemand: 0, industrialDemand: 0,
      demandAdvice: '沿道路规划住宅，打开第一批迁入需求。',
      demandFocus: '住宅',
      demandDriver: '住房缺口',
      demandAction: '沿道路规划住宅区',
      demandUrgency: 0,
      forecastRisk: 0,
      forecastFocus: '稳定',
      forecastAction: '继续扩建并保留现金缓冲',
      cashRunwayDays: 999,
      budgetStress: 0,
      budgetFocus: '稳定',
      budgetDriver: '月度现金流稳定',
      budgetAction: '保留现金缓冲',
      growthBottleneckScore: 0,
      growthBottleneckFocus: '起步',
      growthBottleneckDriver: '等待首个成长卡点',
      growthBottleneckAction: '先接路规划住宅',
      economicSpecializationScore: 0,
      economicSpecializationFocus: '起步',
      economicSpecializationDriver: '等待住商工片区成形',
      economicSpecializationAction: '先接路规划住宅',
      districtPriorityScore: 0,
      districtPriorityFocus: '起步',
      districtPriorityDriver: '等待首个片区成形',
      districtPriorityAction: '先接路规划住宅',
      housingAffordabilityScore: 0,
      housingAffordabilityFocus: '起步',
      housingAffordabilityDriver: '等待住宅片区成形',
      housingAffordabilityAction: '先接路规划住宅',
      buildingUpgradeReadinessScore: 0,
      buildingUpgradeReadyCount: 0,
      buildingUpgradeBlockedCount: 0,
      buildingUpgradeReadinessFocus: '起步',
      buildingUpgradeReadinessDriver: '等待可升级住宅',
      buildingUpgradeReadinessAction: '先让住宅自然开发',
      serviceGapAdvisorScore: 0,
      serviceGapAdvisorFocus: '均衡',
      serviceGapAdvisorDriver: '暂无住宅服务压力',
      serviceGapAdvisorAction: '先接路规划住宅',
      roadHierarchyPressure: 0,
      roadHierarchyFocus: '骨架',
      roadHierarchyDriver: '道路尚未形成压力',
      roadHierarchyAction: '按分区接入道路',
      commuteCorridorScore: 0,
      commuteCorridorFocus: '起步',
      commuteCorridorDriver: '尚未形成通勤压力',
      commuteCorridorAction: '先接路规划住宅',
      healthCoverage: 0, educationCoverage: 0, safetyCoverage: 0,
      securityCoverage: 0, parkCoverage: 0, transitCoverage: 0,
      roadCoverage: 0, serviceGapPressure: 0,
      parkingPressure: 0, walkability: 30, accidentRisk: 0,
      stormwaterResilience: 30, floodRisk: 0, policyBacklog: 0,
      administrationLoad: 0, administrationCapacity: 105,
      administrationUtilization: 0, administrationEfficiency: 100,
      functionalBufferScore: 100,
      landUseConflictPressure: 0,
      landUseConflictCount: 0,
      functionalBufferFocus: '起步',
      functionalBufferDriver: '等待工业与住宅片区成形',
      functionalBufferAction: '工业预留在城市边缘',
      landUseEfficiencyScore: 100,
      vacantZoneTiles: 0,
      developedZoneRatio: 100,
      landUseEfficiencyFocus: '起步',
      landUseEfficiencyDriver: '尚未形成分区压力',
      landUseEfficiencyAction: '先接路规划少量住宅',
      landValue: 30,
      rentPressure: 0, housingCapacity: 0, buildingCount: 0,
      unlockedBuildingIds: ['community_park'],
      alerts: [],
      alertDigest: '城市运行平稳',
      recentEvents: [],
    };
  }

  tick(deltaSeconds: number): boolean {
    let changed = false;
    this.dayAccumulator += deltaSeconds;
    while (this.dayAccumulator >= 1) {
      this.dayAccumulator -= 1;
      this.metrics.day++;
      if (this.processProductionDay()) changed = true;
      this.computeMetrics();
      if (this.processZoneDevelopment()) {
        changed = true;
        this.computeMetrics();
      }
      this.processPopulation();
      this.processEconomy();
      if (this.evaluateObjectives().length > 0) {
        changed = true;
        this.computeMetrics();
      }
    }
    return changed;
  }

  applyTool(x: number, y: number, tool: PlanningTool): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };

    if (tool === 'inspect') {
      return { changed: false, message: `查看地块 (${x}, ${y})` };
    }

    if (tile.terrain === TerrainType.Water) return { changed: false, message: '水域暂时不能规划' };
    if (tile.terrain === TerrainType.Hill) return { changed: false, message: '丘陵暂时不能规划' };

    if (tool === 'road') {
      if (tile.roadId) return { changed: false, message: '这里已经有道路' };
      if (!this.trySpend(ROAD_COST)) return { changed: false, message: '现金不足，无法修建道路' };
      this.grid.setRoad(x, y, 'local');
      this.computeMetrics();
      this.pushCityEvent(`修建道路 (${x},${y})`);
      return { changed: true, message: this.appendObjectiveRewards(`修建道路 -$${ROAD_COST}`, ACTION_EXPERIENCE.road) };
    }

    if (tool === 'erase') {
      if (!tile.roadId && tile.zone === ZoneType.None && !tile.buildingId) {
        return { changed: false, message: '这个地块已经是空地' };
      }
      if (!this.trySpend(ERASE_COST)) return { changed: false, message: '现金不足，无法清理地块' };
      this.grid.clearPlanning(x, y);
      this.computeMetrics();
      this.pushCityEvent(`清理地块 (${x},${y})`);
      return { changed: true, message: this.appendObjectiveRewards(`清理地块 -$${ERASE_COST}`) };
    }

    const serviceBuildingId = this.serviceBuildingFromTool(tool);
    if (serviceBuildingId) return this.placeServiceBuilding(x, y, serviceBuildingId);

    const zone = this.zoneFromTool(tool);
    const stats = ZONE_STATS[zone];
    if (!stats) return { changed: false, message: '暂不支持这个规划工具' };
    if (tile.zone === zone) return { changed: false, message: `这里已经是${stats.label}` };
    if (!this.trySpend(ZONE_COST)) return { changed: false, message: '现金不足，无法划定新区' };

    this.grid.setZone(x, y, zone);
    this.computeMetrics();
    this.pushCityEvent(`划定${stats.label} (${x},${y})`);
    return { changed: true, message: this.appendObjectiveRewards(`划定${stats.label} -$${ZONE_COST}`, ACTION_EXPERIENCE.zone) };
  }

  startProduction(materialId: MaterialId): PlanningActionResult {
    const recipe = PRODUCTION_RECIPES[materialId];
    if (!recipe) return { changed: false, message: '未知生产配方' };
    if (!this.isLevelUnlocked(recipe.unlockLevel)) {
      return { changed: false, message: this.lockedMessage(recipe.label, recipe.unlockLevel) };
    }
    if (this.productionQueue.length >= this.getProductionSlots()) {
      return { changed: false, message: '生产槽已满，等待工厂完成' };
    }
    if (this.getStorageUsed() >= STORAGE_CAPACITY) {
      return { changed: false, message: '仓库已满，先完成订单或升级住宅' };
    }
    if (!this.trySpend(recipe.cashCost)) {
      return { changed: false, message: '现金不足，无法开工生产' };
    }

    this.productionQueue.push({
      id: `job-${this.nextProductionId++}`,
      materialId,
      label: recipe.label,
      remainingDays: recipe.days,
      totalDays: recipe.days,
    });
    this.pushCityEvent(`${recipe.label}开始生产`);
    return { changed: true, message: this.appendObjectiveRewards(`${recipe.label}已排产 -$${recipe.cashCost}`, ACTION_EXPERIENCE.production) };
  }

  fulfillOrder(orderId: string): PlanningActionResult {
    const order = this.orders.find((candidate) => candidate.id === orderId);
    if (!order) return { changed: false, message: '订单不存在' };
    if (!this.hasMaterials(order.required)) {
      return { changed: false, message: '材料不足，无法交付订单' };
    }

    this.consumeMaterials(order.required);
    this.metrics.cash += order.rewardCash;
    this.completedOrders++;
    this.orders.splice(this.orders.indexOf(order), 1);
    this.ensureOrders();
    this.computeMetrics();
    this.pushCityEvent(`${order.title}交付`);
    return { changed: true, message: this.appendObjectiveRewards(`${order.title}交付 +$${order.rewardCash}`, ACTION_EXPERIENCE.order) };
  }

  upgradeResidentialAt(x: number, y: number): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    if (tile.zone !== ZoneType.Residential) return { changed: false, message: '请选择住宅区升级' };
    if (!tile.roadId && !this.hasAdjacentRoad(x, y)) return { changed: false, message: '住宅升级需要临近道路' };

    const currentLevel = this.getResidentialLevel(tile);
    if (currentLevel <= 0) return { changed: false, message: '住宅区还未自然开发，先等待接路入住' };
    if (currentLevel >= MAX_RESIDENTIAL_LEVEL) return { changed: false, message: '住宅已达到当前最高等级' };

    const nextLevel = currentLevel + 1;
    const unlockLevel = RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[nextLevel] ?? 1;
    if (!this.isLevelUnlocked(unlockLevel)) {
      return { changed: false, message: this.lockedMessage(`住宅 ${nextLevel} 级`, unlockLevel) };
    }
    const cost = RESIDENTIAL_UPGRADE_COSTS[nextLevel];
    if (!this.hasMaterials(cost)) return { changed: false, message: `升级需要 ${this.formatMaterialCost(cost)}` };

    this.consumeMaterials(cost);
    this.grid.setBuilding(x, y, `residential_l${nextLevel}`);
    this.metrics.cash += 220 * nextLevel;
    this.computeMetrics();
    this.pushCityEvent(`住宅升级到${nextLevel}级 (${x},${y})`);
    return { changed: true, message: this.appendObjectiveRewards(`住宅升级到 ${nextLevel} 级 +$${220 * nextLevel}`, ACTION_EXPERIENCE.residentialUpgrade) };
  }

  upgradeRoadAt(x: number, y: number): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    if (!tile.roadId) return { changed: false, message: '请选择道路地块升级' };
    if (tile.roadId === 'arterial') return { changed: false, message: '这条道路已经是主干道' };
    if (!this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL)) {
      return { changed: false, message: this.lockedMessage('主干道升级', ROAD_UPGRADE_UNLOCK_LEVEL) };
    }
    if (!this.trySpend(ROAD_UPGRADE_COST)) return { changed: false, message: '现金不足，无法升级道路' };

    this.grid.setRoad(x, y, 'arterial');
    this.computeMetrics();
    this.pushCityEvent(`道路升级为主干道 (${x},${y})`);
    return { changed: true, message: this.appendObjectiveRewards(`道路升级为主干道 -$${ROAD_UPGRADE_COST}`, ACTION_EXPERIENCE.roadUpgrade) };
  }

  getProductionSlots(): number {
    const industrialTiles = this.calculateGridStats().industrialTiles;
    return Math.min(4, Math.max(1, 1 + Math.floor(industrialTiles / 2)));
  }

  getStorageUsed(): number {
    return Object.values(this.materials).reduce((sum, count) => sum + count, 0);
  }

  getStorageCapacity(): number {
    return STORAGE_CAPACITY;
  }

  getResidentialLevel(tile: { zone: ZoneType; buildingId: string }): number {
    if (tile.zone !== ZoneType.Residential) return 0;
    if (tile.buildingId === 'residential_l1') return 1;
    const match = /^residential_l([2-3])$/.exec(tile.buildingId);
    return match ? Number(match[1]) : 0;
  }

  getServiceBuildingLabel(buildingId: string): string {
    return SERVICE_LABELS[buildingId as ServiceBuildingId] ?? '';
  }

  getRoadLabel(roadId: string): string {
    return ROAD_LABELS[roadId] ?? (roadId ? roadId : '无');
  }

  getTileInspectionLegend(): string {
    return TILE_INSPECTION_LEGEND;
  }

  getTileInspection(x: number, y: number): CityTileInspection | null {
    const tile = this.grid.getTile(x, y);
    if (!tile) return null;
    const terrain = INSPECTION_TERRAIN_LABELS[tile.terrain];
    const zone = INSPECTION_ZONE_LABELS[tile.zone];
    const road = tile.roadId ? this.getRoadLabel(tile.roadId) : '无';
    const building = this.getInspectionBuildingLabel(tile.zone, tile.buildingId);
    const overlay = this.getTileOverlaySummary(x, y);
    const title = tile.roadId ? `(${x}, ${y}) ${road}` : `(${x}, ${y}) ${zone}`;
    return {
      title,
      terrain,
      zone,
      road,
      building,
      overlayLabel: overlay.label,
      overlayValue: overlay.value,
      diagnosis: this.getTileDiagnosis(x, y),
      legend: TILE_INSPECTION_LEGEND,
    };
  }

  getPolicyStates(): CityPolicyState[] {
    return POLICY_ORDER.map((policy) => {
      const definition = POLICY_DEFINITIONS[policy];
      return {
        policy,
        label: definition.label,
        shortLabel: definition.shortLabel,
        enabled: this.activePolicies.includes(policy),
        preview: this.getPolicyImpactPreview(policy),
      };
    });
  }

  getPolicyImpactPreview(policy: CityPolicy): CityPolicyImpactPreview {
    const definition = POLICY_DEFINITIONS[policy];
    if (!definition) {
      return {
        policy,
        label: '未知政策',
        nextEnabled: false,
        summary: '未知政策',
        deltas: ['暂无可预览影响'],
      };
    }

    const currentlyEnabled = this.activePolicies.includes(policy);
    const current = this.buildPolicyPreviewMetrics(this.activePolicies);
    const nextPolicies = currentlyEnabled
      ? this.activePolicies.filter((candidate) => candidate !== policy)
      : [...this.activePolicies, policy];
    const next = this.buildPolicyPreviewMetrics(nextPolicies);
    const deltas = [
      this.formatPolicyDelta('月收支', next.monthlyNet - current.monthlyNet, '$'),
      this.formatPolicyDelta('拥堵', next.congestion - current.congestion),
      this.formatPolicyDelta('停车', next.parkingPressure - current.parkingPressure),
      this.formatPolicyDelta('步行', next.walkability - current.walkability),
      this.formatPolicyDelta('事故', next.accidentRisk - current.accidentRisk),
      this.formatPolicyDelta('雨洪', next.stormwaterResilience - current.stormwaterResilience),
      this.formatPolicyDelta('内涝', next.floodRisk - current.floodRisk),
      this.formatPolicyDelta('积压', next.policyBacklog - current.policyBacklog),
    ].filter(Boolean);

    return {
      policy,
      label: definition.label,
      nextEnabled: !currentlyEnabled,
      summary: `${currentlyEnabled ? '关闭' : '启用'}${definition.label}`,
      deltas: deltas.length > 0 ? deltas : ['关键指标变化很小'],
    };
  }

  togglePolicy(policy: CityPolicy): PlanningActionResult {
    const definition = POLICY_DEFINITIONS[policy];
    if (!definition) return { changed: false, message: '未知城市政策' };
    const index = this.activePolicies.indexOf(policy);
    const enabled = index < 0;
    if (enabled) {
      this.activePolicies.push(policy);
    } else {
      this.activePolicies.splice(index, 1);
    }
    this.computeMetrics();
    const message = `${enabled ? '启用' : '关闭'}${definition.label}`;
    this.pushCityEvent(message);
    return { changed: true, message: this.appendObjectiveRewards(message) };
  }

  getInsightStack(limit = 5): CityInsight[] {
    const insights: CityInsight[] = [];
    const objective = this.getObjectives().find((candidate) => !candidate.completed);
    if (objective) {
      insights.push({
        id: `objective:${objective.id}`,
        label: '目标',
        text: `${objective.title}: ${objective.advice}`,
        priority: 1000,
      });
    }

    const candidates: CityInsight[] = [
      {
        id: 'risk',
        label: '风险',
        text: `${this.metrics.forecastFocus}${this.metrics.forecastRisk}: ${this.metrics.forecastAction}`,
        priority: this.metrics.forecastRisk >= 35 ? 700 + this.metrics.forecastRisk : 0,
      },
      {
        id: 'budget',
        label: '预算',
        text: `${this.metrics.budgetFocus}${this.metrics.budgetStress}: ${this.metrics.budgetAction}`,
        priority: this.metrics.budgetStress >= 35 ? 680 + this.metrics.budgetStress : 0,
      },
      {
        id: 'administration',
        label: '行政',
        text: `利用率${this.metrics.administrationUtilization}%/积压${this.metrics.policyBacklog}: ${this.metrics.administrationUtilization > 90 ? '升级城市或关闭低优先级政策' : '政策执行可控'}`,
        priority: this.metrics.administrationUtilization >= 75 || this.metrics.policyBacklog >= 35 ? 670 + Math.max(this.metrics.administrationUtilization, this.metrics.policyBacklog) : 0,
      },
      {
        id: 'growth',
        label: '卡点',
        text: `${this.metrics.growthBottleneckFocus}${this.metrics.growthBottleneckScore}: ${this.metrics.growthBottleneckAction}`,
        priority: this.metrics.growthBottleneckScore >= 35 ? 660 + this.metrics.growthBottleneckScore : 0,
      },
      {
        id: 'district',
        label: '优先级',
        text: `${this.metrics.districtPriorityFocus}${this.metrics.districtPriorityScore}: ${this.metrics.districtPriorityAction}`,
        priority: this.metrics.districtPriorityScore >= 35 ? 640 + this.metrics.districtPriorityScore : 0,
      },
      {
        id: 'functional-buffer',
        label: '缓冲',
        text: `${this.metrics.functionalBufferFocus}${this.metrics.landUseConflictPressure}: ${this.metrics.functionalBufferAction}`,
        priority: this.metrics.landUseConflictPressure >= 25 ? 630 + this.metrics.landUseConflictPressure : 0,
      },
      {
        id: 'land-use',
        label: '用地',
        text: `${this.metrics.landUseEfficiencyFocus}${this.metrics.landUseEfficiencyScore}: ${this.metrics.landUseEfficiencyAction}`,
        priority: this.metrics.landUseEfficiencyScore < 70 ? 625 + (100 - this.metrics.landUseEfficiencyScore) : 0,
      },
      {
        id: 'road',
        label: '道路',
        text: `${this.metrics.roadHierarchyFocus}${this.metrics.roadHierarchyPressure}: ${this.metrics.roadHierarchyAction}`,
        priority: this.metrics.roadHierarchyPressure >= 35 ? 620 + this.metrics.roadHierarchyPressure : 0,
      },
      {
        id: 'commute',
        label: '通勤',
        text: `${this.metrics.commuteCorridorFocus}${this.metrics.commuteCorridorScore}: ${this.metrics.commuteCorridorAction}`,
        priority: this.metrics.commuteCorridorScore >= 35 ? 600 + this.metrics.commuteCorridorScore : 0,
      },
      {
        id: 'service',
        label: '服务',
        text: `${this.metrics.serviceGapAdvisorFocus}${this.metrics.serviceGapAdvisorScore}: ${this.metrics.serviceGapAdvisorAction}`,
        priority: this.metrics.serviceGapAdvisorScore >= 35 ? 580 + this.metrics.serviceGapAdvisorScore : 0,
      },
      {
        id: 'upgrade',
        label: '升级',
        text: `候${this.metrics.buildingUpgradeReadyCount}/阻${this.metrics.buildingUpgradeBlockedCount}: ${this.metrics.buildingUpgradeReadinessAction}`,
        priority: this.metrics.buildingUpgradeReadinessScore >= 35 || this.metrics.buildingUpgradeReadyCount > 0 ? 560 + this.metrics.buildingUpgradeReadinessScore : 0,
      },
      {
        id: 'housing',
        label: '住房',
        text: `${this.metrics.housingAffordabilityFocus}${this.metrics.housingAffordabilityScore}: ${this.metrics.housingAffordabilityAction}`,
        priority: this.metrics.housingAffordabilityScore >= 35 ? 540 + this.metrics.housingAffordabilityScore : 0,
      },
      {
        id: 'economy',
        label: '经济',
        text: `${this.metrics.economicSpecializationFocus}${this.metrics.economicSpecializationScore}: ${this.metrics.economicSpecializationAction}`,
        priority: this.metrics.economicSpecializationScore >= 35 ? 520 + this.metrics.economicSpecializationScore : 0,
      },
      {
        id: 'demand',
        label: '需求',
        text: `${this.metrics.demandFocus}${this.metrics.demandUrgency}: ${this.metrics.demandAction}`,
        priority: this.metrics.demandUrgency >= 45 ? 500 + this.metrics.demandUrgency : 0,
      },
      {
        id: 'alerts',
        label: '提醒',
        text: this.metrics.alertDigest,
        priority: this.metrics.alerts.length > 0 ? 490 + this.metrics.alerts.length * 12 : 0,
      },
      {
        id: 'event',
        label: '事件',
        text: this.metrics.recentEvents[0] ?? '',
        priority: this.metrics.recentEvents.length > 0 ? 470 : 0,
      },
    ];

    insights.push(
      ...candidates
        .filter((insight) => insight.priority > 0 && insight.text.length > 0)
        .sort((a, b) => b.priority - a.priority)
        .slice(0, Math.max(0, limit - insights.length)),
    );

    if (insights.length === 0) {
      insights.push({
        id: 'stable',
        label: '节奏',
        text: '按目标扩建并保留现金缓冲',
        priority: 1,
      });
    }
    return insights.slice(0, limit);
  }

  getObjectives(): CityObjective[] {
    const stats = this.calculateGridStats();
    return OBJECTIVE_DEFINITIONS.map((objective) => ({
      id: objective.id,
      title: objective.title,
      description: objective.description,
      advice: this.getObjectiveAdvice(objective.id, stats),
      rewardCash: objective.rewardCash,
      rewardExperience: objective.rewardExperience,
      completed: this.completedObjectiveIds.has(objective.id),
    }));
  }

  getUnlockState(): CityUnlockState {
    const materials = {} as CityUnlockState['materials'];
    for (const materialId of Object.keys(PRODUCTION_RECIPES) as MaterialId[]) {
      const recipe = PRODUCTION_RECIPES[materialId];
      materials[materialId] = {
        label: recipe.label,
        unlockLevel: recipe.unlockLevel,
        unlocked: this.isLevelUnlocked(recipe.unlockLevel),
      };
    }

    const services = {} as CityUnlockState['services'];
    for (const serviceBuildingId of Object.keys(SERVICE_BUILDINGS) as ServiceBuildingId[]) {
      const service = SERVICE_BUILDINGS[serviceBuildingId];
      services[serviceBuildingId] = {
        label: service.label,
        unlockLevel: service.unlockLevel,
        unlocked: this.isLevelUnlocked(service.unlockLevel),
      };
    }

    return {
      materials,
      services,
      actions: {
        roadUpgrade: {
          label: '主干道升级',
          unlockLevel: ROAD_UPGRADE_UNLOCK_LEVEL,
          unlocked: this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL),
        },
        residentialLevel2: {
          label: '住宅 2 级',
          unlockLevel: RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[2],
          unlocked: this.isLevelUnlocked(RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[2]),
        },
        residentialLevel3: {
          label: '住宅 3 级',
          unlockLevel: RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[3],
          unlocked: this.isLevelUnlocked(RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[3]),
        },
      },
    };
  }

  createSnapshot(nowMs = Date.now()): CitySimulationSnapshot {
    const tiles: CitySimulationSnapshot['tiles'] = [];
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.zone !== ZoneType.None || tile.roadId || tile.buildingId) {
          tiles.push({ x, y, zone: tile.zone, roadId: tile.roadId, buildingId: tile.buildingId });
        }
      }
    }

    return {
      version: 3,
      savedAtMs: nowMs,
      metrics: {
        ...this.metrics,
        alerts: [...this.metrics.alerts],
        alertDigest: this.metrics.alertDigest,
        recentEvents: [...this.metrics.recentEvents],
        unlockedBuildingIds: [...this.metrics.unlockedBuildingIds],
      },
      materials: { ...this.materials },
      productionQueue: this.productionQueue.map((job) => ({ ...job })),
      orders: this.orders.map((order) => ({ ...order, required: { ...order.required } })),
      completedOrders: this.completedOrders,
      completedObjectiveIds: [...this.completedObjectiveIds],
      activePolicies: [...this.activePolicies],
      nextProductionId: this.nextProductionId,
      nextOrderId: this.nextOrderId,
      tiles,
    };
  }

  restoreSnapshot(snapshot: CitySimulationSaveData, nowMs = Date.now()): CityOfflineProgressResult {
    const offlineResult = this.createEmptyOfflineResult();
    if (snapshot.version !== 1 && snapshot.version !== 2 && snapshot.version !== 3) return offlineResult;

    Object.assign(this.metrics, snapshot.metrics);
    this.metrics.recentEvents = this.normalizeRecentEvents((snapshot.metrics as Partial<CityMetrics>).recentEvents);
    this.metrics.cityExperience = Math.max(0, this.metrics.cityExperience ?? 0);
    this.taxLevel = this.isTaxLevel(snapshot.metrics.taxLevel)
      ? snapshot.metrics.taxLevel
      : this.taxLevelFromRate(snapshot.metrics.taxRatePercent);
    this.metrics.taxLevel = this.taxLevel;
    this.refreshCityLevelProgress();
    if (snapshot.version === 2 || snapshot.version === 3) {
      this.materials.wood = Math.max(0, snapshot.materials.wood ?? 0);
      this.materials.metal = Math.max(0, snapshot.materials.metal ?? 0);
      this.materials.plastic = Math.max(0, snapshot.materials.plastic ?? 0);
      this.productionQueue.splice(0, this.productionQueue.length, ...snapshot.productionQueue.map((job) => ({ ...job })));
      this.orders.splice(0, this.orders.length, ...snapshot.orders.map((order) => ({ ...order, required: { ...order.required } })));
      this.completedOrders = Math.max(0, snapshot.completedOrders);
      this.completedObjectiveIds.clear();
      for (const objectiveId of snapshot.completedObjectiveIds ?? []) this.completedObjectiveIds.add(objectiveId);
      const restoredPolicies = [...new Set((snapshot.activePolicies ?? []).filter((policy) => this.isCityPolicy(policy)))];
      this.activePolicies.splice(0, this.activePolicies.length, ...restoredPolicies);
      this.nextProductionId = Math.max(1, snapshot.nextProductionId);
      this.nextOrderId = Math.max(1, snapshot.nextOrderId);
    } else {
      this.materials.wood = 0;
      this.materials.metal = 0;
      this.materials.plastic = 0;
      this.productionQueue.splice(0, this.productionQueue.length);
      this.orders.splice(0, this.orders.length);
      this.completedOrders = 0;
      this.completedObjectiveIds.clear();
      this.activePolicies.splice(0, this.activePolicies.length);
      this.nextProductionId = 1;
      this.nextOrderId = 1;
    }

    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) this.grid.clearPlanning(x, y);
    }

    for (const tile of snapshot.tiles) {
      this.grid.setTerrain(tile.x, tile.y, TerrainType.Plain);
      this.grid.setZone(tile.x, tile.y, tile.zone);
      if (tile.roadId) this.grid.setRoad(tile.x, tile.y, tile.roadId);
      if (tile.buildingId) this.grid.setBuilding(tile.x, tile.y, tile.buildingId);
    }

    this.ensureOrders();
    this.computeMetrics();
    if (this.evaluateObjectives().length > 0) this.computeMetrics();
    if (snapshot.version === 3) return this.applyOfflineProgress(snapshot.savedAtMs, nowMs);
    return offlineResult;
  }

  getTaxRevenue(): number {
    const rate = this.getTaxRatePercent();
    return Math.floor(this.metrics.population * rate * 0.16);
  }

  setTaxLevel(level: CityTaxLevel): PlanningActionResult {
    if (!this.isTaxLevel(level)) return { changed: false, message: '未知税率档位' };
    if (this.taxLevel === level) return { changed: false, message: `税率已是 ${this.getTaxRatePercent()}%` };

    this.taxLevel = level;
    this.computeMetrics();
    this.pushCityEvent(`税率调整为 ${this.getTaxRatePercent()}%`);
    return { changed: true, message: `税率调整为 ${this.getTaxRatePercent()}%` };
  }

  private buildPolicyPreviewMetrics(policies: CityPolicy[]): PolicyPreviewMetrics {
    const stats = this.calculateGridStats();
    const policyEffect = this.getPolicyEffect(policies);
    const policyBacklog = this.calculateAdministration(stats, policies).policyBacklog;
    const roadCoverage = stats.zonedTiles === 0 ? 0 : Math.min(100, (stats.roadCapacity / stats.zonedTiles) * 80);
    const baseCongestion = stats.developedZoneTiles === 0 ? 0 : stats.developedZoneTiles * 5 - stats.roadCapacity * 8;
    const congestion = this.clampPercent(baseCongestion + policyEffect.congestion + policyBacklog * 0.08);
    const pollution = this.clampPercent(stats.pollution + policyEffect.pollution);
    const parkCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.parkCoveredResidentialTiles / stats.residentialTiles) * 100);
    const healthCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.healthCoveredResidentialTiles / stats.residentialTiles) * 100);
    const educationCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.educationCoveredResidentialTiles / stats.residentialTiles) * 100);
    const serviceCoverage = (parkCoverage + healthCoverage + educationCoverage) / 3;
    const parkingPressure = this.clampPercent(stats.developedZoneTiles * 5 + this.metrics.population * 0.04 + congestion * 0.2 - stats.roadCapacity * 3 + policyEffect.parkingPressure);
    const walkability = this.clampPercent(30 + roadCoverage * 0.18 + serviceCoverage * 0.2 - congestion * 0.14 - parkingPressure * 0.08 + policyEffect.walkability);
    const accidentRisk = this.clampPercent(10 + congestion * 0.35 + stats.roads * 0.5 - roadCoverage * 0.08 + policyEffect.accidentRisk);
    const stormwaterResilience = this.clampPercent(28 + parkCoverage * 0.22 + walkability * 0.08 - pollution * 0.1 + policyEffect.stormwaterResilience);
    const floodRisk = this.clampPercent(50 + stats.developedZoneTiles * 1.8 - stormwaterResilience * 0.7 + policyEffect.floodRisk);
    const budget = this.estimateMonthlyBudgetForPolicies(stats, pollution, policies);
    return {
      monthlyNet: budget.net,
      congestion,
      parkingPressure,
      walkability,
      accidentRisk,
      stormwaterResilience,
      floodRisk,
      policyBacklog,
    };
  }

  private getPolicyEffect(policies = this.activePolicies): PolicyEffect {
    const effect = { ...ZERO_POLICY_EFFECT };
    for (const policy of new Set(policies)) {
      const definition = POLICY_DEFINITIONS[policy];
      if (!definition) continue;
      effect.monthlyNet += definition.effect.monthlyNet;
      effect.congestion += definition.effect.congestion;
      effect.pollution += definition.effect.pollution;
      effect.residentialDemand += definition.effect.residentialDemand;
      effect.commercialDemand += definition.effect.commercialDemand;
      effect.industrialDemand += definition.effect.industrialDemand;
      effect.happiness += definition.effect.happiness;
      effect.rentPressure += definition.effect.rentPressure;
      effect.parkingPressure += definition.effect.parkingPressure;
      effect.walkability += definition.effect.walkability;
      effect.accidentRisk += definition.effect.accidentRisk;
      effect.stormwaterResilience += definition.effect.stormwaterResilience;
      effect.floodRisk += definition.effect.floodRisk;
    }
    return effect;
  }

  private calculateAdministration(stats: GridStats, policies: CityPolicy[]): AdministrationState {
    const policyCount = new Set(policies).size;
    const load = Math.round(
      this.metrics.population * 0.04
      + stats.zonedTiles * 3
      + stats.developedZoneTiles * 2
      + stats.serviceBuildings * 8
      + policyCount * 28,
    );
    const capacity = Math.round(70 + this.metrics.cityLevel * 35 + Math.min(45, stats.serviceBuildings * 10));
    const utilization = capacity <= 0 ? 0 : this.clampPercent((load / capacity) * 100);
    const overload = Math.max(0, utilization - 85);
    const policyOverload = Math.max(0, policyCount - Math.max(2, this.metrics.cityLevel + 1));
    const policyBacklog = this.clampPercent(policyCount * 3 + policyOverload * 12 + overload * 1.1);
    const efficiency = this.clampPercent(100 - Math.max(0, utilization - 65) * 0.75 - policyBacklog * 0.22);
    return { load, capacity, utilization, efficiency, policyBacklog };
  }

  private formatPolicyDelta(label: string, delta: number, prefix = ''): string {
    const rounded = Math.round(delta);
    if (rounded === 0) return '';
    const sign = rounded > 0 ? '+' : '-';
    return `${label}${sign}${prefix}${Math.abs(rounded)}`;
  }

  private trySpend(amount: number): boolean {
    if (this.metrics.cash < amount) return false;
    this.metrics.cash -= amount;
    return true;
  }

  private processProductionDay(): boolean {
    let changed = false;
    for (let i = this.productionQueue.length - 1; i >= 0; i--) {
      const job = this.productionQueue[i];
      job.remainingDays = Math.max(0, job.remainingDays - 1);
      if (job.remainingDays > 0) continue;
      if (this.getStorageUsed() >= STORAGE_CAPACITY) {
        job.remainingDays = 0;
        continue;
      }
      this.materials[job.materialId]++;
      this.productionQueue.splice(i, 1);
      this.pushCityEvent(`${job.label}完成 +1`);
      changed = true;
    }
    return changed;
  }

  private processZoneDevelopment(): boolean {
    const demandOrder = [
      {
        zone: ZoneType.Residential,
        demand: this.metrics.residentialDemand,
        minDemand: 45,
        buildingId: 'residential_l1',
      },
      {
        zone: ZoneType.Commercial,
        demand: this.metrics.commercialDemand,
        minDemand: 50,
        buildingId: 'commercial_l1',
      },
      {
        zone: ZoneType.Industrial,
        demand: this.metrics.industrialDemand,
        minDemand: 50,
        buildingId: 'industrial_l1',
      },
    ].sort((a, b) => b.demand - a.demand);

    for (const entry of demandOrder) {
      if (entry.demand < entry.minDemand) continue;
      const tile = this.findVacantDevelopableZone(entry.zone);
      if (!tile) continue;
      this.grid.setBuilding(tile.x, tile.y, entry.buildingId);
      this.pushCityEvent(`${ZONE_STATS[entry.zone]?.label ?? '分区'}自然开发 (${tile.x},${tile.y})`);
      return true;
    }

    return false;
  }

  private findVacantDevelopableZone(zone: ZoneType): { x: number; y: number } | null {
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.zone !== zone || tile.buildingId || tile.roadId) continue;
        if (!this.hasAdjacentRoad(x, y)) continue;
        return { x, y };
      }
    }
    return null;
  }

  private placeServiceBuilding(x: number, y: number, serviceBuildingId: ServiceBuildingId): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    const service = SERVICE_BUILDINGS[serviceBuildingId];
    if (!this.isLevelUnlocked(service.unlockLevel)) {
      return { changed: false, message: this.lockedMessage(service.label, service.unlockLevel) };
    }
    if (tile.terrain === TerrainType.Water) return { changed: false, message: '水域暂时不能建设服务设施' };
    if (tile.roadId) return { changed: false, message: '道路地块不能建设服务设施' };
    if (tile.zone !== ZoneType.None || tile.buildingId) return { changed: false, message: '请在空地建设服务设施' };
    if (!this.hasAdjacentRoad(x, y)) return { changed: false, message: `${service.label}需要临近道路` };
    if (!this.trySpend(service.cashCost)) return { changed: false, message: `现金不足，无法建设${service.label}` };

    this.grid.setZone(x, y, ZoneType.Civic);
    this.grid.setBuilding(x, y, serviceBuildingId);
    this.computeMetrics();
    this.pushCityEvent(`建设${service.label} (${x},${y})`);
    return { changed: true, message: this.appendObjectiveRewards(`建设${service.label} -$${service.cashCost}`, ACTION_EXPERIENCE.service) };
  }

  private applyOfflineProgress(savedAtMs: number, nowMs: number): CityOfflineProgressResult {
    const elapsedMs = Math.max(0, nowMs - savedAtMs);
    const rawDays = Math.floor(elapsedMs / OFFLINE_MS_PER_DAY);
    const daysElapsed = Math.min(rawDays, MAX_OFFLINE_DAYS);
    const result = this.createEmptyOfflineResult();
    result.daysElapsed = daysElapsed;
    result.capped = rawDays > MAX_OFFLINE_DAYS;
    if (daysElapsed <= 0) return result;

    const beforeMaterials = { ...this.materials };
    this.tick(daysElapsed);
    result.materialsProduced = {
      wood: Math.max(0, this.materials.wood - beforeMaterials.wood),
      metal: Math.max(0, this.materials.metal - beforeMaterials.metal),
      plastic: Math.max(0, this.materials.plastic - beforeMaterials.plastic),
    };
    result.storageBlocked = this.getStorageUsed() >= STORAGE_CAPACITY
      && this.productionQueue.some((job) => job.remainingDays <= 0);
    return result;
  }

  private createEmptyOfflineResult(): CityOfflineProgressResult {
    return {
      daysElapsed: 0,
      materialsProduced: { wood: 0, metal: 0, plastic: 0 },
      storageBlocked: false,
      capped: false,
    };
  }

  private normalizeRecentEvents(value: unknown): string[] {
    if (!Array.isArray(value)) return [];
    return value
      .filter((event): event is string => typeof event === 'string' && event.trim().length > 0)
      .slice(0, MAX_RECENT_EVENTS);
  }

  private pushCityEvent(message: string): void {
    const trimmed = message.trim();
    if (!trimmed) return;
    const event = `第${this.metrics.day}天 ${trimmed}`;
    const events = this.normalizeRecentEvents(this.metrics.recentEvents);
    if (events[0] === event) return;
    this.metrics.recentEvents = [event, ...events.filter((candidate) => candidate !== event)].slice(0, MAX_RECENT_EVENTS);
  }

  private appendObjectiveRewards(message: string, experience = 0): string {
    const progressParts: string[] = [];
    if (experience > 0) {
      this.grantExperience(experience);
      progressParts.push(`经验+${experience}`);
    }
    const rewards = this.evaluateObjectives();
    if (rewards.length > 0) progressParts.push(`目标完成：${rewards.join('、')}`);
    if (progressParts.length === 0) return message;
    this.computeMetrics();
    return `${message}；${progressParts.join('；')}`;
  }

  private evaluateObjectives(): string[] {
    const stats = this.calculateGridStats();
    const rewards: string[] = [];
    for (const objective of OBJECTIVE_DEFINITIONS) {
      if (this.completedObjectiveIds.has(objective.id)) continue;
      if (!objective.isMet(this, stats)) continue;
      this.completedObjectiveIds.add(objective.id);
      this.metrics.cash += objective.rewardCash;
      this.grantExperience(objective.rewardExperience);
      this.pushCityEvent(`目标完成：${objective.title}`);
      rewards.push(`${objective.title} +$${objective.rewardCash} 经验+${objective.rewardExperience}`);
    }
    return rewards;
  }

  private getObjectiveAdvice(objectiveId: string, stats: GridStats): string {
    switch (objectiveId) {
      case 'first-road':
        return stats.roads > 0 ? '道路已接通，继续规划住宅。' : '选道路工具，在空地铺第一段路。';
      case 'first-neighborhood':
        if (stats.roads === 0) return '先修道路，再沿路规划住宅。';
        return `再规划 ${Math.max(0, 2 - stats.plannedResidentialTiles)} 块住宅地。`;
      case 'start-factory':
        if (this.getStorageUsed() >= STORAGE_CAPACITY) return '仓库已满，先交付订单或升级住宅。';
        return '点右侧木材按钮，启动第一单生产。';
      case 'first-arterial':
        if (!this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL)) return '先完成前置目标升到 Lv2。';
        if (stats.roads === 0) return '先铺道路，再升级主干道。';
        return '选中普通道路，点升道路。';
      case 'first-delivery':
        return this.getOrderAdvice();
      case 'upgrade-home':
        if (!this.isLevelUnlocked(RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[2])) return '先升到 Lv2 解锁住宅升级。';
        if (stats.residentialTiles === 0) return '先规划住宅并接近道路。';
        return this.hasMaterials(RESIDENTIAL_UPGRADE_COSTS[2])
          ? '选中住宅，点升级住宅。'
          : `准备${this.formatMissingMaterials(RESIDENTIAL_UPGRADE_COSTS[2])}。`;
      case 'first-service':
        if (stats.roads === 0) return '先铺道路，服务建筑要临路。';
        return '选公园工具，建在道路旁。';
      case 'balanced-services':
        return this.getServiceCoverageAdvice();
      case 'administration-capacity': {
        const enabledPolicies = this.getPolicyStates().filter((policy) => policy.enabled).length;
        if (enabledPolicies < 2) return '启用 2 项城市政策，观察行政容量。';
        if (this.metrics.administrationUtilization > 90) return '行政利用率过高，先升级城市或关闭低优先级政策。';
        if (this.metrics.policyBacklog > 35) return '政策积压偏高，暂缓继续加政策。';
        return '行政效率达标，等待目标结算。';
      }
      case 'functional-buffer':
        if (stats.residentialTiles < 2) return '先形成至少 2 块已入住住宅。';
        if (stats.industrialTiles < 1) return '把第一片工业放在住宅外侧并接路。';
        if (this.metrics.landUseConflictPressure > 20) return this.metrics.functionalBufferAction;
        return '缓冲已达标，等待目标结算。';
      case 'compact-development':
        if (stats.zonedTiles < 6) return '规划至少 6 块分区，形成可比较的片区。';
        if (this.metrics.vacantZoneTiles > 3) return this.metrics.landUseEfficiencyAction;
        if (this.metrics.developedZoneRatio < 70) return '等待已接路分区自然开发，暂缓继续外扩。';
        return '用地效率达标，等待目标结算。';
      default:
        return '继续扩建城市并优化路网。';
    }
  }

  private getOrderAdvice(): string {
    const order = this.orders[0];
    if (!order) return '等待新的城市订单刷新。';
    if (this.hasMaterials(order.required)) return '材料已齐，点交付订单。';
    return `补齐${this.formatMissingMaterials(order.required)}后交付。`;
  }

  private getServiceCoverageAdvice(): string {
    if (!this.isLevelUnlocked(3)) return '先升到 Lv3 解锁学校。';
    const gaps = [
      { label: '公园', value: this.metrics.parkCoverage, action: '补公园' },
      { label: '医疗', value: this.metrics.healthCoverage, action: '补诊所' },
      { label: '教育', value: this.metrics.educationCoverage, action: '补学校' },
    ].sort((a, b) => a.value - b.value);
    const focus = gaps[0];
    if (focus.value >= 50) return '三类服务已接近达标，等待目标结算。';
    return `${focus.action}，把${focus.label}覆盖提到 50%。`;
  }

  private formatMissingMaterials(cost: MaterialCost): string {
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .map(([materialId, required]) => {
        const missing = Math.max(0, required - this.materials[materialId]);
        return missing > 0 ? `${MATERIAL_LABELS[materialId]}x${missing}` : '';
      })
      .filter(Boolean)
      .join('、') || '所需材料';
  }

  private grantExperience(amount: number): void {
    this.metrics.cityExperience = Math.max(0, (this.metrics.cityExperience ?? 0) + amount);
    this.refreshCityLevelProgress();
  }

  private refreshCityLevelProgress(): void {
    const experience = Math.max(0, this.metrics.cityExperience ?? 0);
    let level = 1;
    for (let i = 1; i < CITY_LEVEL_EXPERIENCE.length; i++) {
      if (experience >= CITY_LEVEL_EXPERIENCE[i]) level = i + 1;
    }
    this.metrics.cityLevel = level;
    this.metrics.cityExperience = experience;
    this.metrics.cityLevelName = CITY_LEVEL_NAMES[Math.min(level - 1, CITY_LEVEL_NAMES.length - 1)];
    this.metrics.nextLevelExperience = CITY_LEVEL_EXPERIENCE[level] ?? Math.max(experience, CITY_LEVEL_EXPERIENCE[CITY_LEVEL_EXPERIENCE.length - 1]);
    this.metrics.unlockedBuildingIds = (Object.keys(SERVICE_BUILDINGS) as ServiceBuildingId[])
      .filter((serviceBuildingId) => this.isLevelUnlocked(SERVICE_BUILDINGS[serviceBuildingId].unlockLevel));
  }

  private isLevelUnlocked(unlockLevel: number): boolean {
    return this.metrics.cityLevel >= unlockLevel;
  }

  private lockedMessage(label: string, unlockLevel: number): string {
    return `${label}需要城市 Lv${unlockLevel} 解锁`;
  }

  private zoneFromTool(tool: PlanningTool): ZoneType {
    switch (tool) {
      case 'residential': return ZoneType.Residential;
      case 'commercial': return ZoneType.Commercial;
      case 'industrial': return ZoneType.Industrial;
      default: return ZoneType.None;
    }
  }

  private serviceBuildingFromTool(tool: PlanningTool): ServiceBuildingId | null {
    switch (tool) {
      case 'park': return 'community_park';
      case 'clinic': return 'community_clinic';
      case 'school': return 'community_school';
      default: return null;
    }
  }

  private computeMetrics(): void {
    const stats = this.calculateGridStats();
    const policyEffect = this.getPolicyEffect();
    const administration = this.calculateAdministration(stats, this.activePolicies);
    const policyBacklog = administration.policyBacklog;
    const roadCoverage = stats.zonedTiles === 0 ? 0 : Math.min(100, (stats.roadCapacity / stats.zonedTiles) * 80);
    const baseCongestion = stats.developedZoneTiles === 0 ? 0 : stats.developedZoneTiles * 5 - stats.roadCapacity * 8;
    const congestion = this.clampPercent(baseCongestion + policyEffect.congestion + policyBacklog * 0.08);
    const pollution = this.clampPercent(stats.pollution + policyEffect.pollution);
    const parkCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.parkCoveredResidentialTiles / stats.residentialTiles) * 100);
    const healthCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.healthCoveredResidentialTiles / stats.residentialTiles) * 100);
    const educationCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.educationCoveredResidentialTiles / stats.residentialTiles) * 100);
    const serviceCoverage = (parkCoverage + healthCoverage + educationCoverage) / 3;
    const serviceGapPressure = stats.residentialTiles === 0 ? 0 : Math.max(0, 100 - serviceCoverage);
    const rentPressure = stats.housingCapacity === 0
      ? 0
      : this.clampPercent((this.metrics.population / stats.housingCapacity) * 100 - 75 + policyEffect.rentPressure);
    const taxRatePercent = this.getTaxRatePercent();
    const taxPressure = taxRatePercent - 9;
    const landValue = Math.max(10, Math.min(100, 35 + roadCoverage * 0.22 + parkCoverage * 0.12 - pollution * 0.2 - congestion * 0.15));
    const parkingPressure = this.clampPercent(stats.developedZoneTiles * 5 + this.metrics.population * 0.04 + congestion * 0.2 - stats.roadCapacity * 3 + policyEffect.parkingPressure);
    const walkability = this.clampPercent(30 + roadCoverage * 0.18 + serviceCoverage * 0.2 - congestion * 0.14 - parkingPressure * 0.08 + policyEffect.walkability);
    const accidentRisk = this.clampPercent(10 + congestion * 0.35 + stats.roads * 0.5 - roadCoverage * 0.08 + policyEffect.accidentRisk);
    const stormwaterResilience = this.clampPercent(28 + parkCoverage * 0.22 + walkability * 0.08 - pollution * 0.1 + policyEffect.stormwaterResilience);
    const floodRisk = this.clampPercent(50 + stats.developedZoneTiles * 1.8 - stormwaterResilience * 0.7 + policyEffect.floodRisk);
    const bufferAdvisor = this.createFunctionalBufferAdvisor(stats);
    const landUseAdvisor = this.createLandUseEfficiencyAdvisor(stats, roadCoverage);
    const demand = this.calculateDemand(stats, roadCoverage, serviceCoverage, landValue, pollution, congestion, taxPressure, policyEffect, bufferAdvisor.pressure);
    const budget = this.estimateMonthlyBudget(stats, pollution);
    const serviceAdvisor = this.createServiceGapAdvisor(stats, parkCoverage, healthCoverage, educationCoverage);
    const roadAdvisor = this.createRoadHierarchyAdvisor(stats, roadCoverage, congestion);
    const commuteAdvisor = this.createCommuteCorridorAdvisor(stats, roadCoverage, congestion, demand, roadAdvisor);
    const housingAdvisor = this.createHousingAffordabilityAdvisor(stats, demand, rentPressure, serviceCoverage, roadCoverage, landValue, taxPressure, commuteAdvisor);
    const upgradeAdvisor = this.createBuildingUpgradeReadinessAdvisor(stats);

    this.metrics.housingCapacity = stats.housingCapacity;
    this.metrics.buildingCount = stats.developedZoneTiles + stats.roads + stats.serviceBuildings;
    this.metrics.roadCoverage = roadCoverage;
    this.metrics.congestion = congestion;
    this.metrics.pollution = pollution;
    this.metrics.parkCoverage = parkCoverage;
    this.metrics.healthCoverage = healthCoverage;
    this.metrics.educationCoverage = educationCoverage;
    this.metrics.serviceGapPressure = serviceGapPressure;
    this.metrics.rentPressure = rentPressure;
    this.metrics.parkingPressure = parkingPressure;
    this.metrics.walkability = walkability;
    this.metrics.accidentRisk = accidentRisk;
    this.metrics.stormwaterResilience = stormwaterResilience;
    this.metrics.floodRisk = floodRisk;
    this.metrics.policyBacklog = policyBacklog;
    this.metrics.administrationLoad = administration.load;
    this.metrics.administrationCapacity = administration.capacity;
    this.metrics.administrationUtilization = administration.utilization;
    this.metrics.administrationEfficiency = administration.efficiency;
    this.metrics.functionalBufferScore = bufferAdvisor.score;
    this.metrics.landUseConflictPressure = bufferAdvisor.pressure;
    this.metrics.landUseConflictCount = bufferAdvisor.conflictCount;
    this.metrics.functionalBufferFocus = bufferAdvisor.focus;
    this.metrics.functionalBufferDriver = bufferAdvisor.driver;
    this.metrics.functionalBufferAction = bufferAdvisor.action;
    this.metrics.landUseEfficiencyScore = landUseAdvisor.score;
    this.metrics.vacantZoneTiles = landUseAdvisor.vacantZoneTiles;
    this.metrics.developedZoneRatio = landUseAdvisor.developedZoneRatio;
    this.metrics.landUseEfficiencyFocus = landUseAdvisor.focus;
    this.metrics.landUseEfficiencyDriver = landUseAdvisor.driver;
    this.metrics.landUseEfficiencyAction = landUseAdvisor.action;
    this.metrics.taxLevel = this.taxLevel;
    this.metrics.taxRatePercent = taxRatePercent;
    this.metrics.landValue = landValue;
    this.metrics.residentialDemand = demand.residential;
    this.metrics.commercialDemand = demand.commercial;
    this.metrics.industrialDemand = demand.industrial;
    this.metrics.demandAdvice = demand.advice;
    this.metrics.demandFocus = demand.focus;
    this.metrics.demandDriver = demand.driver;
    this.metrics.demandAction = demand.action;
    this.metrics.demandUrgency = demand.urgency;
    this.metrics.happiness = Math.round(Math.max(5, Math.min(100, 50 + roadCoverage * 0.18 + serviceCoverage * 0.18 + walkability * 0.08 + administration.efficiency * 0.04 - pollution * 0.22 - rentPressure * 0.2 - accidentRisk * 0.08 - bufferAdvisor.pressure * 0.12 - taxPressure * 2 - policyBacklog * 0.06 + policyEffect.happiness)));
    this.metrics.cityScore = Math.round(Math.max(1, Math.min(100, 42 + this.metrics.happiness * 0.35 + roadCoverage * 0.18 + serviceCoverage * 0.12 + stormwaterResilience * 0.04 + administration.efficiency * 0.04 + bufferAdvisor.score * 0.03 + landUseAdvisor.score * 0.04 - pollution * 0.2 - floodRisk * 0.06 - bufferAdvisor.pressure * 0.08 - landUseAdvisor.pressure * 0.06)));
    this.refreshCityLevelProgress();
    this.metrics.alerts = this.createAlerts(stats);
    this.metrics.alertDigest = this.createAlertDigest(this.metrics.alerts);
    const forecast = this.createRiskForecast(stats, budget.net);
    const budgetAdvisor = this.createBudgetBreakdownAdvisor(budget);
    const economicAdvisor = this.createEconomicSpecializationAdvisor(stats, demand, roadCoverage, congestion, pollution, landValue);
    const growthAdvisor = this.createGrowthBottleneckAdvisor(
      stats,
      demand,
      forecast,
      budgetAdvisor,
      economicAdvisor,
      serviceAdvisor,
      roadAdvisor,
      commuteAdvisor,
      housingAdvisor,
      upgradeAdvisor,
      bufferAdvisor,
      landUseAdvisor,
    );
    const districtAdvisor = this.createDistrictPriorityAdvisor(stats, demand, budgetAdvisor, serviceAdvisor, roadAdvisor, commuteAdvisor, housingAdvisor, upgradeAdvisor, bufferAdvisor, landUseAdvisor);
    this.metrics.forecastRisk = forecast.risk;
    this.metrics.forecastFocus = forecast.focus;
    this.metrics.forecastAction = forecast.action;
    this.metrics.cashRunwayDays = forecast.cashRunwayDays;
    this.metrics.budgetStress = budgetAdvisor.stress;
    this.metrics.budgetFocus = budgetAdvisor.focus;
    this.metrics.budgetDriver = budgetAdvisor.driver;
    this.metrics.budgetAction = budgetAdvisor.action;
    this.metrics.growthBottleneckScore = growthAdvisor.score;
    this.metrics.growthBottleneckFocus = growthAdvisor.focus;
    this.metrics.growthBottleneckDriver = growthAdvisor.driver;
    this.metrics.growthBottleneckAction = growthAdvisor.action;
    this.metrics.economicSpecializationScore = economicAdvisor.score;
    this.metrics.economicSpecializationFocus = economicAdvisor.focus;
    this.metrics.economicSpecializationDriver = economicAdvisor.driver;
    this.metrics.economicSpecializationAction = economicAdvisor.action;
    this.metrics.districtPriorityScore = districtAdvisor.score;
    this.metrics.districtPriorityFocus = districtAdvisor.focus;
    this.metrics.districtPriorityDriver = districtAdvisor.driver;
    this.metrics.districtPriorityAction = districtAdvisor.action;
    this.metrics.housingAffordabilityScore = housingAdvisor.score;
    this.metrics.housingAffordabilityFocus = housingAdvisor.focus;
    this.metrics.housingAffordabilityDriver = housingAdvisor.driver;
    this.metrics.housingAffordabilityAction = housingAdvisor.action;
    this.metrics.buildingUpgradeReadinessScore = upgradeAdvisor.score;
    this.metrics.buildingUpgradeReadyCount = upgradeAdvisor.readyCount;
    this.metrics.buildingUpgradeBlockedCount = upgradeAdvisor.blockedCount;
    this.metrics.buildingUpgradeReadinessFocus = upgradeAdvisor.focus;
    this.metrics.buildingUpgradeReadinessDriver = upgradeAdvisor.driver;
    this.metrics.buildingUpgradeReadinessAction = upgradeAdvisor.action;
    this.metrics.serviceGapAdvisorScore = serviceAdvisor.score;
    this.metrics.serviceGapAdvisorFocus = serviceAdvisor.focus;
    this.metrics.serviceGapAdvisorDriver = serviceAdvisor.driver;
    this.metrics.serviceGapAdvisorAction = serviceAdvisor.action;
    this.metrics.roadHierarchyPressure = roadAdvisor.pressure;
    this.metrics.roadHierarchyFocus = roadAdvisor.focus;
    this.metrics.roadHierarchyDriver = roadAdvisor.driver;
    this.metrics.roadHierarchyAction = roadAdvisor.action;
    this.metrics.commuteCorridorScore = commuteAdvisor.score;
    this.metrics.commuteCorridorFocus = commuteAdvisor.focus;
    this.metrics.commuteCorridorDriver = commuteAdvisor.driver;
    this.metrics.commuteCorridorAction = commuteAdvisor.action;
  }

  private calculateDemand(
    stats: GridStats,
    roadCoverage: number,
    serviceCoverage: number,
    landValue: number,
    pollution: number,
    congestion: number,
    taxPressure: number,
    policyEffect: PolicyEffect,
    landUseConflictPressure: number,
  ): DemandAnalysis {
    const population = this.metrics.population;
    const targetHousing = Math.max(72, Math.ceil(population * 1.15 + stats.jobs * 0.55 + 48));
    const housingGap = targetHousing - stats.housingCapacity;
    const jobGap = population * 0.45 - stats.jobs;

    const residential = this.clampPercent(48 + housingGap * 0.35 + serviceCoverage * 0.08 + roadCoverage * 0.08 - pollution * 0.18 - congestion * 0.12 - landUseConflictPressure * 0.16 - taxPressure * 4 + policyEffect.residentialDemand);
    const commercial = this.clampPercent(35 + population * 0.18 + landValue * 0.15 + roadCoverage * 0.1 - stats.jobs * 0.12 - congestion * 0.12 - landUseConflictPressure * 0.08 - taxPressure * 3 + policyEffect.commercialDemand);
    const industrial = this.clampPercent(42 + Math.max(0, jobGap) * 0.8 + stats.residentialTiles * 5 - stats.industrialTiles * 14 + roadCoverage * 0.08 - pollution * 0.2 - landUseConflictPressure * 0.1 - taxPressure * 2 + policyEffect.industrialDemand);
    const advice = this.getDemandAdvice(residential, commercial, industrial);
    const top = [
      { key: 'residential', label: '住宅', value: residential },
      { key: 'commercial', label: '商业', value: commercial },
      { key: 'industrial', label: '工业', value: industrial },
    ].sort((a, b) => b.value - a.value)[0];

    let driver = '供需稳定';
    let action = '补道路、服务和订单材料';
    if (top.value < 45) {
      return { residential, commercial, industrial, advice, focus: '均衡', driver, action, urgency: top.value };
    }

    if (top.key === 'residential') {
      if (housingGap > 24) {
        driver = '住房缺口';
        action = '沿道路规划住宅区';
      } else if (serviceCoverage < 45) {
        driver = '服务覆盖不足';
        action = '补公园、诊所或学校';
      } else if (roadCoverage < 55) {
        driver = '道路接入不足';
        action = '先补道路再扩住宅';
      } else if (pollution > 35) {
        driver = '污染压低迁入';
        action = '把工业远离住宅并补公园';
      } else if (landUseConflictPressure > 30) {
        driver = '工业贴近住宅';
        action = '拉开工业距离或补公园缓冲';
      } else if (taxPressure > 0) {
        driver = '税率抑制迁入';
        action = '考虑降税恢复迁入';
      } else {
        driver = '迁入意愿上升';
        action = '继续沿路补住宅';
      }
    } else if (top.key === 'commercial') {
      if (stats.jobs < Math.floor(population * 0.35)) {
        driver = '就业岗位偏少';
        action = '在住宅旁规划商业区';
      } else if (landValue >= 55) {
        driver = '高地价带动客流';
        action = '贴近住宅和公园补商业';
      } else if (roadCoverage < 55) {
        driver = '道路客流不足';
        action = '先补道路接入商业区';
      } else if (congestion > 35) {
        driver = '拥堵压制客流';
        action = '升级瓶颈道路';
      } else {
        driver = '居民消费增长';
        action = '在住宅附近补商业区';
      }
    } else if (stats.jobs < Math.floor(population * 0.45)) {
      driver = '就业缺口';
      action = '远离住宅补工业区';
    } else if (landUseConflictPressure > 30) {
      driver = '用地冲突阻力';
      action = '把新工业放到住宅外侧';
    } else if (stats.industrialTiles === 0 && stats.residentialTiles > 0) {
      driver = '基础产业空白';
      action = '接路规划第一片工业区';
    } else if (roadCoverage < 55) {
      driver = '物流接入不足';
      action = '先铺道路接工业区';
    } else if (pollution > 45) {
      driver = '污染拖累扩张';
      action = '分散工业并补服务';
    } else {
      driver = '订单供应需要材料';
      action = '规划工业并启动生产';
    }

    return { residential, commercial, industrial, advice, focus: top.label, driver, action, urgency: top.value };
  }

  private getDemandAdvice(residential: number, commercial: number, industrial: number): string {
    const top = [
      { key: 'residential', value: residential },
      { key: 'commercial', value: commercial },
      { key: 'industrial', value: industrial },
    ].sort((a, b) => b.value - a.value)[0];

    if (top.value < 45) return '供需暂时稳定，优先补道路、服务和订单材料。';
    if (top.key === 'residential') return '住宅需求最高，沿道路补住宅区并保持服务覆盖。';
    if (top.key === 'commercial') return '商业需求最高，在住宅附近补商业区。';
    return '工业需求最高，远离住宅补工业区并保留道路容量。';
  }

  private clampPercent(value: number): number {
    return Math.round(Math.max(0, Math.min(100, value)));
  }

  private calculateGridStats(): GridStats {
    const stats: GridStats = {
      roads: 0,
      upgradedRoads: 0,
      roadCapacity: 0,
      zonedTiles: 0,
      developedZoneTiles: 0,
      vacantZoneTiles: 0,
      housingCapacity: 0,
      jobs: 0,
      pollution: 0,
      plannedResidentialTiles: 0,
      industrialTiles: 0,
      residentialTiles: 0,
      upgradedResidentialTiles: 0,
      serviceBuildings: 0,
      parkCoveredResidentialTiles: 0,
      healthCoveredResidentialTiles: 0,
      educationCoveredResidentialTiles: 0,
      landUseConflictPressure: 0,
      landUseConflictCount: 0,
    };
    const residentialTiles: Array<{ x: number; y: number }> = [];
    const industrialTiles: Array<{ x: number; y: number }> = [];
    const sensitiveTiles: Array<{ x: number; y: number; kind: '住宅' | '商业' | '服务' }> = [];
    const parkBuffers: Array<{ x: number; y: number }> = [];
    const serviceSources: Array<{ x: number; y: number; definition: ServiceBuildingDefinition }> = [];
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.roadId) {
          stats.roads++;
          stats.roadCapacity += ROAD_CAPACITY[tile.roadId] ?? 1;
          if (tile.roadId === 'arterial') stats.upgradedRoads++;
        }
        const service = SERVICE_BUILDINGS[tile.buildingId as ServiceBuildingId];
        if (service) {
          stats.serviceBuildings++;
          stats.jobs += service.jobs;
          stats.pollution += service.pollution;
          serviceSources.push({ x, y, definition: service });
          if (service.parkValue > 0) {
            parkBuffers.push({ x, y });
          } else {
            sensitiveTiles.push({ x, y, kind: '服务' });
          }
        }
        const zoneStats = ZONE_STATS[tile.zone];
        if (zoneStats) {
          stats.zonedTiles++;
          if (tile.zone === ZoneType.Residential) stats.plannedResidentialTiles++;
          if (!tile.buildingId) continue;

          stats.developedZoneTiles++;
          stats.pollution += zoneStats.pollution;
          if (tile.zone === ZoneType.Residential) {
            stats.housingCapacity += RESIDENTIAL_CAPACITY_BY_LEVEL[this.getResidentialLevel(tile)] ?? 0;
            stats.residentialTiles++;
            residentialTiles.push({ x, y });
            sensitiveTiles.push({ x, y, kind: '住宅' });
            if (this.getResidentialLevel(tile) > 1) stats.upgradedResidentialTiles++;
          } else {
            stats.housingCapacity += zoneStats.housing;
            stats.jobs += zoneStats.jobs;
            if (tile.zone === ZoneType.Commercial) sensitiveTiles.push({ x, y, kind: '商业' });
          }
          if (tile.zone === ZoneType.Industrial) {
            stats.industrialTiles++;
            industrialTiles.push({ x, y });
          }
        }
      }
    }
    for (const residential of residentialTiles) {
      if (this.isResidentialCoveredBy(residential, serviceSources, 'parkValue')) stats.parkCoveredResidentialTiles++;
      if (this.isResidentialCoveredBy(residential, serviceSources, 'healthValue')) stats.healthCoveredResidentialTiles++;
      if (this.isResidentialCoveredBy(residential, serviceSources, 'educationValue')) stats.educationCoveredResidentialTiles++;
    }
    stats.vacantZoneTiles = Math.max(0, stats.zonedTiles - stats.developedZoneTiles);
    const conflicts = this.analyzeLandUseConflicts(industrialTiles, sensitiveTiles, parkBuffers);
    stats.landUseConflictPressure = conflicts.pressure;
    stats.landUseConflictCount = conflicts.count;
    return stats;
  }

  private createFunctionalBufferAdvisor(stats: GridStats): FunctionalBufferAdvisor {
    const pressure = stats.landUseConflictPressure;
    const score = this.clampPercent(100 - pressure);
    if (stats.industrialTiles === 0) {
      return {
        score,
        pressure,
        conflictCount: 0,
        focus: '起步',
        driver: '尚未形成工业压力',
        action: stats.roads > 0 ? '把工业预留在住宅外侧' : '先铺路再规划分区',
      };
    }

    if (pressure <= 20) {
      return {
        score,
        pressure,
        conflictCount: stats.landUseConflictCount,
        focus: '良好',
        driver: '工业与敏感用地间距可控',
        action: '保持公园或道路作缓冲',
      };
    }

    const focus = pressure >= 55 ? '冲突' : '缓冲';
    return {
      score,
      pressure,
      conflictCount: stats.landUseConflictCount,
      focus,
      driver: `${stats.landUseConflictCount}处工业贴近住宅/服务`,
      action: pressure >= 55 ? '拆改贴近住宅的工业或补公园' : '新工业远离住宅并留公园缓冲',
    };
  }

  private createLandUseEfficiencyAdvisor(stats: GridStats, roadCoverage: number): LandUseEfficiencyAdvisor {
    const developedZoneRatio = stats.zonedTiles === 0
      ? 100
      : this.clampPercent((stats.developedZoneTiles / Math.max(1, stats.zonedTiles)) * 100);
    const vacancyShare = stats.zonedTiles === 0 ? 0 : stats.vacantZoneTiles / Math.max(1, stats.zonedTiles);
    const pressure = stats.zonedTiles < 4
      ? 0
      : this.clampPercent(vacancyShare * 115 + Math.max(0, stats.vacantZoneTiles - 4) * 7 - roadCoverage * 0.08);
    const score = this.clampPercent(100 - pressure);

    if (stats.zonedTiles === 0) {
      return {
        score,
        pressure,
        vacantZoneTiles: 0,
        developedZoneRatio,
        focus: '起步',
        driver: '尚未划分可开发片区',
        action: '先沿道路规划住宅',
      };
    }

    if (pressure <= 25) {
      return {
        score,
        pressure,
        vacantZoneTiles: stats.vacantZoneTiles,
        developedZoneRatio,
        focus: '紧凑',
        driver: `开发率${developedZoneRatio}%`,
        action: '按需求小步外扩',
      };
    }

    const action = roadCoverage < 55
      ? '补道路接入空置分区'
      : '暂缓外扩，等待空置分区开发';
    return {
      score,
      pressure,
      vacantZoneTiles: stats.vacantZoneTiles,
      developedZoneRatio,
      focus: stats.vacantZoneTiles >= 6 ? '空置' : '消化',
      driver: `${stats.vacantZoneTiles}块分区待开发/开发率${developedZoneRatio}%`,
      action,
    };
  }

  private analyzeLandUseConflicts(
    industrialTiles: Array<{ x: number; y: number }>,
    sensitiveTiles: Array<{ x: number; y: number; kind: '住宅' | '商业' | '服务' }>,
    parkBuffers: Array<{ x: number; y: number }>,
  ): { pressure: number; count: number } {
    let pressure = 0;
    let count = 0;
    for (const industrial of industrialTiles) {
      const nearest = sensitiveTiles
        .map((sensitive) => ({ ...sensitive, distance: this.manhattanDistance(industrial, sensitive) }))
        .filter((sensitive) => sensitive.distance <= 2)
        .sort((a, b) => a.distance - b.distance)[0];
      if (!nearest) continue;

      const base = nearest.kind === '商业' ? 24 : nearest.kind === '服务' ? 40 : 44;
      const distanceRelief = nearest.distance >= 2 ? 14 : 0;
      const parkRelief = parkBuffers.some((park) => this.manhattanDistance(park, industrial) <= 2 || this.manhattanDistance(park, nearest) <= 2)
        ? 12
        : 0;
      const conflict = Math.max(0, base - distanceRelief - parkRelief);
      if (conflict <= 0) continue;
      pressure += conflict;
      count++;
    }
    return { pressure: this.clampPercent(pressure), count };
  }

  private createAlerts(stats: GridStats): string[] {
    const alerts: string[] = [];
    if (stats.zonedTiles > 0 && stats.roads < Math.ceil(stats.zonedTiles / 4)) alerts.push('道路覆盖不足');
    if (this.metrics.congestion > 35) alerts.push('道路容量不足');
    if (stats.housingCapacity === 0) alerts.push('需要规划住宅区');
    if (stats.jobs < Math.floor(this.metrics.population * 0.35)) alerts.push('就业岗位偏少');
    if (this.metrics.pollution > 55) alerts.push('污染压力上升');
    if (this.metrics.landUseConflictPressure > 35) alerts.push('用地冲突偏高');
    if (this.metrics.landUseEfficiencyScore < 65 && this.metrics.vacantZoneTiles >= 4) alerts.push('空置分区过多');
    if (this.metrics.parkingPressure > 65) alerts.push('停车压力偏高');
    if (this.metrics.accidentRisk > 55) alerts.push('道路安全风险');
    if (this.metrics.floodRisk > 60) alerts.push('内涝风险上升');
    if (this.metrics.administrationUtilization > 90) alerts.push('行政容量满载');
    if (this.metrics.policyBacklog > 55) alerts.push('政策执行积压');
    if (this.metrics.cash < 5000) alerts.push('现金储备偏低');
    if (this.getStorageUsed() >= STORAGE_CAPACITY) alerts.push('仓库容量已满');
    if (stats.residentialTiles >= 2 && this.metrics.serviceGapPressure > 60) alerts.push('公共服务覆盖不足');
    const topDemand = [
      { label: '住宅', value: this.metrics.residentialDemand },
      { label: '商业', value: this.metrics.commercialDemand },
      { label: '工业', value: this.metrics.industrialDemand },
    ].sort((a, b) => b.value - a.value)[0];
    if (topDemand.value >= 75) alerts.push(`${topDemand.label}需求旺盛`);
    return alerts;
  }

  private createAlertDigest(alerts: string[]): string {
    if (alerts.length === 0) return '城市运行平稳';
    const ranked = [...alerts].sort((a, b) => this.alertPriority(b) - this.alertPriority(a));
    const visible = ranked.slice(0, 2);
    const hiddenCount = ranked.length - visible.length;
    return hiddenCount > 0 ? `${visible.join('、')} +${hiddenCount}` : visible.join('、');
  }

  private alertPriority(alert: string): number {
    if (alert.includes('现金')) return 100;
    if (alert.includes('污染')) return 88;
    if (alert.includes('用地冲突')) return 87;
    if (alert.includes('内涝')) return 86;
    if (alert.includes('空置分区')) return 84;
    if (alert.includes('道路容量') || alert.includes('拥堵')) return 82;
    if (alert.includes('行政')) return 81;
    if (alert.includes('政策')) return 80;
    if (alert.includes('公共服务')) return 78;
    if (alert.includes('安全')) return 76;
    if (alert.includes('停车')) return 74;
    if (alert.includes('仓库')) return 72;
    if (alert.includes('就业')) return 64;
    if (alert.includes('道路覆盖')) return 58;
    if (alert.includes('需要规划住宅')) return 54;
    if (alert.includes('需求旺盛')) return 46;
    return 10;
  }

  private estimateMonthlyCashFlow(stats: GridStats, pollution: number): number {
    return this.estimateMonthlyBudget(stats, pollution).net;
  }

  private estimateMonthlyBudget(stats: GridStats, pollution: number): MonthlyBudget {
    return this.estimateMonthlyBudgetForPolicies(stats, pollution, this.activePolicies);
  }

  private estimateMonthlyBudgetForPolicies(stats: GridStats, pollution: number, policies: CityPolicy[]): MonthlyBudget {
    const policyEffect = this.getPolicyEffect(policies);
    const policyBacklogCost = Math.round(this.calculateAdministration(stats, policies).policyBacklog * 1.4);
    const policyNet = policyEffect.monthlyNet - policyBacklogCost;
    const income = Math.floor(this.metrics.population * this.getTaxRatePercent() * 0.16 + stats.jobs * 3);
    const roadCost = stats.roads * 4;
    const zoningCost = stats.zonedTiles * 3;
    const populationCost = Math.floor(this.metrics.population * 0.6);
    const pollutionCost = Math.floor(pollution);
    const expenses = roadCost + zoningCost + populationCost + pollutionCost + Math.max(0, -policyNet);
    const totalIncome = income + Math.max(0, policyNet);
    return { income: totalIncome, roadCost, zoningCost, populationCost, pollutionCost, policyNet, policyBacklogCost, expenses, net: totalIncome - expenses };
  }

  private createBudgetBreakdownAdvisor(budget: MonthlyBudget): BudgetBreakdownAdvisor {
    const topExpense = [
      { focus: '道路维护', amount: budget.roadCost, action: '暂缓铺路，优先升级瓶颈' },
      { focus: '分区维护', amount: budget.zoningCost, action: '先消化已划分地块' },
      { focus: '人口服务', amount: budget.populationCost, action: '交付订单补现金缓冲' },
      { focus: '污染治理', amount: budget.pollutionCost, action: '分散工业并补公园' },
      { focus: '政策执行', amount: Math.max(0, -budget.policyNet), action: '关闭低优先级政策降低积压' },
    ].sort((a, b) => b.amount - a.amount)[0];

    const deficitRatio = budget.net < 0 ? Math.min(1, Math.abs(budget.net) / Math.max(1, budget.income)) : 0;
    const stress = this.metrics.cash < 0
      ? 100
      : budget.net < 0
        ? Math.round(55 + deficitRatio * 45)
        : this.metrics.cash < 5000
          ? 48
          : Math.round(Math.min(35, (budget.expenses / Math.max(1, budget.income)) * 20));

    if (stress < 35) {
      return {
        stress,
        focus: '稳定',
        driver: `月净现金+$${budget.net}`,
        action: '保持现金缓冲',
      };
    }

    return {
      stress,
      focus: topExpense.focus,
      driver: budget.net < 0
        ? `${topExpense.focus}支出$${topExpense.amount}，月净现金$${budget.net}`
        : `${topExpense.focus}是最大支出$${topExpense.amount}`,
      action: topExpense.action,
    };
  }

  private createEconomicSpecializationAdvisor(
    stats: GridStats,
    demand: DemandAnalysis,
    roadCoverage: number,
    congestion: number,
    pollution: number,
    landValue: number,
  ): EconomicSpecializationAdvisor {
    const population = this.metrics.population;
    const storageUsed = this.getStorageUsed();
    const storageLoad = storageUsed / STORAGE_CAPACITY;
    const targetJobs = Math.floor(population * 0.45);
    const jobGap = Math.max(0, targetJobs - stats.jobs);
    const orderMomentum = Math.min(22, this.orders.length * 5 + this.completedOrders * 3);
    const productionMomentum = Math.min(18, this.productionQueue.length * 6 + storageUsed * 0.8);
    const foundationScore = stats.roads === 0
      ? 72
      : stats.housingCapacity === 0
        ? 68
        : stats.zonedTiles === 0
          ? 58
          : roadCoverage < 45
            ? Math.round(62 - roadCoverage * 0.35)
            : 0;
    const industrialScore = this.clampPercent(
      demand.industrial * 0.5
      + Math.min(28, jobGap * 1.15)
      + (stats.industrialTiles === 0 && stats.residentialTiles > 0 ? 18 : 0)
      + Math.min(12, roadCoverage * 0.12)
      + productionMomentum * 0.4
      - pollution * 0.2,
    );
    const commercialScore = this.clampPercent(
      demand.commercial * 0.52
      + Math.min(24, population * 0.22)
      + landValue * 0.18
      + roadCoverage * 0.08
      - congestion * 0.22,
    );
    const logisticsScore = this.clampPercent(
      orderMomentum
      + productionMomentum
      + storageLoad * 35
      + Math.min(18, stats.industrialTiles * 8)
      + (demand.industrial >= 55 ? 10 : 0)
      - (roadCoverage < 45 ? 14 : 0),
    );

    const candidates = [
      {
        score: foundationScore,
        focus: '增长底盘',
        driver: stats.roads === 0
          ? '尚无道路骨架'
          : stats.housingCapacity === 0
            ? '尚无可入住住宅容量'
            : `道路覆盖${Math.round(roadCoverage)}%`,
        action: stats.roads === 0
          ? '先铺第一段道路'
          : stats.housingCapacity === 0
            ? '接路规划住宅片区'
            : '补道路接入分区',
      },
      {
        score: industrialScore,
        focus: '资源工业',
        driver: `工业需求${demand.industrial} 岗位缺口${jobGap}`,
        action: pollution > 50
          ? '分散工业并补公园'
          : roadCoverage < 55
            ? '补道路接工业区'
            : '远离住宅扩工业并排产材料',
      },
      {
        score: commercialScore,
        focus: '邻里商业',
        driver: `商业需求${demand.commercial} 地价${Math.round(landValue)}`,
        action: congestion > 35 ? '升级商业动线瓶颈' : '在住宅旁补商业区',
      },
      {
        score: logisticsScore,
        focus: '订单物流',
        driver: `订单${this.orders.length} 仓库${storageUsed}/${STORAGE_CAPACITY}`,
        action: storageUsed >= STORAGE_CAPACITY ? '交付订单释放仓库' : '按订单排产并优先交付',
      },
    ].sort((a, b) => b.score - a.score)[0];

    if (candidates.score < 35) {
      return {
        score: Math.round(candidates.score),
        focus: '均衡',
        driver: '住商工供需暂无明显倾向',
        action: '按需求补片区并交付订单',
      };
    }

    return {
      score: Math.round(Math.min(100, candidates.score)),
      focus: candidates.focus,
      driver: candidates.driver,
      action: candidates.action,
    };
  }

  private createGrowthBottleneckAdvisor(
    stats: GridStats,
    demand: DemandAnalysis,
    forecast: RiskForecast,
    budgetAdvisor: BudgetBreakdownAdvisor,
    economicAdvisor: EconomicSpecializationAdvisor,
    serviceAdvisor: ServiceGapAdvisor,
    roadAdvisor: RoadHierarchyAdvisor,
    commuteAdvisor: CommuteCorridorAdvisor,
    housingAdvisor: HousingAffordabilityAdvisor,
    upgradeAdvisor: BuildingUpgradeReadinessAdvisor,
    bufferAdvisor: FunctionalBufferAdvisor,
    landUseAdvisor: LandUseEfficiencyAdvisor,
  ): GrowthBottleneckAdvisor {
    const storageUsed = this.getStorageUsed();
    const targetJobs = Math.floor(this.metrics.population * 0.45);
    const jobGap = Math.max(0, targetJobs - stats.jobs);
    const foundationScore = stats.roads === 0
      ? 76
      : stats.housingCapacity === 0
        ? 78
        : stats.developedZoneTiles === 0 && stats.zonedTiles > 0
          ? 56
          : 0;
    const employmentScore = targetJobs === 0 ? 0 : Math.min(100, (jobGap / Math.max(1, targetJobs)) * 100);
    const storageScore = storageUsed >= STORAGE_CAPACITY ? 82 : Math.round((storageUsed / STORAGE_CAPACITY) * 35);
    const mobilityScore = Math.max(roadAdvisor.pressure, commuteAdvisor.score);
    const mobilityAdvisor = roadAdvisor.pressure >= commuteAdvisor.score ? roadAdvisor : commuteAdvisor;

    const candidates = [
      {
        score: foundationScore,
        focus: '起步底盘',
        driver: stats.roads === 0
          ? '城市缺少第一段道路'
          : stats.housingCapacity === 0
            ? '尚无可入住住宅容量'
            : '分区已规划但尚未开发',
        action: stats.roads === 0
          ? '先铺第一段道路'
          : stats.housingCapacity === 0
            ? '接路规划住宅片区'
            : '保持接路等待自然开发',
      },
      {
        score: forecast.risk,
        focus: `${forecast.focus}风险`,
        driver: `${forecast.focus}风险${forecast.risk}`,
        action: forecast.action,
      },
      {
        score: budgetAdvisor.stress,
        focus: '财政',
        driver: budgetAdvisor.driver,
        action: budgetAdvisor.action,
      },
      {
        score: housingAdvisor.score,
        focus: '住房',
        driver: housingAdvisor.driver,
        action: housingAdvisor.action,
      },
      {
        score: mobilityScore,
        focus: roadAdvisor.pressure >= commuteAdvisor.score ? '路网' : '通勤',
        driver: mobilityAdvisor.driver,
        action: mobilityAdvisor.action,
      },
      {
        score: stats.residentialTiles >= 2 ? serviceAdvisor.score : 0,
        focus: '服务',
        driver: serviceAdvisor.driver,
        action: serviceAdvisor.action,
      },
      {
        score: upgradeAdvisor.readyCount > 0 || upgradeAdvisor.blockedCount > 0 ? upgradeAdvisor.score : 0,
        focus: '升级',
        driver: upgradeAdvisor.driver,
        action: upgradeAdvisor.action,
      },
      {
        score: Math.max(economicAdvisor.score, Math.round(employmentScore)),
        focus: '经济',
        driver: jobGap > 0 ? `岗位缺口${jobGap}` : economicAdvisor.driver,
        action: jobGap > 0 ? '补商业或工业岗位' : economicAdvisor.action,
      },
      {
        score: bufferAdvisor.pressure,
        focus: '缓冲',
        driver: bufferAdvisor.driver,
        action: bufferAdvisor.action,
      },
      {
        score: landUseAdvisor.pressure,
        focus: '用地',
        driver: landUseAdvisor.driver,
        action: landUseAdvisor.action,
      },
      {
        score: storageScore,
        focus: '供应链',
        driver: `仓库${storageUsed}/${STORAGE_CAPACITY}`,
        action: storageUsed >= STORAGE_CAPACITY ? '交付订单释放仓库' : '按订单排产补材料',
      },
      {
        score: demand.urgency >= 75 ? demand.urgency : 0,
        focus: '需求',
        driver: `${demand.focus}需求${demand.urgency}`,
        action: demand.action,
      },
    ].sort((a, b) => b.score - a.score)[0];

    if (candidates.score < 35) {
      return {
        score: Math.round(candidates.score),
        focus: '顺畅',
        driver: '暂无明确成长卡点',
        action: '按目标扩建并保留现金',
      };
    }

    return {
      score: Math.round(Math.min(100, candidates.score)),
      focus: candidates.focus,
      driver: candidates.driver,
      action: candidates.action,
    };
  }

  private createDistrictPriorityAdvisor(
    stats: GridStats,
    demand: DemandAnalysis,
    budgetAdvisor: BudgetBreakdownAdvisor,
    serviceAdvisor: ServiceGapAdvisor,
    roadAdvisor: RoadHierarchyAdvisor,
    commuteAdvisor: CommuteCorridorAdvisor,
    housingAdvisor: HousingAffordabilityAdvisor,
    upgradeAdvisor: BuildingUpgradeReadinessAdvisor,
    bufferAdvisor: FunctionalBufferAdvisor,
    landUseAdvisor: LandUseEfficiencyAdvisor,
  ): DistrictPriorityAdvisor {
    const housingPressure = stats.housingCapacity === 0
      ? stats.roads > 0 || stats.zonedTiles > 0 ? 72 : 36
      : Math.max(this.metrics.rentPressure, demand.residential >= 75 ? demand.residential : 0);
    const storagePressure = this.getStorageUsed() >= STORAGE_CAPACITY ? 70 : 0;
    const demandPressure = demand.urgency >= 75 ? demand.urgency : 0;
    const environmentPressure = this.metrics.pollution >= 45 ? this.metrics.pollution : 0;

    const candidates = [
      {
        score: budgetAdvisor.stress,
        focus: '财政',
        driver: budgetAdvisor.driver,
        action: budgetAdvisor.action,
      },
      {
        score: roadAdvisor.pressure,
        focus: '交通',
        driver: roadAdvisor.driver,
        action: roadAdvisor.action,
      },
      {
        score: commuteAdvisor.score,
        focus: '通勤',
        driver: commuteAdvisor.driver,
        action: commuteAdvisor.action,
      },
      {
        score: housingAdvisor.score,
        focus: '住房',
        driver: housingAdvisor.driver,
        action: housingAdvisor.action,
      },
      {
        score: upgradeAdvisor.score,
        focus: '升级',
        driver: upgradeAdvisor.driver,
        action: upgradeAdvisor.action,
      },
      {
        score: stats.residentialTiles >= 2 ? serviceAdvisor.score : 0,
        focus: '服务',
        driver: serviceAdvisor.driver,
        action: serviceAdvisor.action,
      },
      {
        score: Math.round(housingPressure),
        focus: '住房',
        driver: stats.housingCapacity === 0 ? '尚无可入住住宅容量' : `居住压力${Math.round(this.metrics.rentPressure)}`,
        action: demand.focus === '住宅' ? demand.action : '补住宅容量并保持服务覆盖',
      },
      {
        score: Math.round(environmentPressure),
        focus: '环境',
        driver: `污染${Math.round(this.metrics.pollution)}`,
        action: '分散工业并补公园',
      },
      {
        score: bufferAdvisor.pressure,
        focus: '缓冲',
        driver: bufferAdvisor.driver,
        action: bufferAdvisor.action,
      },
      {
        score: landUseAdvisor.pressure,
        focus: '用地',
        driver: landUseAdvisor.driver,
        action: landUseAdvisor.action,
      },
      {
        score: Math.round(demandPressure),
        focus: demand.focus,
        driver: `${demand.focus}需求${demand.urgency}`,
        action: demand.action,
      },
      {
        score: storagePressure,
        focus: '供应',
        driver: '仓库容量已满',
        action: '交付订单或升级住宅',
      },
    ].sort((a, b) => b.score - a.score)[0];

    if (candidates.score < 35) {
      return {
        score: Math.round(candidates.score),
        focus: '均衡',
        driver: '暂无高优先级片区压力',
        action: '按当前目标稳步扩建',
      };
    }

    return {
      score: Math.round(Math.min(100, candidates.score)),
      focus: candidates.focus,
      driver: candidates.driver,
      action: candidates.action,
    };
  }

  private createBuildingUpgradeReadinessAdvisor(stats: GridStats): BuildingUpgradeReadinessAdvisor {
    let readyCount = 0;
    let blockedCount = 0;
    let maxedCount = 0;
    let undevelopedCount = 0;
    let accessBlocked = 0;
    let unlockBlocked = 0;
    let materialBlocked = 0;
    let firstMissingMaterials = '';
    let firstLockedLevel = 0;

    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile || tile.zone !== ZoneType.Residential) continue;

        const currentLevel = this.getResidentialLevel(tile);
        if (currentLevel <= 0) {
          undevelopedCount++;
          continue;
        }
        if (currentLevel >= MAX_RESIDENTIAL_LEVEL) {
          maxedCount++;
          continue;
        }

        const nextLevel = currentLevel + 1;
        const unlockLevel = RESIDENTIAL_UPGRADE_UNLOCK_LEVELS[nextLevel] ?? 1;
        const cost = RESIDENTIAL_UPGRADE_COSTS[nextLevel];
        const hasRoadAccess = Boolean(tile.roadId) || this.hasAdjacentRoad(x, y);

        if (!hasRoadAccess) {
          accessBlocked++;
          blockedCount++;
        } else if (!this.isLevelUnlocked(unlockLevel)) {
          unlockBlocked++;
          blockedCount++;
          if (firstLockedLevel === 0) firstLockedLevel = unlockLevel;
        } else if (!this.hasMaterials(cost)) {
          materialBlocked++;
          blockedCount++;
          if (!firstMissingMaterials && cost) firstMissingMaterials = this.formatMissingMaterials(cost);
        } else {
          readyCount++;
        }
      }
    }

    if (readyCount > 0) {
      return {
        score: Math.min(100, 68 + readyCount * 8),
        readyCount,
        blockedCount,
        focus: '可升级',
        driver: `${readyCount}处住宅材料已齐`,
        action: '选中住宅点升级住宅',
      };
    }

    if (blockedCount > 0) {
      const blockers = [
        { count: materialBlocked, focus: '材料', driver: firstMissingMaterials ? `缺${firstMissingMaterials}` : '升级材料不足', action: firstMissingMaterials ? `排产${firstMissingMaterials}` : '排产升级材料' },
        { count: unlockBlocked, focus: '等级', driver: firstLockedLevel > 0 ? `Lv${firstLockedLevel}解锁下一次升级` : '城市等级不足', action: '完成目标提升城市等级' },
        { count: accessBlocked, focus: '接入', driver: `${accessBlocked}处住宅缺少道路`, action: '补道路接入住宅' },
      ].sort((a, b) => b.count - a.count)[0];
      return {
        score: Math.min(100, 52 + blockedCount * 8),
        readyCount,
        blockedCount,
        focus: blockers.focus,
        driver: blockers.driver,
        action: blockers.action,
      };
    }

    if (undevelopedCount > 0) {
      return {
        score: 32,
        readyCount,
        blockedCount,
        focus: '等待',
        driver: `${undevelopedCount}块住宅待自然开发`,
        action: '保持接路并等待入住',
      };
    }

    if (maxedCount > 0) {
      return {
        score: 12,
        readyCount,
        blockedCount,
        focus: '满级',
        driver: '现有住宅已达当前等级上限',
        action: '继续扩建新住宅片区',
      };
    }

    return {
      score: stats.roads > 0 ? 24 : 0,
      readyCount,
      blockedCount,
      focus: '起步',
      driver: '暂无可升级住宅',
      action: stats.roads > 0 ? '沿道路规划住宅区' : '先铺道路再规划住宅',
    };
  }

  private createHousingAffordabilityAdvisor(
    stats: GridStats,
    demand: DemandAnalysis,
    rentPressure: number,
    serviceCoverage: number,
    roadCoverage: number,
    landValue: number,
    taxPressure: number,
    commuteAdvisor: CommuteCorridorAdvisor,
  ): HousingAffordabilityAdvisor {
    const targetHousing = Math.max(72, Math.ceil(this.metrics.population * 1.15 + stats.jobs * 0.55 + 48));
    const housingGap = Math.max(0, targetHousing - stats.housingCapacity);
    const capacityPressure = stats.housingCapacity === 0
      ? stats.roads > 0 || stats.zonedTiles > 0 ? 78 : 42
      : Math.min(100, (housingGap / Math.max(1, targetHousing)) * 85 + (demand.residential >= 75 ? 15 : 0));
    const affordabilityPressure = Math.max(
      rentPressure,
      landValue >= 70 ? landValue - 25 : 0,
      taxPressure > 0 ? 35 + taxPressure * 5 : 0,
    );
    const servicePressure = stats.residentialTiles >= 2 ? Math.max(0, 65 - serviceCoverage) : 0;
    const accessPressure = stats.zonedTiles > 0 ? Math.max(0, 55 - roadCoverage) : 0;

    const candidates = [
      {
        score: Math.round(capacityPressure),
        focus: '容量',
        driver: stats.housingCapacity === 0 ? '尚无可入住住宅容量' : `住房缺口${housingGap}`,
        action: stats.roads > 0 ? demand.focus === '住宅' ? demand.action : '沿道路补住宅区' : '先铺路再规划住宅',
      },
      {
        score: Math.round(affordabilityPressure),
        focus: '负担',
        driver: `租压${Math.round(rentPressure)} 地价${Math.round(landValue)}`,
        action: taxPressure > 0 ? '降低税率缓和迁入压力' : '补住宅容量并保留服务',
      },
      {
        score: Math.round(servicePressure),
        focus: '宜居',
        driver: `服务覆盖${Math.round(serviceCoverage)}%`,
        action: '补公园、诊所或学校',
      },
      {
        score: Math.round(accessPressure),
        focus: '接入',
        driver: `道路覆盖${Math.round(roadCoverage)}%`,
        action: stats.roads > 0 ? '补道路接入住宅区' : '先铺第一段道路',
      },
      {
        score: Math.round(commuteAdvisor.score * 0.7),
        focus: '通勤',
        driver: commuteAdvisor.driver,
        action: commuteAdvisor.action,
      },
    ].sort((a, b) => b.score - a.score)[0];

    if (candidates.score < 35) {
      return {
        score: Math.round(candidates.score),
        focus: '可负担',
        driver: '住房供给与迁入压力可控',
        action: '随需求补住宅片区',
      };
    }

    return {
      score: Math.round(Math.min(100, candidates.score)),
      focus: candidates.focus,
      driver: candidates.driver,
      action: candidates.action,
    };
  }

  private createCommuteCorridorAdvisor(
    stats: GridStats,
    roadCoverage: number,
    congestion: number,
    demand: DemandAnalysis,
    roadAdvisor: RoadHierarchyAdvisor,
  ): CommuteCorridorAdvisor {
    const targetJobs = Math.floor(this.metrics.population * 0.45);
    const jobGap = Math.max(0, targetJobs - stats.jobs);
    const jobBalancePressure = targetJobs === 0 ? 0 : Math.min(100, (jobGap / Math.max(1, targetJobs)) * 100);
    const accessPressure = stats.zonedTiles === 0 ? stats.roads === 0 ? 20 : 0 : Math.max(0, 70 - roadCoverage);
    const homesWithoutJobsPressure = stats.residentialTiles > 0 && stats.jobs === 0 ? 64 : 0;
    const jobsWithoutHomesPressure = stats.jobs > 0 && stats.housingCapacity === 0 ? 62 : 0;

    const candidates = [
      {
        score: Math.round(congestion),
        focus: '瓶颈',
        driver: `拥堵${Math.round(congestion)}`,
        action: roadAdvisor.action,
      },
      {
        score: Math.round(accessPressure),
        focus: '接入',
        driver: `道路覆盖${Math.round(roadCoverage)}%`,
        action: stats.roads > 0 ? '补道路接入分区' : '先铺第一段道路',
      },
      {
        score: Math.round(Math.max(jobBalancePressure, homesWithoutJobsPressure)),
        focus: '住岗',
        driver: jobGap > 0 ? `岗位缺口${jobGap}` : '住宅片区缺少岗位',
        action: demand.focus === '商业' || demand.focus === '工业' ? demand.action : '在住宅旁补商业或远端工业',
      },
      {
        score: jobsWithoutHomesPressure,
        focus: '迁入',
        driver: '岗位已有但住宅不足',
        action: demand.focus === '住宅' ? demand.action : '补住宅并保持接路',
      },
      {
        score: roadAdvisor.focus === '稳定' ? 0 : Math.round(roadAdvisor.pressure * 0.85),
        focus: '路网',
        driver: roadAdvisor.driver,
        action: roadAdvisor.action,
      },
    ].sort((a, b) => b.score - a.score)[0];

    if (candidates.score < 35) {
      return {
        score: Math.round(candidates.score),
        focus: '顺畅',
        driver: '住岗与道路压力可控',
        action: '继续沿主路扩新区',
      };
    }

    return {
      score: Math.round(Math.min(100, candidates.score)),
      focus: candidates.focus,
      driver: candidates.driver,
      action: candidates.action,
    };
  }

  private createRiskForecast(stats: GridStats, monthlyCashFlow: number): RiskForecast {
    const cashRunwayDays = monthlyCashFlow < 0
      ? Math.max(0, Math.min(999, Math.floor((Math.max(0, this.metrics.cash) / Math.max(1, -monthlyCashFlow)) * 30)))
      : 999;
    const candidates = [
      {
        risk: this.metrics.cash < 0 ? 100 : monthlyCashFlow < 0 ? Math.max(55, 100 - cashRunwayDays) : this.metrics.cash < 5000 ? 52 : 0,
        focus: '财政',
        action: monthlyCashFlow < 0 ? '交付订单并暂缓扩建' : '保留现金缓冲',
      },
      {
        risk: this.metrics.congestion,
        focus: '交通',
        action: this.metrics.congestion > 35 ? '升级瓶颈道路' : '保持道路容量',
      },
      {
        risk: stats.residentialTiles >= 2 ? this.metrics.serviceGapPressure : 0,
        focus: '服务',
        action: '补公园、诊所或学校',
      },
      {
        risk: this.metrics.pollution,
        focus: '环境',
        action: '分散工业并补公园',
      },
      {
        risk: this.metrics.landUseConflictPressure,
        focus: '缓冲',
        action: this.metrics.functionalBufferAction,
      },
      {
        risk: 100 - this.metrics.landUseEfficiencyScore,
        focus: '用地',
        action: this.metrics.landUseEfficiencyAction,
      },
      {
        risk: this.metrics.policyBacklog,
        focus: '政策',
        action: '关闭低优先级政策或提升城市等级',
      },
      {
        risk: this.metrics.floodRisk,
        focus: '雨洪',
        action: '启用绿色规范或补公园',
      },
      {
        risk: this.metrics.accidentRisk,
        focus: '安全',
        action: '启用交通安全或完整街道',
      },
      {
        risk: this.getStorageUsed() >= STORAGE_CAPACITY ? 70 : 0,
        focus: '仓库',
        action: '交付订单或升级住宅',
      },
    ].sort((a, b) => b.risk - a.risk)[0];

    if (candidates.risk < 35) {
      return { risk: Math.round(candidates.risk), focus: '稳定', action: '继续扩建并保留现金缓冲', cashRunwayDays };
    }
    return {
      risk: Math.round(Math.min(100, candidates.risk)),
      focus: candidates.focus,
      action: candidates.action,
      cashRunwayDays,
    };
  }

  private createServiceGapAdvisor(
    stats: GridStats,
    parkCoverage: number,
    healthCoverage: number,
    educationCoverage: number,
  ): ServiceGapAdvisor {
    if (stats.residentialTiles === 0) {
      return {
        score: 0,
        focus: '均衡',
        driver: '暂无住宅服务压力',
        action: stats.roads > 0 ? '沿道路规划住宅区' : '先铺道路再规划住宅',
      };
    }

    const focus = [
      { label: '公园', coverage: parkCoverage, serviceId: 'community_park' as ServiceBuildingId, action: '补公园' },
      { label: '医疗', coverage: healthCoverage, serviceId: 'community_clinic' as ServiceBuildingId, action: '补诊所' },
      { label: '教育', coverage: educationCoverage, serviceId: 'community_school' as ServiceBuildingId, action: '补学校' },
    ].sort((a, b) => a.coverage - b.coverage)[0];

    const score = Math.round(Math.max(0, 100 - focus.coverage));
    if (focus.coverage >= 70) {
      return {
        score,
        focus: '均衡',
        driver: '主要服务已覆盖',
        action: '继续观察新住宅片区',
      };
    }

    const service = SERVICE_BUILDINGS[focus.serviceId];
    const action = this.isLevelUnlocked(service.unlockLevel)
      ? stats.roads > 0 ? focus.action : '先铺道路，服务建筑要临路'
      : `升到 Lv${service.unlockLevel} 解锁${service.label}`;
    return {
      score,
      focus: focus.label,
      driver: `${focus.label}覆盖仅${Math.round(focus.coverage)}%`,
      action,
    };
  }

  private createRoadHierarchyAdvisor(stats: GridStats, roadCoverage: number, congestion: number): RoadHierarchyAdvisor {
    if (stats.roads === 0) {
      return {
        pressure: stats.zonedTiles > 0 ? 72 : 20,
        focus: '接入',
        driver: stats.zonedTiles > 0 ? '分区尚未接入道路' : '道路尚未形成骨架',
        action: '先铺第一段道路',
      };
    }

    if (stats.zonedTiles > 0 && roadCoverage < 55) {
      return {
        pressure: Math.round(Math.max(45, 100 - roadCoverage)),
        focus: '接入',
        driver: `道路覆盖仅${Math.round(roadCoverage)}%`,
        action: '补道路接入分区',
      };
    }

    if (congestion > 35) {
      return {
        pressure: Math.round(congestion),
        focus: '瓶颈',
        driver: `拥堵${Math.round(congestion)}`,
        action: this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL) ? '升级瓶颈道路' : `升到 Lv${ROAD_UPGRADE_UNLOCK_LEVEL} 解锁主干道`,
      };
    }

    if (stats.developedZoneTiles >= 3 && stats.upgradedRoads === 0) {
      return {
        pressure: 58,
        focus: '主干',
        driver: '缺少主干道骨架',
        action: this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL) ? '选择普通道路升级' : `升到 Lv${ROAD_UPGRADE_UNLOCK_LEVEL} 解锁主干道`,
      };
    }

    const arterialShare = stats.roads === 0 ? 0 : stats.upgradedRoads / stats.roads;
    if (stats.roads >= 8 && arterialShare < 0.2) {
      return {
        pressure: 46,
        focus: '层级',
        driver: '主干道占比偏低',
        action: '把核心路段升级为主干道',
      };
    }

    return {
      pressure: Math.round(Math.min(30, Math.max(0, congestion))),
      focus: '稳定',
      driver: '道路容量可控',
      action: '继续按新区补道路',
    };
  }

  private isResidentialCoveredBy(
    residential: { x: number; y: number },
    services: Array<{ x: number; y: number; definition: ServiceBuildingDefinition }>,
    field: 'parkValue' | 'healthValue' | 'educationValue',
  ): boolean {
    return services.some((service) => {
      if (service.definition[field] <= 0) return false;
      return Math.abs(residential.x - service.x) + Math.abs(residential.y - service.y) <= service.definition.radius;
    });
  }

  private getInspectionBuildingLabel(zone: ZoneType, buildingId: string): string {
    if (!buildingId) return zone === ZoneType.None ? '无' : '待开发';
    const service = SERVICE_BUILDINGS[buildingId as ServiceBuildingId];
    if (service) return service.label;
    const residentialLevel = this.getResidentialLevel({ zone, buildingId });
    if (residentialLevel > 0) return `住宅 ${residentialLevel} 级`;
    if (buildingId === 'commercial_l1') return '商业建筑';
    if (buildingId === 'industrial_l1') return '工业建筑';
    return buildingId;
  }

  private getTileBufferRisk(x: number, y: number): number {
    const tile = this.grid.getTile(x, y);
    if (!tile?.buildingId) return 0;
    const service = SERVICE_BUILDINGS[tile.buildingId as ServiceBuildingId];
    const selfKind = this.sensitiveKindForTile(tile.zone, service);
    const isIndustrial = tile.zone === ZoneType.Industrial;
    if (!isIndustrial && !selfKind) return 0;

    let nearest: { x: number; y: number; kind: '住宅' | '商业' | '服务'; distance: number } | null = null;
    for (let ty = 0; ty < this.grid.height; ty++) {
      for (let tx = 0; tx < this.grid.width; tx++) {
        if (tx === x && ty === y) continue;
        const other = this.grid.getTile(tx, ty);
        if (!other?.buildingId) continue;
        const otherService = SERVICE_BUILDINGS[other.buildingId as ServiceBuildingId];
        const otherKind = this.sensitiveKindForTile(other.zone, otherService);
        const matches = isIndustrial ? otherKind : other.zone === ZoneType.Industrial;
        if (!matches) continue;
        const distance = this.manhattanDistance({ x, y }, { x: tx, y: ty });
        if (distance > 2) continue;
        const kind = isIndustrial ? otherKind! : selfKind!;
        if (!nearest || distance < nearest.distance) nearest = { x: tx, y: ty, kind, distance };
      }
    }
    if (!nearest) return 0;

    const base = nearest.kind === '商业' ? 24 : nearest.kind === '服务' ? 40 : 44;
    const distanceRelief = nearest.distance >= 2 ? 14 : 0;
    const parkRelief = this.hasParkBufferNear({ x, y }, nearest) ? 12 : 0;
    return this.clampPercent(base - distanceRelief - parkRelief);
  }

  private sensitiveKindForTile(zone: ZoneType, service?: ServiceBuildingDefinition): '住宅' | '商业' | '服务' | null {
    if (zone === ZoneType.Residential) return '住宅';
    if (zone === ZoneType.Commercial) return '商业';
    if (service && service.parkValue <= 0) return '服务';
    return null;
  }

  private hasParkBufferNear(a: { x: number; y: number }, b: { x: number; y: number }): boolean {
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        const service = tile?.buildingId ? SERVICE_BUILDINGS[tile.buildingId as ServiceBuildingId] : null;
        if (!service || service.parkValue <= 0) continue;
        const park = { x, y };
        if (this.manhattanDistance(park, a) <= 2 || this.manhattanDistance(park, b) <= 2) return true;
      }
    }
    return false;
  }

  private getTileOverlaySummary(x: number, y: number): { label: string; value: string } {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { label: '图层', value: '未知' };
    if (tile.roadId) {
      return { label: '交通', value: `${this.getRoadLabel(tile.roadId)} 容量${ROAD_CAPACITY[tile.roadId] ?? 1}` };
    }

    const service = SERVICE_BUILDINGS[tile.buildingId as ServiceBuildingId];
    if (service) {
      const effects = [
        service.parkValue > 0 ? '公园' : '',
        service.healthValue > 0 ? '医疗' : '',
        service.educationValue > 0 ? '教育' : '',
      ].filter(Boolean).join('/');
      return { label: '服务', value: `${effects || '公共'} 半径${service.radius}` };
    }

    if (tile.terrain !== TerrainType.Plain) return { label: '地形', value: INSPECTION_TERRAIN_LABELS[tile.terrain] };
    const zoneStats = ZONE_STATS[tile.zone];
    if (!zoneStats) {
      return { label: '规划', value: this.hasAdjacentRoad(x, y) ? '临路空地' : '需接道路' };
    }
    if (!tile.buildingId) {
      return { label: '开发', value: this.hasAdjacentRoad(x, y) ? `${zoneStats.label}待开发` : `${zoneStats.label}未接路` };
    }
    if (tile.zone === ZoneType.Residential) {
      const level = this.getResidentialLevel(tile);
      const bufferRisk = this.getTileBufferRisk(x, y);
      return { label: '住房', value: `Lv${level} 容量${RESIDENTIAL_CAPACITY_BY_LEVEL[level] ?? 0}${bufferRisk > 0 ? ` 缓冲${bufferRisk}` : ''}` };
    }
    if (tile.zone === ZoneType.Industrial) {
      const bufferRisk = this.getTileBufferRisk(x, y);
      return { label: '就业', value: `${zoneStats.jobs}岗位 污染${zoneStats.pollution}${bufferRisk > 0 ? ` 缓冲${bufferRisk}` : ''}` };
    }
    return { label: '就业', value: `${zoneStats.jobs}岗位 污染${zoneStats.pollution}` };
  }

  private getTileDiagnosis(x: number, y: number): string {
    const tile = this.grid.getTile(x, y);
    if (!tile) return '地块不在地图内';
    if (tile.terrain === TerrainType.Water) return '水域暂时不能规划，保留作自然边界';
    if (tile.terrain === TerrainType.Hill) return '丘陵暂时不能规划，适合作为远期资源或景观边界';
    if (tile.roadId) {
      return tile.roadId === 'arterial'
        ? '主干道容量高，适合承接新区骨架'
        : this.isLevelUnlocked(ROAD_UPGRADE_UNLOCK_LEVEL) ? '普通道路可升级为主干道缓解瓶颈' : `升到 Lv${ROAD_UPGRADE_UNLOCK_LEVEL} 后可升级主干道`;
    }

    const service = SERVICE_BUILDINGS[tile.buildingId as ServiceBuildingId];
    if (service) return `${service.label}覆盖周边住宅，半径${service.radius}`;

    const hasRoadAccess = this.hasAdjacentRoad(x, y);
    if (tile.zone === ZoneType.None) return hasRoadAccess ? '临路空地，可规划分区或服务建筑' : '未接路空地，先铺道路打开开发';
    if (!hasRoadAccess) return `${INSPECTION_ZONE_LABELS[tile.zone]}未接路，无法自然开发`;
    if (!tile.buildingId) return `${INSPECTION_ZONE_LABELS[tile.zone]}已接路，当前需求${this.getDemandForZone(tile.zone)}`;

    if (tile.zone === ZoneType.Residential) {
      const level = this.getResidentialLevel(tile);
      const bufferRisk = this.getTileBufferRisk(x, y);
      if (bufferRisk > 35) return '住宅贴近工业，建议用道路或公园拉开缓冲';
      if (level <= 0) return '住宅分区等待自然入住';
      if (level >= MAX_RESIDENTIAL_LEVEL) return '住宅已达当前最高等级，继续补新住宅片区';
      const nextLevel = level + 1;
      const cost = RESIDENTIAL_UPGRADE_COSTS[nextLevel];
      return this.hasMaterials(cost) ? `住宅可升级到 ${nextLevel} 级` : `住宅升级需${this.formatMissingMaterials(cost)}`;
    }

    if (tile.zone === ZoneType.Commercial) return '商业提供岗位，靠近住宅与道路客流更稳';
    if (tile.zone === ZoneType.Industrial) {
      const bufferRisk = this.getTileBufferRisk(x, y);
      return bufferRisk > 35 ? '工业贴近住宅或服务，建议迁到边缘或补公园缓冲' : '工业提供岗位和材料基础，注意污染远离住宅';
    }
    return '保持接路并观察服务覆盖';
  }

  private getDemandForZone(zone: ZoneType): number {
    switch (zone) {
      case ZoneType.Residential: return this.metrics.residentialDemand;
      case ZoneType.Commercial: return this.metrics.commercialDemand;
      case ZoneType.Industrial: return this.metrics.industrialDemand;
      default: return 0;
    }
  }

  private processPopulation(): void {
    if (this.metrics.housingCapacity <= 0) {
      this.metrics.population = Math.max(0, this.metrics.population - Math.ceil(this.metrics.population * 0.03));
    } else if (this.metrics.population < this.metrics.housingCapacity) {
      this.metrics.population += Math.max(1, Math.floor((this.metrics.housingCapacity - this.metrics.population) * 0.08));
    } else if (this.metrics.population > this.metrics.housingCapacity) {
      this.metrics.population -= Math.max(1, Math.ceil((this.metrics.population - this.metrics.housingCapacity) * 0.04));
    }
  }

  private processEconomy(): void {
    const stats = this.calculateGridStats();
    this.metrics.cash += this.estimateMonthlyCashFlow(stats, this.metrics.pollution);
    if (this.metrics.cash < 0) this.metrics.cash -= Math.max(0, this.metrics.cash + 500);
  }

  private getTaxRatePercent(): number {
    if (this.taxLevel === CityTaxLevel.High) return 12;
    if (this.taxLevel === CityTaxLevel.Low) return 6;
    return 9;
  }

  private isTaxLevel(value: unknown): value is CityTaxLevel {
    return value === CityTaxLevel.Low || value === CityTaxLevel.Normal || value === CityTaxLevel.High;
  }

  private isCityPolicy(value: unknown): value is CityPolicy {
    return POLICY_ORDER.includes(value as CityPolicy);
  }

  private taxLevelFromRate(rate: number | undefined): CityTaxLevel {
    if (rate === 6) return CityTaxLevel.Low;
    if (rate === 12) return CityTaxLevel.High;
    return CityTaxLevel.Normal;
  }

  private ensureOrders(): void {
    while (this.orders.length < 3) {
      const template = ORDER_TEMPLATES[(this.nextOrderId - 1) % ORDER_TEMPLATES.length];
      this.orders.push({
        id: `order-${this.nextOrderId++}`,
        title: template.title,
        required: { ...template.required },
        rewardCash: template.rewardCash,
      });
    }
  }

  private hasMaterials(cost: MaterialCost | undefined): boolean {
    if (!cost) return false;
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .every(([materialId, required]) => this.materials[materialId] >= required);
  }

  private consumeMaterials(cost: MaterialCost): void {
    for (const [materialId, required] of Object.entries(cost) as Array<[MaterialId, number]>) {
      this.materials[materialId] -= required;
    }
  }

  private formatMaterialCost(cost: MaterialCost): string {
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .map(([materialId, count]) => `${MATERIAL_LABELS[materialId]}x${count}`)
      .join('、');
  }

  private hasAdjacentRoad(x: number, y: number): boolean {
    const offsets = [[0, -1], [1, 0], [0, 1], [-1, 0]];
    return offsets.some(([dx, dy]) => Boolean(this.grid.getTile(x + dx, y + dy)?.roadId));
  }

  private manhattanDistance(a: { x: number; y: number }, b: { x: number; y: number }): number {
    return Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
  }
}

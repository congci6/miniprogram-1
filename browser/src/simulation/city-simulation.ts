import { CityGrid } from './grid';
import {
  CityMaterialInventory,
  CityMetrics,
  CityObjective,
  CityOrder,
  CityPolicy,
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
  housingCapacity: number;
  jobs: number;
  pollution: number;
  industrialTiles: number;
  residentialTiles: number;
  upgradedResidentialTiles: number;
  serviceBuildings: number;
  parkCoveredResidentialTiles: number;
  healthCoveredResidentialTiles: number;
  educationCoveredResidentialTiles: number;
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
    isMet: (_simulation, stats) => stats.residentialTiles >= 2,
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
];

const ZONE_COST = 120;
const ROAD_COST = 180;
const ROAD_UPGRADE_COST = 360;
const ERASE_COST = 20;
const STORAGE_CAPACITY = 30;
const MAX_RESIDENTIAL_LEVEL = 3;
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
      healthCoverage: 0, educationCoverage: 0, safetyCoverage: 0,
      securityCoverage: 0, parkCoverage: 0, transitCoverage: 0,
      roadCoverage: 0, serviceGapPressure: 0, landValue: 30,
      rentPressure: 0, housingCapacity: 0, buildingCount: 0,
      unlockedBuildingIds: ['community_park'],
      alerts: [],
    };
  }

  tick(deltaSeconds: number): void {
    this.dayAccumulator += deltaSeconds;
    while (this.dayAccumulator >= 1) {
      this.dayAccumulator -= 1;
      this.metrics.day++;
      this.processProductionDay();
      this.computeMetrics();
      this.processPopulation();
      this.processEconomy();
      if (this.evaluateObjectives().length > 0) this.computeMetrics();
    }
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
      return { changed: true, message: this.appendObjectiveRewards(`修建道路 -$${ROAD_COST}`, ACTION_EXPERIENCE.road) };
    }

    if (tool === 'erase') {
      if (!tile.roadId && tile.zone === ZoneType.None && !tile.buildingId) {
        return { changed: false, message: '这个地块已经是空地' };
      }
      if (!this.trySpend(ERASE_COST)) return { changed: false, message: '现金不足，无法清理地块' };
      this.grid.clearPlanning(x, y);
      this.computeMetrics();
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
    return { changed: true, message: this.appendObjectiveRewards(`${order.title}交付 +$${order.rewardCash}`, ACTION_EXPERIENCE.order) };
  }

  upgradeResidentialAt(x: number, y: number): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    if (tile.zone !== ZoneType.Residential) return { changed: false, message: '请选择住宅区升级' };
    if (!tile.roadId && !this.hasAdjacentRoad(x, y)) return { changed: false, message: '住宅升级需要临近道路' };

    const currentLevel = this.getResidentialLevel(tile);
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
    const match = /^residential_l([2-3])$/.exec(tile.buildingId);
    return match ? Number(match[1]) : 1;
  }

  getServiceBuildingLabel(buildingId: string): string {
    return SERVICE_LABELS[buildingId as ServiceBuildingId] ?? '';
  }

  getRoadLabel(roadId: string): string {
    return ROAD_LABELS[roadId] ?? (roadId ? roadId : '无');
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
        unlockedBuildingIds: [...this.metrics.unlockedBuildingIds],
      },
      materials: { ...this.materials },
      productionQueue: this.productionQueue.map((job) => ({ ...job })),
      orders: this.orders.map((order) => ({ ...order, required: { ...order.required } })),
      completedOrders: this.completedOrders,
      completedObjectiveIds: [...this.completedObjectiveIds],
      nextProductionId: this.nextProductionId,
      nextOrderId: this.nextOrderId,
      tiles,
    };
  }

  restoreSnapshot(snapshot: CitySimulationSaveData, nowMs = Date.now()): CityOfflineProgressResult {
    const offlineResult = this.createEmptyOfflineResult();
    if (snapshot.version !== 1 && snapshot.version !== 2 && snapshot.version !== 3) return offlineResult;

    Object.assign(this.metrics, snapshot.metrics);
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
    return { changed: true, message: `税率调整为 ${this.getTaxRatePercent()}%` };
  }

  private trySpend(amount: number): boolean {
    if (this.metrics.cash < amount) return false;
    this.metrics.cash -= amount;
    return true;
  }

  private processProductionDay(): void {
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
    }
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
        return `再规划 ${Math.max(0, 2 - stats.residentialTiles)} 块住宅地。`;
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
    const roadCoverage = stats.zonedTiles === 0 ? 0 : Math.min(100, (stats.roadCapacity / stats.zonedTiles) * 80);
    const congestion = stats.zonedTiles === 0 ? 0 : Math.max(0, Math.min(100, stats.zonedTiles * 5 - stats.roadCapacity * 8));
    const pollution = Math.max(0, Math.min(100, stats.pollution));
    const parkCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.parkCoveredResidentialTiles / stats.residentialTiles) * 100);
    const healthCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.healthCoveredResidentialTiles / stats.residentialTiles) * 100);
    const educationCoverage = stats.residentialTiles === 0 ? 0 : Math.min(100, (stats.educationCoveredResidentialTiles / stats.residentialTiles) * 100);
    const serviceCoverage = (parkCoverage + healthCoverage + educationCoverage) / 3;
    const serviceGapPressure = stats.residentialTiles === 0 ? 0 : Math.max(0, 100 - serviceCoverage);
    const rentPressure = stats.housingCapacity === 0
      ? 0
      : Math.max(0, Math.min(100, (this.metrics.population / stats.housingCapacity) * 100 - 75));

    this.metrics.housingCapacity = stats.housingCapacity;
    this.metrics.buildingCount = stats.zonedTiles + stats.roads + stats.serviceBuildings;
    this.metrics.roadCoverage = roadCoverage;
    this.metrics.congestion = congestion;
    this.metrics.pollution = pollution;
    this.metrics.parkCoverage = parkCoverage;
    this.metrics.healthCoverage = healthCoverage;
    this.metrics.educationCoverage = educationCoverage;
    this.metrics.serviceGapPressure = serviceGapPressure;
    this.metrics.rentPressure = rentPressure;
    this.metrics.taxLevel = this.taxLevel;
    this.metrics.taxRatePercent = this.getTaxRatePercent();
    this.metrics.landValue = Math.max(10, Math.min(100, 35 + roadCoverage * 0.22 + parkCoverage * 0.12 - pollution * 0.2 - congestion * 0.15));
    this.metrics.happiness = Math.round(Math.max(5, Math.min(100, 50 + roadCoverage * 0.18 + serviceCoverage * 0.18 - pollution * 0.22 - rentPressure * 0.2)));
    this.metrics.cityScore = Math.round(Math.max(1, Math.min(100, 42 + this.metrics.happiness * 0.35 + roadCoverage * 0.18 + serviceCoverage * 0.12 - pollution * 0.2)));
    this.refreshCityLevelProgress();
    this.metrics.alerts = this.createAlerts(stats);
  }

  private calculateGridStats(): GridStats {
    const stats: GridStats = {
      roads: 0,
      upgradedRoads: 0,
      roadCapacity: 0,
      zonedTiles: 0,
      housingCapacity: 0,
      jobs: 0,
      pollution: 0,
      industrialTiles: 0,
      residentialTiles: 0,
      upgradedResidentialTiles: 0,
      serviceBuildings: 0,
      parkCoveredResidentialTiles: 0,
      healthCoveredResidentialTiles: 0,
      educationCoveredResidentialTiles: 0,
    };
    const residentialTiles: Array<{ x: number; y: number }> = [];
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
        }
        const zoneStats = ZONE_STATS[tile.zone];
        if (zoneStats) {
          stats.zonedTiles++;
          stats.housingCapacity += tile.zone === ZoneType.Residential
            ? RESIDENTIAL_CAPACITY_BY_LEVEL[this.getResidentialLevel(tile)]
            : zoneStats.housing;
          stats.jobs += zoneStats.jobs;
          stats.pollution += zoneStats.pollution;
          if (tile.zone === ZoneType.Industrial) stats.industrialTiles++;
          if (tile.zone === ZoneType.Residential) {
            stats.residentialTiles++;
            residentialTiles.push({ x, y });
            if (this.getResidentialLevel(tile) > 1) stats.upgradedResidentialTiles++;
          }
        }
      }
    }
    for (const residential of residentialTiles) {
      if (this.isResidentialCoveredBy(residential, serviceSources, 'parkValue')) stats.parkCoveredResidentialTiles++;
      if (this.isResidentialCoveredBy(residential, serviceSources, 'healthValue')) stats.healthCoveredResidentialTiles++;
      if (this.isResidentialCoveredBy(residential, serviceSources, 'educationValue')) stats.educationCoveredResidentialTiles++;
    }
    return stats;
  }

  private createAlerts(stats: GridStats): string[] {
    const alerts: string[] = [];
    if (stats.zonedTiles > 0 && stats.roads < Math.ceil(stats.zonedTiles / 4)) alerts.push('道路覆盖不足');
    if (this.metrics.congestion > 35) alerts.push('道路容量不足');
    if (stats.housingCapacity === 0) alerts.push('需要规划住宅区');
    if (stats.jobs < Math.floor(this.metrics.population * 0.35)) alerts.push('就业岗位偏少');
    if (this.metrics.pollution > 55) alerts.push('污染压力上升');
    if (this.metrics.cash < 5000) alerts.push('现金储备偏低');
    if (this.getStorageUsed() >= STORAGE_CAPACITY) alerts.push('仓库容量已满');
    if (stats.residentialTiles >= 2 && this.metrics.serviceGapPressure > 60) alerts.push('公共服务覆盖不足');
    return alerts;
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
    const income = Math.floor(this.metrics.population * this.getTaxRatePercent() * 0.16 + stats.jobs * 3);
    const expenses = Math.floor(stats.roads * 4 + stats.zonedTiles * 3 + this.metrics.population * 0.6 + this.metrics.pollution);
    this.metrics.cash += income - expenses;
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
}

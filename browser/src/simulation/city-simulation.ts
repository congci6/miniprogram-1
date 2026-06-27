import { CityGrid } from './grid';
import {
  CityMaterialInventory,
  CityMetrics,
  CityOrder,
  CityPolicy,
  CityTaxLevel,
  MaterialCost,
  MaterialId,
  PlanningTool,
  ProductionJob,
  TerrainType,
  ZoneType,
} from '@/types/index';

interface GridStats {
  roads: number;
  zonedTiles: number;
  housingCapacity: number;
  jobs: number;
  pollution: number;
  industrialTiles: number;
}

export interface PlanningActionResult {
  changed: boolean;
  message: string;
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

export interface CitySimulationSnapshot {
  version: 2;
  metrics: CityMetrics;
  materials: CityMaterialInventory;
  productionQueue: ProductionJob[];
  orders: CityOrder[];
  completedOrders: number;
  nextProductionId: number;
  nextOrderId: number;
  tiles: TileSnapshot[];
}

export type CitySimulationSaveData = CitySimulationLegacySnapshot | CitySimulationSnapshot;

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

const PRODUCTION_RECIPES: Record<MaterialId, { label: string; days: number; cashCost: number }> = {
  wood: { label: '木材', days: 2, cashCost: 20 },
  metal: { label: '金属', days: 3, cashCost: 35 },
  plastic: { label: '塑料', days: 4, cashCost: 55 },
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

const ZONE_COST = 120;
const ROAD_COST = 180;
const ERASE_COST = 20;
const STORAGE_CAPACITY = 30;
const MAX_RESIDENTIAL_LEVEL = 3;

export class CitySimulation {
  readonly grid: CityGrid;
  metrics: CityMetrics;
  readonly materials: CityMaterialInventory = { wood: 0, metal: 0, plastic: 0 };
  readonly productionQueue: ProductionJob[] = [];
  readonly orders: CityOrder[] = [];
  completedOrders = 0;
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
      cityScore: 50, cityLevelName: '新生街区',
      taxRatePercent: 9, congestion: 0, pollution: 0, crime: 0,
      healthCoverage: 0, educationCoverage: 0, safetyCoverage: 0,
      securityCoverage: 0, parkCoverage: 0, transitCoverage: 0,
      roadCoverage: 0, serviceGapPressure: 0, landValue: 30,
      rentPressure: 0, housingCapacity: 0, buildingCount: 0,
      unlockedBuildingIds: ['community_park', 'community_clinic', 'community_school'],
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
    }
  }

  applyTool(x: number, y: number, tool: PlanningTool): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    if (tile.terrain === TerrainType.Water) return { changed: false, message: '水域暂时不能规划' };

    if (tool === 'inspect') {
      return { changed: false, message: `查看地块 (${x}, ${y})` };
    }

    if (tool === 'road') {
      if (tile.roadId) return { changed: false, message: '这里已经有道路' };
      if (!this.trySpend(ROAD_COST)) return { changed: false, message: '现金不足，无法修建道路' };
      this.grid.setRoad(x, y, 'local');
      this.computeMetrics();
      return { changed: true, message: `修建道路 -$${ROAD_COST}` };
    }

    if (tool === 'erase') {
      if (!tile.roadId && tile.zone === ZoneType.None && !tile.buildingId) {
        return { changed: false, message: '这个地块已经是空地' };
      }
      if (!this.trySpend(ERASE_COST)) return { changed: false, message: '现金不足，无法清理地块' };
      this.grid.clearPlanning(x, y);
      this.computeMetrics();
      return { changed: true, message: `清理地块 -$${ERASE_COST}` };
    }

    const zone = this.zoneFromTool(tool);
    const stats = ZONE_STATS[zone];
    if (!stats) return { changed: false, message: '暂不支持这个规划工具' };
    if (tile.zone === zone) return { changed: false, message: `这里已经是${stats.label}` };
    if (!this.trySpend(ZONE_COST)) return { changed: false, message: '现金不足，无法划定新区' };

    this.grid.setZone(x, y, zone);
    this.computeMetrics();
    return { changed: true, message: `划定${stats.label} -$${ZONE_COST}` };
  }

  startProduction(materialId: MaterialId): PlanningActionResult {
    const recipe = PRODUCTION_RECIPES[materialId];
    if (!recipe) return { changed: false, message: '未知生产配方' };
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
    return { changed: true, message: `${recipe.label}已排产 -$${recipe.cashCost}` };
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
    return { changed: true, message: `${order.title}交付 +$${order.rewardCash}` };
  }

  upgradeResidentialAt(x: number, y: number): PlanningActionResult {
    const tile = this.grid.getTile(x, y);
    if (!tile) return { changed: false, message: '地块不在地图内' };
    if (tile.zone !== ZoneType.Residential) return { changed: false, message: '请选择住宅区升级' };
    if (!tile.roadId && !this.hasAdjacentRoad(x, y)) return { changed: false, message: '住宅升级需要临近道路' };

    const currentLevel = this.getResidentialLevel(tile);
    if (currentLevel >= MAX_RESIDENTIAL_LEVEL) return { changed: false, message: '住宅已达到当前最高等级' };

    const nextLevel = currentLevel + 1;
    const cost = RESIDENTIAL_UPGRADE_COSTS[nextLevel];
    if (!this.hasMaterials(cost)) return { changed: false, message: `升级需要 ${this.formatMaterialCost(cost)}` };

    this.consumeMaterials(cost);
    this.grid.setBuilding(x, y, `residential_l${nextLevel}`);
    this.metrics.cash += 220 * nextLevel;
    this.computeMetrics();
    return { changed: true, message: `住宅升级到 ${nextLevel} 级 +$${220 * nextLevel}` };
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

  createSnapshot(): CitySimulationSnapshot {
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
      version: 2,
      metrics: {
        ...this.metrics,
        alerts: [...this.metrics.alerts],
        unlockedBuildingIds: [...this.metrics.unlockedBuildingIds],
      },
      materials: { ...this.materials },
      productionQueue: this.productionQueue.map((job) => ({ ...job })),
      orders: this.orders.map((order) => ({ ...order, required: { ...order.required } })),
      completedOrders: this.completedOrders,
      nextProductionId: this.nextProductionId,
      nextOrderId: this.nextOrderId,
      tiles,
    };
  }

  restoreSnapshot(snapshot: CitySimulationSaveData): void {
    if (snapshot.version !== 1 && snapshot.version !== 2) return;

    Object.assign(this.metrics, snapshot.metrics);
    if (snapshot.version === 2) {
      this.materials.wood = Math.max(0, snapshot.materials.wood ?? 0);
      this.materials.metal = Math.max(0, snapshot.materials.metal ?? 0);
      this.materials.plastic = Math.max(0, snapshot.materials.plastic ?? 0);
      this.productionQueue.splice(0, this.productionQueue.length, ...snapshot.productionQueue.map((job) => ({ ...job })));
      this.orders.splice(0, this.orders.length, ...snapshot.orders.map((order) => ({ ...order, required: { ...order.required } })));
      this.completedOrders = Math.max(0, snapshot.completedOrders);
      this.nextProductionId = Math.max(1, snapshot.nextProductionId);
      this.nextOrderId = Math.max(1, snapshot.nextOrderId);
    } else {
      this.materials.wood = 0;
      this.materials.metal = 0;
      this.materials.plastic = 0;
      this.productionQueue.splice(0, this.productionQueue.length);
      this.orders.splice(0, this.orders.length);
      this.completedOrders = 0;
      this.nextProductionId = 1;
      this.nextOrderId = 1;
    }

    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) this.grid.clearPlanning(x, y);
    }

    for (const tile of snapshot.tiles) {
      this.grid.setZone(tile.x, tile.y, tile.zone);
      if (tile.roadId) this.grid.setRoad(tile.x, tile.y, tile.roadId);
      if (tile.buildingId) this.grid.setBuilding(tile.x, tile.y, tile.buildingId);
    }

    this.ensureOrders();
    this.computeMetrics();
  }

  getTaxRevenue(): number {
    const rate = this.getTaxRatePercent();
    return Math.floor(this.metrics.population * rate * 0.16);
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

  private zoneFromTool(tool: PlanningTool): ZoneType {
    switch (tool) {
      case 'residential': return ZoneType.Residential;
      case 'commercial': return ZoneType.Commercial;
      case 'industrial': return ZoneType.Industrial;
      default: return ZoneType.None;
    }
  }

  private computeMetrics(): void {
    const stats = this.calculateGridStats();
    const roadCoverage = stats.zonedTiles === 0 ? 0 : Math.min(100, (stats.roads / stats.zonedTiles) * 120);
    const congestion = stats.zonedTiles === 0 ? 0 : Math.max(0, Math.min(100, stats.zonedTiles * 4 - stats.roads * 9));
    const pollution = Math.min(100, stats.pollution);
    const rentPressure = stats.housingCapacity === 0
      ? 0
      : Math.max(0, Math.min(100, (this.metrics.population / stats.housingCapacity) * 100 - 75));

    this.metrics.housingCapacity = stats.housingCapacity;
    this.metrics.buildingCount = stats.zonedTiles + stats.roads;
    this.metrics.roadCoverage = roadCoverage;
    this.metrics.congestion = congestion;
    this.metrics.pollution = pollution;
    this.metrics.rentPressure = rentPressure;
    this.metrics.taxRatePercent = this.getTaxRatePercent();
    this.metrics.landValue = Math.max(10, Math.min(100, 35 + roadCoverage * 0.25 - pollution * 0.2 - congestion * 0.15));
    this.metrics.happiness = Math.round(Math.max(5, Math.min(100, 55 + roadCoverage * 0.2 - pollution * 0.25 - rentPressure * 0.2)));
    this.metrics.cityScore = Math.round(Math.max(1, Math.min(100, 45 + this.metrics.happiness * 0.35 + roadCoverage * 0.2 - pollution * 0.2)));
    this.metrics.cityLevelName = this.metrics.population >= 2500 ? '繁荣城区'
      : this.metrics.population >= 800 ? '成长街区'
        : '新生街区';
    this.metrics.alerts = this.createAlerts(stats);
  }

  private calculateGridStats(): GridStats {
    const stats: GridStats = { roads: 0, zonedTiles: 0, housingCapacity: 0, jobs: 0, pollution: 0, industrialTiles: 0 };
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.roadId) stats.roads++;
        const zoneStats = ZONE_STATS[tile.zone];
        if (zoneStats) {
          stats.zonedTiles++;
          stats.housingCapacity += tile.zone === ZoneType.Residential
            ? RESIDENTIAL_CAPACITY_BY_LEVEL[this.getResidentialLevel(tile)]
            : zoneStats.housing;
          stats.jobs += zoneStats.jobs;
          stats.pollution += zoneStats.pollution;
          if (tile.zone === ZoneType.Industrial) stats.industrialTiles++;
        }
      }
    }
    return stats;
  }

  private createAlerts(stats: GridStats): string[] {
    const alerts: string[] = [];
    if (stats.zonedTiles > 0 && stats.roads < Math.ceil(stats.zonedTiles / 4)) alerts.push('道路覆盖不足');
    if (stats.housingCapacity === 0) alerts.push('需要规划住宅区');
    if (stats.jobs < Math.floor(this.metrics.population * 0.35)) alerts.push('就业岗位偏少');
    if (this.metrics.pollution > 55) alerts.push('污染压力上升');
    if (this.metrics.cash < 5000) alerts.push('现金储备偏低');
    if (this.getStorageUsed() >= STORAGE_CAPACITY) alerts.push('仓库容量已满');
    return alerts;
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

import { CityGrid } from './grid';
import { CityMetrics, CityPolicy, CityTaxLevel, PlanningTool, TerrainType, ZoneType } from '@/types/index';

interface GridStats {
  roads: number;
  zonedTiles: number;
  housingCapacity: number;
  jobs: number;
  pollution: number;
}

export interface PlanningActionResult {
  changed: boolean;
  message: string;
}

const ZONE_STATS: Partial<Record<ZoneType, { housing: number; jobs: number; pollution: number; label: string }>> = {
  [ZoneType.Residential]: { housing: 24, jobs: 0, pollution: 1, label: '住宅区' },
  [ZoneType.Commercial]: { housing: 0, jobs: 18, pollution: 2, label: '商业区' },
  [ZoneType.Industrial]: { housing: 0, jobs: 28, pollution: 7, label: '工业区' },
};

const ZONE_COST = 120;
const ROAD_COST = 180;
const ERASE_COST = 20;

export class CitySimulation {
  readonly grid: CityGrid;
  metrics: CityMetrics;
  private dayAccumulator = 0;
  private taxLevel: CityTaxLevel = CityTaxLevel.Normal;
  private activePolicies: CityPolicy[] = [];

  constructor(w: number, h: number) {
    this.grid = new CityGrid(w, h);
    this.metrics = this.createInitialMetrics();
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

  getTaxRevenue(): number {
    const rate = this.getTaxRatePercent();
    return Math.floor(this.metrics.population * rate * 0.16);
  }

  private trySpend(amount: number): boolean {
    if (this.metrics.cash < amount) return false;
    this.metrics.cash -= amount;
    return true;
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
    const stats: GridStats = { roads: 0, zonedTiles: 0, housingCapacity: 0, jobs: 0, pollution: 0 };
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const tile = this.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.roadId) stats.roads++;
        const zoneStats = ZONE_STATS[tile.zone];
        if (zoneStats) {
          stats.zonedTiles++;
          stats.housingCapacity += zoneStats.housing;
          stats.jobs += zoneStats.jobs;
          stats.pollution += zoneStats.pollution;
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
}

import { CityGrid } from './grid';
import { CityMetrics, CityTaxLevel, CityPolicy, ZoneType } from '@/types/index';

export class CitySimulation {
  readonly grid: CityGrid;
  metrics: CityMetrics;
  private dayAccumulator = 0;
  private taxLevel: CityTaxLevel = CityTaxLevel.Normal;
  private activePolicies: CityPolicy[] = [];

  constructor(w: number, h: number) {
    this.grid = new CityGrid(w, h);
    this.metrics = this.createInitialMetrics();
  }

  private createInitialMetrics(): CityMetrics {
    return {
      day: 1, population: 0, cash: 50000, happiness: 50,
      cityScore: 50, cityLevelName: '\u65b0\u751f\u8857\u533a',
      taxRatePercent: 9, congestion: 0, pollution: 0, crime: 0,
      healthCoverage: 0, educationCoverage: 0, safetyCoverage: 0,
      securityCoverage: 0, parkCoverage: 0, transitCoverage: 0,
      roadCoverage: 0, serviceGapPressure: 0, landValue: 30,
      rentPressure: 0, housingCapacity: 0, buildingCount: 0,
      unlockedBuildingIds: ['community_park','community_clinic','community_school'],
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

  private computeMetrics(): void {
    let roads = 0, buildings = 0;
    for (let y = 0; y < this.grid.height; y++) {
      for (let x = 0; x < this.grid.width; x++) {
        const t = this.grid.getTile(x, y);
        if (!t) continue;
        if (t.roadId) roads++;
        if (t.buildingId) buildings++;
      }
    }
    this.metrics.roadCoverage = Math.min(100, roads / (this.grid.width * this.grid.height) * 100);
    this.metrics.buildingCount = buildings;
  }

  private processPopulation(): void {
    if (this.metrics.housingCapacity > 0 && this.metrics.population < this.metrics.housingCapacity) {
      this.metrics.population += Math.max(1, Math.floor((this.metrics.housingCapacity - this.metrics.population) * 0.05));
    }
  }

  private processEconomy(): void {
    const rate = this.taxLevel === CityTaxLevel.High ? 12 : this.taxLevel === CityTaxLevel.Low ? 6 : 9;
    const income = Math.floor(this.metrics.population * rate);
    const expenses = Math.floor(this.metrics.buildingCount * 5 + this.metrics.population * 2);
    this.metrics.cash += income - expenses;
    if (this.metrics.cash < 0) this.metrics.cash -= Math.max(0, this.metrics.cash + 500);
  }

  getTaxRevenue(): number {
    return Math.floor(this.metrics.population * (this.taxLevel === CityTaxLevel.High ? 12 : 6));
  }
}

import { BALANCE } from '../data/balance';
import { BUILDINGS } from '../data/buildings';
import { getBuildingConfig } from '../data/buildings';
import { ROAD } from '../data/roads';
import { CityGrid } from '../map/grid';
import { manhattanLine, nearestRoadId } from '../map/placement';
import { cloneRoad, roadKey } from '../map/road-graph';
import { zoneForBuildingCategory } from '../map/zoning';
import type {
  CityMetrics,
  CommandResult,
  GameCommand,
  GridPos,
  PlacedBuilding,
  RoadNode,
  SerializedCityState,
} from '../types';
import { defaultDemand, defaultObjective, evaluateCity } from './city-evaluation';
import { recomputeCityServices } from './services';
import { buildingUnlockStatus } from './unlocks';

export class CityState {
  readonly grid: CityGrid;
  metrics: CityMetrics;
  elapsedSeconds: number;
  taxRate: number;
  private nextId: number;
  private readonly buildings = new Map<string, PlacedBuilding>();
  private readonly roads = new Map<string, RoadNode>();

  constructor(grid: CityGrid, metrics: CityMetrics, elapsedSeconds = 0, taxRate = BALANCE.defaultTaxRate, nextId = 1) {
    this.grid = grid;
    this.metrics = metrics;
    this.elapsedSeconds = elapsedSeconds;
    this.taxRate = taxRate;
    this.nextId = nextId;
  }

  static createNew(width = BALANCE.mapWidth, height = BALANCE.mapHeight): CityState {
    const city = new CityState(new CityGrid(width, height), {
      population: BALANCE.startingPopulation,
      cash: BALANCE.initialCash,
      happiness: BALANCE.initialHappiness,
      housingCapacity: 0,
      jobs: 0,
      powerSupply: 0,
      powerDemand: 0,
      waterSupply: 0,
      waterDemand: 0,
      congestion: 0,
      pollution: 0,
      serviceCoverage: 0,
      demand: defaultDemand(),
      cityScore: 50,
      cityLevelName: '新生街区',
      alerts: [],
      roadTiles: 0,
      buildingCount: 0,
      connectedBuildings: 0,
      disconnectedBuildings: 0,
      unlockedBuildingIds: [],
      taxRate: BALANCE.defaultTaxRate,
      activeObjective: defaultObjective(),
    });

    city.seedStartingRoad();
    city.ensureStarterBuildings();
    city.recomputeMetrics();
    return city;
  }

  static deserialize(serialized: SerializedCityState): CityState {
    const city = new CityState(
      CityGrid.fromSerialized(serialized),
      normalizeCityMetrics(serialized.metrics),
      serialized.elapsedSeconds,
      serialized.taxRate,
      serialized.nextId,
    );

    for (const building of serialized.buildings) {
      city.buildings.set(building.id, { ...building, pos: { ...building.pos }, size: { ...building.size } });
    }
    for (const road of serialized.roads) {
      city.roads.set(road.id, cloneRoad(road));
    }

    return city;
  }

  execute(command: GameCommand): CommandResult {
    switch (command.type) {
      case 'BUILD_ROAD':
        return this.buildRoad(command.from, command.to);
      case 'PLACE_BUILDING':
        return this.placeBuilding(command.buildingId, command.pos);
      case 'DEMOLISH':
        return this.demolish(command.pos);
      case 'SET_ZONE':
        this.grid.setZone(command.area, command.zone);
        return { ok: true, message: '分区已更新' };
      default:
        return { ok: false, message: '未知命令' };
    }
  }

  getBuildings(): PlacedBuilding[] {
    return Array.from(this.buildings.values()).map((building) => ({
      ...building,
      pos: { ...building.pos },
      size: { ...building.size },
    }));
  }

  getRoads(): RoadNode[] {
    return Array.from(this.roads.values()).map(cloneRoad);
  }

  getRoadById(id: string): RoadNode | undefined {
    const road = this.roads.get(id);
    return road ? cloneRoad(road) : undefined;
  }

  mutateRoadLoads(loads: Map<string, number>): void {
    for (const road of this.roads.values()) {
      road.load = loads.get(road.id) ?? 0;
    }
  }

  recomputeMetrics(): void {
    this.refreshBuildingRoadConnections();
    const buildings = this.getBuildings();
    const connectedBuildings = buildings.filter((building) => Boolean(building.connectedRoadId)).length;
    this.metrics = {
      ...this.metrics,
      ...recomputeCityServices(buildings),
      roadTiles: this.roads.size,
      buildingCount: buildings.length,
      connectedBuildings,
      disconnectedBuildings: buildings.length - connectedBuildings,
    };
    this.metrics = {
      ...this.metrics,
      taxRate: this.taxRate,
      ...evaluateCity(this.metrics, buildings, this.taxRate),
    };
    this.refreshBuildingUnlocks();
  }

  ensureStarterBuildings(): void {
    if (this.buildings.size > 0) {
      return;
    }

    const centerX = Math.floor(this.grid.width / 2);
    const centerY = Math.floor(this.grid.height / 2);
    const starters: Array<{ configId: string; pos: GridPos }> = [
      { configId: 'residential_pod', pos: { x: centerX - 5, y: centerY - 4 } },
      { configId: 'residential_pod', pos: { x: centerX - 2, y: centerY - 4 } },
      { configId: 'market_corner', pos: { x: centerX + 2, y: centerY - 4 } },
      { configId: 'maker_yard', pos: { x: centerX + 6, y: centerY - 5 } },
      { configId: 'micro_power', pos: { x: centerX - 8, y: centerY + 3 } },
      { configId: 'water_tower', pos: { x: centerX + 4, y: centerY + 3 } },
    ];

    for (const starter of starters) {
      this.seedStarterBuilding(starter.configId, starter.pos);
    }
    this.recomputeMetrics();
  }

  serialize(): SerializedCityState {
    return {
      width: this.grid.width,
      height: this.grid.height,
      tiles: this.grid.serializeTiles(),
      buildings: this.getBuildings(),
      roads: this.getRoads(),
      metrics: { ...this.metrics, unlockedBuildingIds: [...this.metrics.unlockedBuildingIds] },
      elapsedSeconds: this.elapsedSeconds,
      taxRate: this.taxRate,
      nextId: this.nextId,
    };
  }

  private buildRoad(from: GridPos, to: GridPos): CommandResult {
    const points = manhattanLine(from, to).filter((pos, index, all) => {
      return all.findIndex((other) => other.x === pos.x && other.y === pos.y) === index;
    });

    for (const pos of points) {
      if (!this.grid.inBounds(pos)) {
        return { ok: false, message: '道路超出地图边界' };
      }
      const tile = this.grid.getTile(pos);
      if (!tile.roadId && !this.grid.canPlaceRoad(pos)) {
        return { ok: false, message: '道路不能穿过水面或建筑' };
      }
    }

    const newPoints = points.filter((pos) => !this.grid.getTile(pos).roadId);
    const cost = newPoints.length * ROAD.cost;
    if (cost > this.metrics.cash) {
      return { ok: false, message: '现金不足，无法铺路', cost };
    }

    for (const pos of newPoints) {
      this.addRoadTile(pos);
    }
    this.metrics.cash -= cost;
    this.recomputeMetrics();
    return { ok: true, message: `铺设道路 ${newPoints.length} 格`, cost };
  }

  private placeBuilding(configId: string, pos: GridPos): CommandResult {
    const config = getBuildingConfig(configId);
    const unlock = buildingUnlockStatus(config, this.metrics);
    if (!unlock.unlocked) {
      return { ok: false, message: `${config.name}未解锁，${unlock.reason}`, cost: config.cost };
    }

    if (config.cost > this.metrics.cash) {
      return { ok: false, message: '现金不足，无法建造', cost: config.cost };
    }

    const placement = this.grid.canPlaceBuilding(pos, config.size);
    if (!placement.ok) {
      return { ok: false, message: placement.reason ?? '无法建造' };
    }

    const expectedZone = zoneForBuildingCategory(config.category);
    const anchorTile = this.grid.getTile(pos);
    if (expectedZone !== 'none' && anchorTile.zone !== 'none' && anchorTile.zone !== expectedZone) {
      return { ok: false, message: '建筑类型与当前分区不匹配' };
    }

    const id = `building-${this.nextId}`;
    this.nextId += 1;
    this.grid.occupyBuilding(id, pos, config.size);
    const connectedRoadId = nearestRoadId(this.grid, pos, BALANCE.maxRoadSearchDistance, config.size);
    this.buildings.set(id, {
      id,
      configId,
      pos: { ...pos },
      size: { ...config.size },
      connectedRoadId,
    });
    this.metrics.cash -= config.cost;
    this.recomputeMetrics();

    return {
      ok: true,
      message: connectedRoadId ? `${config.name} 已建成` : `${config.name} 已建成，靠近道路效率更高`,
      cost: config.cost,
    };
  }

  private demolish(pos: GridPos): CommandResult {
    if (!this.grid.inBounds(pos)) {
      return { ok: false, message: '拆除位置超出地图' };
    }

    const buildingId = this.grid.findBuildingIdAt(pos);
    if (buildingId) {
      const building = this.buildings.get(buildingId);
      if (!building) {
        return { ok: false, message: '建筑数据缺失' };
      }
      const refund = Math.floor(getBuildingConfig(building.configId).cost * BALANCE.demolishRefundRate);
      this.grid.removeBuilding(buildingId);
      this.buildings.delete(buildingId);
      this.metrics.cash += refund;
      this.recomputeMetrics();
      return { ok: true, message: `已拆除建筑，回收 ${refund}`, cost: -refund };
    }

    const roadId = this.grid.getTile(pos).roadId;
    if (roadId) {
      this.grid.removeRoad(pos);
      this.roads.delete(roadId);
      this.recomputeMetrics();
      return { ok: true, message: '已拆除道路' };
    }

    return { ok: false, message: '这里没有可拆除对象' };
  }

  private addRoadTile(pos: GridPos): void {
    const id = `road-${roadKey(pos)}`;
    this.grid.setRoad(pos, id);
    this.roads.set(id, {
      id,
      pos: { ...pos },
      load: 0,
      capacity: ROAD.capacity,
    });
  }

  private seedStartingRoad(): void {
    const y = Math.floor(this.grid.height / 2);
    const from = Math.max(4, Math.floor(this.grid.width / 2) - 5);
    const to = Math.min(this.grid.width - 5, from + 10);
    for (let x = from; x <= to; x += 1) {
      if (this.grid.canPlaceRoad({ x, y })) {
        this.addRoadTile({ x, y });
      }
    }
  }

  private seedStarterBuilding(configId: string, pos: GridPos): void {
    const config = getBuildingConfig(configId);
    const placement = this.grid.canPlaceBuilding(pos, config.size);
    if (!placement.ok) {
      return;
    }

    const id = `building-${this.nextId}`;
    this.nextId += 1;
    this.grid.occupyBuilding(id, pos, config.size);
    this.buildings.set(id, {
      id,
      configId,
      pos: { ...pos },
      size: { ...config.size },
      connectedRoadId: nearestRoadId(this.grid, pos, BALANCE.maxRoadSearchDistance, config.size),
    });
  }

  private refreshBuildingRoadConnections(): void {
    for (const building of this.buildings.values()) {
      building.connectedRoadId = nearestRoadId(
        this.grid,
        building.pos,
        BALANCE.maxRoadSearchDistance,
        building.size,
      );
    }
  }

  private refreshBuildingUnlocks(): void {
    const unlocked = new Set(this.metrics.unlockedBuildingIds);
    for (const config of BUILDINGS) {
      const status = buildingUnlockStatus(config, {
        ...this.metrics,
        unlockedBuildingIds: [...unlocked],
      });
      if (status.unlocked) {
        unlocked.add(config.id);
      }
    }
    this.metrics = {
      ...this.metrics,
      unlockedBuildingIds: [...unlocked],
    };
  }
}

function normalizeCityMetrics(metrics: CityMetrics): CityMetrics {
  return {
    population: metrics.population ?? 0,
    cash: metrics.cash ?? BALANCE.initialCash,
    happiness: metrics.happiness ?? BALANCE.initialHappiness,
    housingCapacity: metrics.housingCapacity ?? 0,
    jobs: metrics.jobs ?? 0,
    powerSupply: metrics.powerSupply ?? 0,
    powerDemand: metrics.powerDemand ?? 0,
    waterSupply: metrics.waterSupply ?? 0,
    waterDemand: metrics.waterDemand ?? 0,
    congestion: metrics.congestion ?? 0,
    pollution: metrics.pollution ?? 0,
    serviceCoverage: metrics.serviceCoverage ?? 0,
    demand: metrics.demand ?? defaultDemand(),
    cityScore: metrics.cityScore ?? 50,
    cityLevelName: metrics.cityLevelName ?? '新生街区',
    alerts: metrics.alerts ?? [],
    roadTiles: metrics.roadTiles ?? 0,
    buildingCount: metrics.buildingCount ?? 0,
    connectedBuildings: metrics.connectedBuildings ?? 0,
    disconnectedBuildings: metrics.disconnectedBuildings ?? 0,
    unlockedBuildingIds: metrics.unlockedBuildingIds ?? [],
    taxRate: metrics.taxRate ?? BALANCE.defaultTaxRate,
    activeObjective: metrics.activeObjective ?? defaultObjective(),
  };
}

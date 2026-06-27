import { BALANCE } from '../data/balance';
import { CityState } from './city-state';
import { getBuildingConfig } from '../data/buildings';
import { nearestRoadId } from '../map/placement';
import type { ZoneType } from '../types';
import { settleEconomy } from './economy';
import { updateHappiness } from './happiness';
import { updatePollution } from './pollution';
import { updatePopulation } from './population';
import { updateTraffic } from './traffic';

const ZONE_BUILDING_MAP: Record<string, string | undefined> = {
  residential: 'residential_pod',
  commercial: 'market_corner',
  industrial: 'maker_yard',
};

function updateZoneDevelopment(city: CityState): void {
  const demand = city.metrics.demand;
  city.grid.forEachTile((tile, pos) => {
    if (tile.zone === 'none' || tile.buildingId) return;
    const buildingId = ZONE_BUILDING_MAP[tile.zone];
    if (!buildingId) return;
    const config = getBuildingConfig(buildingId);
    if (config.cost > city.metrics.cash) return;

    let zoneDemand = 0;
    if (tile.zone === 'residential') zoneDemand = demand.residential;
    else if (tile.zone === 'commercial') zoneDemand = demand.commercial;
    else if (tile.zone === 'industrial') zoneDemand = demand.industrial;
    if (zoneDemand < 15) return;

    const connectedRoadId = nearestRoadId(city.grid, pos, BALANCE.maxRoadSearchDistance, config.size);
    if (!connectedRoadId) return;

    const placement = city.grid.canPlaceBuilding(pos, config.size);
    if (!placement.ok) return;

    const id = 'auto-' + buildingId + '-' + pos.x + '-' + pos.y;
    city.grid.occupyBuilding(id, pos, config.size);
    city.metrics.cash -= config.cost;
  });
}

export type TickResult = {
  economySettled: boolean;
};

export function tickCity(city: CityState, deltaSeconds: number): TickResult {
  const beforeEconomyBucket = Math.floor(city.elapsedSeconds / BALANCE.economyIntervalSeconds);
  city.elapsedSeconds += deltaSeconds;
  city.recomputeMetrics();
  updatePollution(city);
  updateTraffic(city);
  updateHappiness(city);

  const beforePopulationBucket = Math.floor((city.elapsedSeconds - deltaSeconds) / BALANCE.populationIntervalSeconds);
  const afterPopulationBucket = Math.floor(city.elapsedSeconds / BALANCE.populationIntervalSeconds);
  if (afterPopulationBucket > beforePopulationBucket) {
    updatePopulation(city);
  }

  const afterEconomyBucket = Math.floor(city.elapsedSeconds / BALANCE.economyIntervalSeconds);
  const economySettled = afterEconomyBucket > beforeEconomyBucket;
  if (economySettled) {
    settleEconomy(city);
  }
  city.recomputeMetrics();

  const beforeDevBucket = Math.floor((city.elapsedSeconds - deltaSeconds) / 2);
  const afterDevBucket = Math.floor(city.elapsedSeconds / 2);
  if (afterDevBucket > beforeDevBucket) {
    updateZoneDevelopment(city);
  }

  return { economySettled };
}

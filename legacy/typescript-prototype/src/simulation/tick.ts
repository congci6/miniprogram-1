import { BALANCE } from '../data/balance';
import { getBuildingConfig } from '../data/buildings';
import { nearestRoadId } from '../map/placement';
import { settleEconomy } from './economy';
import { updateHappiness } from './happiness';
import { updatePollution } from './pollution';
import { updatePopulation } from './population';
import { updateTraffic } from './traffic';
import { checkBuildingUpgrades } from './upgrade';
import type { CityState } from './city-state';

const ZONE_BUILDING_MAP: Record<string, string | undefined> = {
  residential: 'residential_pod',
  commercial: 'market_corner',
  industrial: 'maker_yard',
};

function updateZoneDevelopment(city: CityState): number {
  let developed = 0;
  const demand = city.metrics.demand;

  city.grid.forEachTile((tile, pos) => {
    if (tile.zone === 'none' || tile.buildingId) {
      return;
    }
    const buildingId = ZONE_BUILDING_MAP[tile.zone];
    if (!buildingId) {
      return;
    }

    const config = getBuildingConfig(buildingId);
    if (config.cost > city.metrics.cash) {
      return;
    }

    let zoneDemand = 0;
    if (tile.zone === 'residential') zoneDemand = demand.residential;
    else if (tile.zone === 'commercial') zoneDemand = demand.commercial;
    else if (tile.zone === 'industrial') zoneDemand = demand.industrial;
    if (zoneDemand < 15) {
      return;
    }

    const connectedRoadId = nearestRoadId(city.grid, pos, BALANCE.maxRoadSearchDistance, config.size);
    if (!connectedRoadId) {
      return;
    }

    if (city.developZonedBuilding(buildingId, pos)) {
      developed += 1;
    }
  });

  return developed;
}

export type TickResult = {
  economySettled: boolean;
  autoDeveloped: number;
  upgradedBuildings: number;
  worldChanged: boolean;
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

  let autoDeveloped = 0;
  const beforeDevBucket = Math.floor((city.elapsedSeconds - deltaSeconds) / 2);
  const afterDevBucket = Math.floor(city.elapsedSeconds / 2);
  if (afterDevBucket > beforeDevBucket) {
    autoDeveloped = updateZoneDevelopment(city);
  }

  let upgradedBuildings = 0;
  const beforeUpgradeBucket = Math.floor((city.elapsedSeconds - deltaSeconds) / 5);
  const afterUpgradeBucket = Math.floor(city.elapsedSeconds / 5);
  if (afterUpgradeBucket > beforeUpgradeBucket) {
    upgradedBuildings = checkBuildingUpgrades(city);
  }

  const worldChanged = autoDeveloped > 0 || upgradedBuildings > 0;
  if (worldChanged) {
    city.recomputeMetrics();
  }

  return { economySettled, autoDeveloped, upgradedBuildings, worldChanged };
}

import { getBuildingConfig, UPGRADE_STAGES } from '../data/buildings';
import type { BuildingCategory, BuildingUpgradeStage, PlacedBuilding } from '../types';
import type { CityState } from './city-state';

const DEFAULT_STAGE = UPGRADE_STAGES[0];
const GROWTH_CATEGORIES: ReadonlySet<BuildingCategory> = new Set(['residential', 'commercial', 'industrial']);

export function getStageAtLevel(level: number): BuildingUpgradeStage {
  return UPGRADE_STAGES[Math.min(level, UPGRADE_STAGES.length - 1)] ?? DEFAULT_STAGE;
}

export function getAppliedStage(building: PlacedBuilding): BuildingUpgradeStage {
  const config = getBuildingConfig(building.configId);
  return GROWTH_CATEGORIES.has(config.category) ? getStageAtLevel(building.level) : DEFAULT_STAGE;
}

export function canNaturallyUpgrade(building: PlacedBuilding): boolean {
  return GROWTH_CATEGORIES.has(getBuildingConfig(building.configId).category);
}

export function checkBuildingUpgrades(city: CityState): number {
  const now = city.elapsedSeconds;
  const metrics = city.metrics;
  let upgraded = 0;

  for (const building of city.getBuildings()) {
    if (!canNaturallyUpgrade(building)) {
      continue;
    }

    const nextStage = UPGRADE_STAGES[building.level + 1];
    if (!nextStage) {
      continue;
    }

    const age = now - building.placedAt;
    if (age < nextStage.requiredAgeSeconds) {
      continue;
    }
    if (!building.connectedRoadId) {
      continue;
    }
    if (metrics.serviceCoverage < nextStage.minServiceCoverage * 100) {
      continue;
    }
    if (metrics.happiness < nextStage.minHappiness) {
      continue;
    }
    if (!demandSatisfied(building, metrics.demand, nextStage.minDemand)) {
      continue;
    }

    if (city.applyBuildingUpgrade(building.id, nextStage.level)) {
      upgraded += 1;
    }
  }

  return upgraded;
}

function demandSatisfied(
  building: PlacedBuilding,
  demand: { residential: number; commercial: number; industrial: number },
  minDemand: number,
): boolean {
  const category = getBuildingConfig(building.configId).category;
  switch (category) {
    case 'residential':
      return demand.residential >= minDemand;
    case 'commercial':
      return demand.commercial >= minDemand;
    case 'industrial':
      return demand.industrial >= minDemand;
    default:
      return true;
  }
}

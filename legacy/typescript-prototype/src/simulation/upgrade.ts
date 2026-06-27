import { getBuildingConfig, UPGRADE_STAGES } from '../data/buildings';
import type { BuildingCategory, BuildingUpgradeStage, PlacedBuilding } from '../types';
import type { CityState } from './city-state';

const DEFAULT_STAGE = UPGRADE_STAGES[0];
const GROWTH_CATEGORIES: ReadonlySet<BuildingCategory> = new Set(['residential', 'commercial', 'industrial']);

export type BuildingUpgradeReadiness = {
  atMax: boolean;
  ready: boolean;
  nextLevel?: number;
  summary: string;
  detail: string;
};

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

export function describeUpgradeReadiness(city: CityState, building: PlacedBuilding): BuildingUpgradeReadiness {
  if (!canNaturallyUpgrade(building)) {
    return {
      atMax: true,
      ready: false,
      summary: '???????????',
      detail: '????????????????',
    };
  }

  const nextStage = UPGRADE_STAGES[building.level + 1];
  if (!nextStage) {
    return {
      atMax: true,
      ready: false,
      summary: '???????',
      detail: '?????????',
    };
  }

  const missing = missingUpgradeConditions(city, building, nextStage);
  if (missing.length === 0) {
    return {
      atMax: false,
      ready: true,
      nextLevel: nextStage.level,
      summary: `?? Lv.${nextStage.level + 1} ????`,
      detail: '?????????',
    };
  }

  return {
    atMax: false,
    ready: false,
    nextLevel: nextStage.level,
    summary: `???? Lv.${nextStage.level + 1}`,
    detail: `????${missing.slice(0, 2).join(' / ')}`,
  };
}

export function checkBuildingUpgrades(city: CityState): number {
  let upgraded = 0;

  for (const building of city.getBuildings()) {
    if (!canNaturallyUpgrade(building)) {
      continue;
    }

    const nextStage = UPGRADE_STAGES[building.level + 1];
    if (!nextStage) {
      continue;
    }

    if (missingUpgradeConditions(city, building, nextStage).length > 0) {
      continue;
    }

    if (city.applyBuildingUpgrade(building.id, nextStage.level)) {
      upgraded += 1;
    }
  }

  return upgraded;
}

function missingUpgradeConditions(city: CityState, building: PlacedBuilding, nextStage: BuildingUpgradeStage): string[] {
  const missing: string[] = [];
  const age = city.elapsedSeconds - building.placedAt;
  if (age < nextStage.requiredAgeSeconds) {
    missing.push(`?? ${Math.floor(age)}/${nextStage.requiredAgeSeconds}s`);
  }
  if (!building.connectedRoadId) {
    missing.push('??');
  }
  if (city.metrics.serviceCoverage < nextStage.minServiceCoverage * 100) {
    missing.push(`?? ${city.metrics.serviceCoverage}/${Math.round(nextStage.minServiceCoverage * 100)}%`);
  }
  if (city.metrics.happiness < nextStage.minHappiness) {
    missing.push(`?? ${Math.round(city.metrics.happiness)}/${nextStage.minHappiness}`);
  }
  if (!demandSatisfied(building, city.metrics.demand, nextStage.minDemand)) {
    const category = getBuildingConfig(building.configId).category;
    if (category === 'residential') missing.push(`???? ${city.metrics.demand.residential}/${nextStage.minDemand}`);
    if (category === 'commercial') missing.push(`???? ${city.metrics.demand.commercial}/${nextStage.minDemand}`);
    if (category === 'industrial') missing.push(`???? ${city.metrics.demand.industrial}/${nextStage.minDemand}`);
  }
  return missing;
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

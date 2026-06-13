import { getBuildingConfig } from '../data/buildings';
import type { BuildingConfig, CityMetrics } from '../types';

export type UnlockStatus = {
  unlocked: boolean;
  reason: string;
  progress: number;
  required: number;
};

export function buildingUnlockStatus(config: BuildingConfig, metrics: CityMetrics): UnlockStatus {
  if (metrics.unlockedBuildingIds.includes(config.id)) {
    return { unlocked: true, reason: '已解锁', progress: 1, required: 1 };
  }

  const unlock = config.unlock;
  if (!unlock) {
    return { unlocked: true, reason: '已解锁', progress: 1, required: 1 };
  }

  const populationRequired = unlock.minPopulation ?? 0;
  if (metrics.population < populationRequired) {
    return {
      unlocked: false,
      reason: `需要人口 ${populationRequired}`,
      progress: Math.floor(metrics.population),
      required: populationRequired,
    };
  }

  const scoreRequired = unlock.minCityScore ?? 0;
  if (metrics.cityScore < scoreRequired) {
    return {
      unlocked: false,
      reason: `需要评分 ${scoreRequired}`,
      progress: metrics.cityScore,
      required: scoreRequired,
    };
  }

  return { unlocked: true, reason: '已解锁', progress: 1, required: 1 };
}

export function buildingIdUnlockStatus(buildingId: string, metrics: CityMetrics): UnlockStatus {
  return buildingUnlockStatus(getBuildingConfig(buildingId), metrics);
}

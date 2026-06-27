import { getBuildingConfig } from '../data/buildings';
import type { CityMetrics, PlacedBuilding } from '../types';
import { getAppliedStage } from './upgrade';

export function recomputeCityServices(
  buildings: PlacedBuilding[],
): Pick<
  CityMetrics,
  | 'housingCapacity'
  | 'jobs'
  | 'powerSupply'
  | 'powerDemand'
  | 'waterSupply'
  | 'waterDemand'
  | 'serviceCoverage'
> {
  let housingCapacity = 0;
  let jobs = 0;
  let powerSupply = 0;
  let powerDemand = 0;
  let waterSupply = 0;
  let waterDemand = 0;
  let residentialCapacity = 0;
  let servicedResidentialCapacity = 0;
  const serviceBuildings = buildings.filter((placed) => {
    const config = getBuildingConfig(placed.configId);
    return config.category === 'service' && Boolean(placed.connectedRoadId) && (config.serviceRadius ?? 0) > 0;
  });

  for (const placed of buildings) {
    const config = getBuildingConfig(placed.configId);
    const stage = getAppliedStage(placed);
    const efficiency = placed.connectedRoadId ? 1 : 0.2;
    const buildingCapacity = Math.floor((config.capacity ?? 0) * stage.capacityMultiplier * efficiency);
    const buildingJobs = Math.floor((config.jobs ?? 0) * stage.jobsMultiplier * efficiency);

    housingCapacity += buildingCapacity;
    jobs += buildingJobs;
    powerSupply += Math.floor((config.powerOutput ?? 0) * efficiency);
    powerDemand += config.powerUse ?? 0;
    waterSupply += Math.floor((config.waterOutput ?? 0) * efficiency);
    waterDemand += config.waterUse ?? 0;

    if (config.category === 'residential' && buildingCapacity > 0) {
      residentialCapacity += buildingCapacity;
      if (isCoveredByService(placed, serviceBuildings)) {
        servicedResidentialCapacity += buildingCapacity;
      }
    }
  }

  return {
    housingCapacity,
    jobs,
    powerSupply,
    powerDemand,
    waterSupply,
    waterDemand,
    serviceCoverage:
      residentialCapacity === 0 ? 0 : Math.round((servicedResidentialCapacity / residentialCapacity) * 100),
  };
}

export function buildingUpkeep(buildings: PlacedBuilding[]): number {
  return buildings.reduce((sum, placed) => {
    const config = getBuildingConfig(placed.configId);
    const stage = getAppliedStage(placed);
    return sum + config.upkeep * stage.upkeepMultiplier;
  }, 0);
}

export function jobsByCategory(buildings: PlacedBuilding[], category: 'commercial' | 'industrial'): number {
  return buildings.reduce((sum, placed) => {
    const config = getBuildingConfig(placed.configId);
    if (config.category !== category) {
      return sum;
    }
    const stage = getAppliedStage(placed);
    return sum + Math.floor((config.jobs ?? 0) * stage.jobsMultiplier);
  }, 0);
}

function isCoveredByService(building: PlacedBuilding, services: PlacedBuilding[]): boolean {
  const buildingCenter = buildingCenterPoint(building);
  return services.some((service) => {
    const config = getBuildingConfig(service.configId);
    const serviceCenter = buildingCenterPoint(service);
    const distance = Math.abs(buildingCenter.x - serviceCenter.x) + Math.abs(buildingCenter.y - serviceCenter.y);
    return distance <= (config.serviceRadius ?? 0);
  });
}

function buildingCenterPoint(building: PlacedBuilding): { x: number; y: number } {
  return {
    x: building.pos.x + building.size.w / 2,
    y: building.pos.y + building.size.h / 2,
  };
}

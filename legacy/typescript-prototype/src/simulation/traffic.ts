import { BALANCE } from '../data/balance';
import { getBuildingConfig } from '../data/buildings';
import type { CityState } from './city-state';

export function updateTraffic(city: CityState): void {
  const loads = new Map<string, number>();
  const roads = city.getRoads();
  const buildings = city.getBuildings();
  const connectedBuildings = buildings.filter((building) => Boolean(building.connectedRoadId));
  const totalJobs = city.metrics.jobs;
  const commuteDemand = Math.max(0, Math.min(city.metrics.population, totalJobs) * 0.18);

  for (const building of connectedBuildings) {
    const config = getBuildingConfig(building.configId);
    const baseDemand =
      config.category === 'residential'
        ? commuteDemand / Math.max(1, connectedBuildings.length)
        : (config.jobs ?? 0) * 0.12;
    const roadId = building.connectedRoadId!;
    loads.set(roadId, (loads.get(roadId) ?? 0) + baseDemand);
  }

  const spillover = roads.length > 0 ? commuteDemand / roads.length : 0;
  for (const road of roads) {
    loads.set(road.id, (loads.get(road.id) ?? 0) + spillover);
  }

  city.mutateRoadLoads(loads);

  if (roads.length === 0) {
    city.metrics.congestion = 0;
    return;
  }

  const congestion = roads.reduce((sum, road) => {
    const load = loads.get(road.id) ?? 0;
    return sum + Math.max(0, load / BALANCE.roadCapacity - 0.75);
  }, 0);
  city.metrics.congestion = Math.round((congestion / roads.length) * 100);
}

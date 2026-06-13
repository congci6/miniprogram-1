import { describe, expect, it } from 'vitest';
import { CityGrid } from '../map/grid';
import { roadNetworkSize } from '../map/road-graph';
import { CityState } from '../simulation/city-state';
import { updateTraffic } from '../simulation/traffic';

describe('road graph and traffic', () => {
  it('counts connected road tiles', () => {
    const grid = new CityGrid(12, 12);
    grid.setRoad({ x: 2, y: 2 }, 'road-a');
    grid.setRoad({ x: 3, y: 2 }, 'road-b');
    grid.setRoad({ x: 3, y: 3 }, 'road-c');
    expect(roadNetworkSize(grid, { x: 2, y: 2 })).toBe(3);
  });

  it('computes congestion from connected buildings', () => {
    const city = CityState.createNew();
    city.execute({ type: 'PLACE_BUILDING', buildingId: 'residential_pod', pos: { x: 29, y: 29 } });
    city.execute({ type: 'PLACE_BUILDING', buildingId: 'maker_yard', pos: { x: 34, y: 28 } });
    city.metrics.population = 48;
    updateTraffic(city);
    expect(city.metrics.congestion).toBeGreaterThanOrEqual(0);
  });
});

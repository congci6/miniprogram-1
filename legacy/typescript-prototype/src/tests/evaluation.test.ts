import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { buildingIdUnlockStatus } from '../simulation/unlocks';

describe('city evaluation', () => {
  it('tracks demand, score, level, and alerts for the starter city', () => {
    const city = CityState.createNew();

    expect(city.metrics.cityLevelName).toBe('新生街区');
    expect(city.metrics.cityScore).toBeGreaterThan(0);
    expect(city.metrics.roadTiles).toBeGreaterThan(0);
    expect(city.metrics.connectedBuildings).toBe(city.metrics.buildingCount);
    expect(city.metrics.serviceCoverage).toBe(0);
    expect(city.metrics.unlockedBuildingIds).toEqual(
      expect.arrayContaining(['residential_pod', 'market_corner', 'micro_power', 'water_tower']),
    );
    expect(buildingIdUnlockStatus('pocket_park', city.metrics).unlocked).toBe(false);
    expect(city.metrics.activeObjective.title).toBe('吸引第一批居民');
    expect(city.metrics.demand.residential).toBeGreaterThanOrEqual(0);
    expect(city.metrics.demand.commercial).toBeGreaterThanOrEqual(0);
    expect(city.metrics.demand.industrial).toBeGreaterThanOrEqual(0);
    expect(city.metrics.alerts).toEqual([]);
  });

  it('surfaces service shortages as city alerts', () => {
    const city = CityState.createNew();

    const result = city.execute({ type: 'DEMOLISH', pos: { x: 24, y: 35 } });

    expect(result.ok).toBe(true);
    expect(city.metrics.alerts).toContain('电力不足');
  });

  it('penalizes buildings without road access and reconnects them when roads arrive', () => {
    const city = CityState.createNew();
    const beforeConnected = city.metrics.connectedBuildings;

    const placement = city.execute({ type: 'PLACE_BUILDING', buildingId: 'residential_pod', pos: { x: 8, y: 8 } });

    expect(placement.ok).toBe(true);
    expect(city.metrics.disconnectedBuildings).toBe(1);
    expect(city.metrics.connectedBuildings).toBe(beforeConnected);
    expect(city.metrics.alerts).toContain('1栋未接路');

    const road = city.execute({ type: 'BUILD_ROAD', from: { x: 8, y: 10 }, to: { x: 10, y: 10 } });

    expect(road.ok).toBe(true);
    expect(city.metrics.disconnectedBuildings).toBe(0);
    expect(city.metrics.connectedBuildings).toBe(beforeConnected + 1);
  });

  it('unlocks parks as a milestone and uses them for service coverage', () => {
    const city = CityState.createNew();
    const parkPos = { x: 30, y: 33 };

    const locked = city.execute({ type: 'PLACE_BUILDING', buildingId: 'pocket_park', pos: parkPos });
    expect(locked.ok).toBe(false);
    expect(locked.message).toContain('未解锁');

    city.metrics.population = 48;
    city.metrics.cityScore = 60;
    city.recomputeMetrics();

    expect(city.metrics.unlockedBuildingIds).toContain('pocket_park');
    const placed = city.execute({ type: 'PLACE_BUILDING', buildingId: 'pocket_park', pos: parkPos });
    expect(placed.ok).toBe(true);
    expect(city.metrics.serviceCoverage).toBeGreaterThan(0);
  });
});

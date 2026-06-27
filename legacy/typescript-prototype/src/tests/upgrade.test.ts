import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { checkBuildingUpgrades } from '../simulation/upgrade';

describe('building upgrades', () => {
  it('upgrades a connected residential building when conditions are met', () => {
    const city = CityState.createNew();
    const result = city.execute({ type: 'PLACE_BUILDING', buildingId: 'residential_pod', pos: { x: 24, y: 29 } });
    expect(result.ok).toBe(true);

    city.metrics.serviceCoverage = 80;
    city.metrics.happiness = 75;
    city.metrics.demand = {
      residential: 60,
      commercial: 30,
      industrial: 30,
    };
    city.elapsedSeconds = 35;

    const before = city.getBuildingAt({ x: 24, y: 29 });
    expect(before?.level).toBe(0);

    const upgraded = checkBuildingUpgrades(city);
    const after = city.getBuildingAt({ x: 24, y: 29 });
    const afterLevels = city.getBuildings().map((building) => building.level);

    expect(upgraded).toBeGreaterThanOrEqual(1);
    expect(after?.level).toBe(1);
    expect(afterLevels.some((level) => level > 0)).toBe(true);
  });
});

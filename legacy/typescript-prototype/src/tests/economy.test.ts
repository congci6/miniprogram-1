import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { tickCity } from '../simulation/tick';

describe('city economy and population', () => {
  it('charges construction costs and grows population into housing capacity', () => {
    const city = CityState.createNew();
    const startingCash = city.metrics.cash;

    expect(city.execute({ type: 'PLACE_BUILDING', buildingId: 'residential_pod', pos: { x: 12, y: 12 } }).ok).toBe(
      true,
    );

    expect(city.metrics.cash).toBeLessThan(startingCash);
    tickCity(city, 12);
    expect(city.metrics.housingCapacity).toBeGreaterThan(0);
    expect(city.metrics.population).toBeGreaterThan(0);
    expect(city.metrics.happiness).toBeGreaterThan(0);
  });

  it('starts with visible starter buildings around the first road', () => {
    const city = CityState.createNew();
    expect(city.getBuildings().length).toBeGreaterThanOrEqual(5);
    expect(city.metrics.housingCapacity).toBeGreaterThan(0);
    expect(city.metrics.powerSupply).toBeGreaterThan(0);
    expect(city.metrics.waterSupply).toBeGreaterThan(0);
  });
});

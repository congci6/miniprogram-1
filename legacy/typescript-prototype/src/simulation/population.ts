import { BALANCE } from '../data/balance';
import type { CityState } from './city-state';

export function updatePopulation(city: CityState): void {
  const capacity = city.metrics.housingCapacity;
  const current = city.metrics.population;
  const happinessFactor = Math.max(0.05, city.metrics.happiness / 100);
  const serviceFactor =
    city.metrics.powerDemand > city.metrics.powerSupply || city.metrics.waterDemand > city.metrics.waterSupply
      ? 0.45
      : 1;

  if (capacity > current) {
    const gap = capacity - current;
    const growth = Math.ceil(Math.min(BALANCE.maxPopulationGrowthPerTick, gap * 0.08 * happinessFactor * serviceFactor));
    city.metrics.population += growth;
  } else if (capacity < current || city.metrics.happiness < 32) {
    const pressure = capacity < current ? current - capacity : current * 0.04;
    city.metrics.population = Math.max(0, current - Math.ceil(pressure * 0.08));
  }
}

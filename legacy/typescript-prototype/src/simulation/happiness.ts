import { BALANCE } from '../data/balance';
import type { CityState } from './city-state';

function clamp(value: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, value));
}

export function updateHappiness(city: CityState): void {
  const employmentRatio =
    city.metrics.population === 0 ? 1 : Math.min(1.2, city.metrics.jobs / Math.max(1, city.metrics.population * 0.55));
  const employmentBonus = (employmentRatio - 0.7) * 18;
  const taxPenalty = Math.max(0, city.taxRate - 0.1) * 220;
  const pollutionPenalty = city.metrics.pollution * 0.28;
  const congestionPenalty = city.metrics.congestion * 0.35;
  const serviceBonus = Math.min(18, city.metrics.serviceCoverage * 0.18);
  const powerPenalty = city.metrics.powerDemand > city.metrics.powerSupply ? 12 : 0;
  const waterPenalty = city.metrics.waterDemand > city.metrics.waterSupply ? 12 : 0;
  const housingPressure = city.metrics.population >= city.metrics.housingCapacity && city.metrics.housingCapacity > 0 ? 8 : 0;

  city.metrics.happiness = Math.round(
    clamp(
      BALANCE.baseHappiness +
        employmentBonus -
        taxPenalty -
        pollutionPenalty -
        congestionPenalty -
        powerPenalty -
        waterPenalty -
        housingPressure +
        serviceBonus,
      5,
      96,
    ),
  );
}

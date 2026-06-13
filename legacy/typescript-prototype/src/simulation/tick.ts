import { BALANCE } from '../data/balance';
import type { CityState } from './city-state';
import { settleEconomy } from './economy';
import { updateHappiness } from './happiness';
import { updatePollution } from './pollution';
import { updatePopulation } from './population';
import { updateTraffic } from './traffic';

export type TickResult = {
  economySettled: boolean;
};

export function tickCity(city: CityState, deltaSeconds: number): TickResult {
  const beforeEconomyBucket = Math.floor(city.elapsedSeconds / BALANCE.economyIntervalSeconds);
  city.elapsedSeconds += deltaSeconds;
  city.recomputeMetrics();
  updatePollution(city);
  updateTraffic(city);
  updateHappiness(city);

  const beforePopulationBucket = Math.floor((city.elapsedSeconds - deltaSeconds) / BALANCE.populationIntervalSeconds);
  const afterPopulationBucket = Math.floor(city.elapsedSeconds / BALANCE.populationIntervalSeconds);
  if (afterPopulationBucket > beforePopulationBucket) {
    updatePopulation(city);
  }

  const afterEconomyBucket = Math.floor(city.elapsedSeconds / BALANCE.economyIntervalSeconds);
  const economySettled = afterEconomyBucket > beforeEconomyBucket;
  if (economySettled) {
    settleEconomy(city);
  }
  city.recomputeMetrics();

  return { economySettled };
}

import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { demandAdvisor } from '../simulation/demand-advisor';

describe('demand advisor', () => {
  it('recommends housing when residential demand dominates', () => {
    const city = CityState.createNew();
    city.metrics.demand = {
      residential: 78,
      commercial: 30,
      industrial: 22,
    };
    city.metrics.serviceCoverage = 42;

    const advice = demandAdvisor(city.metrics);
    expect(advice.focus).toBe('residential');
    expect(advice.text).toContain('??');
  });

  it('reports balanced demand when all categories are modest', () => {
    const city = CityState.createNew();
    city.metrics.demand = {
      residential: 38,
      commercial: 36,
      industrial: 34,
    };

    const advice = demandAdvisor(city.metrics);
    expect(advice.focus).toBe('balanced');
    expect(advice.urgency).toBe('low');
  });
});

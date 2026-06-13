import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { createSave, deserializeSave, serializeSave } from '../simulation/save';

describe('save game', () => {
  it('round-trips a versioned save', () => {
    const city = CityState.createNew();
    const beforeCount = city.getBuildings().length;
    expect(city.execute({ type: 'PLACE_BUILDING', buildingId: 'residential_pod', pos: { x: 12, y: 12 } }).ok).toBe(
      true,
    );

    const restored = deserializeSave(serializeSave(createSave(city, 1000)));
    expect(restored.serialize().buildings).toHaveLength(beforeCount + 1);
    expect(restored.metrics.cash).toBe(city.metrics.cash);
  });

  it('loads older saves that do not contain evaluation fields', () => {
    const city = CityState.createNew();
    const serialized = city.serialize();
    const legacyMetrics = { ...serialized.metrics } as Partial<typeof serialized.metrics>;
    delete legacyMetrics.demand;
    delete legacyMetrics.alerts;
    delete legacyMetrics.cityScore;
    delete legacyMetrics.cityLevelName;
    delete legacyMetrics.roadTiles;
    delete legacyMetrics.buildingCount;
    delete legacyMetrics.connectedBuildings;
    delete legacyMetrics.disconnectedBuildings;
    delete legacyMetrics.serviceCoverage;
    delete legacyMetrics.unlockedBuildingIds;
    delete legacyMetrics.activeObjective;

    const restored = CityState.deserialize({
      ...serialized,
      metrics: legacyMetrics as typeof serialized.metrics,
    });

    expect(restored.metrics.cityLevelName).toBe('新生街区');
    expect(restored.metrics.cityScore).toBeGreaterThan(0);
    expect(restored.metrics.demand.residential).toBeGreaterThanOrEqual(0);
    expect(restored.metrics.serviceCoverage).toBeGreaterThanOrEqual(0);
    expect(restored.metrics.unlockedBuildingIds).toEqual([]);
    expect(restored.metrics.activeObjective.required).toBeGreaterThan(0);
    expect(restored.metrics.alerts).toEqual([]);
  });
});

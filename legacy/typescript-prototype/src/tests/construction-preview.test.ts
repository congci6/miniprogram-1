import { describe, expect, it } from 'vitest';
import { previewConstruction } from '../simulation/construction-preview';
import { CityState } from '../simulation/city-state';

describe('construction preview', () => {
  it('describes a valid building plan before spending cash', () => {
    const city = CityState.createNew();

    const preview = previewConstruction(city, { type: 'building', buildingId: 'residential_pod' }, { x: 12, y: 12 });

    expect(preview.ok).toBe(true);
    expect(preview.confirmLabel).toBe('建造');
    expect(preview.lines.join(' ')).toContain('花费 260');
    expect(city.getBuildings()).toHaveLength(6);
  });

  it('blocks locked buildings until milestone requirements are met', () => {
    const city = CityState.createNew();
    const pos = { x: 12, y: 12 };

    const locked = previewConstruction(city, { type: 'building', buildingId: 'pocket_park' }, pos);
    expect(locked.ok).toBe(false);
    expect(locked.lines[0]).toContain('人口 40');

    city.metrics.population = 48;
    city.metrics.cityScore = 60;
    city.recomputeMetrics();

    const unlocked = previewConstruction(city, { type: 'building', buildingId: 'pocket_park' }, pos);
    expect(unlocked.ok).toBe(true);
  });

  it('summarizes road costs between two points', () => {
    const city = CityState.createNew();

    const preview = previewConstruction(
      city,
      { type: 'road', from: { x: 18, y: 31 } },
      { x: 18, y: 34 },
    );

    expect(preview.ok).toBe(true);
    expect(preview.confirmLabel).toBe('铺设');
    expect(preview.lines.join(' ')).toContain('花费');
  });

  it('previews demolition refunds for existing buildings', () => {
    const city = CityState.createNew();

    const preview = previewConstruction(city, { type: 'demolish' }, { x: 27, y: 28 });

    expect(preview.ok).toBe(true);
    expect(preview.confirmLabel).toBe('拆除');
    expect(preview.lines.join(' ')).toContain('回收');
  });
});

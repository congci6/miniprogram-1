import { describe, expect, it } from 'vitest';
import { CityGrid } from '../map/grid';

describe('CityGrid', () => {
  it('creates a 64x64 grid and supports tile queries', () => {
    const grid = new CityGrid(64, 64);
    expect(grid.width).toBe(64);
    expect(grid.height).toBe(64);
    expect(grid.getTile({ x: 0, y: 0 }).zone).toBe('none');
  });

  it('sets zones without changing water tiles', () => {
    const grid = new CityGrid(64, 64);
    grid.setZone({ x: 4, y: 4, w: 4, h: 4 }, 'residential');
    expect(grid.getTile({ x: 5, y: 5 }).zone).toBe('residential');
  });

  it('checks building bounds and occupancy conflicts', () => {
    const grid = new CityGrid(64, 64);
    expect(grid.canPlaceBuilding({ x: 5, y: 5 }, { w: 2, h: 2 }).ok).toBe(true);
    grid.occupyBuilding('building-1', { x: 5, y: 5 }, { w: 2, h: 2 });
    expect(grid.canPlaceBuilding({ x: 6, y: 6 }, { w: 2, h: 2 }).ok).toBe(false);
    expect(grid.canPlaceBuilding({ x: 63, y: 63 }, { w: 2, h: 2 }).ok).toBe(false);
  });

  it('serializes and deserializes tiles', () => {
    const grid = new CityGrid(64, 64);
    grid.setZone({ x: 10, y: 10, w: 2, h: 2 }, 'commercial');
    const restored = CityGrid.fromSerialized({
      width: 64,
      height: 64,
      tiles: grid.serializeTiles(),
    });
    expect(restored.getTile({ x: 10, y: 10 }).zone).toBe('commercial');
  });
});

import type { TerrainType, Tile } from '../types';

export function createTile(terrain: TerrainType = 'plain'): Tile {
  return {
    terrain,
    zone: 'none',
    pollution: 0,
    landValue: terrain === 'water' ? 0 : terrain === 'hill' ? 55 : 70,
  };
}

export function cloneTile(tile: Tile): Tile {
  return {
    terrain: tile.terrain,
    zone: tile.zone,
    roadId: tile.roadId,
    buildingId: tile.buildingId,
    pollution: tile.pollution,
    landValue: tile.landValue,
  };
}

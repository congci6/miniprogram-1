import type { GridPos, TerrainType } from '../types';

export function terrainForPosition(pos: GridPos, width: number, height: number): TerrainType {
  const dx = pos.x - width * 0.72;
  const dy = pos.y - height * 0.28;
  const waterBand = Math.sin((pos.x + pos.y) * 0.18) * 2.5 + height * 0.64;

  if (Math.abs(pos.y - waterBand) < 1.2 && pos.x > width * 0.12 && pos.x < width * 0.9) {
    return 'water';
  }

  if (dx * dx + dy * dy < 120 || (pos.x > width * 0.78 && pos.y > height * 0.68)) {
    return 'hill';
  }

  return 'plain';
}

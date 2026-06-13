import type { GridPos } from '../types';
import type { CityGrid } from './grid';

export function nearestRoadId(
  grid: CityGrid,
  origin: GridPos,
  maxDistance: number,
  size: { w: number; h: number } = { w: 1, h: 1 },
): string | undefined {
  let closest: { id: string; distance: number } | undefined;

  grid.forEachTile((tile, pos) => {
    if (!tile.roadId) {
      return;
    }
    const maxX = origin.x + size.w - 1;
    const maxY = origin.y + size.h - 1;
    const dx = pos.x < origin.x ? origin.x - pos.x : pos.x > maxX ? pos.x - maxX : 0;
    const dy = pos.y < origin.y ? origin.y - pos.y : pos.y > maxY ? pos.y - maxY : 0;
    const distance = dx + dy;
    if (distance <= maxDistance && (!closest || distance < closest.distance)) {
      closest = { id: tile.roadId, distance };
    }
  });

  return closest?.id;
}

export function manhattanLine(from: GridPos, to: GridPos): GridPos[] {
  const points: GridPos[] = [];
  const stepX = from.x <= to.x ? 1 : -1;
  const stepY = from.y <= to.y ? 1 : -1;

  for (let x = from.x; x !== to.x + stepX; x += stepX) {
    points.push({ x, y: from.y });
  }
  for (let y = from.y + stepY; y !== to.y + stepY; y += stepY) {
    points.push({ x: to.x, y });
  }

  return points;
}

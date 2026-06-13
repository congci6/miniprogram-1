import type { GridPos, RoadNode } from '../types';
import type { CityGrid } from './grid';

export function roadKey(pos: GridPos): string {
  return `${pos.x}:${pos.y}`;
}

export function getRoadNeighbors(grid: CityGrid, pos: GridPos): GridPos[] {
  const candidates = [
    { x: pos.x + 1, y: pos.y },
    { x: pos.x - 1, y: pos.y },
    { x: pos.x, y: pos.y + 1 },
    { x: pos.x, y: pos.y - 1 },
  ];
  return candidates.filter((candidate) => grid.inBounds(candidate) && Boolean(grid.getTile(candidate).roadId));
}

export function roadNetworkSize(grid: CityGrid, start: GridPos): number {
  if (!grid.inBounds(start) || !grid.getTile(start).roadId) {
    return 0;
  }

  const seen = new Set<string>();
  const queue = [start];
  while (queue.length > 0) {
    const current = queue.shift()!;
    const key = roadKey(current);
    if (seen.has(key)) {
      continue;
    }
    seen.add(key);
    queue.push(...getRoadNeighbors(grid, current));
  }

  return seen.size;
}

export function cloneRoad(road: RoadNode): RoadNode {
  return {
    id: road.id,
    pos: { ...road.pos },
    load: road.load,
    capacity: road.capacity,
  };
}

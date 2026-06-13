import { getBuildingConfig } from '../data/buildings';
import type { CityState } from './city-state';

export function updatePollution(city: CityState): void {
  city.grid.forEachTile((tile) => {
    tile.pollution = Math.max(0, tile.pollution * 0.82 - 0.04);
  });

  for (const building of city.getBuildings()) {
    const config = getBuildingConfig(building.configId);
    const pollution = config.pollution ?? 0;
    if (pollution <= 0) {
      continue;
    }
    const radius = config.category === 'industrial' ? 5 : 4;
    for (let y = building.pos.y - radius; y <= building.pos.y + radius; y += 1) {
      for (let x = building.pos.x - radius; x <= building.pos.x + radius; x += 1) {
        const pos = { x, y };
        if (!city.grid.inBounds(pos)) {
          continue;
        }
        const distance = Math.abs(x - building.pos.x) + Math.abs(y - building.pos.y);
        if (distance <= radius) {
          const tile = city.grid.getTile(pos);
          const waterMultiplier = tile.terrain === 'water' ? 1.5 : 1;
          tile.pollution += Math.max(0, pollution * (1 - distance / (radius + 1))) * waterMultiplier;
        }
      }
    }
  }

  let total = 0;
  city.grid.forEachTile((tile) => {
    total += tile.pollution;
  });
  city.metrics.pollution = Math.round((total / (city.grid.width * city.grid.height)) * 10) / 10;
}

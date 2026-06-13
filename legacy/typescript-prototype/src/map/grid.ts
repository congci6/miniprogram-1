import type { GridPos, GridRect, SerializedCityState, Tile, ZoneType } from '../types';
import { createTile, cloneTile } from './tile';
import { terrainForPosition } from './terrain';

export class CityGrid {
  readonly width: number;
  readonly height: number;
  private readonly tiles: Tile[];

  constructor(width: number, height: number, tiles?: Tile[]) {
    if (width <= 0 || height <= 0) {
      throw new Error('Grid dimensions must be positive.');
    }
    this.width = width;
    this.height = height;
    this.tiles =
      tiles?.map(cloneTile) ??
      Array.from({ length: width * height }, (_, index) => {
        const pos = { x: index % width, y: Math.floor(index / width) };
        return createTile(terrainForPosition(pos, width, height));
      });

    if (this.tiles.length !== width * height) {
      throw new Error('Tile data does not match grid dimensions.');
    }
  }

  static fromSerialized(city: Pick<SerializedCityState, 'width' | 'height' | 'tiles'>): CityGrid {
    return new CityGrid(city.width, city.height, city.tiles);
  }

  inBounds(pos: GridPos): boolean {
    return pos.x >= 0 && pos.y >= 0 && pos.x < this.width && pos.y < this.height;
  }

  rectInBounds(rect: GridRect): boolean {
    return rect.w > 0 && rect.h > 0 && this.inBounds({ x: rect.x, y: rect.y }) && this.inBounds({
      x: rect.x + rect.w - 1,
      y: rect.y + rect.h - 1,
    });
  }

  index(pos: GridPos): number {
    if (!this.inBounds(pos)) {
      throw new Error(`Grid position out of bounds: ${pos.x}, ${pos.y}`);
    }
    return pos.y * this.width + pos.x;
  }

  getTile(pos: GridPos): Tile {
    return this.tiles[this.index(pos)];
  }

  getTileCopy(pos: GridPos): Tile {
    return cloneTile(this.getTile(pos));
  }

  setZone(area: GridRect, zone: ZoneType): void {
    if (!this.rectInBounds(area)) {
      throw new Error('Zone area is outside the map.');
    }
    for (const pos of this.positionsInRect(area)) {
      const tile = this.getTile(pos);
      if (tile.terrain !== 'water') {
        tile.zone = zone;
      }
    }
  }

  canPlaceBuilding(pos: GridPos, size: { w: number; h: number }): { ok: boolean; reason?: string } {
    const rect = { x: pos.x, y: pos.y, w: size.w, h: size.h };
    if (!this.rectInBounds(rect)) {
      return { ok: false, reason: '建筑超出地图边界' };
    }

    for (const tilePos of this.positionsInRect(rect)) {
      const tile = this.getTile(tilePos);
      if (tile.terrain === 'water') {
        return { ok: false, reason: '水面不能建造' };
      }
      if (tile.buildingId) {
        return { ok: false, reason: '地块已有建筑' };
      }
      if (tile.roadId) {
        return { ok: false, reason: '道路上不能建造建筑' };
      }
    }

    return { ok: true };
  }

  occupyBuilding(buildingId: string, pos: GridPos, size: { w: number; h: number }): void {
    const result = this.canPlaceBuilding(pos, size);
    if (!result.ok) {
      throw new Error(result.reason ?? 'Building cannot be placed.');
    }
    for (const tilePos of this.positionsInRect({ x: pos.x, y: pos.y, w: size.w, h: size.h })) {
      this.getTile(tilePos).buildingId = buildingId;
    }
  }

  removeBuilding(buildingId: string): void {
    for (const tile of this.tiles) {
      if (tile.buildingId === buildingId) {
        tile.buildingId = undefined;
      }
    }
  }

  canPlaceRoad(pos: GridPos): boolean {
    if (!this.inBounds(pos)) {
      return false;
    }
    const tile = this.getTile(pos);
    return tile.terrain !== 'water' && !tile.buildingId;
  }

  setRoad(pos: GridPos, roadId: string): void {
    if (!this.canPlaceRoad(pos)) {
      throw new Error('Road cannot be placed on this tile.');
    }
    this.getTile(pos).roadId = roadId;
  }

  removeRoad(pos: GridPos): void {
    if (this.inBounds(pos)) {
      this.getTile(pos).roadId = undefined;
    }
  }

  findBuildingIdAt(pos: GridPos): string | undefined {
    return this.inBounds(pos) ? this.getTile(pos).buildingId : undefined;
  }

  serializeTiles(): Tile[] {
    return this.tiles.map(cloneTile);
  }

  positionsInRect(rect: GridRect): GridPos[] {
    const positions: GridPos[] = [];
    for (let y = rect.y; y < rect.y + rect.h; y += 1) {
      for (let x = rect.x; x < rect.x + rect.w; x += 1) {
        positions.push({ x, y });
      }
    }
    return positions;
  }

  forEachTile(callback: (tile: Tile, pos: GridPos) => void): void {
    for (let y = 0; y < this.height; y += 1) {
      for (let x = 0; x < this.width; x += 1) {
        callback(this.getTile({ x, y }), { x, y });
      }
    }
  }
}

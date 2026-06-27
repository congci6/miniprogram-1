import { GridPos, ZoneType, TerrainType, RoadTier } from '@/types/index';

export interface Tile {
  pos: GridPos; zone: ZoneType; terrain: TerrainType;
  roadId: string; buildingId: string; elevation: number;
}

export class CityGrid {
  readonly width: number; readonly height: number;
  private tiles: Tile[][] = [];

  constructor(width: number, height: number) {
    this.width = width; this.height = height;
    for (let y = 0; y < height; y++) {
      this.tiles[y] = [];
      for (let x = 0; x < width; x++) {
        this.tiles[y][x] = {
          pos: { x, y }, zone: ZoneType.None,
          terrain: TerrainType.Plain, roadId: '',
          buildingId: '', elevation: 0,
        };
      }
    }
  }

  getTile(x: number, y: number): Tile | undefined {
    if (x < 0 || x >= this.width || y < 0 || y >= this.height) return undefined;
    return this.tiles[y][x];
  }

  inBounds(x: number, y: number): boolean {
    return x >= 0 && x < this.width && y >= 0 && y < this.height;
  }

  setZone(x: number, y: number, zone: ZoneType): void {
    const t = this.getTile(x, y); if (t) t.zone = zone;
  }
  setRoad(x: number, y: number, id: string): void {
    const t = this.getTile(x, y); if (t) t.roadId = id;
  }
  setBuilding(x: number, y: number, id: string): void {
    const t = this.getTile(x, y); if (t) t.buildingId = id;
  }
  getTileData(): Tile[][] { return this.tiles; }
}

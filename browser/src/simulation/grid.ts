import { GridPos, ZoneType, TerrainType } from '@/types/index';

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
        const terrain = this.createTerrain(x, y, width, height);
        this.tiles[y][x] = {
          pos: { x, y }, zone: ZoneType.None,
          terrain, roadId: '',
          buildingId: '', elevation: terrain === TerrainType.Hill ? 1 : 0,
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
  setTerrain(x: number, y: number, terrain: TerrainType, elevation = 0): void {
    const t = this.getTile(x, y);
    if (!t) return;
    t.terrain = terrain;
    t.elevation = elevation;
  }

  clearPlanning(x: number, y: number): void {
    const t = this.getTile(x, y);
    if (!t) return;
    t.zone = ZoneType.None;
    t.roadId = '';
    t.buildingId = '';
  }

  getTileData(): Tile[][] { return this.tiles; }

  private createTerrain(x: number, y: number, width: number, height: number): TerrainType {
    const westRiver = x <= 1 && y >= 3 && y <= height - 3;
    const northLake = y <= 1 && x >= 3 && x <= 8;
    const southBend = y >= height - 2 && x >= 2 && x <= 6;
    if (westRiver || northLake || southBend) return TerrainType.Water;

    const eastRidge = x >= width - 3 && y >= 2 && y <= height - 4;
    const northEastHill = x >= width - 6 && y <= 3;
    const scatteredHill = (x === width - 7 && y === 5) || (x === width - 5 && y === height - 6);
    if (eastRidge || northEastHill || scatteredHill) return TerrainType.Hill;

    return TerrainType.Plain;
  }
}

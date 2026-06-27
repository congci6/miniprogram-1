import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { ServiceBuildingId, ZoneType, TerrainType } from '@/types/index';

const SERVICE_MARKER_COLORS: Record<ServiceBuildingId, number> = {
  community_park: 0x8fe06f,
  community_clinic: 0xff7f9f,
  community_school: 0xf2d479,
};

export class IsometricRenderer {
  private scene: Phaser.Scene;
  private sim: CitySimulation;
  private gfx: Phaser.GameObjects.Graphics;
  private hoverTile: { x: number; y: number } | null = null;
  readonly TILE_W = 64;
  readonly TILE_H = 32;

  constructor(scene: Phaser.Scene, sim: CitySimulation) {
    this.scene = scene;
    this.sim = sim;
    this.gfx = scene.add.graphics();
    this.render();
  }

  isoToWorld(tx: number, ty: number): { x: number; y: number } {
    const cx = this.sim.grid.width / 2;
    const cy = this.sim.grid.height / 2;
    const dx = tx - cx, dy = ty - cy;
    return { x: (dx - dy) * (this.TILE_W / 2), y: (dx + dy) * (this.TILE_H / 2) };
  }

  worldToIso(wx: number, wy: number): { x: number; y: number } | null {
    const cx = this.sim.grid.width / 2;
    const cy = this.sim.grid.height / 2;
    const tx = ((wx / (this.TILE_W / 2)) + (wy / (this.TILE_H / 2))) / 2 + cx;
    const ty = ((wy / (this.TILE_H / 2)) - (wx / (this.TILE_W / 2))) / 2 + cy;
    return { x: Math.floor(tx), y: Math.floor(ty) };
  }

  getTileAtWorld(wx: number, wy: number): { x: number; y: number } | null {
    const iso = this.worldToIso(wx, wy);
    if (!iso || !this.sim.grid.inBounds(iso.x, iso.y)) return null;
    return iso;
  }

  setHoverTile(tile: { x: number; y: number } | null): void {
    if (this.hoverTile?.x === tile?.x && this.hoverTile?.y === tile?.y) return;
    this.hoverTile = tile;
    this.render();
  }

  render(): void {
    this.gfx.clear();
    for (let y = 0; y < this.sim.grid.height; y++)
      for (let x = 0; x < this.sim.grid.width; x++)
        this.drawTile(x, y);
  }

  private drawTile(x: number, y: number): void {
    const tile = this.sim.grid.getTile(x, y);
    if (!tile) return;
    const { x: wx, y: wy } = this.isoToWorld(x, y);
    const hw = this.TILE_W / 2, hh = this.TILE_H / 2;

    // diamond top half
    let color = this.getColor(tile.zone, tile.terrain);
    this.gfx.fillStyle(color, 0.85);
    this.gfx.fillTriangle(wx, wy - hh, wx - hw, wy, wx, wy + hh);
    // bottom half
    this.gfx.fillTriangle(wx, wy - hh, wx + hw, wy, wx, wy + hh);

    // border
    this.gfx.lineStyle(1, 0x333333, 0.25);
    this.gfx.strokeRect(wx - hw, wy - hh, this.TILE_W, this.TILE_H);

    if (tile.roadId) this.drawRoad(tile.roadId, wx, wy, hw, hh);

    this.drawServiceMarker(tile.buildingId, wx, wy);

    if (this.hoverTile?.x === x && this.hoverTile.y === y) {
      this.gfx.lineStyle(2, 0xf7f1b5, 0.9);
      this.gfx.strokeRect(wx - hw, wy - hh, this.TILE_W, this.TILE_H);
    }
  }

  private drawServiceMarker(buildingId: string, wx: number, wy: number): void {
    const color = SERVICE_MARKER_COLORS[buildingId as ServiceBuildingId];
    if (!color) return;
    this.gfx.fillStyle(color, 0.95);
    this.gfx.fillCircle(wx, wy - 8, 7);
    this.gfx.lineStyle(2, 0xffffff, 0.7);
    this.gfx.strokeCircle(wx, wy - 8, 7);
  }

  private drawRoad(roadId: string, wx: number, wy: number, hw: number, hh: number): void {
    const arterial = roadId === 'arterial';
    const roadWidth = arterial ? 0.5 : 0.38;
    const roadLength = arterial ? 0.68 : 0.56;
    this.gfx.fillStyle(arterial ? 0x22292f : 0x2f3437, 0.92);
    this.gfx.fillTriangle(wx, wy - hh * roadWidth, wx - hw * roadLength, wy, wx, wy + hh * roadWidth);
    this.gfx.fillTriangle(wx, wy - hh * roadWidth, wx + hw * roadLength, wy, wx, wy + hh * roadWidth);
    this.gfx.lineStyle(arterial ? 2 : 1, arterial ? 0x8ec9ff : 0xf2d479, arterial ? 0.75 : 0.5);
    this.gfx.strokeRect(wx - hw * (arterial ? 0.52 : 0.42), wy - hh * (arterial ? 0.32 : 0.24), this.TILE_W * (arterial ? 1.04 : 0.84), this.TILE_H * (arterial ? 0.64 : 0.48));
  }

  private getColor(zone: ZoneType, terrain: TerrainType): number {
    if (terrain === TerrainType.Water) return 0x2277cc;
    if (terrain === TerrainType.Hill) return 0x7a8651;
    switch (zone) {
      case ZoneType.Residential: return 0x77cc55;
      case ZoneType.Commercial: return 0x4488ff;
      case ZoneType.Industrial: return 0xff8844;
      case ZoneType.Office: return 0xaa88ff;
      case ZoneType.MixedUse: return 0xffcc44;
      case ZoneType.Civic: return 0xff6688;
      case ZoneType.Utility: return 0x888888;
      default: return 0x446633;
    }
  }
}

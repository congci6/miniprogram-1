import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { ServiceBuildingId, ZoneType, TerrainType } from '@/types/index';
import type { Tile } from '@/simulation/grid';

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

    if (!tile.roadId) this.drawZoneMarker(tile, wx, wy);
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

  private drawZoneMarker(tile: Tile, wx: number, wy: number): void {
    if (!tile.buildingId) {
      this.drawVacantZoneMarker(tile.zone, wx, wy);
      return;
    }

    switch (tile.zone) {
      case ZoneType.Residential:
        this.drawResidentialMarker(tile.buildingId, wx, wy);
        return;
      case ZoneType.Commercial:
        this.drawCommercialMarker(wx, wy);
        return;
      case ZoneType.Industrial:
        this.drawIndustrialMarker(wx, wy);
        return;
      case ZoneType.Office:
        this.drawOfficeMarker(wx, wy);
        return;
      case ZoneType.MixedUse:
        this.drawMixedUseMarker(wx, wy);
        return;
      default:
    }
  }

  private drawVacantZoneMarker(zone: ZoneType, wx: number, wy: number): void {
    const color = zone === ZoneType.Residential
      ? 0xd8e6ba
      : zone === ZoneType.Commercial
        ? 0xc7dcff
        : 0xf1c08b;
    this.gfx.fillStyle(color, 0.22);
    this.gfx.fillCircle(wx, wy - 5, 5);
    this.gfx.lineStyle(2, color, 0.65);
    this.gfx.strokeCircle(wx, wy - 5, 5);
  }

  private drawResidentialMarker(buildingId: string, wx: number, wy: number): void {
    const level = this.getResidentialLevel(buildingId);
    const width = 10 + level * 2;
    const height = 7 + level * 2;
    this.gfx.fillStyle(0xf3e2bd, 0.95);
    this.gfx.fillRect(wx - width / 2, wy - height - 2, width, height);
    this.gfx.fillStyle(level >= 3 ? 0xb9473f : 0xc85a44, 0.95);
    this.gfx.fillTriangle(wx - width / 2 - 2, wy - height - 2, wx + width / 2 + 2, wy - height - 2, wx, wy - height - 9);
    if (level >= 2) {
      this.gfx.fillStyle(0x8fc7ff, 0.8);
      this.gfx.fillRect(wx - 3, wy - height + 1, 2, 2);
      this.gfx.fillRect(wx + 2, wy - height + 1, 2, 2);
    }
  }

  private drawCommercialMarker(wx: number, wy: number): void {
    this.gfx.fillStyle(0xd8e7ff, 0.92);
    this.gfx.fillRect(wx - 10, wy - 19, 8, 17);
    this.gfx.fillStyle(0xb5d3ff, 0.92);
    this.gfx.fillRect(wx, wy - 15, 9, 13);
    this.gfx.fillStyle(0x3f6fa9, 0.7);
    this.gfx.fillRect(wx - 8, wy - 15, 4, 2);
    this.gfx.fillRect(wx + 2, wy - 11, 5, 2);
  }

  private drawIndustrialMarker(wx: number, wy: number): void {
    this.gfx.fillStyle(0xd89b62, 0.94);
    this.gfx.fillRect(wx - 11, wy - 11, 18, 9);
    this.gfx.fillStyle(0xb86f45, 0.95);
    this.gfx.fillTriangle(wx - 11, wy - 11, wx - 4, wy - 18, wx + 2, wy - 11);
    this.gfx.fillTriangle(wx - 1, wy - 11, wx + 6, wy - 16, wx + 7, wy - 11);
    this.gfx.fillStyle(0x5d6268, 0.95);
    this.gfx.fillRect(wx + 8, wy - 20, 4, 18);
  }

  private drawMixedUseMarker(wx: number, wy: number): void {
    this.gfx.fillStyle(0xf2ddb0, 0.95);
    this.gfx.fillRect(wx - 10, wy - 18, 9, 16);
    this.gfx.fillStyle(0xd8e7ff, 0.92);
    this.gfx.fillRect(wx, wy - 15, 10, 13);
    this.gfx.fillStyle(0xc85a44, 0.95);
    this.gfx.fillTriangle(wx - 12, wy - 18, wx, wy - 18, wx - 6, wy - 24);
    this.gfx.fillStyle(0x3f6fa9, 0.75);
    this.gfx.fillRect(wx + 3, wy - 11, 4, 2);
  }

  private drawOfficeMarker(wx: number, wy: number): void {
    this.gfx.fillStyle(0xd7ccff, 0.94);
    this.gfx.fillRect(wx - 9, wy - 23, 8, 21);
    this.gfx.fillStyle(0xb9a7f5, 0.94);
    this.gfx.fillRect(wx, wy - 19, 9, 17);
    this.gfx.fillStyle(0x5b4aa0, 0.72);
    for (let row = 0; row < 3; row++) {
      this.gfx.fillRect(wx - 7, wy - 19 + row * 5, 3, 2);
      this.gfx.fillRect(wx + 3, wy - 16 + row * 5, 3, 2);
    }
  }

  private getResidentialLevel(buildingId: string): number {
    const match = /^residential_l([2-3])$/.exec(buildingId);
    return match ? Number(match[1]) : 1;
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

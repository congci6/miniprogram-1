import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { ZoneType, TerrainType } from '@/types/index';

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

    if (tile.roadId) {
      this.gfx.fillStyle(0x2f3437, 0.9);
      this.gfx.fillTriangle(wx, wy - hh * 0.38, wx - hw * 0.56, wy, wx, wy + hh * 0.38);
      this.gfx.fillTriangle(wx, wy - hh * 0.38, wx + hw * 0.56, wy, wx, wy + hh * 0.38);
      this.gfx.lineStyle(1, 0xf2d479, 0.5);
      this.gfx.strokeRect(wx - hw * 0.42, wy - hh * 0.24, this.TILE_W * 0.84, this.TILE_H * 0.48);
    }

    if (this.hoverTile?.x === x && this.hoverTile.y === y) {
      this.gfx.lineStyle(2, 0xf7f1b5, 0.9);
      this.gfx.strokeRect(wx - hw, wy - hh, this.TILE_W, this.TILE_H);
    }
  }

  private getColor(zone: ZoneType, terrain: TerrainType): number {
    if (terrain === TerrainType.Water) return 0x2277cc;
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

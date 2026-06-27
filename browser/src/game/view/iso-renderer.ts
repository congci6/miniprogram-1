import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { ZoneType, TerrainType } from '@/types/index';

export class IsometricRenderer {
  private scene: Phaser.Scene;
  private sim: CitySimulation;
  private gfx: Phaser.GameObjects.Graphics;
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

  handleClick(wx: number, wy: number): void {
    const iso = this.worldToIso(wx, wy);
    if (iso && this.sim.grid.inBounds(iso.x, iso.y))
      console.log(`Click tile (${iso.x}, ${iso.y}) zone=${ZoneType[this.sim.grid.getTile(iso.x, iso.y)!.zone]}`);
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

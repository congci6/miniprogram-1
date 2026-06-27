import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { IsometricRenderer } from '@/game/view/iso-renderer';

export class GameScene extends Phaser.Scene {
  private sim!: CitySimulation;
  private isoRender!: IsometricRenderer;
  private hudTimer = 0;

  constructor() { super({ key: 'GameScene' }); }

  create(): void {
    this.sim = new CitySimulation(24, 18);
    this.isoRender = new IsometricRenderer(this, this.sim);

    this.cameras.main.setZoom(1.8);
    this.cameras.main.centerOn(0, 0);

    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => {
      const wp = this.cameras.main.getWorldPoint(p.x, p.y);
      this.isoRender.handleClick(wp.x, wp.y);
    });
  }

  update(_time: number, delta: number): void {
    this.sim.tick(delta / 1000);
    this.hudTimer += delta / 1000;
    if (this.hudTimer >= 0.5) {
      this.hudTimer = 0;
      window.dispatchEvent(new CustomEvent('city-metrics-update', {
        detail: { metrics: this.sim.metrics },
      }));
    }
  }
}

import * as Phaser from 'phaser';
import { CitySimulation } from '@/simulation/city-simulation';
import { IsometricRenderer } from '@/game/view/iso-renderer';
import { MaterialId, PlanningTool } from '@/types/index';

export class GameScene extends Phaser.Scene {
  private sim!: CitySimulation;
  private isoRender!: IsometricRenderer;
  private hudTimer = 0;
  private selectedTool: PlanningTool = 'inspect';
  private selectedTile: { x: number; y: number } | null = null;
  private paintedThisDrag = new Set<string>();

  constructor() { super({ key: 'GameScene' }); }

  create(): void {
    this.sim = new CitySimulation(24, 18);
    this.isoRender = new IsometricRenderer(this, this.sim);

    this.cameras.main.setZoom(1.8);
    this.cameras.main.centerOn(0, 0);

    window.addEventListener('city-tool-change', ((event: Event) => {
      this.selectedTool = (event as CustomEvent<{ tool: PlanningTool }>).detail.tool;
      this.paintedThisDrag.clear();
      this.publishMetrics();
    }) as EventListener);
    window.addEventListener('city-production-start', ((event: Event) => {
      const materialId = (event as CustomEvent<{ materialId: MaterialId }>).detail.materialId;
      const result = this.sim.startProduction(materialId);
      this.publishMetrics(result.message);
    }) as EventListener);
    window.addEventListener('city-order-fulfill', ((event: Event) => {
      const orderId = (event as CustomEvent<{ orderId: string }>).detail.orderId;
      const result = this.sim.fulfillOrder(orderId);
      this.publishMetrics(result.message);
    }) as EventListener);
    window.addEventListener('city-upgrade-selected-residential', () => {
      if (!this.selectedTile) {
        this.publishMetrics('请先选择一个住宅地块');
        return;
      }
      const result = this.sim.upgradeResidentialAt(this.selectedTile.x, this.selectedTile.y);
      if (result.changed) this.isoRender.render();
      window.dispatchEvent(new CustomEvent('city-tile-selected', {
        detail: { tile: this.sim.grid.getTile(this.selectedTile.x, this.selectedTile.y), message: result.message },
      }));
      this.publishMetrics(result.message);
    });

    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => this.applyToolAtPointer(p));
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => {
      const tile = this.tileFromPointer(p);
      this.isoRender.setHoverTile(tile);
      if (p.isDown && this.selectedTool !== 'inspect') this.applyToolAtPointer(p);
    });
    this.input.on('pointerup', () => this.paintedThisDrag.clear());

    this.publishMetrics('选择工具后点击地块开始规划');
  }

  update(_time: number, delta: number): void {
    this.sim.tick(delta / 1000);
    this.hudTimer += delta / 1000;
    if (this.hudTimer >= 0.5) {
      this.hudTimer = 0;
      this.publishMetrics();
    }
  }

  private applyToolAtPointer(pointer: Phaser.Input.Pointer): void {
    const tile = this.tileFromPointer(pointer);
    if (!tile) return;

    const paintKey = `${this.selectedTool}:${tile.x}:${tile.y}`;
    if (this.selectedTool !== 'inspect' && this.paintedThisDrag.has(paintKey)) return;
    this.paintedThisDrag.add(paintKey);

    const result = this.sim.applyTool(tile.x, tile.y, this.selectedTool);
    const selectedTile = this.sim.grid.getTile(tile.x, tile.y);
    this.selectedTile = tile;
    if (result.changed) this.isoRender.render();

    window.dispatchEvent(new CustomEvent('city-tile-selected', {
      detail: { tile: selectedTile, message: result.message },
    }));
    this.publishMetrics(result.message);
  }

  private tileFromPointer(pointer: Phaser.Input.Pointer): { x: number; y: number } | null {
    const worldPoint = this.cameras.main.getWorldPoint(pointer.x, pointer.y);
    return this.isoRender.getTileAtWorld(worldPoint.x, worldPoint.y);
  }

  private publishMetrics(message = ''): void {
    window.dispatchEvent(new CustomEvent('city-metrics-update', {
      detail: {
        metrics: this.sim.metrics,
        materials: this.sim.materials,
        productionQueue: this.sim.productionQueue,
        productionSlots: this.sim.getProductionSlots(),
        storageUsed: this.sim.getStorageUsed(),
        storageCapacity: this.sim.getStorageCapacity(),
        orders: this.sim.orders,
        completedOrders: this.sim.completedOrders,
        selectedTool: this.selectedTool,
        message,
      },
    }));
  }
}

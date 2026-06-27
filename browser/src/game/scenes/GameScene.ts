import * as Phaser from 'phaser';
import { CityOfflineProgressResult, CitySimulation, CitySimulationSaveData } from '@/simulation/city-simulation';
import { IsometricRenderer } from '@/game/view/iso-renderer';
import { CityTaxLevel, MaterialId, PlanningTool } from '@/types/index';

const BROWSER_SAVE_KEY = 'pocket-city-planner-browser-save';
const MATERIAL_LABELS: Record<MaterialId, string> = {
  wood: '木材',
  metal: '金属',
  plastic: '塑料',
};
const MIN_CAMERA_ZOOM = 0.9;
const MAX_CAMERA_ZOOM = 2.4;

export class GameScene extends Phaser.Scene {
  private sim!: CitySimulation;
  private isoRender!: IsometricRenderer;
  private hudTimer = 0;
  private saveTimer = 0;
  private selectedTool: PlanningTool = 'inspect';
  private selectedTile: { x: number; y: number } | null = null;
  private paintedThisDrag = new Set<string>();
  private isCameraPanning = false;
  private panStart: { pointerX: number; pointerY: number; scrollX: number; scrollY: number } | null = null;

  constructor() { super({ key: 'GameScene' }); }

  create(): void {
    this.sim = new CitySimulation(24, 18);
    const restoreMessage = this.restore();
    this.isoRender = new IsometricRenderer(this, this.sim);

    this.cameras.main.setZoom(1.8);
    this.cameras.main.centerOn(0, 0);
    this.input.mouse?.disableContextMenu();
    window.addEventListener('beforeunload', () => this.save());

    window.addEventListener('city-tool-change', ((event: Event) => {
      this.selectedTool = (event as CustomEvent<{ tool: PlanningTool }>).detail.tool;
      this.paintedThisDrag.clear();
      this.publishMetrics();
    }) as EventListener);
    window.addEventListener('city-production-start', ((event: Event) => {
      const materialId = (event as CustomEvent<{ materialId: MaterialId }>).detail.materialId;
      const result = this.sim.startProduction(materialId);
      if (result.changed) this.save();
      this.publishMetrics(result.message);
    }) as EventListener);
    window.addEventListener('city-order-fulfill', ((event: Event) => {
      const orderId = (event as CustomEvent<{ orderId: string }>).detail.orderId;
      const result = this.sim.fulfillOrder(orderId);
      if (result.changed) this.save();
      this.publishMetrics(result.message);
    }) as EventListener);
    window.addEventListener('city-tax-level-change', ((event: Event) => {
      const level = (event as CustomEvent<{ level: CityTaxLevel }>).detail.level;
      const result = this.sim.setTaxLevel(level);
      if (result.changed) this.save();
      this.publishMetrics(result.message);
    }) as EventListener);
    window.addEventListener('city-upgrade-selected-residential', () => {
      if (!this.selectedTile) {
        this.publishMetrics('请先选择一个住宅地块');
        return;
      }
      const result = this.sim.upgradeResidentialAt(this.selectedTile.x, this.selectedTile.y);
      if (result.changed) this.isoRender.render();
      if (result.changed) this.save();
      window.dispatchEvent(new CustomEvent('city-tile-selected', {
        detail: { tile: this.sim.grid.getTile(this.selectedTile.x, this.selectedTile.y), message: result.message },
      }));
      this.publishMetrics(result.message);
    });
    window.addEventListener('city-upgrade-selected-road', () => {
      if (!this.selectedTile) {
        this.publishMetrics('请先选择一段道路');
        return;
      }
      const result = this.sim.upgradeRoadAt(this.selectedTile.x, this.selectedTile.y);
      if (result.changed) this.isoRender.render();
      if (result.changed) this.save();
      window.dispatchEvent(new CustomEvent('city-tile-selected', {
        detail: { tile: this.sim.grid.getTile(this.selectedTile.x, this.selectedTile.y), message: result.message },
      }));
      this.publishMetrics(result.message);
    });

    this.input.on('pointerdown', (p: Phaser.Input.Pointer) => {
      if (this.shouldPanCamera(p)) {
        this.startCameraPan(p);
        return;
      }
      this.applyToolAtPointer(p);
    });
    this.input.on('pointermove', (p: Phaser.Input.Pointer) => {
      if (this.isCameraPanning) {
        this.updateCameraPan(p);
        return;
      }
      const tile = this.tileFromPointer(p);
      this.isoRender.setHoverTile(tile);
      if (p.isDown && this.selectedTool !== 'inspect') this.applyToolAtPointer(p);
    });
    this.input.on('pointerup', () => {
      this.isCameraPanning = false;
      this.panStart = null;
      this.paintedThisDrag.clear();
    });
    this.input.on('wheel', (_pointer: Phaser.Input.Pointer, _objects: Phaser.GameObjects.GameObject[], _dx: number, dy: number) => {
      this.zoomCamera(dy);
    });

    this.publishMetrics(restoreMessage || '选择工具后点击地块开始规划');
  }

  update(_time: number, delta: number): void {
    const simulationChanged = this.sim.tick(delta / 1000);
    if (simulationChanged) {
      this.isoRender.render();
      this.save();
    }
    this.hudTimer += delta / 1000;
    this.saveTimer += delta / 1000;
    if (this.hudTimer >= 0.5) {
      this.hudTimer = 0;
      this.publishMetrics();
    }
    if (this.saveTimer >= 5) {
      this.saveTimer = 0;
      this.save();
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
    if (result.changed) this.save();

    window.dispatchEvent(new CustomEvent('city-tile-selected', {
      detail: { tile: selectedTile, message: result.message },
    }));
    this.publishMetrics(result.message);
  }

  private shouldPanCamera(pointer: Phaser.Input.Pointer): boolean {
    return pointer.rightButtonDown() || pointer.middleButtonDown();
  }

  private startCameraPan(pointer: Phaser.Input.Pointer): void {
    this.isCameraPanning = true;
    this.panStart = {
      pointerX: pointer.x,
      pointerY: pointer.y,
      scrollX: this.cameras.main.scrollX,
      scrollY: this.cameras.main.scrollY,
    };
    this.paintedThisDrag.clear();
  }

  private updateCameraPan(pointer: Phaser.Input.Pointer): void {
    if (!this.panStart) return;
    const zoom = this.cameras.main.zoom;
    this.cameras.main.scrollX = this.panStart.scrollX - (pointer.x - this.panStart.pointerX) / zoom;
    this.cameras.main.scrollY = this.panStart.scrollY - (pointer.y - this.panStart.pointerY) / zoom;
  }

  private zoomCamera(deltaY: number): void {
    const currentZoom = this.cameras.main.zoom;
    const nextZoom = Phaser.Math.Clamp(currentZoom + (deltaY > 0 ? -0.12 : 0.12), MIN_CAMERA_ZOOM, MAX_CAMERA_ZOOM);
    this.cameras.main.setZoom(nextZoom);
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
        objectives: this.sim.getObjectives(),
        unlockState: this.sim.getUnlockState(),
        selectedTool: this.selectedTool,
        message,
      },
    }));
  }

  private restore(): string {
    try {
      const raw = window.localStorage.getItem(BROWSER_SAVE_KEY);
      if (!raw) return '';
      const data: unknown = JSON.parse(raw);
      if (!this.isSaveData(data)) return '';
      const offline = this.sim.restoreSnapshot(data);
      this.save();
      return this.formatOfflineMessage(offline) || '已读取本地城市存档';
    } catch (error) {
      console.warn('Failed to restore browser city save', error);
      return '';
    }
  }

  private save(): void {
    try {
      window.localStorage.setItem(BROWSER_SAVE_KEY, JSON.stringify(this.sim.createSnapshot()));
    } catch (error) {
      console.warn('Failed to save browser city', error);
    }
  }

  private isSaveData(value: unknown): value is CitySimulationSaveData {
    if (!value || typeof value !== 'object') return false;
    const candidate = value as Partial<CitySimulationSaveData>;
    return (candidate.version === 1 || candidate.version === 2 || candidate.version === 3)
      && Array.isArray(candidate.tiles)
      && typeof candidate.metrics === 'object';
  }

  private formatOfflineMessage(result: CityOfflineProgressResult): string {
    if (result.daysElapsed <= 0) return '';
    const produced = (Object.entries(result.materialsProduced) as Array<[MaterialId, number]>)
      .filter(([, count]) => count > 0)
      .map(([materialId, count]) => `${MATERIAL_LABELS[materialId]}x${count}`)
      .join('、');
    const suffixes = [
      produced ? `产出 ${produced}` : '',
      result.storageBlocked ? '仓库已满，生产暂停' : '',
      result.capped ? '已达到离线结算上限' : '',
    ].filter(Boolean);
    return `离线推进 ${result.daysElapsed} 天${suffixes.length ? '，' + suffixes.join('，') : ''}`;
  }
}

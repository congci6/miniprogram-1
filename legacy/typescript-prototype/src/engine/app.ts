import { buildingIdForTool } from '../ui/build-menu';
import type * as THREE from 'three';
import { HudController, type HudAction } from '../ui/hud';
import { ToastQueue } from '../ui/toast';
import { TOOLBAR_ITEMS, toolUnlockStatus, type BuildToolId } from '../ui/toolbar';
import { CameraRig } from './camera';
import { FrameLoop } from './frame-loop';
import { InputController } from './input';
import { createRenderer } from './renderer';
import { createRuntimeCanvas, getWx, type RuntimeCanvas } from '../platform/wx-canvas';
import { LocalStorageAdapter } from '../platform/wx-storage';
import { registerShareEntry } from '../platform/wx-share';
import { CityState } from '../simulation/city-state';
import { previewConstruction, type ConstructionPreview } from '../simulation/construction-preview';
import { createSave, deserializeSave, serializeSave } from '../simulation/save';
import { tickCity } from '../simulation/tick';
import type { GameCommand, GridPos, OverlayMode } from '../types';
import { CityScene } from '../view/city-scene';
import { OverlayLayer } from '../view/overlay-layer';

const SAVE_KEY = 'pocket-city-planner-save-v1';

type LifecycleWx = {
  onHide?: (callback: () => void) => void;
  onShow?: (callback: () => void) => void;
};

export class CityGameApp {
  private runtime!: RuntimeCanvas;
  private renderer!: THREE.WebGLRenderer;
  private cameraRig!: CameraRig;
  private input!: InputController;
  private city!: CityState;
  private cityScene!: CityScene;
  private overlay!: OverlayLayer;
  private readonly hud = new HudController();
  private readonly toast = new ToastQueue();
  private readonly storage = new LocalStorageAdapter();
  private readonly loop = new FrameLoop();
  private selectedTool: BuildToolId = 'residential_pod';
  private overlayMode: OverlayMode = 'normal';
  private readonly knownUnlockedTools = new Set<BuildToolId>();
  private buildPreview?: ConstructionPreview;
  private pendingConfirmation?: { tool: BuildToolId; pos: GridPos };
  private roadAnchor?: GridPos;
  private lastAutosave = 0;

  start(): void {
    this.runtime = createRuntimeCanvas();
    this.renderer = createRenderer(this.runtime);
    this.cameraRig = new CameraRig(this.runtime.width, this.runtime.height);
    this.city = this.loadCity();
    this.rememberUnlockedTools();
    this.cityScene = new CityScene(this.city);
    this.overlay = new OverlayLayer(this.runtime.width, this.runtime.height, this.hud);
    this.input = new InputController(this.runtime, {
      onTap: (x, y) => this.handleTap(x, y),
      onDrag: (dx, dy) => this.cameraRig.pan(dx, dy),
      onPinch: (scale) => this.cameraRig.zoomBy(scale),
    });

    registerShareEntry();
    this.registerLifecycle();
    this.input.attach();
    this.toast.show('选择底部工具，在地图上建造城市');
    this.loop.start((deltaSeconds, now) => this.frame(deltaSeconds, now));
  }

  private frame(deltaSeconds: number, now: number): void {
    tickCity(this.city, deltaSeconds);
    this.announceNewUnlocks();
    if (this.overlayMode !== 'normal') {
      this.cityScene.syncOverlay(this.city);
    }
    this.renderer.render(this.cityScene.scene, this.cameraRig.camera);
    this.overlay.update({
      metrics: this.city.metrics,
      selectedTool: this.selectedTool,
      overlayMode: this.overlayMode,
      buildPreview: this.buildPreview,
      toast: this.toast.current(now),
      roadAnchor: this.roadAnchor ? `${this.roadAnchor.x},${this.roadAnchor.y}` : undefined,
    });
    this.overlay.render(this.renderer);

    if (now - this.lastAutosave > 15000) {
      this.saveCity(false);
      this.lastAutosave = now;
    }
  }

  private handleTap(x: number, y: number): void {
    const action = this.hud.hitTest(x, y);
    if (action) {
      this.handleHudAction(action);
      return;
    }

    const gridPos = this.cityScene.pickGrid(x, y, this.runtime.width, this.runtime.height, this.cameraRig.camera);
    if (!gridPos) {
      return;
    }
    this.cityScene.setSelection(gridPos);

    if (this.selectedTool === 'road') {
      this.buildPreview = previewConstruction(this.city, { type: 'road', from: this.roadAnchor }, gridPos);
      if (!this.roadAnchor) {
        if (!this.buildPreview.ok) {
          this.toast.show(this.buildPreview.lines[0] ?? '这里不能作为道路起点');
          return;
        }
        this.roadAnchor = gridPos;
        this.toast.show('已选择道路起点，再点一次确定终点');
        return;
      }
      if (!this.buildPreview.ok) {
        this.toast.show(this.buildPreview.lines[0] ?? '道路方案不可行');
        return;
      }
      this.runCommand({ type: 'BUILD_ROAD', from: this.roadAnchor, to: gridPos });
      this.roadAnchor = undefined;
      this.clearPlacementPreview();
      return;
    }

    if (this.selectedTool === 'demolish') {
      this.previewOrConfirm({ type: 'DEMOLISH', pos: gridPos }, gridPos);
      return;
    }

    const buildingId = buildingIdForTool(this.selectedTool);
    if (buildingId) {
      this.previewOrConfirm({ type: 'PLACE_BUILDING', buildingId, pos: gridPos }, gridPos);
    }
  }

  private handleHudAction(action: HudAction): void {
    switch (action.type) {
      case 'select-tool':
        {
          const unlock = toolUnlockStatus(action.tool, this.city.metrics);
          if (!unlock.unlocked) {
            this.toast.show(`${toolName(action.tool)}未解锁，${unlock.reason}`);
            break;
          }
        }
        this.selectedTool = action.tool;
        this.roadAnchor = undefined;
        this.clearPlacementPreview();
        this.toast.show(`已选择 ${toolName(action.tool)}`);
        break;
      case 'save':
        this.saveCity(true);
        break;
      case 'cycle-overlay':
        this.cycleOverlayMode();
        break;
      case 'new-city':
        this.city = CityState.createNew();
        this.cityScene = new CityScene(this.city);
        this.cityScene.setOverlayMode(this.overlayMode, this.city);
        this.selectedTool = 'residential_pod';
        this.roadAnchor = undefined;
        this.clearPlacementPreview();
        this.rememberUnlockedTools();
        this.toast.show('已创建新城市');
        break;
    }
  }

  private runCommand(command: GameCommand): void {
    const result = this.city.execute(command);
    this.toast.show(result.message);
    if (result.ok) {
      this.cityScene.sync(this.city);
      this.saveCity(false);
    }
  }

  private loadCity(): CityState {
    const raw = this.storage.getItem(SAVE_KEY);
    if (!raw) {
      return CityState.createNew();
    }
    try {
      const city = deserializeSave(raw);
      city.ensureStarterBuildings();
      city.recomputeMetrics();
      return city;
    } catch (error) {
      console.warn('Save is corrupted, creating a new city.', error);
      this.storage.removeItem(SAVE_KEY);
      return CityState.createNew();
    }
  }

  private saveCity(showToast: boolean): void {
    this.storage.setItem(SAVE_KEY, serializeSave(createSave(this.city)));
    if (showToast) {
      this.toast.show('城市已保存');
    }
  }

  private registerLifecycle(): void {
    const wx = getWx() as LifecycleWx | undefined;
    wx?.onHide?.(() => this.saveCity(false));
    wx?.onShow?.(() => this.toast.show('欢迎回来，城市已恢复'));
  }

  private cycleOverlayMode(): void {
    this.overlayMode =
      this.overlayMode === 'normal' ? 'traffic' : this.overlayMode === 'traffic' ? 'pollution' : 'normal';
    this.cityScene.setOverlayMode(this.overlayMode, this.city);
    this.toast.show(`已切换到${overlayName(this.overlayMode)}`);
  }

  private previewOrConfirm(command: GameCommand, pos: GridPos): void {
    const target =
      command.type === 'PLACE_BUILDING'
        ? { type: 'building' as const, buildingId: command.buildingId }
        : { type: 'demolish' as const };
    this.buildPreview = previewConstruction(this.city, target, pos);
    const isSamePending =
      this.pendingConfirmation?.tool === this.selectedTool &&
      this.pendingConfirmation.pos.x === pos.x &&
      this.pendingConfirmation.pos.y === pos.y;

    if (!this.buildPreview.ok) {
      this.pendingConfirmation = undefined;
      this.toast.show(this.buildPreview.lines[0] ?? '方案不可行');
      return;
    }

    if (!isSamePending) {
      this.pendingConfirmation = { tool: this.selectedTool, pos: { ...pos } };
      this.toast.show(`${this.buildPreview.confirmLabel}预览，再次点击确认`);
      return;
    }

    this.runCommand(command);
    this.clearPlacementPreview();
  }

  private clearPlacementPreview(): void {
    this.buildPreview = undefined;
    this.pendingConfirmation = undefined;
  }

  private rememberUnlockedTools(): void {
    this.knownUnlockedTools.clear();
    for (const item of TOOLBAR_ITEMS) {
      if (toolUnlockStatus(item.id, this.city.metrics).unlocked) {
        this.knownUnlockedTools.add(item.id);
      }
    }
  }

  private announceNewUnlocks(): void {
    for (const item of TOOLBAR_ITEMS) {
      const status = toolUnlockStatus(item.id, this.city.metrics);
      if (status.unlocked && !this.knownUnlockedTools.has(item.id)) {
        this.knownUnlockedTools.add(item.id);
        this.toast.show(`${item.label}已解锁`);
      }
    }
  }
}

export function bootGame(): void {
  const app = new CityGameApp();
  app.start();
}

function toolName(tool: BuildToolId): string {
  switch (tool) {
    case 'road':
      return '道路';
    case 'residential_pod':
      return '住宅';
    case 'market_corner':
      return '商业';
    case 'maker_yard':
      return '工业';
    case 'pocket_park':
      return '公园';
    case 'micro_power':
      return '电力';
    case 'water_tower':
      return '水务';
    case 'demolish':
      return '拆除';
  }
}

function overlayName(mode: OverlayMode): string {
  switch (mode) {
    case 'normal':
      return '普通视图';
    case 'traffic':
      return '交通图层';
    case 'pollution':
      return '污染图层';
  }
}

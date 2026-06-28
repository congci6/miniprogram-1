import { CityOfflineProgressResult, CitySimulation, type CitySimulationSaveData } from '@/simulation/city-simulation';
import { CityPolicy, CityPolicyImpactPreview, CityTimeScale, CityTaxLevel, CityUnlockActionId, MaterialCost, MaterialId, PlanningTool, ServiceBuildingId, TerrainType, ZoneType } from '@/types/index';
import type { Tile } from '@/simulation/grid';

declare const wx: WeChatRuntime | undefined;
declare const GameGlobal: Record<string, unknown> | undefined;

interface WeChatRuntime {
  createCanvas(): WeChatCanvas;
  getSystemInfoSync(): { windowWidth: number; windowHeight: number; pixelRatio?: number };
  onTouchStart(callback: (event: WeChatTouchEvent) => void): void;
  onTouchMove(callback: (event: WeChatTouchEvent) => void): void;
  onTouchEnd(callback: (event: WeChatTouchEvent) => void): void;
  onHide?(callback: () => void): void;
  onShow?(callback: () => void): void;
  setStorageSync?(key: string, value: unknown): void;
  getStorageSync?(key: string): unknown;
  vibrateShort?(options?: { type?: FeedbackType }): void;
}

interface WeChatCanvas {
  width: number;
  height: number;
  getContext(type: '2d'): CanvasRenderingContext2D;
  requestAnimationFrame?(callback: FrameRequestCallback): number;
}

interface WeChatTouchEvent {
  touches?: Array<{ clientX: number; clientY: number }>;
  changedTouches?: Array<{ clientX: number; clientY: number }>;
}

interface ToolButton {
  tool: PlanningTool;
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
}

interface ActionButton {
  kind: 'produce' | 'fulfillOrder' | 'upgrade' | 'upgradeRoad' | 'tax' | 'timeScale' | 'policy';
  label: string;
  lockedMessage?: string;
  x: number;
  y: number;
  width: number;
  height: number;
  materialId?: MaterialId;
  orderId?: string;
  taxLevel?: CityTaxLevel;
  timeScale?: CityTimeScale;
  policy?: CityPolicy;
  selected?: boolean;
}

type FeedbackType = 'light' | 'medium' | 'heavy';

interface SaveOptions {
  announce?: boolean;
  feedback?: FeedbackType;
}

interface RestoreOptions {
  announceMissing?: boolean;
  feedback?: FeedbackType;
  resave?: boolean;
}

interface StorageReadResult {
  status: 'ok' | 'unavailable' | 'failed';
  value?: unknown;
}

const RUNTIME_MARKER = 'NON_UNITY_WECHAT_CANVAS_RUNTIME';
const SAVE_KEY = 'pocket-city-planner-save-v1';
const TILE_W = 48;
const TILE_H = 24;
const GRID_W = 24;
const GRID_H = 18;
const MIN_VIEWPORT_SCALE = 0.65;
const MAX_VIEWPORT_SCALE = 1.65;
const TOOL_LABELS: Record<PlanningTool, string> = {
  inspect: '查看',
  road: '道路',
  residential: '住宅',
  commercial: '商业',
  industrial: '工业',
  park: '公园',
  clinic: '诊所',
  school: '学校',
  erase: '清理',
};
const TOOLS: PlanningTool[] = ['inspect', 'road', 'residential', 'commercial', 'industrial', 'park', 'clinic', 'school', 'erase'];
const SERVICE_TOOL_TO_BUILDING: Partial<Record<PlanningTool, ServiceBuildingId>> = {
  park: 'community_park',
  clinic: 'community_clinic',
  school: 'community_school',
};
const MATERIAL_LABELS: Record<MaterialId, string> = {
  wood: '木材',
  metal: '金属',
  plastic: '塑料',
};
const TAX_LABELS: Record<CityTaxLevel, string> = {
  [CityTaxLevel.Low]: '低税',
  [CityTaxLevel.Normal]: '标准',
  [CityTaxLevel.High]: '高税',
};
const TIME_SCALE_LABELS: Record<CityTimeScale, string> = {
  0: '暂停',
  1: '1x',
  2: '2x',
  4: '4x',
};
const SERVICE_MARKER_COLORS: Record<string, string> = {
  community_park: '#8fe06f',
  community_clinic: '#ff7f9f',
  community_school: '#f2d479',
};

class WeChatCityGame {
  private readonly canvas: WeChatCanvas;
  private readonly ctx: CanvasRenderingContext2D;
  private readonly dpr: number;
  private readonly sim = new CitySimulation(GRID_W, GRID_H);
  private readonly buttons: ToolButton[] = [];
  private readonly actionButtons: ActionButton[] = [];
  private selectedTool: PlanningTool = 'inspect';
  private selectedTile: Tile | null = null;
  private timeScale: CityTimeScale = 1;
  private policyPreview: CityPolicyImpactPreview | null = null;
  private statusText = '选择工具后点击地块开始规划';
  private lastPaintKey = '';
  private lastTime = Date.now();
  private width: number;
  private height: number;
  private originX: number;
  private originY: number;
  private viewportScale = 1;
  private touchMode: 'none' | 'paint' | 'pan' | 'pinch' = 'none';
  private panStart: { touchX: number; touchY: number; originX: number; originY: number; moved: boolean } | null = null;
  private pinchStart: { distance: number; scale: number; originX: number; originY: number; centerX: number; centerY: number } | null = null;

  constructor(private readonly runtime: WeChatRuntime) {
    const info = runtime.getSystemInfoSync();
    this.dpr = Math.max(1, info.pixelRatio ?? 1);
    this.width = info.windowWidth;
    this.height = info.windowHeight;
    this.canvas = runtime.createCanvas();
    this.canvas.width = Math.floor(this.width * this.dpr);
    this.canvas.height = Math.floor(this.height * this.dpr);
    this.ctx = this.canvas.getContext('2d');
    this.originX = this.width / 2;
    this.originY = Math.max(70, this.height * 0.2);

    this.restore();
    this.layoutTools();
    this.layoutActionButtons();
    this.bindInput();
    this.startLoop();
  }

  private bindInput(): void {
    this.runtime.onTouchStart((event) => this.handleTouch(event, true));
    this.runtime.onTouchMove((event) => this.handleTouch(event, false));
    this.runtime.onTouchEnd((event) => this.handleTouchEnd(event));
    this.runtime.onHide?.(() => this.save({ announce: true, feedback: 'medium' }));
    this.runtime.onShow?.(() => {
      this.restore({ announceMissing: true, feedback: 'light' });
    });
  }

  private startLoop(): void {
    const requestFrame = this.canvas.requestAnimationFrame?.bind(this.canvas)
      ?? globalThis.requestAnimationFrame?.bind(globalThis)
      ?? ((callback: FrameRequestCallback) => globalThis.setTimeout(() => callback(Date.now()), 16));

    const frame = (): void => {
      const now = Date.now();
      const delta = Math.min(0.25, (now - this.lastTime) / 1000);
      this.lastTime = now;
      if (this.timeScale > 0 && this.sim.tick(delta * this.timeScale)) this.save();
      this.draw();
      requestFrame(frame);
    };

    requestFrame(frame);
  }

  private handleTouch(event: WeChatTouchEvent, allowToolSwitch: boolean): void {
    if ((event.touches?.length ?? 0) >= 2) {
      this.handlePinch(event.touches!);
      return;
    }

    const touch = event.touches?.[0] ?? event.changedTouches?.[0];
    if (!touch) return;
    const x = touch.clientX;
    const y = touch.clientY;

    if (allowToolSwitch) {
      const button = this.buttons.find((candidate) => this.pointInRect(x, y, candidate));
      if (button) {
        const lockedMessage = this.toolLockedMessage(button.tool);
        if (lockedMessage) {
          this.statusText = lockedMessage;
          this.vibrate('light');
          return;
        }
        this.selectedTool = button.tool;
        this.statusText = `当前工具: ${button.label}`;
        this.vibrate('light');
        return;
      }

      const actionButton = this.actionButtons.find((candidate) => this.pointInRect(x, y, candidate));
      if (actionButton) {
        if (actionButton.lockedMessage) {
          this.statusText = actionButton.lockedMessage;
          this.vibrate('light');
          return;
        }
        this.handleAction(actionButton);
        return;
      }

      if (this.selectedTool === 'inspect') {
        this.startPan(x, y);
        return;
      }
    }

    if (this.touchMode === 'pan') {
      this.updatePan(x, y);
      return;
    }

    if (this.touchMode === 'pinch') return;

    this.touchMode = 'paint';
    this.applyToolAtScreen(x, y);
  }

  private handleTouchEnd(event: WeChatTouchEvent): void {
    const touch = event.changedTouches?.[0] ?? event.touches?.[0];
    if (this.touchMode === 'pan' && this.panStart && !this.panStart.moved && touch) {
      this.applyToolAtScreen(touch.clientX, touch.clientY);
    }
    this.touchMode = 'none';
    this.panStart = null;
    this.pinchStart = null;
    this.lastPaintKey = '';
    this.save();
  }

  private applyToolAtScreen(x: number, y: number): void {
    const tilePos = this.worldToTile(x, y);
    if (!tilePos || !this.sim.grid.inBounds(tilePos.x, tilePos.y)) return;

    const paintKey = `${this.selectedTool}:${tilePos.x}:${tilePos.y}`;
    if (paintKey === this.lastPaintKey && this.selectedTool !== 'inspect') return;
    this.lastPaintKey = paintKey;

    const result = this.sim.applyTool(tilePos.x, tilePos.y, this.selectedTool);
    this.selectedTile = this.sim.grid.getTile(tilePos.x, tilePos.y) ?? null;
    this.statusText = result.message;
    if (result.changed) {
      this.vibrate('medium');
      this.save();
    }
  }

  private startPan(x: number, y: number): void {
    this.touchMode = 'pan';
    this.panStart = { touchX: x, touchY: y, originX: this.originX, originY: this.originY, moved: false };
  }

  private updatePan(x: number, y: number): void {
    if (!this.panStart) return;
    const dx = x - this.panStart.touchX;
    const dy = y - this.panStart.touchY;
    if (Math.abs(dx) + Math.abs(dy) > 8) this.panStart.moved = true;
    this.originX = this.panStart.originX + dx;
    this.originY = this.panStart.originY + dy;
  }

  private handlePinch(touches: Array<{ clientX: number; clientY: number }>): void {
    const first = touches[0];
    const second = touches[1];
    const centerX = (first.clientX + second.clientX) / 2;
    const centerY = (first.clientY + second.clientY) / 2;
    const distance = Math.hypot(first.clientX - second.clientX, first.clientY - second.clientY);
    if (this.touchMode !== 'pinch' || !this.pinchStart) {
      this.touchMode = 'pinch';
      this.pinchStart = { distance, scale: this.viewportScale, originX: this.originX, originY: this.originY, centerX, centerY };
      return;
    }

    const nextScale = this.clampViewportScale(this.pinchStart.scale * (distance / Math.max(1, this.pinchStart.distance)));
    const mapX = (this.pinchStart.centerX - this.pinchStart.originX) / this.pinchStart.scale;
    const mapY = (this.pinchStart.centerY - this.pinchStart.originY) / this.pinchStart.scale;
    this.viewportScale = nextScale;
    this.originX = centerX - mapX * nextScale;
    this.originY = centerY - mapY * nextScale;
  }

  private clampViewportScale(value: number): number {
    return Math.max(MIN_VIEWPORT_SCALE, Math.min(MAX_VIEWPORT_SCALE, value));
  }

  private draw(): void {
    this.ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    this.ctx.clearRect(0, 0, this.width, this.height);
    this.drawBackground();
    this.drawGrid();
    this.drawTopBar();
    this.drawSidePanel();
    this.drawManagementPanel();
    this.drawToolBar();
    this.drawStatus();
  }

  private drawBackground(): void {
    const gradient = this.ctx.createLinearGradient(0, 0, 0, this.height);
    gradient.addColorStop(0, '#14241f');
    gradient.addColorStop(1, '#1f2436');
    this.ctx.fillStyle = gradient;
    this.ctx.fillRect(0, 0, this.width, this.height);
  }

  private drawGrid(): void {
    this.ctx.save();
    this.ctx.translate(this.originX, this.originY);
    this.ctx.scale(this.viewportScale, this.viewportScale);
    for (let y = 0; y < this.sim.grid.height; y++) {
      for (let x = 0; x < this.sim.grid.width; x++) {
        const tile = this.sim.grid.getTile(x, y);
        if (!tile) continue;
        const pos = this.tileToWorld(x, y);
        this.drawDiamond(pos.x, pos.y, this.colorForTile(tile), '#243b2c', 0.94);
        if (!tile.roadId) this.drawZoneMarker(tile, pos.x, pos.y);
        if (tile.roadId) this.drawRoad(tile.roadId, pos.x, pos.y);
        this.drawServiceMarker(tile, pos.x, pos.y);
      }
    }

    if (this.selectedTile) {
      const pos = this.tileToWorld(this.selectedTile.pos.x, this.selectedTile.pos.y);
      this.drawDiamond(pos.x, pos.y, 'rgba(247,241,181,0.14)', '#f7f1b5', 1);
    }
    this.ctx.restore();
  }

  private drawDiamond(x: number, y: number, fill: string, stroke: string, alpha: number): void {
    this.ctx.save();
    this.ctx.globalAlpha = alpha;
    this.ctx.beginPath();
    this.ctx.moveTo(x, y - TILE_H / 2);
    this.ctx.lineTo(x + TILE_W / 2, y);
    this.ctx.lineTo(x, y + TILE_H / 2);
    this.ctx.lineTo(x - TILE_W / 2, y);
    this.ctx.closePath();
    this.ctx.fillStyle = fill;
    this.ctx.fill();
    this.ctx.strokeStyle = stroke;
    this.ctx.lineWidth = 1;
    this.ctx.stroke();
    this.ctx.restore();
  }

  private drawRoad(roadId: string, x: number, y: number): void {
    const arterial = roadId === 'arterial';
    this.ctx.beginPath();
    this.ctx.moveTo(x, y - TILE_H * (arterial ? 0.28 : 0.2));
    this.ctx.lineTo(x + TILE_W * (arterial ? 0.42 : 0.34), y);
    this.ctx.lineTo(x, y + TILE_H * (arterial ? 0.28 : 0.2));
    this.ctx.lineTo(x - TILE_W * (arterial ? 0.42 : 0.34), y);
    this.ctx.closePath();
    this.ctx.fillStyle = arterial ? '#22292f' : '#2d3437';
    this.ctx.fill();
    this.ctx.strokeStyle = arterial ? 'rgba(142,201,255,0.78)' : 'rgba(242,212,121,0.55)';
    this.ctx.lineWidth = arterial ? 2 : 1;
    this.ctx.stroke();
  }

  private drawServiceMarker(tile: Tile, x: number, y: number): void {
    const color = SERVICE_MARKER_COLORS[tile.buildingId];
    if (!color) return;
    this.ctx.beginPath();
    this.ctx.arc(x, y - 7, 6, 0, Math.PI * 2);
    this.ctx.fillStyle = color;
    this.ctx.fill();
    this.ctx.strokeStyle = 'rgba(255,255,255,0.72)';
    this.ctx.lineWidth = 2;
    this.ctx.stroke();
  }

  private drawZoneMarker(tile: Tile, x: number, y: number): void {
    if (!tile.buildingId) {
      this.drawVacantZoneMarker(tile.zone, x, y);
      return;
    }

    switch (tile.zone) {
      case ZoneType.Residential:
        this.drawResidentialMarker(tile.buildingId, x, y);
        return;
      case ZoneType.Commercial:
        this.drawCommercialMarker(x, y);
        return;
      case ZoneType.Industrial:
        this.drawIndustrialMarker(x, y);
        return;
      case ZoneType.Office:
        this.drawOfficeMarker(x, y);
        return;
      case ZoneType.MixedUse:
        this.drawMixedUseMarker(x, y);
        return;
      default:
    }
  }

  private drawVacantZoneMarker(zone: ZoneType, x: number, y: number): void {
    const color = zone === ZoneType.Residential
      ? '#d8e6ba'
      : zone === ZoneType.Commercial
        ? '#c7dcff'
        : '#f1c08b';
    this.ctx.save();
    this.ctx.beginPath();
    this.ctx.arc(x, y - 5, 5, 0, Math.PI * 2);
    this.ctx.globalAlpha = 0.22;
    this.ctx.fillStyle = color;
    this.ctx.fill();
    this.ctx.globalAlpha = 0.65;
    this.ctx.strokeStyle = color;
    this.ctx.lineWidth = 2;
    this.ctx.stroke();
    this.ctx.restore();
  }

  private drawResidentialMarker(buildingId: string, x: number, y: number): void {
    const level = this.residentialLevelFromBuilding(buildingId);
    const width = 8 + level * 2;
    const height = 6 + level * 2;
    this.ctx.fillStyle = '#f3e2bd';
    this.ctx.fillRect(x - width / 2, y - height - 2, width, height);
    this.ctx.beginPath();
    this.ctx.moveTo(x - width / 2 - 2, y - height - 2);
    this.ctx.lineTo(x + width / 2 + 2, y - height - 2);
    this.ctx.lineTo(x, y - height - 8);
    this.ctx.closePath();
    this.ctx.fillStyle = level >= 3 ? '#b9473f' : '#c85a44';
    this.ctx.fill();
    if (level >= 2) {
      this.ctx.fillStyle = '#8fc7ff';
      this.ctx.fillRect(x - 3, y - height + 1, 2, 2);
      this.ctx.fillRect(x + 2, y - height + 1, 2, 2);
    }
  }

  private drawCommercialMarker(x: number, y: number): void {
    this.ctx.fillStyle = '#d8e7ff';
    this.ctx.fillRect(x - 9, y - 18, 8, 16);
    this.ctx.fillStyle = '#b5d3ff';
    this.ctx.fillRect(x + 1, y - 14, 8, 12);
    this.ctx.fillStyle = '#3f6fa9';
    this.ctx.fillRect(x - 7, y - 14, 4, 2);
    this.ctx.fillRect(x + 3, y - 10, 4, 2);
  }

  private drawIndustrialMarker(x: number, y: number): void {
    this.ctx.fillStyle = '#d89b62';
    this.ctx.fillRect(x - 10, y - 11, 17, 9);
    this.ctx.beginPath();
    this.ctx.moveTo(x - 10, y - 11);
    this.ctx.lineTo(x - 4, y - 17);
    this.ctx.lineTo(x + 1, y - 11);
    this.ctx.lineTo(x + 6, y - 15);
    this.ctx.lineTo(x + 7, y - 11);
    this.ctx.closePath();
    this.ctx.fillStyle = '#b86f45';
    this.ctx.fill();
    this.ctx.fillStyle = '#5d6268';
    this.ctx.fillRect(x + 8, y - 19, 4, 17);
  }

  private drawMixedUseMarker(x: number, y: number): void {
    this.ctx.fillStyle = '#f2ddb0';
    this.ctx.fillRect(x - 9, y - 17, 8, 15);
    this.ctx.fillStyle = '#d8e7ff';
    this.ctx.fillRect(x, y - 14, 9, 12);
    this.ctx.fillStyle = '#c85a44';
    this.ctx.beginPath();
    this.ctx.moveTo(x - 11, y - 17);
    this.ctx.lineTo(x, y - 17);
    this.ctx.lineTo(x - 5, y - 23);
    this.ctx.closePath();
    this.ctx.fill();
    this.ctx.fillStyle = '#3f6fa9';
    this.ctx.fillRect(x + 2, y - 10, 4, 2);
  }

  private drawOfficeMarker(x: number, y: number): void {
    this.ctx.fillStyle = '#d7ccff';
    this.ctx.fillRect(x - 8, y - 22, 7, 20);
    this.ctx.fillStyle = '#b9a7f5';
    this.ctx.fillRect(x, y - 18, 8, 16);
    this.ctx.fillStyle = '#5b4aa0';
    for (let row = 0; row < 3; row++) {
      this.ctx.fillRect(x - 6, y - 18 + row * 5, 3, 2);
      this.ctx.fillRect(x + 2, y - 15 + row * 5, 3, 2);
    }
  }

  private drawTopBar(): void {
    const m = this.sim.metrics;
    this.ctx.fillStyle = 'rgba(18,24,28,0.9)';
    this.ctx.fillRect(0, 0, this.width, 42);
    this.ctx.fillStyle = '#f4f7ef';
    this.ctx.font = 'bold 14px sans-serif';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillText(`第 ${m.day} 天 Lv${m.cityLevel}`, 14, 21);
    this.ctx.fillText(`人口 ${m.population.toLocaleString()}`, this.width * 0.25, 21);
    this.ctx.fillText(`现金 $${m.cash.toLocaleString()}`, this.width * 0.48, 21);
    this.ctx.fillText(`幸福 ${m.happiness}`, this.width * 0.72, 21);
    this.ctx.fillText(`评分 ${m.cityScore}`, this.width - 90, 21);
  }

  private drawSidePanel(): void {
    const m = this.sim.metrics;
    const inspection = this.selectedTile ? this.sim.getTileInspection(this.selectedTile.pos.x, this.selectedTile.pos.y) : null;
    const x = 12;
    const panelHeight = 250;
    const y = this.height - panelHeight - 18;
    const width = 238;
    this.ctx.fillStyle = 'rgba(18,24,28,0.82)';
    this.roundRect(x, y, width, panelHeight, 6);
    this.ctx.fill();
    this.ctx.fillStyle = '#dbe6df';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'top';

    const lines = [
      this.compactText(`等级: Lv${m.cityLevel} ${m.cityLevelName} 税${m.taxRatePercent}% 行${m.administrationEfficiency}/${m.administrationUtilization}%`, 28),
      this.compactText(`缓冲: ${m.functionalBufferScore}/冲${m.landUseConflictCount} ${m.functionalBufferAction}`, 28),
      this.compactText(`用地: ${m.landUseEfficiencyScore}/空${m.vacantZoneTiles} ${m.landUseEfficiencyAction}`, 28),
      this.compactText(`品质: ${m.developmentQualityScore}/低${m.lowQualityBuildingCount} ${m.developmentQualityAction}`, 28),
      this.compactText(`住/混/办: ${m.housingCapacity.toLocaleString()}/${m.mixedUseBuildings}/${m.officeBuildings} ${m.housingAffordabilityFocus}${m.housingAffordabilityScore}`, 28),
      this.compactText(`道路/通勤: ${Math.round(m.roadCoverage)}% ${m.roadHierarchyFocus}${m.roadHierarchyPressure}/${m.commuteCorridorFocus}${m.commuteCorridorScore}`, 28),
      this.compactText(`需求: 住${m.residentialDemand} 商${m.commercialDemand} 工${m.industrialDemand}`, 28),
      this.compactText(`经济: ${m.economicSpecializationFocus}${m.economicSpecializationScore} 游${m.visitors} 才${m.workforceSkill}/缺${m.laborShortage}`, 28),
      this.compactText(`驱动: ${m.demandDriver} -> ${m.demandAction}`, 28),
      inspection
        ? this.compactText(`地块: ${inspection.title}`, 28)
        : '地块: 未选择',
      inspection
        ? this.compactText(`图层: ${inspection.overlayLabel} ${inspection.overlayValue}`, 28)
        : this.compactText(this.sim.getTileInspectionLegend(), 28),
      inspection
        ? this.compactText(`诊断: ${inspection.diagnosis}`, 28)
        : this.compactText(`卡点/缓冲: ${m.growthBottleneckFocus}${m.growthBottleneckScore}/${m.functionalBufferFocus}${m.landUseConflictPressure}`, 28),
      inspection && inspection.building !== '无'
        ? this.compactText(`建筑: ${inspection.building}`, 28)
        : `订单交付: ${this.sim.completedOrders}`,
      this.compactText(`事件: ${m.recentEvents[0] ?? '暂无'}`, 22),
      this.compactText(`提醒: ${m.alertDigest}`, 28),
    ];

    lines.forEach((line, index) => this.ctx.fillText(line, x + 12, y + 12 + index * 16));
  }

  private drawManagementPanel(): void {
    const x = this.width - 262;
    const y = 54;
    const width = 250;
    const height = 450;
    this.layoutActionButtons();
    this.ctx.fillStyle = 'rgba(18,24,28,0.82)';
    this.roundRect(x, y, width, height, 6);
    this.ctx.fill();
    this.ctx.fillStyle = '#dbe6df';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'top';

    const firstOrder = this.sim.orders[0];
    const production = this.sim.productionQueue.length
      ? this.sim.productionQueue.map((job) => `${job.label}${job.remainingDays}天`).join(' ')
      : '空闲';
    const objective = this.sim.getObjectives().find((candidate) => !candidate.completed);
    const policyPreviewLine = this.policyPreview
      ? this.compactText(`${this.policyPreview.summary}: ${this.policyPreview.deltas.slice(0, 3).join(' ')}`, 30)
      : '政策: 点按钮查看影响';
    const insightLines = this.sim.getInsightStack(4).slice(0, 3)
      .map((insight) => this.compactText(`${insight.label}: ${insight.text}`, 30));
    const lines = [
      this.compactText(`仓库 ${this.sim.getStorageUsed()}/${this.sim.getStorageCapacity()}  ${this.materialLine()}`, 30),
      this.compactText(`工厂 ${this.sim.productionQueue.length}/${this.sim.getProductionSlots()}  ${production}`, 30),
      firstOrder ? this.compactText(`订单: ${firstOrder.title} +$${firstOrder.rewardCash}`, 30) : '订单: 暂无',
      firstOrder ? this.compactText(`需求: ${this.formatCost(firstOrder.required)}`, 30) : '需求: 无',
      policyPreviewLine,
      ...insightLines,
      objective ? this.compactText(`目标: ${objective.title} +$${objective.rewardCash} 经验+${objective.rewardExperience}`, 30) : '目标: 阶段目标已完成',
      objective ? this.compactText(objective.description, 30) : '继续扩建城市并优化路网',
      objective ? this.compactText(`建议: ${objective.advice}`, 30) : '建议: 继续优化服务和路网',
    ];

    lines.forEach((line, index) => this.ctx.fillText(line, x + 12, y + 12 + index * 17));

    this.actionButtons.forEach((button) => {
      const locked = Boolean(button.lockedMessage);
      const selectedTax = button.kind === 'tax' && button.taxLevel === this.sim.metrics.taxLevel;
      const selectedTimeScale = button.kind === 'timeScale' && button.timeScale === this.timeScale;
      const selectedPolicy = button.kind === 'policy' && Boolean(button.selected);
      const highlighted = button.kind === 'upgrade' || selectedTax || selectedTimeScale || selectedPolicy;
      this.ctx.fillStyle = locked ? '#30363a' : highlighted ? '#6ea85f' : '#263239';
      this.roundRect(button.x, button.y, button.width, button.height, 5);
      this.ctx.fill();
      this.ctx.strokeStyle = 'rgba(255,255,255,0.16)';
      this.ctx.stroke();
      this.ctx.fillStyle = locked ? '#8f9b95' : highlighted ? '#07100b' : '#edf7ef';
      this.ctx.font = '12px sans-serif';
      this.ctx.textAlign = 'center';
      this.ctx.textBaseline = 'middle';
      this.ctx.fillText(button.label, button.x + button.width / 2, button.y + button.height / 2);
      this.ctx.textAlign = 'left';
    });
  }

  private drawToolBar(): void {
    const unlockState = this.sim.getUnlockState();
    this.buttons.forEach((button) => {
      const selected = button.tool === this.selectedTool;
      const serviceBuildingId = SERVICE_TOOL_TO_BUILDING[button.tool];
      const unlockEntry = serviceBuildingId ? unlockState.services[serviceBuildingId] : null;
      const locked = unlockEntry ? !unlockEntry.unlocked : false;
      this.ctx.fillStyle = locked ? '#30363a' : selected ? '#6ea85f' : '#263239';
      this.roundRect(button.x, button.y, button.width, button.height, 5);
      this.ctx.fill();
      this.ctx.strokeStyle = locked ? 'rgba(255,255,255,0.08)' : selected ? '#b7e39a' : 'rgba(255,255,255,0.18)';
      this.ctx.stroke();
      this.ctx.fillStyle = locked ? '#8f9b95' : selected ? '#07100b' : '#edf7ef';
      this.ctx.font = `${selected ? 'bold ' : ''}13px sans-serif`;
      this.ctx.textAlign = 'center';
      this.ctx.textBaseline = 'middle';
      this.ctx.fillText(button.label + this.lockSuffix(unlockEntry), button.x + button.width / 2, button.y + button.height / 2);
      this.ctx.textAlign = 'left';
    });
  }

  private drawStatus(): void {
    const width = Math.min(280, Math.max(170, this.statusText.length * 12));
    const x = this.width - width - 12;
    const y = this.height - 48;
    this.ctx.fillStyle = 'rgba(18,24,28,0.82)';
    this.roundRect(x, y, width, 34, 6);
    this.ctx.fill();
    this.ctx.fillStyle = '#f2d479';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillText(this.statusText, x + 10, y + 17);
  }

  private layoutTools(): void {
    this.buttons.length = 0;
    const buttonWidth = Math.min(66, Math.max(48, (this.width - 48) / TOOLS.length));
    const totalWidth = buttonWidth * TOOLS.length + (TOOLS.length - 1) * 6;
    let x = (this.width - totalWidth) / 2;
    const y = this.height - 48;
    for (const tool of TOOLS) {
      this.buttons.push({ tool, label: TOOL_LABELS[tool], x, y, width: buttonWidth, height: 34 });
      x += buttonWidth + 6;
    }
  }

  private layoutActionButtons(): void {
    this.actionButtons.length = 0;
    const x = this.width - 250;
    const width = 48;
    const gap = 6;
    const unlockState = this.sim.getUnlockState();
    const timeY = 250;
    ([0, 1, 2, 4] as CityTimeScale[]).forEach((timeScale, index) => {
      this.actionButtons.push({
        kind: 'timeScale',
        timeScale,
        label: TIME_SCALE_LABELS[timeScale],
        x: x + index * 62,
        y: timeY,
        width: 56,
        height: 28,
      });
    });
    const productionY = timeY + 36;
    (Object.keys(MATERIAL_LABELS) as MaterialId[]).forEach((materialId, index) => {
      const unlockEntry = unlockState.materials[materialId];
      this.actionButtons.push({
        kind: 'produce',
        materialId,
        label: MATERIAL_LABELS[materialId] + this.lockSuffix(unlockEntry),
        lockedMessage: unlockEntry.unlocked ? undefined : this.lockedMessage(unlockEntry.label, unlockEntry.unlockLevel),
        x: x + index * (width + gap),
        y: productionY,
        width,
        height: 28,
      });
    });
    this.actionButtons.push({
      kind: 'fulfillOrder',
      orderId: this.sim.orders[0]?.id,
      label: '交付',
      x,
      y: productionY + 36,
      width: 74,
      height: 28,
    });
    const residentialUpgrade = unlockState.actions[this.selectedResidentialUpgradeAction()];
    this.actionButtons.push({
      kind: 'upgrade',
      label: '升级住宅' + this.lockSuffix(residentialUpgrade),
      lockedMessage: residentialUpgrade.unlocked ? undefined : this.lockedMessage(residentialUpgrade.label, residentialUpgrade.unlockLevel),
      x: x + 82,
      y: productionY + 36,
      width: 86,
      height: 28,
    });
    const roadUpgrade = unlockState.actions.roadUpgrade;
    this.actionButtons.push({
      kind: 'upgradeRoad',
      label: '升道路' + this.lockSuffix(roadUpgrade),
      lockedMessage: roadUpgrade.unlocked ? undefined : this.lockedMessage(roadUpgrade.label, roadUpgrade.unlockLevel),
      x: x + 176,
      y: productionY + 36,
      width: 66,
      height: 28,
    });
    const taxY = productionY + 72;
    ([CityTaxLevel.Low, CityTaxLevel.Normal, CityTaxLevel.High] as CityTaxLevel[]).forEach((taxLevel, index) => {
      this.actionButtons.push({
        kind: 'tax',
        taxLevel,
        label: TAX_LABELS[taxLevel],
        x: x + index * 62,
        y: taxY,
        width: 56,
        height: 28,
      });
    });
    const policyY = taxY + 36;
    this.sim.getPolicyStates().forEach((policyState, index) => {
      const column = index % 3;
      const row = Math.floor(index / 3);
      this.actionButtons.push({
        kind: 'policy',
        policy: policyState.policy,
        selected: policyState.enabled,
        label: policyState.shortLabel,
        x: x + column * 82,
        y: policyY + row * 30,
        width: 74,
        height: 24,
      });
    });
  }

  private selectedResidentialUpgradeAction(): CityUnlockActionId {
    const nextLevel = this.selectedTile?.zone === ZoneType.Residential
      ? Math.min(3, this.sim.getResidentialLevel(this.selectedTile) + 1)
      : 2;
    return nextLevel >= 3 ? 'residentialLevel3' : 'residentialLevel2';
  }

  private serviceToolUnlockEntry(tool: PlanningTool): { label: string; unlockLevel: number; unlocked: boolean } | null {
    const serviceBuildingId = SERVICE_TOOL_TO_BUILDING[tool];
    return serviceBuildingId ? this.sim.getUnlockState().services[serviceBuildingId] : null;
  }

  private toolLockedMessage(tool: PlanningTool): string {
    const unlockEntry = this.serviceToolUnlockEntry(tool);
    return unlockEntry && !unlockEntry.unlocked ? this.lockedMessage(unlockEntry.label, unlockEntry.unlockLevel) : '';
  }

  private lockSuffix(entry?: { unlockLevel: number; unlocked: boolean } | null): string {
    return entry && !entry.unlocked ? `Lv${entry.unlockLevel}` : '';
  }

  private lockedMessage(label: string, unlockLevel: number): string {
    return `${label}需要城市 Lv${unlockLevel} 解锁`;
  }

  private handleAction(button: ActionButton): void {
    const result = button.kind === 'produce' && button.materialId
      ? this.sim.startProduction(button.materialId)
      : button.kind === 'fulfillOrder' && button.orderId
        ? this.sim.fulfillOrder(button.orderId)
        : button.kind === 'upgrade' && this.selectedTile
          ? this.sim.upgradeResidentialAt(this.selectedTile.pos.x, this.selectedTile.pos.y)
          : button.kind === 'upgradeRoad' && this.selectedTile
            ? this.sim.upgradeRoadAt(this.selectedTile.pos.x, this.selectedTile.pos.y)
            : button.kind === 'tax' && button.taxLevel !== undefined
              ? this.sim.setTaxLevel(button.taxLevel)
              : button.kind === 'timeScale' && button.timeScale !== undefined
                ? this.setTimeScale(button.timeScale)
                : button.kind === 'policy' && button.policy !== undefined
                  ? this.togglePolicy(button.policy)
                  : { changed: false, message: button.kind === 'upgradeRoad' ? '请先选择道路地块' : '请先选择住宅地块' };
    this.statusText = result.message;
    if (result.changed) {
      this.vibrate('medium');
      this.save();
    }
  }

  private setTimeScale(timeScale: CityTimeScale): { changed: boolean; message: string } {
    if (this.timeScale === timeScale) {
      return { changed: false, message: `速度已是 ${TIME_SCALE_LABELS[timeScale]}` };
    }
    this.timeScale = timeScale;
    return {
      changed: true,
      message: timeScale === 0 ? '城市已暂停' : `模拟速度 ${TIME_SCALE_LABELS[timeScale]}`,
    };
  }

  private togglePolicy(policy: CityPolicy): { changed: boolean; message: string } {
    this.policyPreview = this.sim.getPolicyImpactPreview(policy);
    return this.sim.togglePolicy(policy);
  }

  private residentialLevelFromBuilding(buildingId: string): number {
    if (buildingId === 'residential_l1') return 1;
    const match = /^residential_l([2-3])$/.exec(buildingId);
    return match ? Number(match[1]) : 0;
  }

  private colorForTile(tile: Tile): string {
    if (tile.terrain === TerrainType.Water) return '#2677c9';
    if (tile.terrain === TerrainType.Hill) return '#7a8651';
    switch (tile.zone) {
      case ZoneType.Residential: return '#6ec35b';
      case ZoneType.Commercial: return '#4c8df2';
      case ZoneType.Industrial: return '#d98243';
      case ZoneType.Office: return '#9b83df';
      case ZoneType.MixedUse: return '#d6b54a';
      case ZoneType.Civic: return '#dc6d87';
      case ZoneType.Utility: return '#858b8c';
      default: return '#36572f';
    }
  }

  private tileToWorld(tx: number, ty: number): { x: number; y: number } {
    const dx = tx - GRID_W / 2;
    const dy = ty - GRID_H / 2;
    return {
      x: (dx - dy) * (TILE_W / 2),
      y: (dx + dy) * (TILE_H / 2),
    };
  }

  private worldToTile(wx: number, wy: number): { x: number; y: number } | null {
    const localX = (wx - this.originX) / this.viewportScale;
    const localY = (wy - this.originY) / this.viewportScale;
    const tx = (localX / (TILE_W / 2) + localY / (TILE_H / 2)) / 2 + GRID_W / 2;
    const ty = (localY / (TILE_H / 2) - localX / (TILE_W / 2)) / 2 + GRID_H / 2;
    return { x: Math.floor(tx), y: Math.floor(ty) };
  }

  private pointInRect(x: number, y: number, rect: { x: number; y: number; width: number; height: number }): boolean {
    return x >= rect.x && x <= rect.x + rect.width && y >= rect.y && y <= rect.y + rect.height;
  }

  private roundRect(x: number, y: number, width: number, height: number, radius: number): void {
    this.ctx.beginPath();
    this.ctx.moveTo(x + radius, y);
    this.ctx.arcTo(x + width, y, x + width, y + height, radius);
    this.ctx.arcTo(x + width, y + height, x, y + height, radius);
    this.ctx.arcTo(x, y + height, x, y, radius);
    this.ctx.arcTo(x, y, x + width, y, radius);
    this.ctx.closePath();
  }

  private restore(options: RestoreOptions = {}): boolean {
    const { announceMissing = false, feedback, resave = true } = options;
    const read = this.readSave();
    if (read.status === 'unavailable') {
      if (announceMissing) this.statusText = '本地存档不可用';
      if (feedback) this.vibrate(feedback);
      return false;
    }
    if (read.status === 'failed') {
      if (announceMissing) this.statusText = '读取存档失败，继续规划';
      if (feedback) this.vibrate('heavy');
      return false;
    }
    if (!this.isSaveData(read.value)) {
      if (announceMissing) this.statusText = '城市继续运行';
      if (feedback) this.vibrate(feedback);
      return false;
    }
    const data = read.value;
    const offline = this.sim.restoreSnapshot(data);
    this.statusText = this.formatOfflineMessage(offline) || '已读取本地城市存档';
    if (feedback) this.vibrate(feedback);
    if (resave) this.save();
    return true;
  }

  private save(options: SaveOptions = {}): boolean {
    const { announce = false, feedback } = options;
    if (!this.runtime.setStorageSync) {
      if (announce) this.statusText = '本地存档不可用';
      if (feedback) this.vibrate('heavy');
      return false;
    }

    try {
      this.runtime.setStorageSync(SAVE_KEY, this.sim.createSnapshot());
      if (announce) this.statusText = '城市已安全保存';
      if (feedback) this.vibrate(feedback);
      return true;
    } catch {
      if (announce) this.statusText = '保存失败，继续规划';
      if (feedback) this.vibrate('heavy');
      return false;
    }
  }

  private readSave(): StorageReadResult {
    if (!this.runtime.getStorageSync) return { status: 'unavailable' };
    try {
      return { status: 'ok', value: this.runtime.getStorageSync(SAVE_KEY) };
    } catch {
      return { status: 'failed' };
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

  private materialLine(): string {
    return (Object.keys(MATERIAL_LABELS) as MaterialId[])
      .map((materialId) => `${MATERIAL_LABELS[materialId]}${this.sim.materials[materialId]}`)
      .join(' ');
  }

  private compactText(text: string, maxChars: number): string {
    const ellipsis = '...';
    const maxWidth = maxChars * 7.5;
    if (text.length <= maxChars && this.ctx.measureText(text).width <= maxWidth) return text;
    if (this.ctx.measureText(text).width <= maxWidth) return text;

    let low = 0;
    let high = text.length;
    while (low < high) {
      const mid = Math.ceil((low + high) / 2);
      const candidate = `${text.slice(0, mid)}${ellipsis}`;
      if (this.ctx.measureText(candidate).width <= maxWidth) {
        low = mid;
      } else {
        high = mid - 1;
      }
    }

    return `${text.slice(0, low)}${ellipsis}`;
  }

  private formatCost(cost: MaterialCost): string {
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .map(([materialId, count]) => `${MATERIAL_LABELS[materialId]}x${count}`)
      .join('、');
  }

  private vibrate(type: FeedbackType): void {
    try {
      this.runtime.vibrateShort?.({ type });
    } catch {
      // Tactile feedback is optional on some WeChat devices and simulators.
    }
  }
}

function boot(): void {
  const runtimeGlobal = typeof GameGlobal !== 'undefined' ? GameGlobal : globalThis as unknown as Record<string, unknown>;
  runtimeGlobal.__POCKET_CITY_RUNTIME__ = RUNTIME_MARKER;

  if (typeof wx === 'undefined') {
    console.warn('Pocket City mini game runtime requires WeChat wx APIs.');
    return;
  }

  new WeChatCityGame(wx);
}

boot();

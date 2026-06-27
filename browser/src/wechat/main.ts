import { CityOfflineProgressResult, CitySimulation, type CitySimulationSaveData } from '@/simulation/city-simulation';
import { MaterialCost, MaterialId, PlanningTool, TerrainType, ZoneType } from '@/types/index';
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
  vibrateShort?(options?: { type?: 'light' | 'medium' | 'heavy' }): void;
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
  kind: 'produce' | 'fulfillOrder' | 'upgrade';
  label: string;
  x: number;
  y: number;
  width: number;
  height: number;
  materialId?: MaterialId;
  orderId?: string;
}

const RUNTIME_MARKER = 'NON_UNITY_WECHAT_CANVAS_RUNTIME';
const SAVE_KEY = 'pocket-city-planner-save-v1';
const TILE_W = 48;
const TILE_H = 24;
const GRID_W = 24;
const GRID_H = 18;
const TOOL_LABELS: Record<PlanningTool, string> = {
  inspect: '查看',
  road: '道路',
  residential: '住宅',
  commercial: '商业',
  industrial: '工业',
  erase: '清理',
};
const TOOLS: PlanningTool[] = ['inspect', 'road', 'residential', 'commercial', 'industrial', 'erase'];
const ZONE_LABELS: Record<ZoneType, string> = {
  [ZoneType.None]: '未规划',
  [ZoneType.Residential]: '住宅',
  [ZoneType.Commercial]: '商业',
  [ZoneType.Industrial]: '工业',
  [ZoneType.Civic]: '市政',
  [ZoneType.Utility]: '设施',
  [ZoneType.Office]: '办公',
  [ZoneType.MixedUse]: '混合',
};
const TERRAIN_LABELS: Record<TerrainType, string> = {
  [TerrainType.Plain]: '平地',
  [TerrainType.Water]: '水域',
  [TerrainType.Hill]: '丘陵',
};
const MATERIAL_LABELS: Record<MaterialId, string> = {
  wood: '木材',
  metal: '金属',
  plastic: '塑料',
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
  private statusText = '选择工具后点击地块开始规划';
  private lastPaintKey = '';
  private lastTime = Date.now();
  private width: number;
  private height: number;
  private originX: number;
  private originY: number;

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
    this.runtime.onTouchEnd(() => {
      this.lastPaintKey = '';
      this.save();
    });
    this.runtime.onHide?.(() => this.save());
    this.runtime.onShow?.(() => {
      if (!this.restore()) this.statusText = '城市已恢复，继续规划';
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
      this.sim.tick(delta);
      this.draw();
      requestFrame(frame);
    };

    requestFrame(frame);
  }

  private handleTouch(event: WeChatTouchEvent, allowToolSwitch: boolean): void {
    const touch = event.touches?.[0] ?? event.changedTouches?.[0];
    if (!touch) return;
    const x = touch.clientX;
    const y = touch.clientY;

    if (allowToolSwitch) {
      const button = this.buttons.find((candidate) => this.pointInRect(x, y, candidate));
      if (button) {
        this.selectedTool = button.tool;
        this.statusText = `当前工具: ${button.label}`;
        this.vibrate('light');
        return;
      }

      const actionButton = this.actionButtons.find((candidate) => this.pointInRect(x, y, candidate));
      if (actionButton) {
        this.handleAction(actionButton);
        return;
      }
    }

    const tilePos = this.worldToTile(x, y);
    if (!tilePos || !this.sim.grid.inBounds(tilePos.x, tilePos.y)) return;

    const paintKey = `${this.selectedTool}:${tilePos.x}:${tilePos.y}`;
    if (paintKey === this.lastPaintKey && this.selectedTool !== 'inspect') return;
    this.lastPaintKey = paintKey;

    const result = this.sim.applyTool(tilePos.x, tilePos.y, this.selectedTool);
    this.selectedTile = this.sim.grid.getTile(tilePos.x, tilePos.y) ?? null;
    this.statusText = result.message;
    if (result.changed) {
      this.vibrate('light');
      this.save();
    }
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
    for (let y = 0; y < this.sim.grid.height; y++) {
      for (let x = 0; x < this.sim.grid.width; x++) {
        const tile = this.sim.grid.getTile(x, y);
        if (!tile) continue;
        const pos = this.tileToWorld(x, y);
        this.drawDiamond(pos.x, pos.y, this.colorForTile(tile), '#243b2c', 0.94);
        if (tile.roadId) this.drawRoad(pos.x, pos.y);
      }
    }

    if (this.selectedTile) {
      const pos = this.tileToWorld(this.selectedTile.pos.x, this.selectedTile.pos.y);
      this.drawDiamond(pos.x, pos.y, 'rgba(247,241,181,0.14)', '#f7f1b5', 1);
    }
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

  private drawRoad(x: number, y: number): void {
    this.ctx.beginPath();
    this.ctx.moveTo(x, y - TILE_H * 0.2);
    this.ctx.lineTo(x + TILE_W * 0.34, y);
    this.ctx.lineTo(x, y + TILE_H * 0.2);
    this.ctx.lineTo(x - TILE_W * 0.34, y);
    this.ctx.closePath();
    this.ctx.fillStyle = '#2d3437';
    this.ctx.fill();
    this.ctx.strokeStyle = 'rgba(242,212,121,0.55)';
    this.ctx.lineWidth = 1;
    this.ctx.stroke();
  }

  private drawTopBar(): void {
    const m = this.sim.metrics;
    this.ctx.fillStyle = 'rgba(18,24,28,0.9)';
    this.ctx.fillRect(0, 0, this.width, 42);
    this.ctx.fillStyle = '#f4f7ef';
    this.ctx.font = 'bold 14px sans-serif';
    this.ctx.textBaseline = 'middle';
    this.ctx.fillText(`第 ${m.day} 天`, 14, 21);
    this.ctx.fillText(`人口 ${m.population.toLocaleString()}`, this.width * 0.25, 21);
    this.ctx.fillText(`现金 $${m.cash.toLocaleString()}`, this.width * 0.48, 21);
    this.ctx.fillText(`幸福 ${m.happiness}`, this.width * 0.72, 21);
    this.ctx.fillText(`评分 ${m.cityScore}`, this.width - 90, 21);
  }

  private drawSidePanel(): void {
    const m = this.sim.metrics;
    const x = 12;
    const y = this.height - 168;
    const width = 238;
    this.ctx.fillStyle = 'rgba(18,24,28,0.82)';
    this.roundRect(x, y, width, 150, 6);
    this.ctx.fill();

    const lines = [
      `等级: ${m.cityLevelName}`,
      `住房容量: ${m.housingCapacity.toLocaleString()}`,
      `已开发地块: ${m.buildingCount}`,
      `道路覆盖: ${Math.round(m.roadCoverage)}%`,
      `污染/拥堵: ${Math.round(m.pollution)} / ${Math.round(m.congestion)}`,
      this.selectedTile
        ? `地块: (${this.selectedTile.pos.x}, ${this.selectedTile.pos.y}) ${ZONE_LABELS[this.selectedTile.zone]}`
        : '地块: 未选择',
      this.selectedTile
        ? `地形: ${TERRAIN_LABELS[this.selectedTile.terrain]} 道路: ${this.selectedTile.roadId ? '已连接' : '无'}`
        : '点击地图查看详情',
      this.selectedTile?.zone === ZoneType.Residential
        ? `住宅等级: ${this.sim.getResidentialLevel(this.selectedTile)}`
        : `订单交付: ${this.sim.completedOrders}`,
      m.alerts.length ? `提醒: ${m.alerts.slice(0, 2).join('、')}` : '提醒: 城市运行平稳',
    ];

    this.ctx.fillStyle = '#dbe6df';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'top';
    lines.forEach((line, index) => this.ctx.fillText(line, x + 12, y + 12 + index * 16));
  }

  private drawManagementPanel(): void {
    const x = this.width - 262;
    const y = 54;
    const width = 250;
    const height = 222;
    this.layoutActionButtons();
    this.ctx.fillStyle = 'rgba(18,24,28,0.82)';
    this.roundRect(x, y, width, height, 6);
    this.ctx.fill();

    const firstOrder = this.sim.orders[0];
    const production = this.sim.productionQueue.length
      ? this.sim.productionQueue.map((job) => `${job.label}${job.remainingDays}天`).join(' ')
      : '空闲';
    const objective = this.sim.getObjectives().find((candidate) => !candidate.completed);
    const lines = [
      `仓库 ${this.sim.getStorageUsed()}/${this.sim.getStorageCapacity()}  ${this.materialLine()}`,
      `工厂 ${this.sim.productionQueue.length}/${this.sim.getProductionSlots()}  ${production}`,
      firstOrder ? `订单: ${firstOrder.title} +$${firstOrder.rewardCash}` : '订单: 暂无',
      firstOrder ? `需求: ${this.formatCost(firstOrder.required)}` : '需求: 无',
      objective ? `目标: ${objective.title} +$${objective.rewardCash}` : '目标: 阶段目标已完成',
      objective ? objective.description : '继续扩建城市并优化路网',
    ];

    this.ctx.fillStyle = '#dbe6df';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'top';
    lines.forEach((line, index) => this.ctx.fillText(line, x + 12, y + 12 + index * 18));

    this.actionButtons.forEach((button) => {
      this.ctx.fillStyle = button.kind === 'upgrade' ? '#6ea85f' : '#263239';
      this.roundRect(button.x, button.y, button.width, button.height, 5);
      this.ctx.fill();
      this.ctx.strokeStyle = 'rgba(255,255,255,0.16)';
      this.ctx.stroke();
      this.ctx.fillStyle = button.kind === 'upgrade' ? '#07100b' : '#edf7ef';
      this.ctx.font = '12px sans-serif';
      this.ctx.textAlign = 'center';
      this.ctx.textBaseline = 'middle';
      this.ctx.fillText(button.label, button.x + button.width / 2, button.y + button.height / 2);
      this.ctx.textAlign = 'left';
    });
  }

  private drawToolBar(): void {
    this.buttons.forEach((button) => {
      const selected = button.tool === this.selectedTool;
      this.ctx.fillStyle = selected ? '#6ea85f' : '#263239';
      this.roundRect(button.x, button.y, button.width, button.height, 5);
      this.ctx.fill();
      this.ctx.strokeStyle = selected ? '#b7e39a' : 'rgba(255,255,255,0.18)';
      this.ctx.stroke();
      this.ctx.fillStyle = selected ? '#07100b' : '#edf7ef';
      this.ctx.font = `${selected ? 'bold ' : ''}13px sans-serif`;
      this.ctx.textAlign = 'center';
      this.ctx.textBaseline = 'middle';
      this.ctx.fillText(button.label, button.x + button.width / 2, button.y + button.height / 2);
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
    const y = 176;
    const width = 48;
    const gap = 6;
    (Object.keys(MATERIAL_LABELS) as MaterialId[]).forEach((materialId, index) => {
      this.actionButtons.push({
        kind: 'produce',
        materialId,
        label: MATERIAL_LABELS[materialId],
        x: x + index * (width + gap),
        y,
        width,
        height: 28,
      });
    });
    this.actionButtons.push({
      kind: 'fulfillOrder',
      orderId: this.sim.orders[0]?.id,
      label: '交付',
      x,
      y: y + 36,
      width: 74,
      height: 28,
    });
    this.actionButtons.push({
      kind: 'upgrade',
      label: '升级住宅',
      x: x + 82,
      y: y + 36,
      width: 86,
      height: 28,
    });
  }

  private handleAction(button: ActionButton): void {
    const result = button.kind === 'produce' && button.materialId
      ? this.sim.startProduction(button.materialId)
      : button.kind === 'fulfillOrder' && button.orderId
        ? this.sim.fulfillOrder(button.orderId)
        : button.kind === 'upgrade' && this.selectedTile
          ? this.sim.upgradeResidentialAt(this.selectedTile.pos.x, this.selectedTile.pos.y)
          : { changed: false, message: '请先选择住宅地块' };
    this.statusText = result.message;
    if (result.changed) {
      this.vibrate('light');
      this.save();
    }
  }

  private colorForTile(tile: Tile): string {
    if (tile.terrain === TerrainType.Water) return '#2677c9';
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
      x: this.originX + (dx - dy) * (TILE_W / 2),
      y: this.originY + (dx + dy) * (TILE_H / 2),
    };
  }

  private worldToTile(wx: number, wy: number): { x: number; y: number } | null {
    const localX = wx - this.originX;
    const localY = wy - this.originY;
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

  private restore(): boolean {
    const data = this.runtime.getStorageSync?.(SAVE_KEY);
    if (!this.isSaveData(data)) return false;
    const offline = this.sim.restoreSnapshot(data);
    this.statusText = this.formatOfflineMessage(offline) || '已读取本地城市存档';
    this.save();
    return true;
  }

  private save(): void {
    this.runtime.setStorageSync?.(SAVE_KEY, this.sim.createSnapshot());
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

  private formatCost(cost: MaterialCost): string {
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .map(([materialId, count]) => `${MATERIAL_LABELS[materialId]}x${count}`)
      .join('、');
  }

  private vibrate(type: 'light' | 'medium' | 'heavy'): void {
    this.runtime.vibrateShort?.({ type });
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

import { CitySimulation } from '@/simulation/city-simulation';
import { CityMetrics, PlanningTool, TerrainType, ZoneType } from '@/types/index';
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

interface CitySaveData {
  version: 1;
  metrics: CityMetrics;
  tiles: Array<{ x: number; y: number; zone: ZoneType; roadId: string; buildingId: string }>;
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

class WeChatCityGame {
  private readonly canvas: WeChatCanvas;
  private readonly ctx: CanvasRenderingContext2D;
  private readonly dpr: number;
  private readonly sim = new CitySimulation(GRID_W, GRID_H);
  private readonly buttons: ToolButton[] = [];
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
      this.statusText = '城市已恢复，继续规划';
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
      m.alerts.length ? `提醒: ${m.alerts.slice(0, 2).join('、')}` : '提醒: 城市运行平稳',
    ];

    this.ctx.fillStyle = '#dbe6df';
    this.ctx.font = '12px sans-serif';
    this.ctx.textBaseline = 'top';
    lines.forEach((line, index) => this.ctx.fillText(line, x + 12, y + 12 + index * 16));
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

  private pointInRect(x: number, y: number, rect: ToolButton): boolean {
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

  private createSaveData(): CitySaveData {
    const tiles: CitySaveData['tiles'] = [];
    for (let y = 0; y < this.sim.grid.height; y++) {
      for (let x = 0; x < this.sim.grid.width; x++) {
        const tile = this.sim.grid.getTile(x, y);
        if (!tile) continue;
        if (tile.zone !== ZoneType.None || tile.roadId || tile.buildingId) {
          tiles.push({ x, y, zone: tile.zone, roadId: tile.roadId, buildingId: tile.buildingId });
        }
      }
    }

    return {
      version: 1,
      metrics: { ...this.sim.metrics, alerts: [...this.sim.metrics.alerts], unlockedBuildingIds: [...this.sim.metrics.unlockedBuildingIds] },
      tiles,
    };
  }

  private restore(): void {
    const data = this.runtime.getStorageSync?.(SAVE_KEY);
    if (!this.isSaveData(data)) return;

    Object.assign(this.sim.metrics, data.metrics);
    for (const tile of data.tiles) {
      this.sim.grid.clearPlanning(tile.x, tile.y);
      this.sim.grid.setZone(tile.x, tile.y, tile.zone);
      if (tile.roadId) this.sim.grid.setRoad(tile.x, tile.y, tile.roadId);
      if (tile.buildingId) this.sim.grid.setBuilding(tile.x, tile.y, tile.buildingId);
    }
    this.statusText = '已读取本地城市存档';
  }

  private save(): void {
    this.runtime.setStorageSync?.(SAVE_KEY, this.createSaveData());
  }

  private isSaveData(value: unknown): value is CitySaveData {
    if (!value || typeof value !== 'object') return false;
    const candidate = value as Partial<CitySaveData>;
    return candidate.version === 1 && Array.isArray(candidate.tiles) && typeof candidate.metrics === 'object';
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

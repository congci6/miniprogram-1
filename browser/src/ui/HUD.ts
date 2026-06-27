import { CityMetrics, PlanningTool, TerrainType, ZoneType } from '@/types/index';
import type { Tile } from '@/simulation/grid';

const TOOL_LABELS: Record<PlanningTool, string> = {
  inspect: '查看',
  road: '道路',
  residential: '住宅',
  commercial: '商业',
  industrial: '工业',
  erase: '清理',
};

const ZONE_LABELS: Record<ZoneType, string> = {
  [ZoneType.None]: '未规划',
  [ZoneType.Residential]: '住宅区',
  [ZoneType.Commercial]: '商业区',
  [ZoneType.Industrial]: '工业区',
  [ZoneType.Civic]: '市政区',
  [ZoneType.Utility]: '设施区',
  [ZoneType.Office]: '办公区',
  [ZoneType.MixedUse]: '混合区',
};

const TERRAIN_LABELS: Record<TerrainType, string> = {
  [TerrainType.Plain]: '平地',
  [TerrainType.Water]: '水域',
  [TerrainType.Hill]: '丘陵',
};

export class HUD {
  private topBar: HTMLElement;
  private sidePanel: HTMLElement;
  private toolBar: HTMLElement;
  private statusLine: HTMLElement;
  private selectedTool: PlanningTool = 'inspect';
  private selectedTile: Tile | null = null;
  private selectedMessage = '';
  private buttons = new Map<PlanningTool, HTMLButtonElement>();

  constructor() {
    const c = document.getElementById('hud-overlay')!;
    c.style.pointerEvents = 'none';

    this.topBar = document.createElement('div');
    this.topBar.style.cssText =
      'position:absolute;top:0;left:0;right:0;padding:8px 16px;' +
      'background:rgba(18,24,28,0.82);color:#f4f7ef;font-size:14px;' +
      'display:flex;gap:16px;justify-content:space-between;pointer-events:auto;z-index:20;' +
      'border-bottom:1px solid rgba(255,255,255,0.1);';
    c.appendChild(this.topBar);

    this.toolBar = document.createElement('div');
    this.toolBar.style.cssText =
      'position:absolute;left:50%;bottom:12px;transform:translateX(-50%);' +
      'display:flex;gap:6px;padding:6px;background:rgba(18,24,28,0.82);' +
      'border:1px solid rgba(255,255,255,0.12);border-radius:6px;' +
      'pointer-events:auto;z-index:30;box-shadow:0 8px 24px rgba(0,0,0,0.28);';
    c.appendChild(this.toolBar);

    (Object.keys(TOOL_LABELS) as PlanningTool[]).forEach((tool) => {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = TOOL_LABELS[tool];
      button.title = TOOL_LABELS[tool];
      button.style.cssText =
        'min-width:52px;height:34px;border:1px solid rgba(255,255,255,0.14);' +
        'border-radius:5px;background:#263239;color:#edf7ef;font-size:13px;' +
        'cursor:pointer;padding:0 10px;';
      button.addEventListener('click', () => this.selectTool(tool));
      this.buttons.set(tool, button);
      this.toolBar.appendChild(button);
    });

    this.sidePanel = document.createElement('div');
    this.sidePanel.style.cssText =
      'position:absolute;bottom:12px;left:12px;padding:10px 12px;' +
      'background:rgba(18,24,28,0.78);color:#dbe6df;font-size:12px;' +
      'border:1px solid rgba(255,255,255,0.1);border-radius:6px;' +
      'pointer-events:auto;z-index:20;min-width:220px;max-width:300px;line-height:1.55;';
    c.appendChild(this.sidePanel);

    this.statusLine = document.createElement('div');
    this.statusLine.style.cssText =
      'position:absolute;right:12px;bottom:12px;padding:8px 10px;' +
      'background:rgba(18,24,28,0.78);color:#f2d479;font-size:12px;' +
      'border:1px solid rgba(255,255,255,0.1);border-radius:6px;' +
      'pointer-events:auto;z-index:20;max-width:260px;';
    c.appendChild(this.statusLine);

    window.addEventListener('city-metrics-update', ((e: CustomEvent) => {
      if (e.detail.selectedTool) this.selectedTool = e.detail.selectedTool;
      if (e.detail.message) this.selectedMessage = e.detail.message;
      this.update(e.detail.metrics);
    }) as EventListener);

    window.addEventListener('city-tile-selected', ((e: CustomEvent) => {
      this.selectedTile = e.detail.tile ?? null;
      this.selectedMessage = e.detail.message ?? '';
      this.renderSidePanel();
    }) as EventListener);

    this.updateButtonState();
  }

  private update(m: CityMetrics): void {
    this.topBar.innerHTML =
      '<span>第 ' + m.day + ' 天</span>' +
      '<span>人口: ' + m.population.toLocaleString() + '</span>' +
      '<span>现金: $' + m.cash.toLocaleString() + '</span>' +
      '<span>幸福度: ' + m.happiness + '</span>' +
      '<span>评分: ' + m.cityScore + '</span>';
    this.renderSidePanel(m);
    this.statusLine.textContent = this.selectedMessage || `当前工具: ${TOOL_LABELS[this.selectedTool]}`;
    this.updateButtonState();
  }

  private selectTool(tool: PlanningTool): void {
    this.selectedTool = tool;
    this.selectedMessage = `当前工具: ${TOOL_LABELS[tool]}`;
    this.updateButtonState();
    window.dispatchEvent(new CustomEvent('city-tool-change', { detail: { tool } }));
  }

  private renderSidePanel(metrics?: CityMetrics): void {
    const tileText = this.selectedTile
      ? '<br>地块: (' + this.selectedTile.pos.x + ', ' + this.selectedTile.pos.y + ')' +
        '<br>地形: ' + TERRAIN_LABELS[this.selectedTile.terrain] +
        '<br>分区: ' + ZONE_LABELS[this.selectedTile.zone] +
        '<br>道路: ' + (this.selectedTile.roadId ? '已连接' : '无')
      : '<br>地块: 未选择';

    if (!metrics) {
      this.sidePanel.innerHTML = tileText;
      return;
    }

    this.sidePanel.innerHTML =
      '等级: ' + metrics.cityLevelName + '<br>' +
      '住房容量: ' + metrics.housingCapacity.toLocaleString() + '<br>' +
      '已开发地块: ' + metrics.buildingCount + '<br>' +
      '道路覆盖: ' + Math.round(metrics.roadCoverage) + '%<br>' +
      '污染: ' + Math.round(metrics.pollution) + ' / 拥堵: ' + Math.round(metrics.congestion) +
      tileText +
      (metrics.alerts.length ? '<br>提醒: ' + metrics.alerts.join('、') : '');
  }

  private updateButtonState(): void {
    this.buttons.forEach((button, tool) => {
      const selected = tool === this.selectedTool;
      button.style.background = selected ? '#6ea85f' : '#263239';
      button.style.color = selected ? '#07100b' : '#edf7ef';
      button.style.borderColor = selected ? '#b7e39a' : 'rgba(255,255,255,0.14)';
      button.style.fontWeight = selected ? '700' : '500';
    });
  }
}

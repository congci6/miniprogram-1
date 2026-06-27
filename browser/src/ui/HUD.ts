import {
  CityMaterialInventory,
  CityMetrics,
  CityObjective,
  CityOrder,
  CityTaxLevel,
  CityUnlockActionId,
  CityUnlockState,
  MaterialId,
  PlanningTool,
  ProductionJob,
  ServiceBuildingId,
  TerrainType,
  ZoneType,
} from '@/types/index';
import type { Tile } from '@/simulation/grid';

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

const SERVICE_BUILDING_LABELS: Record<string, string> = {
  residential_l1: '住宅 1 级',
  residential_l2: '住宅 2 级',
  residential_l3: '住宅 3 级',
  commercial_l1: '商业建筑',
  industrial_l1: '工业建筑',
  community_park: '社区公园',
  community_clinic: '社区诊所',
  community_school: '社区学校',
};
const SERVICE_TOOL_TO_BUILDING: Partial<Record<PlanningTool, ServiceBuildingId>> = {
  park: 'community_park',
  clinic: 'community_clinic',
  school: 'community_school',
};

const ROAD_LABELS: Record<string, string> = {
  local: '普通道路',
  arterial: '主干道',
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
  private managementPanel: HTMLElement;
  private toolBar: HTMLElement;
  private statusLine: HTMLElement;
  private selectedTool: PlanningTool = 'inspect';
  private selectedTile: Tile | null = null;
  private selectedMessage = '';
  private metrics: CityMetrics | null = null;
  private materials: CityMaterialInventory = { wood: 0, metal: 0, plastic: 0 };
  private productionQueue: ProductionJob[] = [];
  private productionSlots = 1;
  private storageUsed = 0;
  private storageCapacity = 30;
  private orders: CityOrder[] = [];
  private completedOrders = 0;
  private objectives: CityObjective[] = [];
  private unlockState: CityUnlockState | null = null;
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

    this.managementPanel = document.createElement('div');
    this.managementPanel.style.cssText =
      'position:absolute;top:54px;right:12px;width:286px;padding:10px 12px;' +
      'background:rgba(18,24,28,0.82);color:#dbe6df;font-size:12px;' +
      'border:1px solid rgba(255,255,255,0.1);border-radius:6px;' +
      'pointer-events:auto;z-index:22;line-height:1.45;max-height:calc(100vh - 128px);overflow:auto;';
    c.appendChild(this.managementPanel);

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
      'pointer-events:auto;z-index:20;max-width:280px;';
    c.appendChild(this.statusLine);

    window.addEventListener('city-metrics-update', ((e: CustomEvent) => {
      if (e.detail.selectedTool) this.selectedTool = e.detail.selectedTool;
      if (e.detail.message) this.selectedMessage = e.detail.message;
      this.materials = e.detail.materials ?? this.materials;
      this.productionQueue = e.detail.productionQueue ?? this.productionQueue;
      this.productionSlots = e.detail.productionSlots ?? this.productionSlots;
      this.storageUsed = e.detail.storageUsed ?? this.storageUsed;
      this.storageCapacity = e.detail.storageCapacity ?? this.storageCapacity;
      this.orders = e.detail.orders ?? this.orders;
      this.completedOrders = e.detail.completedOrders ?? this.completedOrders;
      this.objectives = e.detail.objectives ?? this.objectives;
      this.unlockState = e.detail.unlockState ?? this.unlockState;
      this.update(e.detail.metrics);
    }) as EventListener);

    window.addEventListener('city-tile-selected', ((e: CustomEvent) => {
      this.selectedTile = e.detail.tile ?? null;
      this.selectedMessage = e.detail.message ?? '';
      this.renderSidePanel();
    }) as EventListener);

    this.updateButtonState();
    this.renderManagementPanel();
  }

  private update(m: CityMetrics): void {
    this.metrics = m;
    this.topBar.innerHTML =
      '<span>第 ' + m.day + ' 天 / Lv ' + m.cityLevel + '</span>' +
      '<span>人口: ' + m.population.toLocaleString() + '</span>' +
      '<span>现金: $' + m.cash.toLocaleString() + '</span>' +
      '<span>经验: ' + m.cityExperience + '/' + m.nextLevelExperience + '</span>' +
      '<span>幸福度: ' + m.happiness + '</span>' +
      '<span>评分: ' + m.cityScore + '</span>';
    this.renderSidePanel(m);
    this.renderManagementPanel();
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
        (this.selectedTile.buildingId
          ? '<br>建筑: ' + (SERVICE_BUILDING_LABELS[this.selectedTile.buildingId] ?? this.selectedTile.buildingId)
          : '') +
        '<br>道路: ' + (this.selectedTile.roadId ? (ROAD_LABELS[this.selectedTile.roadId] ?? '已连接') : '无') +
        (this.selectedTile.zone === ZoneType.Residential
          ? '<br>住宅等级: ' + this.residentialLevelLabel(this.selectedTile)
          : '')
      : '<br>地块: 未选择';

    if (!metrics) {
      this.sidePanel.innerHTML = tileText;
      return;
    }

    const recentEventsText = metrics.recentEvents.length
      ? '<br>近期事件:<br>' + metrics.recentEvents.slice(0, 2).join('<br>')
      : '';

    this.sidePanel.innerHTML =
      '等级: Lv ' + metrics.cityLevel + ' ' + metrics.cityLevelName + '<br>' +
      '住房容量: ' + metrics.housingCapacity.toLocaleString() + '<br>' +
      '已开发地块: ' + metrics.buildingCount + '<br>' +
      '道路覆盖: ' + Math.round(metrics.roadCoverage) + '%<br>' +
      '税率: ' + metrics.taxRatePercent + '%<br>' +
      '需求: 住' + metrics.residentialDemand + ' / 商' + metrics.commercialDemand + ' / 工' + metrics.industrialDemand + '<br>' +
      '服务覆盖: 园' + Math.round(metrics.parkCoverage) + '% / 医' + Math.round(metrics.healthCoverage) + '% / 学' + Math.round(metrics.educationCoverage) + '%<br>' +
      '污染: ' + Math.round(metrics.pollution) + ' / 拥堵: ' + Math.round(metrics.congestion) +
      tileText +
      '<br>提醒: ' + metrics.alertDigest +
      recentEventsText;
  }

  private renderManagementPanel(): void {
    const inventoryText = (Object.keys(MATERIAL_LABELS) as MaterialId[])
      .map((materialId) => `${MATERIAL_LABELS[materialId]} ${this.materials[materialId]}`)
      .join(' / ');
    const productionText = this.productionQueue.length
      ? this.productionQueue.map((job) => `${job.label} ${job.remainingDays}/${job.totalDays}天`).join('<br>')
      : '生产队列空闲';
    const residentialUpgrade = this.selectedResidentialUpgradeAction();
    const residentialUpgradeEntry = residentialUpgrade ? this.unlockState?.actions[residentialUpgrade] : null;
    const roadUpgradeEntry = this.unlockState?.actions.roadUpgrade ?? null;
    const residentialUpgradeLocked = residentialUpgradeEntry ? !residentialUpgradeEntry.unlocked : false;
    const roadUpgradeLocked = roadUpgradeEntry ? !roadUpgradeEntry.unlocked : false;
    const currentTaxLevel = this.metrics?.taxLevel ?? CityTaxLevel.Normal;
    const taxRatePercent = this.metrics?.taxRatePercent ?? 9;
    const cashRunwayDays = this.metrics?.cashRunwayDays ?? 999;
    const cashRunwayText = cashRunwayDays >= 999 ? '稳定' : cashRunwayDays + '天';

    this.managementPanel.innerHTML =
      '<strong>财政</strong> 税率 ' + taxRatePercent + '%<br>' +
      '<div style="margin:6px 0;display:flex;gap:6px;flex-wrap:wrap">' +
      this.taxButtonHtml(CityTaxLevel.Low, currentTaxLevel) +
      this.taxButtonHtml(CityTaxLevel.Normal, currentTaxLevel) +
      this.taxButtonHtml(CityTaxLevel.High, currentTaxLevel) +
      '</div>' +
      '<span style="color:#f2d479">风险: ' + (this.metrics?.forecastRisk ?? 0) +
      ' / ' + (this.metrics?.forecastFocus ?? '稳定') +
      ' -> ' + (this.metrics?.forecastAction ?? '继续扩建并保留现金缓冲') +
      ' / 现金续航: ' + cashRunwayText + '</span><br><br>' +
      '<strong>分区需求</strong> 住' + (this.metrics?.residentialDemand ?? 0) +
      ' / 商' + (this.metrics?.commercialDemand ?? 0) +
      ' / 工' + (this.metrics?.industrialDemand ?? 0) + '<br>' +
      '<span style="color:#d8e6ba">' + (this.metrics?.demandAdvice ?? '') + '</span><br>' +
      '<span style="color:#b8c7d9">焦点: ' + (this.metrics?.demandFocus ?? '均衡') +
      ' / 驱动: ' + (this.metrics?.demandDriver ?? '供需稳定') +
      ' / 行动: ' + (this.metrics?.demandAction ?? '继续优化路网') + '</span><br><br>' +
      '<strong>仓库</strong> ' + this.storageUsed + '/' + this.storageCapacity + '<br>' +
      inventoryText + '<br><br>' +
      '<strong>工厂</strong> ' + this.productionQueue.length + '/' + this.productionSlots + '<br>' +
      '<div style="margin:6px 0;display:flex;gap:6px;flex-wrap:wrap">' +
      this.productionButtonHtml('wood') +
      this.productionButtonHtml('metal') +
      this.productionButtonHtml('plastic') +
      '</div>' +
      productionText + '<br><br>' +
      '<strong>城市订单</strong> 已交付 ' + this.completedOrders + '<br>' +
      this.orders.map((order) => this.orderHtml(order)).join('') +
      '<br><strong>城市目标</strong><br>' +
      this.objectives.map((objective) => this.objectiveHtml(objective)).join('') +
      '<div style="margin-top:8px;display:flex;gap:6px;flex-wrap:wrap">' +
      '<button data-action="upgrade" ' + this.disabledAttribute(residentialUpgradeLocked) + ' style="' + this.actionButtonStyle('#6ea85f', residentialUpgradeLocked) + '">升级选中住宅' + this.lockSuffix(residentialUpgradeEntry) + '</button>' +
      '<button data-action="upgrade-road" ' + this.disabledAttribute(roadUpgradeLocked) + ' style="' + this.actionButtonStyle('#3f5f82', roadUpgradeLocked) + '">升级选中道路' + this.lockSuffix(roadUpgradeEntry) + '</button>' +
      '</div>';

    this.managementPanel.querySelectorAll<HTMLButtonElement>('button[data-material]').forEach((button) => {
      button.addEventListener('click', () => {
        const materialId = button.dataset.material as MaterialId;
        window.dispatchEvent(new CustomEvent('city-production-start', { detail: { materialId } }));
      });
    });
    this.managementPanel.querySelectorAll<HTMLButtonElement>('button[data-order]').forEach((button) => {
      button.addEventListener('click', () => {
        window.dispatchEvent(new CustomEvent('city-order-fulfill', { detail: { orderId: button.dataset.order } }));
      });
    });
    this.managementPanel.querySelectorAll<HTMLButtonElement>('button[data-tax-level]').forEach((button) => {
      button.addEventListener('click', () => {
        const level = Number(button.dataset.taxLevel) as CityTaxLevel;
        window.dispatchEvent(new CustomEvent('city-tax-level-change', { detail: { level } }));
      });
    });
    this.managementPanel.querySelector<HTMLButtonElement>('button[data-action="upgrade"]')
      ?.addEventListener('click', () => window.dispatchEvent(new CustomEvent('city-upgrade-selected-residential')));
    this.managementPanel.querySelector<HTMLButtonElement>('button[data-action="upgrade-road"]')
      ?.addEventListener('click', () => window.dispatchEvent(new CustomEvent('city-upgrade-selected-road')));
  }

  private productionButtonHtml(materialId: MaterialId): string {
    const unlockEntry = this.unlockState?.materials[materialId] ?? null;
    const locked = unlockEntry ? !unlockEntry.unlocked : false;
    return '<button data-material="' + materialId + '" ' + this.disabledAttribute(locked) + ' style="' + this.actionButtonStyle('#263239', locked) + '">' +
      MATERIAL_LABELS[materialId] + this.lockSuffix(unlockEntry) +
      '</button>';
  }

  private taxButtonHtml(level: CityTaxLevel, currentLevel: CityTaxLevel): string {
    const selected = level === currentLevel;
    return '<button data-tax-level="' + level + '" style="' + this.actionButtonStyle(selected ? '#6ea85f' : '#263239') + '">' +
      TAX_LABELS[level] +
      '</button>';
  }

  private orderHtml(order: CityOrder): string {
    return '<div style="margin-top:6px;padding-top:6px;border-top:1px solid rgba(255,255,255,0.08)">' +
      order.title + ' +' + order.rewardCash + '<br>' +
      '<span style="color:#aebbb4">' + this.formatCost(order.required) + '</span> ' +
      '<button data-order="' + order.id + '" style="' + this.actionButtonStyle('#3f5f82') + '">交付</button>' +
      '</div>';
  }

  private objectiveHtml(objective: CityObjective): string {
    const state = objective.completed ? '已完成' : '待推进';
    const color = objective.completed ? '#9ed58e' : '#f2d479';
    return '<div style="margin-top:5px;color:' + color + '">' +
      state + ' ' + objective.title + '<br>' +
      '<span style="color:#aebbb4">' + objective.description + ' +$' + objective.rewardCash + ' / 经验+' + objective.rewardExperience + '</span>' +
      (objective.completed ? '' : '<br><span style="color:#d8e6ba">建议: ' + objective.advice + '</span>') +
      '</div>';
  }

  private actionButtonStyle(background: string, locked = false): string {
    return 'height:28px;border:1px solid rgba(255,255,255,0.16);border-radius:5px;' +
      'background:' + (locked ? '#30363a' : background) + ';color:' + (locked ? '#8f9b95' : '#edf7ef') +
      ';font-size:12px;cursor:' + (locked ? 'not-allowed' : 'pointer') + ';padding:0 8px;opacity:' + (locked ? '0.72' : '1') + ';';
  }

  private formatCost(cost: Partial<Record<MaterialId, number>>): string {
    return (Object.entries(cost) as Array<[MaterialId, number]>)
      .map(([materialId, count]) => MATERIAL_LABELS[materialId] + 'x' + count)
      .join('、');
  }

  private residentialLevelLabel(tile: Tile): string {
    const level = this.residentialLevel(tile);
    return level > 0 ? level + '级' : '待开发';
  }

  private residentialLevel(tile: Tile): number {
    if (tile.buildingId === 'residential_l1') return 1;
    const match = /^residential_l([2-3])$/.exec(tile.buildingId);
    return match ? Number(match[1]) : 0;
  }

  private selectedResidentialUpgradeAction(): CityUnlockActionId {
    const nextLevel = this.selectedTile?.zone === ZoneType.Residential
      ? Math.min(3, this.residentialLevel(this.selectedTile) + 1)
      : 2;
    return nextLevel >= 3 ? 'residentialLevel3' : 'residentialLevel2';
  }

  private serviceToolUnlockEntry(tool: PlanningTool): CityUnlockState['services'][ServiceBuildingId] | null {
    const serviceBuildingId = SERVICE_TOOL_TO_BUILDING[tool];
    return serviceBuildingId ? this.unlockState?.services[serviceBuildingId] ?? null : null;
  }

  private lockSuffix(entry?: { unlockLevel: number; unlocked: boolean } | null): string {
    return entry && !entry.unlocked ? ' Lv' + entry.unlockLevel : '';
  }

  private disabledAttribute(locked: boolean): string {
    return locked ? 'disabled aria-disabled="true"' : '';
  }

  private updateButtonState(): void {
    this.buttons.forEach((button, tool) => {
      const selected = tool === this.selectedTool;
      const unlockEntry = this.serviceToolUnlockEntry(tool);
      const locked = unlockEntry ? !unlockEntry.unlocked : false;
      button.disabled = locked;
      button.textContent = TOOL_LABELS[tool] + this.lockSuffix(unlockEntry);
      button.title = locked ? TOOL_LABELS[tool] + ' Lv' + unlockEntry?.unlockLevel + '解锁' : TOOL_LABELS[tool];
      button.style.background = locked ? '#30363a' : selected ? '#6ea85f' : '#263239';
      button.style.color = locked ? '#8f9b95' : selected ? '#07100b' : '#edf7ef';
      button.style.borderColor = locked ? 'rgba(255,255,255,0.08)' : selected ? '#b7e39a' : 'rgba(255,255,255,0.14)';
      button.style.fontWeight = selected ? '700' : '500';
      button.style.cursor = locked ? 'not-allowed' : 'pointer';
    });
  }
}

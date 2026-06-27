import type { ConstructionPreview } from '../simulation/construction-preview';
import type { CityMetrics, OverlayMode } from '../types';
import { formatAlertLine, formatCityTitle, formatMetrics, formatObjectiveLine } from './info-panel';
import type { BuildToolId } from './toolbar';
import { nextLockedToolbarItem, TOOLBAR_ITEMS, toolUnlockStatus } from './toolbar';

export type HudAction =
  | { type: 'select-tool'; tool: BuildToolId }
  | { type: 'save' }
  | { type: 'cycle-overlay' }
  | { type: 'new-city' }
  | { type: 'change_tax' };

type Rect = { x: number; y: number; w: number; h: number };

type Button = Rect & {
  action: HudAction;
  label: string;
  color: string;
};

export type HudState = {
  metrics: CityMetrics;
  selectedTool: BuildToolId;
  overlayMode: OverlayMode;
  buildPreview?: ConstructionPreview;
  taxRate: number;
  toast?: string;
  roadAnchor?: string;
  selectedBuildingLabels?: string[];
  demandAdvisorLabel?: string;
};

export class HudController {
  private width = 0;
  private height = 0;
  private buttons: Button[] = [];

  layout(width: number, height: number): void {
    this.width = width;
    this.height = height;
    const safeBottom = 18;
    const gap = 7;
    const buttonHeight = 42;
    const sidePad = 12;
    const totalGap = gap * (TOOLBAR_ITEMS.length - 1);
    const buttonWidth = Math.floor((width - sidePad * 2 - totalGap) / TOOLBAR_ITEMS.length);
    const toolbarY = height - safeBottom - buttonHeight;

    this.buttons = TOOLBAR_ITEMS.map((item, index) => ({
      x: sidePad + index * (buttonWidth + gap),
      y: toolbarY,
      w: buttonWidth,
      h: buttonHeight,
      label: item.label,
      color: item.color,
      action: { type: 'select-tool', tool: item.id },
    }));

    this.buttons.push({
      x: width - 168,
      y: 14,
      w: 72,
      h: 34,
      label: '图层',
      color: '#334155',
      action: { type: 'cycle-overlay' },
    });

    this.buttons.push({
      x: width - 84,
      y: 14,
      w: 72,
      h: 34,
      label: '保存',
      color: '#0f766e',
      action: { type: 'save' },
    });
  }

  hitTest(x: number, y: number): HudAction | undefined {
    return this.buttons.find((button) => pointInRect(x, y, button))?.action;
  }

  draw(ctx: CanvasRenderingContext2D, state: HudState): void {
    ctx.clearRect(0, 0, this.width, this.height);
    ctx.save();
    ctx.textBaseline = 'middle';
    this.drawTopPanel(ctx, state);
    this.drawSelectedBuildingBadge(ctx, state.selectedBuildingLabels);
    this.drawDemandAdvisorBadge(ctx, state.selectedBuildingLabels, state.demandAdvisorLabel);
    this.drawOverlayBadge(ctx, state.overlayMode);
    if (state.buildPreview) {
      this.drawBuildPreview(ctx, state.buildPreview);
    }
    this.drawToolbar(ctx, state.selectedTool, state.metrics);
    if (state.toast) {
      this.drawToast(ctx, state.toast);
    }
    ctx.restore();
  }

  private drawBuildPreview(ctx: CanvasRenderingContext2D, preview: ConstructionPreview): void {
    const panelWidth = Math.min(372, this.width - 24);
    const panelHeight = 72;
    const x = 12;
    const y = this.height - 104 - panelHeight;
    if (y < 154) {
      return;
    }

    roundedRect(ctx, x, y, panelWidth, panelHeight, 8);
    ctx.fillStyle = preview.ok ? 'rgba(15, 23, 42, 0.82)' : 'rgba(127, 29, 29, 0.82)';
    ctx.fill();

    ctx.fillStyle = '#f8fafc';
    ctx.font = '600 13px sans-serif';
    ctx.textAlign = 'start';
    ctx.fillText(`方案预览 ${preview.title}`, x + 14, y + 17);

    roundedRect(ctx, x + panelWidth - 74, y + 9, 58, 20, 6);
    ctx.fillStyle = preview.ok ? 'rgba(34, 197, 94, 0.9)' : 'rgba(248, 113, 113, 0.9)';
    ctx.fill();
    ctx.fillStyle = '#ffffff';
    ctx.font = '11px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(preview.ok ? preview.confirmLabel : '不可行', x + panelWidth - 45, y + 19);

    ctx.textAlign = 'start';
    ctx.font = '11px sans-serif';
    const lineColors = preview.ok ? ['#dbeafe', '#dcfce7', '#fef3c7'] : ['#fee2e2', '#fecaca', '#fef3c7'];
    preview.lines.slice(0, 3).forEach((line, index) => {
      ctx.fillStyle = lineColors[index] ?? '#e2e8f0';
      ctx.fillText(line, x + 14, y + 39 + index * 14);
    });
  }

  private drawSelectedBuildingBadge(ctx: CanvasRenderingContext2D, lines?: string[]): void {
    if (!lines || lines.length === 0) {
      return;
    }
    const width = Math.min(380, Math.max(210, Math.max(...lines.map((line) => line.length)) * 10));
    const height = 18 + lines.length * 16;
    roundedRect(ctx, 12, 156, width, height, 8);
    ctx.fillStyle = 'rgba(30, 41, 59, 0.82)';
    ctx.fill();
    ctx.fillStyle = '#fde68a';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'start';
    lines.forEach((line, index) => {
      ctx.fillText(line, 24, 170 + index * 15);
    });
  }

  private drawDemandAdvisorBadge(
    ctx: CanvasRenderingContext2D,
    selectedBuildingLabels: string[] | undefined,
    label: string | undefined,
  ): void {
    if (!label) {
      return;
    }
    const y = 156 + (selectedBuildingLabels && selectedBuildingLabels.length > 0 ? 18 + selectedBuildingLabels.length * 16 + 8 : 0);
    const width = Math.min(420, Math.max(240, label.length * 10));
    roundedRect(ctx, 12, y, width, 28, 8);
    ctx.fillStyle = 'rgba(15, 23, 42, 0.78)';
    ctx.fill();
    ctx.fillStyle = '#bfdbfe';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'start';
    ctx.fillText(label, 24, y + 14);
  }

  private drawOverlayBadge(ctx: CanvasRenderingContext2D, overlayMode: OverlayMode): void {
    const label = overlayLabel(overlayMode);
    const width = 92;
    const x = this.width - 266;
    if (x < 620) {
      return;
    }
    roundedRect(ctx, x, 16, width, 30, 8);
    ctx.fillStyle = overlayMode === 'normal' ? 'rgba(15, 23, 42, 0.58)' : 'rgba(14, 116, 144, 0.82)';
    ctx.fill();
    ctx.fillStyle = '#e0f2fe';
    ctx.font = '12px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(label, x + width / 2, 31);
    ctx.textAlign = 'start';
  }

  private drawTopPanel(ctx: CanvasRenderingContext2D, state: HudState): void {
    const lines = formatMetrics(state.metrics);
    const panelWidth = Math.min(344, Math.max(292, this.width * 0.34));
    const panelHeight = 136;
    roundedRect(ctx, 12, 14, panelWidth, panelHeight, 8);
    ctx.fillStyle = 'rgba(16, 24, 40, 0.82)';
    ctx.fill();

    ctx.fillStyle = '#f8fafc';
    ctx.font = '600 15px sans-serif';
    ctx.fillText('口袋城市规划师', 24, 32);

    ctx.fillStyle = '#fef3c7';
    ctx.font = '12px sans-serif';
    ctx.fillText(formatCityTitle(state.metrics), 24, 52);

    ctx.fillStyle = '#d7f5e8';
    ctx.font = '12px sans-serif';
    ctx.fillText(lines.slice(0, 3).join('  '), 24, 73);
    ctx.fillStyle = '#bfdbfe';
    ctx.fillText(lines.slice(3).join('  '), 24, 92);

    ctx.fillStyle = state.metrics.alerts.length > 0 ? '#fecaca' : '#bbf7d0';
    ctx.fillText(formatAlertLine(state.metrics), 24, 111);

    ctx.fillStyle = '#f8fafc';
    ctx.font = '600 12px sans-serif';
    ctx.fillText(formatObjectiveLine(state.metrics), 24, 130);
    this.drawObjectiveProgress(ctx, state.metrics, 190, 126, panelWidth - 204);

    this.drawDemandPanel(ctx, state.metrics, 24 + panelWidth, 14);
    this.drawUnlockPanel(ctx, state.metrics, 24 + panelWidth, 132);

    if (state.roadAnchor) {
      ctx.fillStyle = '#fde68a';
      ctx.fillText(`道路起点 ${state.roadAnchor}`, 24, 130);
    }
  }

  private drawUnlockPanel(ctx: CanvasRenderingContext2D, metrics: CityMetrics, x: number, y: number): void {
    const nextUnlock = nextLockedToolbarItem(metrics);
    const panelWidth = Math.min(268, this.width - x - 112);
    if (!nextUnlock || panelWidth < 190) {
      return;
    }

    roundedRect(ctx, x, y, panelWidth, 50, 8);
    ctx.fillStyle = 'rgba(21, 128, 61, 0.76)';
    ctx.fill();
    ctx.fillStyle = '#f0fdf4';
    ctx.font = '600 12px sans-serif';
    ctx.fillText(`下个解锁 ${nextUnlock.item.label}`, x + 14, y + 16);
    ctx.font = '11px sans-serif';
    ctx.fillStyle = '#dcfce7';
    ctx.fillText(nextUnlock.status.reason, x + 14, y + 32);

    const progress = Math.min(1, nextUnlock.status.progress / Math.max(1, nextUnlock.status.required));
    roundedRect(ctx, x + panelWidth - 96, y + 27, 78, 8, 4);
    ctx.fillStyle = 'rgba(240, 253, 244, 0.24)';
    ctx.fill();
    roundedRect(ctx, x + panelWidth - 96, y + 27, Math.max(4, 78 * progress), 8, 4);
    ctx.fillStyle = '#bef264';
    ctx.fill();
  }

  private drawObjectiveProgress(ctx: CanvasRenderingContext2D, metrics: CityMetrics, x: number, y: number, width: number): void {
    if (width < 68) {
      return;
    }
    const objective = metrics.activeObjective;
    const progress = objective.required <= 0 ? 1 : Math.min(1, objective.progress / objective.required);
    roundedRect(ctx, x, y, width, 8, 4);
    ctx.fillStyle = 'rgba(226, 232, 240, 0.22)';
    ctx.fill();
    roundedRect(ctx, x, y, Math.max(4, width * progress), 8, 4);
    ctx.fillStyle = objective.done ? '#22c55e' : '#facc15';
    ctx.fill();
  }

  private drawDemandPanel(ctx: CanvasRenderingContext2D, metrics: CityMetrics, x: number, y: number): void {
    const panelWidth = Math.min(268, this.width - x - 112);
    if (panelWidth < 190) {
      return;
    }

    roundedRect(ctx, x, y, panelWidth, 112, 8);
    ctx.fillStyle = 'rgba(15, 23, 42, 0.72)';
    ctx.fill();
    ctx.fillStyle = '#f8fafc';
    ctx.font = '600 13px sans-serif';
    ctx.fillText('城市需求', x + 14, y + 20);

    this.drawDemandBar(ctx, '住', metrics.demand.residential, '#22c55e', x + 14, y + 42, panelWidth - 28);
    this.drawDemandBar(ctx, '商', metrics.demand.commercial, '#38bdf8', x + 14, y + 68, panelWidth - 28);
    this.drawDemandBar(ctx, '工', metrics.demand.industrial, '#f97316', x + 14, y + 94, panelWidth - 28);
  }

  private drawDemandBar(
    ctx: CanvasRenderingContext2D,
    label: string,
    value: number,
    color: string,
    x: number,
    y: number,
    width: number,
  ): void {
    ctx.fillStyle = '#cbd5e1';
    ctx.font = '12px sans-serif';
    ctx.fillText(label, x, y);
    const barX = x + 24;
    const barWidth = width - 54;
    roundedRect(ctx, barX, y - 6, barWidth, 10, 5);
    ctx.fillStyle = 'rgba(226, 232, 240, 0.2)';
    ctx.fill();
    roundedRect(ctx, barX, y - 6, Math.max(4, (barWidth * value) / 100), 10, 5);
    ctx.fillStyle = color;
    ctx.fill();
    ctx.fillStyle = '#e2e8f0';
    ctx.textAlign = 'right';
    ctx.fillText(`${value}`, x + width, y);
    ctx.textAlign = 'start';
  }

  private drawToolbar(ctx: CanvasRenderingContext2D, selectedTool: BuildToolId, metrics: CityMetrics): void {
    for (const button of this.buttons) {
      const isSelected =
        button.action.type === 'select-tool' && button.action.tool === selectedTool;
      const unlock =
        button.action.type === 'select-tool'
          ? toolUnlockStatus(button.action.tool, metrics)
          : { unlocked: true, reason: '', progress: 1, required: 1 };
      roundedRect(ctx, button.x, button.y, button.w, button.h, 8);
      ctx.fillStyle = isSelected ? button.color : unlock.unlocked ? 'rgba(15, 23, 42, 0.72)' : 'rgba(15, 23, 42, 0.46)';
      ctx.fill();
      ctx.lineWidth = isSelected ? 2 : 1;
      ctx.strokeStyle = isSelected ? '#ffffff' : unlock.unlocked ? 'rgba(255,255,255,0.2)' : 'rgba(255,255,255,0.12)';
      ctx.stroke();
      ctx.fillStyle = unlock.unlocked ? '#ffffff' : '#94a3b8';
      ctx.font = `${button.w < 46 ? 10 : 12}px sans-serif`;
      ctx.textAlign = 'center';
      ctx.fillText(button.label, button.x + button.w / 2, button.y + (unlock.unlocked ? button.h / 2 : 16));
      if (!unlock.unlocked && button.w >= 52) {
        ctx.font = `${button.w < 64 ? 9 : 10}px sans-serif`;
        ctx.fillText('未解锁', button.x + button.w / 2, button.y + 30);
      }
    }
    ctx.textAlign = 'start';
  }

  private drawToast(ctx: CanvasRenderingContext2D, message: string): void {
    const width = Math.min(this.width - 40, Math.max(180, message.length * 14));
    const x = (this.width - width) / 2;
    const y = this.height - 116;
    roundedRect(ctx, x, y, width, 36, 8);
    ctx.fillStyle = 'rgba(2, 6, 23, 0.78)';
    ctx.fill();
    ctx.fillStyle = '#ffffff';
    ctx.font = '13px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(message, this.width / 2, y + 18);
    ctx.textAlign = 'start';
  }
}

function pointInRect(x: number, y: number, rect: Rect): boolean {
  return x >= rect.x && y >= rect.y && x <= rect.x + rect.w && y <= rect.y + rect.h;
}

function overlayLabel(mode: OverlayMode): string {
  switch (mode) {
    case 'normal':
      return '普通视图';
    case 'traffic':
      return '交通图层';
    case 'pollution':
      return '污染图层';
    case 'zone':
      return '区划图层';
  }
}

function roundedRect(ctx: CanvasRenderingContext2D, x: number, y: number, w: number, h: number, r: number): void {
  const radius = Math.min(r, w / 2, h / 2);
  ctx.beginPath();
  ctx.moveTo(x + radius, y);
  ctx.arcTo(x + w, y, x + w, y + h, radius);
  ctx.arcTo(x + w, y + h, x, y + h, radius);
  ctx.arcTo(x, y + h, x, y, radius);
  ctx.arcTo(x, y, x + w, y, radius);
  ctx.closePath();
}

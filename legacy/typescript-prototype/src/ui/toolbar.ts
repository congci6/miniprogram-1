import type { CityMetrics, ZoneType } from '../types';
import { buildingIdUnlockStatus, type UnlockStatus } from '../simulation/unlocks';

export type BuildToolId =
  | 'road'
  | 'zone_residential'
  | 'zone_commercial'
  | 'zone_industrial'
  | 'zone_clear'
  | 'residential_pod'
  | 'market_corner'
  | 'maker_yard'
  | 'pocket_park'
  | 'micro_power'
  | 'water_tower'
  | 'demolish';

const ZONE_TOOL_IDS: ReadonlySet<BuildToolId> = new Set([
  'zone_residential', 'zone_commercial', 'zone_industrial', 'zone_clear',
]);

export type ToolbarItem = {
  id: BuildToolId;
  label: string;
  color: string;
};

export const TOOLBAR_ITEMS: ToolbarItem[] = [
  { id: 'zone_residential', label: '住宅区', color: '#86efac' },
  { id: 'zone_commercial', label: '商业区', color: '#7dd3fc' },
  { id: 'zone_industrial', label: '工业区', color: '#fdba74' },
  { id: 'zone_clear', label: '清空区划', color: '#d1d5db' },
  { id: 'road', label: '道路', color: '#4b5563' },
  { id: 'residential_pod', label: '住宅', color: '#22c55e' },
  { id: 'market_corner', label: '商业', color: '#38bdf8' },
  { id: 'maker_yard', label: '工业', color: '#f97316' },
  { id: 'pocket_park', label: '公园', color: '#84cc16' },
  { id: 'micro_power', label: '电力', color: '#facc15' },
  { id: 'water_tower', label: '水务', color: '#2dd4bf' },
  { id: 'demolish', label: '拆除', color: '#ef4444' },
];

export function isBuildingTool(tool: BuildToolId): boolean {
  return !isZoneTool(tool) && tool !== 'road' && tool !== 'demolish';
}

export function isZoneTool(tool: BuildToolId): boolean {
  return ZONE_TOOL_IDS.has(tool);
}

export function zoneTypeForTool(tool: BuildToolId): ZoneType {
  if (tool === 'zone_residential') return 'residential';
  if (tool === 'zone_commercial') return 'commercial';
  if (tool === 'zone_industrial') return 'industrial';
  return 'none';
}

export function toolUnlockStatus(tool: BuildToolId, metrics: CityMetrics): UnlockStatus {
  if (isZoneTool(tool)) {
    return { unlocked: true, reason: '已解锁', progress: 1, required: 1 };
  }
  if (!isBuildingTool(tool)) {
    return { unlocked: true, reason: '已解锁', progress: 1, required: 1 };
  }
  return buildingIdUnlockStatus(tool, metrics);
}

export function nextLockedToolbarItem(metrics: CityMetrics): { item: ToolbarItem; status: UnlockStatus } | undefined {
  let closest: { item: ToolbarItem; status: UnlockStatus; ratio: number } | undefined;
  for (const item of TOOLBAR_ITEMS) {
    const status = toolUnlockStatus(item.id, metrics);
    if (!status.unlocked) {
      const ratio = status.progress / Math.max(1, status.required);
      if (!closest || ratio > closest.ratio) {
        closest = { item, status, ratio };
      }
    }
  }
  return closest ? { item: closest.item, status: closest.status } : undefined;
}

import type { BuildToolId } from './toolbar';

export function buildingIdForTool(tool: BuildToolId): string | undefined {
  if (
    tool === 'road' ||
    tool === 'demolish' ||
    tool === 'zone_residential' ||
    tool === 'zone_commercial' ||
    tool === 'zone_industrial' ||
    tool === 'zone_clear'
  ) {
    return undefined;
  }
  return tool;
}

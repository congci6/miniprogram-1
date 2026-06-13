import type { BuildToolId } from './toolbar';

export function buildingIdForTool(tool: BuildToolId): string | undefined {
  if (tool === 'road' || tool === 'demolish') {
    return undefined;
  }
  return tool;
}

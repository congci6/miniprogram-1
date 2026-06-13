import type { BuildingCategory, ZoneType } from '../types';

export function zoneForBuildingCategory(category: BuildingCategory): ZoneType {
  if (category === 'residential' || category === 'commercial' || category === 'industrial') {
    return category;
  }
  return 'none';
}

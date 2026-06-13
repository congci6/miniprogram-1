import type { BuildingConfig } from '../types';

export const BUILDINGS: BuildingConfig[] = [
  {
    id: 'residential_pod',
    name: '住宅舱',
    category: 'residential',
    size: { w: 2, h: 2 },
    cost: 260,
    upkeep: 4,
    capacity: 48,
    jobs: 0,
    powerUse: 2,
    waterUse: 2,
    pollution: 0,
    modelKey: 'residential',
  },
  {
    id: 'market_corner',
    name: '街角商铺',
    category: 'commercial',
    size: { w: 2, h: 2 },
    cost: 420,
    upkeep: 8,
    capacity: 0,
    jobs: 24,
    powerUse: 4,
    waterUse: 2,
    pollution: 1,
    modelKey: 'commercial',
  },
  {
    id: 'maker_yard',
    name: '制造工坊',
    category: 'industrial',
    size: { w: 3, h: 3 },
    cost: 760,
    upkeep: 14,
    capacity: 0,
    jobs: 60,
    powerUse: 8,
    waterUse: 5,
    pollution: 8,
    unlock: {
      minPopulation: 80,
      minCityScore: 55,
    },
    modelKey: 'industrial',
  },
  {
    id: 'pocket_park',
    name: '口袋公园',
    category: 'service',
    size: { w: 2, h: 2 },
    cost: 540,
    upkeep: 10,
    capacity: 0,
    jobs: 4,
    powerUse: 1,
    waterUse: 1,
    pollution: 0,
    serviceRadius: 8,
    unlock: {
      minPopulation: 40,
      minCityScore: 55,
    },
    modelKey: 'park',
  },
  {
    id: 'micro_power',
    name: '微型电站',
    category: 'utility',
    size: { w: 3, h: 2 },
    cost: 900,
    upkeep: 18,
    powerOutput: 72,
    waterUse: 1,
    pollution: 5,
    serviceRadius: 10,
    modelKey: 'power',
  },
  {
    id: 'water_tower',
    name: '净水塔',
    category: 'utility',
    size: { w: 2, h: 2 },
    cost: 680,
    upkeep: 12,
    powerUse: 2,
    waterOutput: 80,
    pollution: 0,
    serviceRadius: 10,
    modelKey: 'water',
  },
];

export const BUILDING_BY_ID = new Map(BUILDINGS.map((building) => [building.id, building]));

export function getBuildingConfig(id: string): BuildingConfig {
  const config = BUILDING_BY_ID.get(id);
  if (!config) {
    throw new Error(`Unknown building id: ${id}`);
  }
  return config;
}

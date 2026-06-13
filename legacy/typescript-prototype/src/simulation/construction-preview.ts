import { BALANCE } from '../data/balance';
import { getBuildingConfig } from '../data/buildings';
import { ROAD } from '../data/roads';
import { manhattanLine, nearestRoadId } from '../map/placement';
import { zoneForBuildingCategory } from '../map/zoning';
import type { GridPos } from '../types';
import type { CityState } from './city-state';
import { buildingIdUnlockStatus } from './unlocks';

export type ConstructionTarget =
  | { type: 'building'; buildingId: string }
  | { type: 'road'; from?: GridPos }
  | { type: 'demolish' };

export type ConstructionPreview = {
  title: string;
  lines: string[];
  ok: boolean;
  confirmLabel: string;
};

export function previewConstruction(city: CityState, target: ConstructionTarget, pos: GridPos): ConstructionPreview {
  if (!city.grid.inBounds(pos)) {
    return blockedPreview('地图边界外', ['请选择地图内的地块']);
  }

  switch (target.type) {
    case 'building':
      return previewBuilding(city, target.buildingId, pos);
    case 'road':
      return previewRoad(city, target.from, pos);
    case 'demolish':
      return previewDemolish(city, pos);
  }
}

function previewBuilding(city: CityState, buildingId: string, pos: GridPos): ConstructionPreview {
  const config = getBuildingConfig(buildingId);
  const tile = city.grid.getTile(pos);
  const baseLines = [
    `花费 ${config.cost}  维护 ${config.upkeep}`,
    buildingEffectLine(config),
    `地价 ${Math.round(tile.landValue)}  污染 ${Math.round(tile.pollution)}`,
  ].filter(Boolean);

  const unlock = buildingIdUnlockStatus(buildingId, city.metrics);
  if (!unlock.unlocked) {
    return blockedPreview(config.name, [unlock.reason, ...baseLines]);
  }

  if (config.cost > city.metrics.cash) {
    return blockedPreview(config.name, ['现金不足', ...baseLines]);
  }

  const placement = city.grid.canPlaceBuilding(pos, config.size);
  if (!placement.ok) {
    return blockedPreview(config.name, [placement.reason ?? '无法建造', ...baseLines]);
  }

  const expectedZone = zoneForBuildingCategory(config.category);
  if (expectedZone !== 'none' && tile.zone !== 'none' && tile.zone !== expectedZone) {
    return blockedPreview(config.name, ['建筑类型与当前分区不匹配', ...baseLines]);
  }

  const connectedRoadId = nearestRoadId(city.grid, pos, BALANCE.maxRoadSearchDistance, config.size);
  return {
    title: config.name,
    lines: [
      baseLines[0],
      baseLines[1],
      connectedRoadId ? '接路良好，建筑可满效率运行' : '附近无道路，建成后只有 20% 效率',
      baseLines[2],
      '再次点击同一地块确认',
    ].filter(Boolean),
    ok: true,
    confirmLabel: '建造',
  };
}

function previewRoad(city: CityState, from: GridPos | undefined, pos: GridPos): ConstructionPreview {
  if (!from) {
    const tile = city.grid.getTile(pos);
    const canStart = Boolean(tile.roadId) || city.grid.canPlaceRoad(pos);
    return {
      title: '道路起点',
      lines: [
        `坐标 ${pos.x},${pos.y}`,
        `单格成本 ${ROAD.cost}`,
        canStart ? '再次选择终点铺设道路' : '水面或建筑上不能铺路',
      ],
      ok: canStart,
      confirmLabel: '设为起点',
    };
  }

  const points = uniquePositions(manhattanLine(from, pos));
  const blocked = points.find((point) => {
    const tile = city.grid.getTile(point);
    return !tile.roadId && !city.grid.canPlaceRoad(point);
  });
  const newPoints = points.filter((point) => !city.grid.getTile(point).roadId);
  const cost = newPoints.length * ROAD.cost;
  if (blocked) {
    return blockedPreview('道路方案', [`${blocked.x},${blocked.y} 不能铺路`, `长度 ${points.length} 格`]);
  }
  if (cost > city.metrics.cash) {
    return blockedPreview('道路方案', ['现金不足', `新建 ${newPoints.length} 格  花费 ${cost}`]);
  }

  return {
    title: '道路方案',
    lines: [`长度 ${points.length} 格`, `新建 ${newPoints.length} 格  花费 ${cost}`, '点击终点后铺设折线路径'],
    ok: true,
    confirmLabel: '铺设',
  };
}

function previewDemolish(city: CityState, pos: GridPos): ConstructionPreview {
  const buildingId = city.grid.findBuildingIdAt(pos);
  if (buildingId) {
    const building = city.getBuildings().find((item) => item.id === buildingId);
    if (!building) {
      return blockedPreview('拆除', ['建筑数据缺失']);
    }
    const config = getBuildingConfig(building.configId);
    const refund = Math.floor(config.cost * BALANCE.demolishRefundRate);
    return {
      title: `拆除 ${config.name}`,
      lines: [`回收 ${refund}`, '会移除建筑容量、岗位或服务', '再次点击同一地块确认'],
      ok: true,
      confirmLabel: '拆除',
    };
  }

  if (city.grid.getTile(pos).roadId) {
    return {
      title: '拆除道路',
      lines: ['可能让附近建筑失去接路效率', '再次点击同一地块确认'],
      ok: true,
      confirmLabel: '拆除',
    };
  }

  return blockedPreview('拆除', ['这里没有可拆除对象']);
}

function buildingEffectLine(config: ReturnType<typeof getBuildingConfig>): string {
  const effects: string[] = [];
  if (config.capacity) effects.push(`住宅 +${config.capacity}`);
  if (config.jobs) effects.push(`岗位 +${config.jobs}`);
  if (config.powerOutput) effects.push(`供电 +${config.powerOutput}`);
  if (config.waterOutput) effects.push(`供水 +${config.waterOutput}`);
  if (config.serviceRadius) effects.push(`服务半径 ${config.serviceRadius}`);
  if (config.powerUse) effects.push(`用电 ${config.powerUse}`);
  if (config.waterUse) effects.push(`用水 ${config.waterUse}`);
  if (config.pollution) effects.push(`污染 ${config.pollution}`);
  return effects.join('  ');
}

function blockedPreview(title: string, lines: string[]): ConstructionPreview {
  return {
    title,
    lines,
    ok: false,
    confirmLabel: '不可执行',
  };
}

function uniquePositions(points: GridPos[]): GridPos[] {
  const seen = new Set<string>();
  const unique: GridPos[] = [];
  for (const point of points) {
    const key = `${point.x},${point.y}`;
    if (!seen.has(key)) {
      unique.push(point);
      seen.add(key);
    }
  }
  return unique;
}

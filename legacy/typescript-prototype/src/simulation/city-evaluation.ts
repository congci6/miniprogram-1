import { CITY_LEVELS } from '../data/levels';
import { getBuildingConfig } from '../data/buildings';
import { STARTER_MISSIONS } from '../data/missions';
import type { CityMetrics, CityObjective, DemandMetrics, PlacedBuilding } from '../types';

export function evaluateCity(metrics: CityMetrics, buildings: PlacedBuilding[], taxRate: number): Pick<
  CityMetrics,
  'demand' | 'cityScore' | 'cityLevelName' | 'alerts' | 'activeObjective'
> {
  const cityScore = calculateCityScore(metrics);
  const evaluatedMetrics = { ...metrics, cityScore };
  return {
    demand: calculateDemand(metrics, buildings, taxRate),
    cityScore,
    cityLevelName: cityLevelName(metrics.population),
    alerts: buildAlerts(metrics),
    activeObjective: activeObjective(evaluatedMetrics),
  };
}

export function defaultDemand(): DemandMetrics {
  return {
    residential: 55,
    commercial: 38,
    industrial: 35,
  };
}

export function defaultObjective(): CityObjective {
  return {
    title: '规划第一片街区',
    hint: '先铺路，再围绕道路发展住宅和服务。',
    progress: 0,
    required: 1,
    done: false,
  };
}

function calculateDemand(metrics: CityMetrics, buildings: PlacedBuilding[], taxRate: number): DemandMetrics {
  const categoryCounts = countBuildings(buildings);
  const housingVacancy = Math.max(0, metrics.housingCapacity - metrics.population);
  const jobCapacity = Math.max(1, metrics.jobs);
  const employmentPressure = metrics.population === 0 ? 0 : Math.max(0, metrics.population * 0.55 - jobCapacity);
  const servicePenalty = serviceShortage(metrics) * 22;

  const residential =
    48 +
    metrics.happiness * 0.38 -
    housingVacancy * 0.35 -
    Math.max(0, taxRate) * 80 -
    servicePenalty -
    metrics.pollution * 0.35 +
    metrics.serviceCoverage * 0.12;

  const commercial =
    28 +
    Math.min(42, metrics.population * 0.09) +
    metrics.happiness * 0.16 -
    categoryCounts.commercial * 12 -
    metrics.congestion * 0.22 -
    servicePenalty * 0.7;

  const industrial =
    24 +
    Math.min(40, metrics.population * 0.08) +
    employmentPressure * 0.35 -
    categoryCounts.industrial * 10 -
    metrics.pollution * 0.45 -
    servicePenalty * 0.6;

  return {
    residential: clampScore(residential),
    commercial: clampScore(commercial),
    industrial: clampScore(industrial),
  };
}

function calculateCityScore(metrics: CityMetrics): number {
  const powerReliability = metrics.powerDemand === 0 ? 100 : Math.min(100, (metrics.powerSupply / metrics.powerDemand) * 100);
  const waterReliability = metrics.waterDemand === 0 ? 100 : Math.min(100, (metrics.waterSupply / metrics.waterDemand) * 100);
  const moneyScore = metrics.cash >= 0 ? Math.min(100, 55 + metrics.cash / 220) : Math.max(0, 35 + metrics.cash / 100);
  const capacityScore =
    metrics.housingCapacity === 0 ? 45 : Math.min(100, 60 + ((metrics.housingCapacity - metrics.population) / metrics.housingCapacity) * 45);

  return clampScore(
    metrics.happiness * 0.34 +
      powerReliability * 0.12 +
      waterReliability * 0.12 +
      moneyScore * 0.12 +
      capacityScore * 0.1 +
      metrics.serviceCoverage * 0.08 +
      Math.min(100, metrics.population / 12) * 0.1 -
      metrics.disconnectedBuildings * 4 -
      metrics.congestion * 0.28 -
      metrics.pollution * 0.35,
  );
}

function buildAlerts(metrics: CityMetrics): string[] {
  const alerts: string[] = [];
  if (metrics.powerDemand > metrics.powerSupply) alerts.push('电力不足');
  if (metrics.waterDemand > metrics.waterSupply) alerts.push('水务不足');
  if (metrics.disconnectedBuildings > 0) alerts.push(`${metrics.disconnectedBuildings}栋未接路`);
  if (metrics.population >= metrics.housingCapacity && metrics.housingCapacity > 0) alerts.push('住宅紧张');
  if (metrics.jobs < metrics.population * 0.45) alerts.push('岗位不足');
  if (metrics.congestion >= 30) alerts.push('道路拥堵');
  if (metrics.pollution >= 12) alerts.push('污染偏高');
  if (metrics.population >= 80 && metrics.serviceCoverage < 35) alerts.push('公共服务不足');
  if (metrics.happiness < 40) alerts.push('幸福偏低');
  if (metrics.cash < 0) alerts.push('财政赤字');
  return alerts.slice(0, 4);
}

function activeObjective(metrics: CityMetrics): CityObjective {
  for (const mission of STARTER_MISSIONS) {
    const progress = missionProgress(metrics, mission.target);
    if (progress < mission.required) {
      return {
        title: mission.title,
        hint: mission.hint,
        progress,
        required: mission.required,
        done: false,
      };
    }
  }

  return {
    title: '提高城市评分',
    hint: '继续扩展路网，保持服务充足并控制污染。',
    progress: metrics.cityScore,
    required: 85,
    done: metrics.cityScore >= 85,
  };
}

function missionProgress(metrics: CityMetrics, target: string): number {
  switch (target) {
    case 'roadTiles':
      return metrics.roadTiles;
    case 'connectedBuildings':
      return metrics.connectedBuildings;
    case 'population':
      return Math.floor(metrics.population);
    case 'cityScore':
      return metrics.cityScore;
    case 'serviceCoverage':
      return metrics.serviceCoverage;
    default:
      return 0;
  }
}

function cityLevelName(population: number): string {
  return [...CITY_LEVELS]
    .sort((a, b) => b.minPopulation - a.minPopulation)
    .find((level) => population >= level.minPopulation)?.name ?? CITY_LEVELS[0].name;
}

function countBuildings(buildings: PlacedBuilding[]): Record<'residential' | 'commercial' | 'industrial', number> {
  const counts = {
    residential: 0,
    commercial: 0,
    industrial: 0,
  };
  for (const building of buildings) {
    const category = getBuildingConfig(building.configId).category;
    if (category === 'residential' || category === 'commercial' || category === 'industrial') {
      counts[category] += 1;
    }
  }
  return counts;
}

function serviceShortage(metrics: CityMetrics): number {
  const power = metrics.powerDemand === 0 ? 0 : Math.max(0, 1 - metrics.powerSupply / metrics.powerDemand);
  const water = metrics.waterDemand === 0 ? 0 : Math.max(0, 1 - metrics.waterSupply / metrics.waterDemand);
  return Math.max(power, water);
}

function clampScore(value: number): number {
  return Math.round(Math.max(0, Math.min(100, value)));
}

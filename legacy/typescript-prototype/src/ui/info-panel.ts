import type { CityMetrics } from '../types';

export function formatMetrics(metrics: CityMetrics): string[] {
  return [
    `现金 ${Math.round(metrics.cash)}`,
    `人口 ${Math.round(metrics.population)}/${metrics.housingCapacity}`,
    `幸福 ${Math.round(metrics.happiness)}`,
    `电力 ${metrics.powerDemand}/${metrics.powerSupply}`,
    `水务 ${metrics.waterDemand}/${metrics.waterSupply}`,
    `服务 ${metrics.serviceCoverage}%`,
  ];
}

export function formatCityTitle(metrics: CityMetrics): string {
  return `${metrics.cityLevelName}  评分 ${metrics.cityScore}`;
}

export function formatAlertLine(metrics: CityMetrics): string {
  return metrics.alerts.length > 0 ? metrics.alerts.join(' / ') : '运行平稳';
}

export function formatObjectiveLine(metrics: CityMetrics): string {
  const objective = metrics.activeObjective;
  return `${objective.title} ${Math.min(objective.progress, objective.required)}/${objective.required}`;
}

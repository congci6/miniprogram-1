import { CityMetrics } from '@/types/index';

export class HUD {
  private topBar: HTMLElement;
  private sidePanel: HTMLElement;

  constructor() {
    const c = document.getElementById('hud-overlay')!;
    c.style.pointerEvents = 'none';

    this.topBar = document.createElement('div');
    this.topBar.style.cssText =
      'position:absolute;top:0;left:0;right:0;padding:8px 16px;' +
      'background:rgba(0,0,0,0.65);color:#fff;font-size:14px;' +
      'display:flex;justify-content:space-between;pointer-events:auto;z-index:20;';
    c.appendChild(this.topBar);

    this.sidePanel = document.createElement('div');
    this.sidePanel.style.cssText =
      'position:absolute;bottom:8px;left:8px;padding:8px;' +
      'background:rgba(0,0,0,0.55);color:#ccc;font-size:12px;' +
      'border-radius:4px;pointer-events:auto;z-index:20;max-width:280px;';
    c.appendChild(this.sidePanel);

    window.addEventListener('city-metrics-update', ((e: CustomEvent) => {
      this.update(e.detail.metrics);
    }) as EventListener);
  }

  private update(m: CityMetrics): void {
    this.topBar.innerHTML =
      '<span>第 ' + m.day + ' 天</span>' +
      '<span>人口: ' + m.population + '</span>' +
      '<span>现金: $' + m.cash.toLocaleString() + '</span>' +
      '<span>幸福度: ' + m.happiness + '</span>' +
      '<span>评分: ' + m.cityScore + '</span>';
    this.sidePanel.innerHTML =
      '住房容量: ' + m.housingCapacity + '<br>' +
      '建筑: ' + m.buildingCount + '<br>' +
      '道路覆盖: ' + Math.round(m.roadCoverage) + '%<br>' +
      (m.alerts.length ? '⚠ ' + m.alerts.join('<br>⚠ ') : '');
  }
}

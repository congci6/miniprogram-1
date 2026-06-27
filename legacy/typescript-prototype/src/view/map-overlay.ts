import * as THREE from 'three';
import type { CityState } from '../simulation/city-state';
import type { OverlayMode } from '../types';

export class MapOverlay extends THREE.Group {
  private readonly sourceGeometry = new THREE.BoxGeometry(0.96, 0.04, 0.96);
  private mode: OverlayMode = 'normal';

  constructor() {
    super();
    this.name = 'MapOverlay';
  }

  setMode(mode: OverlayMode, city: CityState): void {
    if (this.mode === mode && this.children.length > 0) {
      return;
    }
    this.mode = mode;
    this.sync(city);
  }

  sync(city: CityState): void {
    this.clearMeshes();
    if (this.mode === 'normal') {
      return;
    }

    if (this.mode === 'traffic') {
      this.buildTrafficOverlay(city);
      return;
    }

    this.buildPollutionOverlay(city);
  }

  dispose(): void {
    this.clearMeshes();
    this.sourceGeometry.dispose();
  }

  private buildTrafficOverlay(city: CityState): void {
    const geometryByLevel = new Map<number, THREE.Geometry>();
    const dummy = new THREE.Mesh(this.sourceGeometry);
    for (const road of city.getRoads()) {
      const pressure = road.capacity <= 0 ? 0 : road.load / road.capacity;
      const level = pressure > 0.85 ? 2 : pressure > 0.5 ? 1 : 0;
      const geometry = geometryByLevel.get(level) ?? new THREE.Geometry();
      dummy.position.set(road.pos.x - city.grid.width / 2 + 0.5, 0.14, road.pos.y - city.grid.height / 2 + 0.5);
      dummy.scale.set(0.92, 1, 0.92);
      dummy.rotation.set(0, 0, 0);
      dummy.updateMatrix();
      geometry.merge(this.sourceGeometry, dummy.matrix);
      geometryByLevel.set(level, geometry);
    }
    this.addLevelMeshes(geometryByLevel, [0x38bdf8, 0xfacc15, 0xef4444], 0.62);
  }

  private buildPollutionOverlay(city: CityState): void {
    const geometryByLevel = new Map<number, THREE.Geometry>();
    const dummy = new THREE.Mesh(this.sourceGeometry);
    city.grid.forEachTile((tile, pos) => {
      if (tile.pollution < 1.6) {
        return;
      }
      const level = tile.pollution > 10 ? 2 : tile.pollution > 5 ? 1 : 0;
      const geometry = geometryByLevel.get(level) ?? new THREE.Geometry();
      dummy.position.set(pos.x - city.grid.width / 2 + 0.5, 0.13, pos.y - city.grid.height / 2 + 0.5);
      dummy.scale.set(0.95, 1, 0.95);
      dummy.rotation.set(0, 0, 0);
      dummy.updateMatrix();
      geometry.merge(this.sourceGeometry, dummy.matrix);
      geometryByLevel.set(level, geometry);
    });
    this.addLevelMeshes(geometryByLevel, [0xfbbf24, 0xf97316, 0xb91c1c], 0.46);
  }

  private buildZoneOverlay(city: CityState): void {
    const colors: Record<string, number> = {
      residential: 0x22c55e,
      commercial: 0x38bdf8,
      industrial: 0xf97316,
    };
    const geometryByZone = new Map<string, THREE.Geometry>();
    const dummy = new THREE.Mesh(this.sourceGeometry);
    city.grid.forEachTile((tile, pos) => {
      if (tile.zone === 'none') {
        return;
      }
      const geometry = geometryByZone.get(tile.zone) ?? new THREE.Geometry();
      dummy.position.set(pos.x - city.grid.width / 2 + 0.5, 0.12, pos.y - city.grid.height / 2 + 0.5);
      dummy.scale.set(0.94, 1, 0.94);
      dummy.rotation.set(0, 0, 0);
      dummy.updateMatrix();
      geometry.merge(this.sourceGeometry, dummy.matrix);
      geometryByZone.set(tile.zone, geometry);
    });
    for (const [zone, geometry] of geometryByZone.entries()) {
      geometry.computeFaceNormals();
      geometry.computeBoundingSphere();
      const material = new THREE.MeshBasicMaterial({
        color: colors[zone] ?? 0x9ca3af,
        transparent: true,
        opacity: 0.35,
        depthWrite: false,
      });
      this.add(new THREE.Mesh(geometry, material));
    }
  }

  private addLevelMeshes(geometryByLevel: Map<number, THREE.Geometry>, colors: number[], opacity: number): void {
    for (const [level, geometry] of geometryByLevel.entries()) {
      geometry.computeFaceNormals();
      geometry.computeBoundingSphere();
      const material = new THREE.MeshBasicMaterial({
        color: colors[level],
        transparent: true,
        opacity,
        depthWrite: false,
      });
      this.add(new THREE.Mesh(geometry, material));
    }
  }

  private clearMeshes(): void {
    for (const child of [...this.children]) {
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        const material = child.material;
        if (Array.isArray(material)) {
          material.forEach((item) => item.dispose());
        } else {
          material.dispose();
        }
      }
      this.remove(child);
    }
  }
}

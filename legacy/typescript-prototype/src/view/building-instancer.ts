import * as THREE from 'three';
import { getBuildingConfig } from '../data/buildings';
import type { CityState } from '../simulation/city-state';
import type { BuildingCategory, PlacedBuilding } from '../types';

const CATEGORY_COLORS: Record<BuildingCategory, number> = {
  residential: 0xfff1a6,
  commercial: 0x2f9bd8,
  industrial: 0xf77f00,
  utility: 0xfcbf49,
  service: 0x84cc16,
};

const MODEL_HEIGHTS: Record<string, number> = {
  residential: 1.25,
  commercial: 1.55,
  industrial: 1.35,
  power: 1.9,
  water: 2.1,
  park: 0.42,
};

export class BuildingInstancer extends THREE.Group {
  private readonly geometry = new THREE.BoxGeometry(1, 1, 1);

  constructor() {
    super();
    this.name = 'BuildingInstancer';
  }

  sync(city: CityState): void {
    this.clearMeshes();
    const byModel = new Map<string, PlacedBuilding[]>();
    for (const building of city.getBuildings()) {
      const config = getBuildingConfig(building.configId);
      const list = byModel.get(config.modelKey) ?? [];
      list.push(building);
      byModel.set(config.modelKey, list);
    }

    for (const [modelKey, buildings] of byModel.entries()) {
      const firstConfig = getBuildingConfig(buildings[0].configId);
      const material = new THREE.MeshLambertMaterial({
        color: CATEGORY_COLORS[firstConfig.category],
        flatShading: true,
      });
      const geometry = new THREE.Geometry();
      const dummy = new THREE.Mesh(this.geometry);

      buildings.forEach((building) => {
        const config = getBuildingConfig(building.configId);
        const levelScale = 1 + building.level * 0.1;
        const height = (MODEL_HEIGHTS[modelKey] ?? 1) * levelScale;
        dummy.position.set(
          building.pos.x - city.grid.width / 2 + building.size.w / 2,
          height / 2,
          building.pos.y - city.grid.height / 2 + building.size.h / 2,
        );
        dummy.scale.set(building.size.w * 0.82 * levelScale, height, building.size.h * 0.82 * levelScale);
        dummy.rotation.y = config.category === 'industrial' ? Math.PI / 4 : 0;
        dummy.updateMatrix();
        geometry.merge(this.geometry, dummy.matrix);
      });

      // Tint color by highest level in this group
      const maxLevel = Math.max(...buildings.map((b) => b.level));
      if (maxLevel > 0) {
        const tint = new THREE.Color(material.color);
        const brightness = 0.15 * maxLevel;
        tint.r = Math.min(1, tint.r + brightness);
        tint.g = Math.min(1, tint.g + brightness);
        tint.b = Math.min(1, tint.b + brightness);
        material.color.copy(tint);
      }

      geometry.computeFaceNormals();
      geometry.computeBoundingSphere();
      const mesh = new THREE.Mesh(geometry, material);
      this.add(mesh);
    }
  }

  dispose(): void {
    this.clearMeshes();
    this.geometry.dispose();
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

import * as THREE from 'three';
import type { CityState } from '../simulation/city-state';

export class RoadMesh extends THREE.Group {
  private sourceGeometry = new THREE.BoxGeometry(0.94, 0.09, 0.94);
  private material = new THREE.MeshLambertMaterial({ color: 0x263241 });

  constructor() {
    super();
    this.name = 'RoadMesh';
  }

  sync(city: CityState): void {
    this.clearMeshes();
    const roads = city.getRoads();
    if (roads.length === 0) {
      return;
    }
    const geometry = new THREE.Geometry();
    const dummy = new THREE.Mesh(this.sourceGeometry);

    roads.forEach((road) => {
      const heat = Math.min(1, road.load / Math.max(1, road.capacity));
      dummy.position.set(road.pos.x - city.grid.width / 2 + 0.5, 0.02 + heat * 0.02, road.pos.y - city.grid.height / 2 + 0.5);
      dummy.scale.set(0.9, 1, 0.9);
      dummy.rotation.set(0, 0, 0);
      dummy.updateMatrix();
      geometry.merge(this.sourceGeometry, dummy.matrix);
    });
    geometry.computeFaceNormals();
    geometry.computeBoundingSphere();
    const mesh = new THREE.Mesh(geometry, this.material);
    this.add(mesh);
  }

  dispose(): void {
    this.clearMeshes();
    this.sourceGeometry.dispose();
    this.material.dispose();
  }

  private clearMeshes(): void {
    for (const child of [...this.children]) {
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
      }
      this.remove(child);
    }
  }
}

import * as THREE from 'three';
import type { CityGrid } from '../map/grid';
import type { TerrainType } from '../types';

const TERRAIN_COLORS: Record<TerrainType, number> = {
  plain: 0x66b86f,
  water: 0x2f9ed8,
  hill: 0x8fa35f,
};

export class TileLayer extends THREE.Group {
  constructor(grid: CityGrid) {
    super();
    this.name = 'TileLayer';
    this.build(grid);
  }

  private build(grid: CityGrid): void {
    const positionsByTerrain = new Map<TerrainType, THREE.Vector3[]>();
    grid.forEachTile((tile, pos) => {
      const terrainPositions = positionsByTerrain.get(tile.terrain) ?? [];
      terrainPositions.push(new THREE.Vector3(pos.x - grid.width / 2 + 0.5, -0.05, pos.y - grid.height / 2 + 0.5));
      positionsByTerrain.set(tile.terrain, terrainPositions);
    });

    const sourceGeometry = new THREE.BoxGeometry(0.98, 0.08, 0.98);
    const dummy = new THREE.Mesh(sourceGeometry);
    for (const [terrain, positions] of positionsByTerrain.entries()) {
      const material = new THREE.MeshLambertMaterial({ color: TERRAIN_COLORS[terrain] });
      const geometry = new THREE.Geometry();
      positions.forEach((position) => {
        dummy.position.copy(position);
        dummy.rotation.set(0, 0, 0);
        dummy.scale.set(1, 1, 1);
        dummy.updateMatrix();
        geometry.merge(sourceGeometry, dummy.matrix);
      });
      geometry.computeFaceNormals();
      geometry.computeBoundingSphere();
      const mesh = new THREE.Mesh(geometry, material);
      this.add(mesh);
    }
    sourceGeometry.dispose();
  }
}

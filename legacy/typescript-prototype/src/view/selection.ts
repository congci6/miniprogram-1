import * as THREE from 'three';
import type { GridPos } from '../types';

export class SelectionMarker extends THREE.Group {
  private readonly mesh: THREE.Mesh;

  constructor() {
    super();
    const geometry = new THREE.BoxGeometry(1.04, 0.08, 1.04);
    const material = new THREE.MeshBasicMaterial({
      color: 0xfff3a3,
      transparent: true,
      opacity: 0.58,
      depthWrite: false,
    });
    this.mesh = new THREE.Mesh(geometry, material);
    this.mesh.visible = false;
    this.add(this.mesh);
  }

  setTile(pos: GridPos | undefined, gridWidth: number, gridHeight: number): void {
    if (!pos) {
      this.mesh.visible = false;
      return;
    }
    this.mesh.visible = true;
    this.mesh.position.set(pos.x - gridWidth / 2 + 0.5, 0.09, pos.y - gridHeight / 2 + 0.5);
  }
}

export function screenToGrid(
  clientX: number,
  clientY: number,
  width: number,
  height: number,
  camera: THREE.Camera,
  gridWidth: number,
  gridHeight: number,
): GridPos | undefined {
  const raycaster = new THREE.Raycaster();
  const ndc = new THREE.Vector2((clientX / width) * 2 - 1, -(clientY / height) * 2 + 1);
  const plane = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  const point = new THREE.Vector3();
  raycaster.setFromCamera(ndc, camera);
  const hit = raycaster.ray.intersectPlane(plane, point);
  if (!hit) {
    return undefined;
  }

  const x = Math.floor(point.x + gridWidth / 2);
  const y = Math.floor(point.z + gridHeight / 2);
  if (x < 0 || y < 0 || x >= gridWidth || y >= gridHeight) {
    return undefined;
  }

  return { x, y };
}

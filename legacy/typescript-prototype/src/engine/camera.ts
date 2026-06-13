import * as THREE from 'three';

export class CameraRig {
  readonly camera: THREE.OrthographicCamera;
  private width: number;
  private height: number;
  private zoom = 1.18;
  private readonly frustumSize = 24;

  constructor(width: number, height: number) {
    this.width = width;
    this.height = height;
    this.camera = new THREE.OrthographicCamera(-1, 1, 1, -1, 0.1, 200);
    this.camera.position.set(18, 22, 18);
    this.camera.lookAt(0, 0, 0);
    this.resize(width, height);
  }

  resize(width: number, height: number): void {
    this.width = width;
    this.height = height;
    const aspect = width / Math.max(1, height);
    const size = this.frustumSize / this.zoom;
    this.camera.left = (-size * aspect) / 2;
    this.camera.right = (size * aspect) / 2;
    this.camera.top = size / 2;
    this.camera.bottom = -size / 2;
    this.camera.near = 0.1;
    this.camera.far = 200;
    this.camera.updateProjectionMatrix();
  }

  pan(deltaX: number, deltaY: number): void {
    const unitsPerPixel = this.frustumSize / this.zoom / Math.max(1, this.height);
    const right = new THREE.Vector3(1, 0, -1).normalize();
    const upGround = new THREE.Vector3(1, 0, 1).normalize();
    this.camera.position.addScaledVector(right, -deltaX * unitsPerPixel * 1.45);
    this.camera.position.addScaledVector(upGround, deltaY * unitsPerPixel * 1.45);
  }

  zoomBy(scale: number): void {
    this.zoom = Math.max(0.55, Math.min(2.6, this.zoom * scale));
    this.resize(this.width, this.height);
  }
}

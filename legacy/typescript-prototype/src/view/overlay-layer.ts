import * as THREE from 'three';
import { create2dCanvas } from '../platform/wx-canvas';
import type { HudController, HudState } from '../ui/hud';

export class OverlayLayer {
  private readonly canvas: HTMLCanvasElement;
  private readonly context: CanvasRenderingContext2D;
  private readonly texture: THREE.CanvasTexture;
  private readonly scene = new THREE.Scene();
  private readonly camera: THREE.OrthographicCamera;
  private readonly mesh: THREE.Mesh;
  private lastWidth: number;
  private lastHeight: number;

  constructor(width: number, height: number, private readonly hud: HudController) {
    this.lastWidth = width;
    this.lastHeight = height;
    this.canvas = create2dCanvas(width, height);
    const context = this.canvas.getContext('2d');
    if (!context) {
      throw new Error('2D HUD context is unavailable.');
    }
    this.context = context;
    this.texture = new THREE.CanvasTexture(this.canvas);
    this.texture.minFilter = THREE.LinearFilter;
    this.texture.magFilter = THREE.LinearFilter;
    this.camera = new THREE.OrthographicCamera(0, width, height, 0, -10, 10);
    const geometry = new THREE.PlaneGeometry(width, height);
    const material = new THREE.MeshBasicMaterial({
      map: this.texture,
      transparent: true,
      depthTest: false,
      depthWrite: false,
    });
    this.mesh = new THREE.Mesh(geometry, material);
    this.mesh.position.set(width / 2, height / 2, 0);
    this.scene.add(this.mesh);
    this.hud.layout(width, height);
  }

  update(state: HudState): void {
    this.hud.draw(this.context, state);
    this.texture.needsUpdate = true;
  }

  render(renderer: THREE.WebGLRenderer): void {
    const previousAutoClear = renderer.autoClear;
    renderer.autoClear = false;
    renderer.clearDepth();
    renderer.render(this.scene, this.camera);
    renderer.autoClear = previousAutoClear;
  }

  resize(width: number, height: number): void {
    if (width === this.lastWidth && height === this.lastHeight) {
      return;
    }
    this.lastWidth = width;
    this.lastHeight = height;
    this.canvas.width = Math.max(1, Math.floor(width));
    this.canvas.height = Math.max(1, Math.floor(height));
    this.camera.right = width;
    this.camera.bottom = height;
    this.camera.updateProjectionMatrix();
    this.mesh.geometry.dispose();
    this.mesh.geometry = new THREE.PlaneGeometry(width, height);
    this.mesh.position.set(width / 2, height / 2, 0);
    this.hud.layout(width, height);
  }
}

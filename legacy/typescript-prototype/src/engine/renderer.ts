import * as THREE from 'three';
import type { RuntimeCanvas } from '../platform/wx-canvas';

export function createRenderer(runtime: RuntimeCanvas): THREE.WebGLRenderer {
  const context =
    (runtime.canvas.getContext('webgl', {
      antialias: true,
      alpha: false,
      preserveDrawingBuffer: false,
    }) as WebGLRenderingContext | null) ??
    (runtime.canvas.getContext('experimental-webgl', {
      antialias: true,
      alpha: false,
      preserveDrawingBuffer: false,
    }) as WebGLRenderingContext | null);

  const renderer = new THREE.WebGLRenderer({
    canvas: runtime.canvas,
    context: context ?? undefined,
    antialias: true,
    alpha: false,
  });
  renderer.setPixelRatio(runtime.pixelRatio);
  renderer.setSize(runtime.width, runtime.height, false);
  renderer.setClearColor(0xb9d9cf, 1);
  renderer.info.autoReset = true;
  return renderer;
}

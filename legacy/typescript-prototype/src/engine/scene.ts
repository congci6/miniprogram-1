import * as THREE from 'three';

export function createBaseScene(): THREE.Scene {
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0xb9d9cf);
  scene.fog = new THREE.Fog(0xb9d9cf, 42, 92);

  const ambient = new THREE.AmbientLight(0xffffff, 0.72);
  scene.add(ambient);

  const sun = new THREE.DirectionalLight(0xffffff, 0.88);
  sun.position.set(18, 30, 12);
  scene.add(sun);

  return scene;
}

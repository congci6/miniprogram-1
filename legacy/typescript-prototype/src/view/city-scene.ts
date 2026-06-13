import * as THREE from 'three';
import { createBaseScene } from '../engine/scene';
import type { CityState } from '../simulation/city-state';
import type { GridPos, OverlayMode } from '../types';
import { BuildingInstancer } from './building-instancer';
import { MapOverlay } from './map-overlay';
import { RoadMesh } from './road-mesh';
import { screenToGrid, SelectionMarker } from './selection';
import { TileLayer } from './tile-layer';

export class CityScene {
  readonly scene: THREE.Scene;
  private readonly roads = new RoadMesh();
  private readonly buildings = new BuildingInstancer();
  private readonly overlay = new MapOverlay();
  private readonly selection = new SelectionMarker();
  private overlayMode: OverlayMode = 'normal';

  constructor(private readonly city: CityState) {
    this.scene = createBaseScene();
    this.scene.add(new TileLayer(city.grid));
    this.scene.add(this.roads);
    this.scene.add(this.buildings);
    this.scene.add(this.overlay);
    this.scene.add(this.selection);
    this.sync(city);
  }

  sync(city: CityState): void {
    this.roads.sync(city);
    this.buildings.sync(city);
    this.overlay.sync(city);
  }

  syncOverlay(city: CityState): void {
    this.overlay.sync(city);
  }

  setOverlayMode(mode: OverlayMode, city: CityState): void {
    this.overlayMode = mode;
    this.overlay.setMode(mode, city);
  }

  getOverlayMode(): OverlayMode {
    return this.overlayMode;
  }

  setSelection(pos: GridPos | undefined): void {
    this.selection.setTile(pos, this.city.grid.width, this.city.grid.height);
  }

  pickGrid(clientX: number, clientY: number, width: number, height: number, camera: THREE.Camera): GridPos | undefined {
    return screenToGrid(clientX, clientY, width, height, camera, this.city.grid.width, this.city.grid.height);
  }
}

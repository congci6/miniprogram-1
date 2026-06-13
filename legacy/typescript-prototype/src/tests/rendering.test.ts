import * as THREE from 'three';
import { describe, expect, it } from 'vitest';
import { CityState } from '../simulation/city-state';
import { BuildingInstancer } from '../view/building-instancer';
import { MapOverlay } from '../view/map-overlay';

describe('building rendering', () => {
  it('creates visible merged mesh geometry for starter buildings', () => {
    const city = CityState.createNew();
    const buildings = new BuildingInstancer();

    buildings.sync(city);

    const meshChildren = buildings.children.filter((child): child is THREE.Mesh => child instanceof THREE.Mesh);
    const vertexCount = meshChildren.reduce((sum, mesh) => {
      return mesh.geometry instanceof THREE.Geometry ? sum + mesh.geometry.vertices.length : sum;
    }, 0);

    expect(meshChildren.length).toBeGreaterThan(0);
    expect(vertexCount).toBeGreaterThan(0);
  });
});

describe('map overlays', () => {
  it('creates traffic overlay geometry for road coverage', () => {
    const city = CityState.createNew();
    const overlay = new MapOverlay();

    overlay.setMode('traffic', city);

    const meshChildren = overlay.children.filter((child): child is THREE.Mesh => child instanceof THREE.Mesh);
    expect(meshChildren.length).toBeGreaterThan(0);
  });

  it('creates pollution overlay geometry when pollution exists', () => {
    const city = CityState.createNew();
    city.grid.getTile({ x: 10, y: 10 }).pollution = 8;
    const overlay = new MapOverlay();

    overlay.setMode('pollution', city);

    const meshChildren = overlay.children.filter((child): child is THREE.Mesh => child instanceof THREE.Mesh);
    expect(meshChildren.length).toBeGreaterThan(0);
  });
});

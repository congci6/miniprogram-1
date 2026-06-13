import { existsSync, readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import { BUILDINGS } from '../data/buildings';

const unityFiles = [
  'unity/Assets/Scripts/PocketCity/Core/CityTypes.cs',
  'unity/Assets/Scripts/PocketCity/Core/CityConfig.cs',
  'unity/Assets/Scripts/PocketCity/Simulation/CityGridCore.cs',
  'unity/Assets/Scripts/PocketCity/Simulation/CitySimulationCore.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/CityGameController.cs',
  'unity/Assets/Scripts/PocketCity/Runtime/WeChatMiniGameBridge.cs',
  'unity/Assets/Plugins/WebGL/WeChatBridge.jslib',
  'unity/Assets/Editor/PocketCity/DefaultCityConfigFactory.cs',
  'unity/Packages/manifest.json',
];

describe('unity migration scaffold', () => {
  it('keeps the expected Unity scaffold files in place', () => {
    for (const file of unityFiles) {
      expect(existsSync(file), file).toBe(true);
    }
  });

  it('keeps the Unity default config factory aligned with current building ids', () => {
    const factory = readFileSync('unity/Assets/Editor/PocketCity/DefaultCityConfigFactory.cs', 'utf8');

    for (const building of BUILDINGS) {
      expect(factory).toContain(`Id = "${building.id}"`);
    }
  });

  it('keeps Unity gameplay core on map, road, preview, and service concepts', () => {
    const core = readFileSync('unity/Assets/Scripts/PocketCity/Simulation/CitySimulationCore.cs', 'utf8');
    expect(core).toContain('TryBuildRoad');
    expect(core).toContain('PreviewBuilding');
    expect(core).toContain('NearestRoadId');
    expect(core).toContain('ServiceCoverage');
  });
});

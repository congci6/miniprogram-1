# LOW_POLY_ISOMETRIC_REFERENCE_UI

This pass moves the Unity mini game toward the bright low-poly isometric city-builder reference.

## Runtime Visuals

- `CityMapRenderer` keeps terrain, roads, buildings, overlays, scenery, and future-area guides procedural.
- Terrain colors are brighter for grass, water, and hills, with small deterministic tile shade variation.
- Roads use a lighter slate material plus `RoadCenterMark` strips.
- Open scenery tiles can spawn lightweight `LowPolyTreeCanopy`, tree trunk, and `LowPolyRock` cubes.
- `LockedRegionDashedOutline` marks a future expansion area without changing simulation or placement rules.

## Camera And Lighting

- `PrototypeSceneFactory.CreateCamera` uses a diagonal isometric offset: `new Vector3(-42f, 48f, -42f)`.
- The camera background is a pale sky color.
- The directional light is warmer and slightly stronger to support a fresh low-poly look.

## HUD Direction

- `CityRuntimeHud` keeps all existing counts intact: 8 top stats, 33 demand stats, 14 overlay buttons, 48 tool buttons, 7 control buttons, and 9 policy buttons.
- The top strip uses a dark green translucent resource style.
- The inspector/task panel uses a pale green-white surface.
- Overlay controls sit as a vertical operation stack near the right task card.
- Active overlay/tool/policy states use cyan or green highlights.
- `Mini Map Zoom` adds a bottom-right minimap/zoom cluster without adding gameplay buttons.

## Guardrails

- Do not add TypeScript/Vite runtime paths.
- Do not add `workers` to `miniprogram/game.json`.
- Do not introduce WebGL2-only code paths, SharedArrayBuffer, createImageBitmap, or Worker-based runtime code.
- Continue using `npm.cmd run verify` and the forbidden-string scan after visual changes.

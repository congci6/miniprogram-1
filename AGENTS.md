# AGENTS.md

## Project
This is a Unity-first WeChat Mini Game city-building simulation. The retired TypeScript prototype is stored under `legacy/typescript-prototype/` only for migration reference.

## Rules
- Active gameplay work belongs in `unity/Assets/Scripts/PocketCity`.
- Keep core simulation in `PocketCity.Core` and `PocketCity.Simulation` independent from Unity scene objects and WeChat APIs.
- Unity runtime glue belongs in `PocketCity.Runtime`.
- WeChat platform calls must stay behind `WeChatMiniGameBridge` or WebGL `.jslib` files.
- Balance and building definitions should be generated into `CityConfig` assets, not hard-coded in scene behaviours.
- Do not reintroduce a TypeScript / Three.js runtime as an active version.
- Do not copy assets, UI, names, mechanics, task text, or balance values from existing city-builder IP.

## Verification
- Run `npm run verify` after scaffold or file-structure changes.
- In a Unity-equipped environment, open `unity/`, run the default config generator, and check the Console for C# compile errors.
- For release candidates, export through the WeChat mini game conversion SDK and record package size and FPS.

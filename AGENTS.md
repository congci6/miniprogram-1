# AGENTS.md

## Project
This is a non-Unity WeChat Mini Game city-building simulation. The current playable runtime is generated from the TypeScript project under `browser/`; `unity/` is retained only as historical migration reference.

## Rules
- Active gameplay work belongs in `browser/src`.
- Keep core simulation in `browser/src/simulation` and shared types in `browser/src/types` independent from DOM, Phaser scene objects, and WeChat APIs.
- Browser-only debug glue may use Phaser under `browser/src/game` and DOM HUD code under `browser/src/ui`.
- WeChat platform calls belong in `browser/src/wechat/main.ts` behind the local `WeChatRuntime` interface; do not hand-edit generated `miniprogram/game.js`.
- Do not add active gameplay code under `unity/`; migrate or mirror useful historical behavior into the TypeScript simulation/runtime instead.
- Do not introduce Three.js, Worker, WebGL2, SharedArrayBuffer, DOM, or Phaser dependencies into the WeChat Canvas runtime.
- Do not copy assets, UI, names, mechanics, task text, or balance values from existing city-builder IP.

## Verification
- Run `npm run verify` after scaffold or file-structure changes.
- Run `npm run smoke:wechat` after touching `browser/src/wechat/main.ts`, generated package flow, or save/lifecycle behavior.
- For release candidates, build with `npm run build:wechat`, open `miniprogram/` in WeChat DevTools, and record package size and FPS.

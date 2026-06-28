import { existsSync, readFileSync, readdirSync, statSync } from 'node:fs';

const modeArg = process.argv.find((arg) => arg.startsWith('--mode='));
const verifyMode = modeArg ? modeArg.slice('--mode='.length) : (process.env.VERIFY_WECHAT_MODE || 'scaffold');
assert(['scaffold', 'exported'].includes(verifyMode), `Unknown verify mode: ${verifyMode}. Expected scaffold or exported.`);

const requiredFiles = [
  'browser/package.json',
  'browser/tsconfig.json',
  'browser/vite.wechat.config.ts',
  'browser/src/wechat/main.ts',
  'browser/src/simulation/city-simulation.ts',
  'browser/src/types/index.ts',
  'miniprogram/game.js',
  'miniprogram/game.json',
  'miniprogram/project.config.json',
];

const retiredRootRuntimeFiles = [
  'src',
  'index.html',
  'tsconfig.json',
  'vite.config.ts',
  'vitest.config.ts',
  'package-lock.json',
];

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

function walkTextFiles(root) {
  if (!existsSync(root)) return [];
  const files = [];
  for (const entry of readdirSync(root)) {
    const fullPath = `${root}/${entry}`;
    if (statSync(fullPath).isDirectory()) {
      files.push(...walkTextFiles(fullPath));
    } else if (/\.(js|json|ts|tsx|mjs|cjs)$/u.test(fullPath)) {
      files.push(fullPath);
    }
  }

  return files;
}

function assertNoForbiddenMiniGameMarkers() {
  const forbiddenMarkers = [
    '"workers"',
    'texImage3D',
    'WebGL2RenderingContext',
    'webgl2',
    'SharedArrayBuffer',
    'createImageBitmap',
    'new Worker',
    'Worker(',
  ];

  for (const file of walkTextFiles('miniprogram')) {
    const source = readFileSync(file, 'utf8');
    for (const marker of forbiddenMarkers) {
      assert(!source.includes(marker), `Forbidden mini game runtime marker "${marker}" found in ${file}`);
    }
  }
}

function assertNoForbiddenWechatCanvasRuntimeMarkers() {
  const forbiddenMarkers = [
    'UNITY_BUILD_PENDING',
    'Unity build pending',
    'document',
    'Phaser',
    'Worker',
    'SharedArrayBuffer',
    'webgl2',
    'createImageBitmap',
    'window.',
    'UnityEngine',
    'WeChatMiniGameBridge',
  ];
  const files = [
    'browser/src/wechat/main.ts',
    'miniprogram/game.js',
  ];

  for (const file of files) {
    const source = readFileSync(file, 'utf8');
    for (const marker of forbiddenMarkers) {
      assert(!source.includes(marker), `Forbidden WeChat Canvas runtime marker "${marker}" found in ${file}`);
    }
  }
}

for (const file of requiredFiles) {
  assert(existsSync(file), `Missing required active WeChat runtime file: ${file}`);
}

for (const file of retiredRootRuntimeFiles) {
  assert(!existsSync(file), `Retired root TypeScript runtime artifact is still active: ${file}`);
}

assertNoForbiddenMiniGameMarkers();
assertNoForbiddenWechatCanvasRuntimeMarkers();

const rootPackageJson = JSON.parse(readFileSync('package.json', 'utf8'));
assert(!rootPackageJson.dependencies, 'Root package.json must not declare runtime dependencies.');
assert(!rootPackageJson.devDependencies, 'Root package.json must not declare dev dependencies.');

const gameJson = JSON.parse(readFileSync('miniprogram/game.json', 'utf8'));
assert(!Object.prototype.hasOwnProperty.call(gameJson, 'workers'), 'miniprogram/game.json must not contain workers.');
assert(gameJson.deviceOrientation === 'landscape', 'WeChat mini game must stay landscape.');

const wechatSource = readFileSync('browser/src/wechat/main.ts', 'utf8');
for (const marker of [
  'NON_UNITY_WECHAT_CANVAS_RUNTIME',
  'createCanvas',
  'CanvasRenderingContext2D',
  'onTouchStart',
  'onTouchMove',
  'onTouchEnd',
  'onHide',
  'onShow',
  'setStorageSync',
  'getStorageSync',
  'vibrateShort',
]) {
  assert(wechatSource.includes(marker), `WeChat Canvas runtime source missing marker: ${marker}`);
}

const gameJs = readFileSync('miniprogram/game.js', 'utf8');
assert(gameJs.trim().length > 0, 'miniprogram/game.js must not be empty.');
assert(gameJs.includes('NON_UNITY_WECHAT_CANVAS_RUNTIME'), 'miniprogram/game.js must contain the non-Unity WeChat Canvas runtime marker.');
if (verifyMode === 'exported') {
  assert(!gameJs.includes('UNITY_BUILD_PENDING'), 'miniprogram/game.js must be replaced by playable mini game output in exported mode.');
  assert(!gameJs.includes('Unity build pending'), 'miniprogram/game.js must not contain the placeholder modal in exported mode.');
} else {
  assert(!gameJs.includes('UNITY_BUILD_PENDING'), 'miniprogram/game.js must not be the old Unity placeholder.');
}

console.log(`WeChat runtime verification passed (mode: ${verifyMode}).`);

import { readFileSync } from 'node:fs';
import { Script, createContext } from 'node:vm';

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const source = readFileSync('miniprogram/game.js', 'utf8');
assert(source.includes('NON_UNITY_WECHAT_CANVAS_RUNTIME'), 'Generated game.js is missing the non-Unity runtime marker.');

const storage = new Map();
const frameCallbacks = [];
const textDraws = [];
const lifecycleCallbacks = { hide: null, show: null };
const touchCallbacks = { start: null, move: null, end: null };
const calls = {
  createCanvas: 0,
  getContext: 0,
  fillText: 0,
  fillRect: 0,
  requestAnimationFrame: 0,
  setStorageSync: 0,
  getStorageSync: 0,
  vibrateShort: 0,
};

const context2d = {
  fillStyle: '',
  strokeStyle: '',
  font: '',
  textAlign: 'left',
  textBaseline: 'top',
  lineWidth: 1,
  globalAlpha: 1,
  setTransform() {},
  clearRect() {},
  fillRect() { calls.fillRect += 1; },
  beginPath() {},
  moveTo() {},
  lineTo() {},
  closePath() {},
  fill() {},
  stroke() {},
  save() {},
  restore() {},
  translate() {},
  scale() {},
  arc() {},
  arcTo() {},
  createLinearGradient() {
    return { addColorStop() {} };
  },
  fillText(text, x, y) {
    calls.fillText += 1;
    textDraws.push({ text: String(text), x, y, textAlign: this.textAlign });
  },
  measureText(text) {
    const width = Array.from(String(text)).reduce((total, ch) => total + (ch.charCodeAt(0) > 127 ? 12 : 7), 0);
    return { width };
  },
};

const canvas = {
  width: 0,
  height: 0,
  getContext(type) {
    assert(type === '2d', `Unexpected canvas context type: ${type}`);
    calls.getContext += 1;
    return context2d;
  },
  requestAnimationFrame(callback) {
    calls.requestAnimationFrame += 1;
    frameCallbacks.push(callback);
    return frameCallbacks.length;
  },
};

const wx = {
  createCanvas() {
    calls.createCanvas += 1;
    return canvas;
  },
  getSystemInfoSync() {
    return { windowWidth: 812, windowHeight: 375, pixelRatio: 2 };
  },
  onTouchStart(callback) { touchCallbacks.start = callback; },
  onTouchMove(callback) { touchCallbacks.move = callback; },
  onTouchEnd(callback) { touchCallbacks.end = callback; },
  onHide(callback) { lifecycleCallbacks.hide = callback; },
  onShow(callback) { lifecycleCallbacks.show = callback; },
  setStorageSync(key, value) {
    calls.setStorageSync += 1;
    storage.set(key, value);
  },
  getStorageSync(key) {
    calls.getStorageSync += 1;
    return storage.get(key);
  },
  vibrateShort() { calls.vibrateShort += 1; },
};

const sandbox = {
  console,
  wx,
  GameGlobal: {},
  setTimeout(callback) {
    frameCallbacks.push(callback);
    return frameCallbacks.length;
  },
  clearTimeout() {},
};

const context = createContext(sandbox);
new Script(source, { filename: 'miniprogram/game.js' }).runInContext(context);

function tap(x, y) {
  const touch = { clientX: x, clientY: y };
  touchCallbacks.start({ touches: [touch] });
  touchCallbacks.end({ changedTouches: [touch] });
}

function findToolbarCenter(index) {
  const toolbarTexts = textDraws
    .filter((draw) => draw.textAlign === 'center' && draw.y > 320 && draw.y < 365)
    .sort((left, right) => left.x - right.x);
  assert(toolbarTexts.length >= 9, `Runtime should draw all toolbar labels; got ${toolbarTexts.length}.`);
  return toolbarTexts[index];
}

function screenForTile(x, y) {
  const tileW = 48;
  const tileH = 24;
  const gridW = 24;
  const gridH = 18;
  const originX = wx.getSystemInfoSync().windowWidth / 2;
  const originY = Math.max(70, wx.getSystemInfoSync().windowHeight * 0.2);
  const dx = x - gridW / 2;
  const dy = y - gridH / 2;
  return {
    x: originX + (dx - dy) * (tileW / 2),
    y: originY + (dx + dy) * (tileH / 2),
  };
}

assert(sandbox.GameGlobal.__POCKET_CITY_RUNTIME__ === 'NON_UNITY_WECHAT_CANVAS_RUNTIME', 'Runtime marker was not published to GameGlobal.');
assert(calls.createCanvas === 1, 'Runtime should create exactly one canvas.');
assert(calls.getContext === 1, 'Runtime should request one 2D canvas context.');
assert(touchCallbacks.start && touchCallbacks.move && touchCallbacks.end, 'Runtime should register touch callbacks.');
assert(lifecycleCallbacks.hide && lifecycleCallbacks.show, 'Runtime should register lifecycle callbacks.');
assert(canvas.width === 1624 && canvas.height === 750, `Runtime should size the canvas by DPR; got ${canvas.width}x${canvas.height}.`);
assert(frameCallbacks.length > 0, 'Runtime should schedule an animation frame.');

const firstFrame = frameCallbacks.shift();
firstFrame(Date.now() + 16);
assert(calls.fillRect > 0, 'Runtime should draw filled canvas shapes during the first frame.');
assert(calls.fillText > 0, 'Runtime should draw UI text during the first frame.');
assert(calls.requestAnimationFrame >= 2, 'Runtime should schedule the next frame after drawing.');

const savesBeforeInteraction = calls.setStorageSync;
const vibrationsBeforeInteraction = calls.vibrateShort;
const roadToolCenter = findToolbarCenter(1);
const mapCenter = screenForTile(12, 9);
tap(roadToolCenter.x, roadToolCenter.y);
tap(mapCenter.x, mapCenter.y);
assert(calls.vibrateShort > vibrationsBeforeInteraction, 'Runtime should vibrate after tool selection or placement.');
assert(calls.setStorageSync > savesBeforeInteraction, 'Runtime should save after placing a road.');
const snapshotAfterInteraction = Array.from(storage.values()).at(-1);
assert(
  snapshotAfterInteraction?.tiles?.some((tile) => tile.x === 12 && tile.y === 9 && tile.roadId === 'local'),
  'Runtime should apply the selected road tool to the tapped map tile.',
);

lifecycleCallbacks.hide();
assert(calls.setStorageSync > 0 && storage.size > 0, 'Runtime should save city state on hide.');
lifecycleCallbacks.show();
assert(calls.getStorageSync > 0, 'Runtime should read city state on show.');

console.log('WeChat runtime smoke passed.');

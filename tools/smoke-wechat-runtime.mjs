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
  fillText() { calls.fillText += 1; },
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
  vibrateShort() {},
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

lifecycleCallbacks.hide();
assert(calls.setStorageSync > 0 && storage.size > 0, 'Runtime should save city state on hide.');
lifecycleCallbacks.show();
assert(calls.getStorageSync > 0, 'Runtime should read city state on show.');

console.log('WeChat runtime smoke passed.');

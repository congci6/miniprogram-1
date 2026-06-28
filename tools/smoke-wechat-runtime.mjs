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

function drawNextFrame(label) {
  assert(frameCallbacks.length > 0, `Runtime should schedule ${label}.`);
  textDraws.length = 0;
  const frame = frameCallbacks.shift();
  frame(Date.now() + 16);
}

function latestStorageKey() {
  const key = Array.from(storage.keys()).at(-1);
  assert(key, 'Runtime should have a saved city snapshot key.');
  return key;
}

function latestSnapshot() {
  const snapshot = Array.from(storage.values()).at(-1);
  assert(snapshot, 'Runtime should have a saved city snapshot.');
  return snapshot;
}

function cloneSnapshot(snapshot) {
  return JSON.parse(JSON.stringify(snapshot));
}

function restoreSnapshotThroughStorage(snapshot) {
  const copy = cloneSnapshot(snapshot);
  copy.savedAtMs = Date.now();
  storage.set(latestStorageKey(), copy);
  lifecycleCallbacks.show();
}

function unlockLevelTwo(snapshot) {
  snapshot.metrics.cash = Math.max(snapshot.metrics.cash ?? 0, 50000);
  snapshot.metrics.cityExperience = Math.max(snapshot.metrics.cityExperience ?? 0, 100);
}

function setMaterials(snapshot, materials) {
  snapshot.materials = {
    wood: materials.wood ?? 0,
    metal: materials.metal ?? 0,
    plastic: materials.plastic ?? 0,
  };
}

function findSavedTile(snapshot, x, y) {
  return snapshot.tiles.find((tile) => tile.x === x && tile.y === y);
}

function upsertSavedTile(snapshot, nextTile) {
  const existing = findSavedTile(snapshot, nextTile.x, nextTile.y);
  const tile = {
    zone: 0,
    roadId: '',
    buildingId: '',
    buildingAgeDays: 0,
    ...existing,
    ...nextTile,
  };
  if (existing) {
    Object.assign(existing, tile);
  } else {
    snapshot.tiles.push(tile);
  }
}

function findCenteredTextInBand(minY, maxY, index, minimumCount, label) {
  const textCenters = textDraws
    .filter((draw) => draw.textAlign === 'center' && draw.y > minY && draw.y < maxY)
    .sort((left, right) => left.x - right.x);
  assert(textCenters.length >= minimumCount, `Runtime should draw ${label}; got ${textCenters.length}.`);
  assert(textCenters[index], `Runtime should draw ${label} at index ${index}.`);
  return textCenters[index];
}

function findToolbarCenter(index) {
  return findCenteredTextInBand(320, 365, index, 9, 'all toolbar labels');
}

function findTimeScaleCenter(index) {
  return findCenteredTextInBand(155, 180, index, 4, 'time scale controls');
}

function findProductionCenter(index) {
  return findCenteredTextInBand(180, 205, index, 3, 'production controls');
}

function findOrderActionCenter(index) {
  return findCenteredTextInBand(210, 235, index, 3, 'order and upgrade controls');
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

drawNextFrame('the first frame');
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

drawNextFrame('management controls after road placement');

const fastTimeButtonCenter = findTimeScaleCenter(3);
const savesBeforeTimeScale = calls.setStorageSync;
tap(fastTimeButtonCenter.x, fastTimeButtonCenter.y);
assert(calls.setStorageSync > savesBeforeTimeScale, 'Runtime should save after changing time scale.');

const woodProductionButtonCenter = findProductionCenter(0);
const savesBeforeProduction = calls.setStorageSync;
tap(woodProductionButtonCenter.x, woodProductionButtonCenter.y);
assert(calls.setStorageSync > savesBeforeProduction, 'Runtime should save after starting production.');
const snapshotAfterProduction = latestSnapshot();
assert(
  snapshotAfterProduction?.productionQueue?.some((job) => job.materialId === 'wood' && job.remainingDays > 0),
  'Runtime should start a wood production job through the management panel.',
);

const orderReadySnapshot = cloneSnapshot(snapshotAfterProduction);
unlockLevelTwo(orderReadySnapshot);
const firstOrder = orderReadySnapshot.orders?.[0];
assert(firstOrder, 'Runtime should keep at least one city order available.');
setMaterials(orderReadySnapshot, firstOrder.required);
const completedOrdersBefore = orderReadySnapshot.completedOrders;
restoreSnapshotThroughStorage(orderReadySnapshot);
drawNextFrame('order-ready management controls');
const fulfillOrderButtonCenter = findOrderActionCenter(0);
const savesBeforeOrder = calls.setStorageSync;
tap(fulfillOrderButtonCenter.x, fulfillOrderButtonCenter.y);
assert(calls.setStorageSync > savesBeforeOrder, 'Runtime should save after fulfilling an order.');
const snapshotAfterOrder = latestSnapshot();
assert(snapshotAfterOrder.completedOrders === completedOrdersBefore + 1, 'Runtime should fulfill an order through the management panel.');
assert(
  Object.values(snapshotAfterOrder.materials).every((count) => count === 0),
  'Runtime should consume the required materials when fulfilling an order.',
);

const roadUpgradeReadySnapshot = cloneSnapshot(snapshotAfterOrder);
unlockLevelTwo(roadUpgradeReadySnapshot);
upsertSavedTile(roadUpgradeReadySnapshot, { x: 12, y: 9, zone: 0, roadId: 'local', buildingId: '', buildingAgeDays: 0 });
restoreSnapshotThroughStorage(roadUpgradeReadySnapshot);
drawNextFrame('road-upgrade-ready management controls');
tap(mapCenter.x, mapCenter.y);
drawNextFrame('selected road management controls');
const roadUpgradeButtonCenter = findOrderActionCenter(2);
const savesBeforeRoadUpgrade = calls.setStorageSync;
tap(roadUpgradeButtonCenter.x, roadUpgradeButtonCenter.y);
assert(calls.setStorageSync > savesBeforeRoadUpgrade, 'Runtime should save after upgrading a road.');
const snapshotAfterRoadUpgrade = latestSnapshot();
assert(
  findSavedTile(snapshotAfterRoadUpgrade, 12, 9)?.roadId === 'arterial',
  'Runtime should upgrade the selected road through the management panel.',
);

const residentialUpgradeReadySnapshot = cloneSnapshot(snapshotAfterRoadUpgrade);
unlockLevelTwo(residentialUpgradeReadySnapshot);
setMaterials(residentialUpgradeReadySnapshot, { wood: 2, metal: 1 });
upsertSavedTile(residentialUpgradeReadySnapshot, { x: 12, y: 9, zone: 0, roadId: 'arterial', buildingId: '', buildingAgeDays: 0 });
upsertSavedTile(residentialUpgradeReadySnapshot, { x: 13, y: 9, zone: 1, roadId: '', buildingId: 'residential_l1', buildingAgeDays: 12 });
restoreSnapshotThroughStorage(residentialUpgradeReadySnapshot);
drawNextFrame('residential-upgrade-ready controls');
const inspectToolCenter = findToolbarCenter(0);
const residentialTileCenter = screenForTile(13, 9);
tap(inspectToolCenter.x, inspectToolCenter.y);
tap(residentialTileCenter.x, residentialTileCenter.y);
drawNextFrame('selected residential management controls');
const residentialUpgradeButtonCenter = findOrderActionCenter(1);
const savesBeforeResidentialUpgrade = calls.setStorageSync;
tap(residentialUpgradeButtonCenter.x, residentialUpgradeButtonCenter.y);
assert(calls.setStorageSync > savesBeforeResidentialUpgrade, 'Runtime should save after upgrading a residential tile.');
const snapshotAfterResidentialUpgrade = latestSnapshot();
assert(
  findSavedTile(snapshotAfterResidentialUpgrade, 13, 9)?.buildingId === 'residential_l2',
  'Runtime should upgrade the selected residential tile through the management panel.',
);
assert(
  snapshotAfterResidentialUpgrade.materials.wood === 0 && snapshotAfterResidentialUpgrade.materials.metal === 0,
  'Runtime should consume residential upgrade materials.',
);

drawNextFrame('management controls before tax and policy actions');
const highTaxButtonCenter = findCenteredTextInBand(235, 260, 2, 3, 'tax controls');
const savesBeforeTax = calls.setStorageSync;
tap(highTaxButtonCenter.x, highTaxButtonCenter.y);
assert(calls.setStorageSync > savesBeforeTax, 'Runtime should save after changing tax level.');
const snapshotAfterTax = Array.from(storage.values()).at(-1);
assert(snapshotAfterTax?.metrics?.taxLevel === 2, 'Runtime should apply the high tax button through the management panel.');
assert(snapshotAfterTax?.metrics?.taxRatePercent === 12, 'Runtime should save the high tax rate after tapping the management panel.');

const firstPolicyButtonCenter = findCenteredTextInBand(260, 310, 0, 5, 'policy controls');
const savesBeforePolicy = calls.setStorageSync;
tap(firstPolicyButtonCenter.x, firstPolicyButtonCenter.y);
assert(calls.setStorageSync > savesBeforePolicy, 'Runtime should save after toggling a policy.');
const snapshotAfterPolicy = Array.from(storage.values()).at(-1);
assert(snapshotAfterPolicy?.activePolicies?.length === 1, 'Runtime should toggle a policy through the management panel.');

lifecycleCallbacks.hide();
assert(calls.setStorageSync > 0 && storage.size > 0, 'Runtime should save city state on hide.');
lifecycleCallbacks.show();
assert(calls.getStorageSync > 0, 'Runtime should read city state on show.');

const corruptReadsBefore = calls.getStorageSync;
const corruptSaveKey = latestStorageKey();
storage.set(corruptSaveKey, { version: 999, tiles: [], metrics: null });
lifecycleCallbacks.show();
assert(calls.getStorageSync > corruptReadsBefore, 'Runtime should try to read corrupted city state on show.');
drawNextFrame('corrupted save recovery');
assert(calls.fillRect > 0 && calls.fillText > 0, 'Runtime should keep drawing after a corrupted save fallback.');
const recoverySavesBefore = calls.setStorageSync;
const recoveryRoadTile = screenForTile(11, 9);
tap(roadToolCenter.x, roadToolCenter.y);
tap(recoveryRoadTile.x, recoveryRoadTile.y);
assert(calls.setStorageSync > recoverySavesBefore, 'Runtime should continue saving after a corrupted save fallback.');
const snapshotAfterCorruptFallback = latestSnapshot();
assert(
  findSavedTile(snapshotAfterCorruptFallback, 11, 9)?.roadId === 'local',
  'Runtime should continue applying tools after ignoring a corrupted save.',
);

function runFallbackRuntimeSmoke(label, options = {}) {
  const localFrameCallbacks = [];
  const localLifecycleCallbacks = { hide: null, show: null };
  const localTouchCallbacks = { start: null, move: null, end: null };
  const localCalls = {
    createCanvas: 0,
    getContext: 0,
    fillText: 0,
    fillRect: 0,
    requestAnimationFrame: 0,
    setStorageSync: 0,
    getStorageSync: 0,
    vibrateShort: 0,
  };

  const localContext2d = {
    fillStyle: '',
    strokeStyle: '',
    font: '',
    textAlign: 'left',
    textBaseline: 'top',
    lineWidth: 1,
    globalAlpha: 1,
    setTransform() {},
    clearRect() {},
    fillRect() { localCalls.fillRect += 1; },
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
    fillText() { localCalls.fillText += 1; },
    measureText(text) {
      const width = Array.from(String(text)).reduce((total, ch) => total + (ch.charCodeAt(0) > 127 ? 12 : 7), 0);
      return { width };
    },
  };

  const localCanvas = {
    width: 0,
    height: 0,
    getContext(type) {
      assert(type === '2d', `${label}: unexpected canvas context type ${type}.`);
      localCalls.getContext += 1;
      return localContext2d;
    },
    requestAnimationFrame(callback) {
      localCalls.requestAnimationFrame += 1;
      localFrameCallbacks.push(callback);
      return localFrameCallbacks.length;
    },
  };

  const localWx = {
    createCanvas() {
      localCalls.createCanvas += 1;
      return localCanvas;
    },
    getSystemInfoSync() {
      return { windowWidth: 812, windowHeight: 375, pixelRatio: 2 };
    },
    onTouchStart(callback) { localTouchCallbacks.start = callback; },
    onTouchMove(callback) { localTouchCallbacks.move = callback; },
    onTouchEnd(callback) { localTouchCallbacks.end = callback; },
    onHide(callback) { localLifecycleCallbacks.hide = callback; },
    onShow(callback) { localLifecycleCallbacks.show = callback; },
  };

  if (!options.omitStorage) {
    localWx.setStorageSync = () => {
      localCalls.setStorageSync += 1;
      if (options.throwStorage) throw new Error(`${label}: storage write failed`);
    };
    localWx.getStorageSync = () => {
      localCalls.getStorageSync += 1;
      if (options.throwStorage) throw new Error(`${label}: storage read failed`);
      return undefined;
    };
  }

  if (!options.omitVibrate) {
    localWx.vibrateShort = () => {
      localCalls.vibrateShort += 1;
      if (options.throwVibrate) throw new Error(`${label}: vibrate failed`);
    };
  }

  const localSandbox = {
    console,
    wx: localWx,
    GameGlobal: {},
    setTimeout(callback) {
      localFrameCallbacks.push(callback);
      return localFrameCallbacks.length;
    },
    clearTimeout() {},
  };

  new Script(source, { filename: `miniprogram/game.js:${label}` }).runInContext(createContext(localSandbox));
  assert(localSandbox.GameGlobal.__POCKET_CITY_RUNTIME__ === 'NON_UNITY_WECHAT_CANVAS_RUNTIME', `${label}: runtime marker was not published.`);
  assert(localCalls.createCanvas === 1, `${label}: runtime should create one canvas.`);
  assert(localCalls.getContext === 1, `${label}: runtime should request one 2D context.`);
  assert(localTouchCallbacks.start && localTouchCallbacks.end, `${label}: runtime should register touch callbacks.`);
  assert(localLifecycleCallbacks.hide && localLifecycleCallbacks.show, `${label}: runtime should register lifecycle callbacks.`);
  assert(localFrameCallbacks.length > 0, `${label}: runtime should schedule a frame.`);
  localFrameCallbacks.shift()(Date.now() + 16);
  assert(localCalls.fillRect > 0, `${label}: runtime should draw canvas shapes.`);
  assert(localCalls.fillText > 0, `${label}: runtime should draw UI text.`);

  const roadButtonTouch = { clientX: 190, clientY: 344 };
  localTouchCallbacks.start({ touches: [roadButtonTouch] });
  localTouchCallbacks.end({ changedTouches: [roadButtonTouch] });
  localLifecycleCallbacks.hide();
  localLifecycleCallbacks.show();
  if (options.throwStorage) {
    assert(localCalls.getStorageSync > 0, `${label}: runtime should exercise failing storage reads.`);
    assert(localCalls.setStorageSync > 0, `${label}: runtime should exercise failing storage writes.`);
  }
  if (options.throwVibrate) {
    assert(localCalls.vibrateShort > 0, `${label}: runtime should exercise failing haptic feedback.`);
  }
}

runFallbackRuntimeSmoke('missing storage and haptics', { omitStorage: true, omitVibrate: true });
runFallbackRuntimeSmoke('throwing storage and haptics', { throwStorage: true, throwVibrate: true });

console.log('WeChat runtime smoke passed.');

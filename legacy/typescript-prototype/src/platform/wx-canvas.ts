export type RuntimeCanvas = {
  canvas: HTMLCanvasElement;
  width: number;
  height: number;
  pixelRatio: number;
  isWeChat: boolean;
};

type WxLike = {
  createCanvas?: () => HTMLCanvasElement;
  getSystemInfoSync?: () => { windowWidth: number; windowHeight: number; pixelRatio?: number };
  getWindowInfo?: () => { windowWidth: number; windowHeight: number; pixelRatio?: number };
  requestAnimationFrame?: (callback: FrameRequestCallback) => number;
  cancelAnimationFrame?: (handle: number) => void;
};

export function getWx(): WxLike | undefined {
  return (globalThis as { wx?: WxLike }).wx;
}

export function isWeChatRuntime(): boolean {
  return typeof getWx()?.createCanvas === 'function';
}

export function createRuntimeCanvas(): RuntimeCanvas {
  const wx = getWx();
  if (wx?.createCanvas && (wx.getWindowInfo || wx.getSystemInfoSync)) {
    const info = wx.getWindowInfo?.() ?? wx.getSystemInfoSync?.();
    if (!info) {
      throw new Error('WeChat window info is unavailable.');
    }
    const pixelRatio = info.pixelRatio ?? 1;
    const canvas = wx.createCanvas();
    canvas.width = Math.floor(info.windowWidth * pixelRatio);
    canvas.height = Math.floor(info.windowHeight * pixelRatio);
    return {
      canvas,
      width: info.windowWidth,
      height: info.windowHeight,
      pixelRatio,
      isWeChat: true,
    };
  }

  if (typeof document === 'undefined') {
    throw new Error('No canvas runtime is available.');
  }

  const canvas = document.createElement('canvas');
  const pixelRatio = window.devicePixelRatio || 1;
  const width = window.innerWidth || 390;
  const height = window.innerHeight || 844;
  canvas.width = Math.floor(width * pixelRatio);
  canvas.height = Math.floor(height * pixelRatio);
  canvas.style.width = `${width}px`;
  canvas.style.height = `${height}px`;
  canvas.style.display = 'block';
  document.body.style.margin = '0';
  document.body.style.overflow = 'hidden';
  document.body.appendChild(canvas);

  return { canvas, width, height, pixelRatio, isWeChat: false };
}

export function create2dCanvas(width: number, height: number): HTMLCanvasElement {
  const wx = getWx();
  const canvas = wx?.createCanvas ? wx.createCanvas() : document.createElement('canvas');
  canvas.width = Math.max(1, Math.floor(width));
  canvas.height = Math.max(1, Math.floor(height));
  return canvas;
}

export function requestRuntimeFrame(callback: FrameRequestCallback): number {
  const wx = getWx();
  if (wx?.requestAnimationFrame) {
    return wx.requestAnimationFrame(callback);
  }
  if (typeof requestAnimationFrame === 'function') {
    return requestAnimationFrame(callback);
  }
  return Number(setTimeout(() => callback(Date.now()), 16));
}

export function cancelRuntimeFrame(handle: number): void {
  const wx = getWx();
  if (wx?.cancelAnimationFrame) {
    wx.cancelAnimationFrame(handle);
    return;
  }
  if (typeof cancelAnimationFrame === 'function') {
    cancelAnimationFrame(handle);
    return;
  }
  clearTimeout(handle);
}

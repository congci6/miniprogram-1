import { getWx, type RuntimeCanvas } from '../platform/wx-canvas';

type TouchPoint = { x: number; y: number };

export type InputCallbacks = {
  onTap: (x: number, y: number) => void;
  onDrag: (deltaX: number, deltaY: number) => void;
  onPinch: (scale: number) => void;
};

type TouchWx = {
  onTouchStart?: (callback: (event: { touches: Array<{ clientX: number; clientY: number }> }) => void) => void;
  onTouchMove?: (callback: (event: { touches: Array<{ clientX: number; clientY: number }> }) => void) => void;
  onTouchEnd?: (callback: (event: { changedTouches: Array<{ clientX: number; clientY: number }> }) => void) => void;
  onTouchCancel?: (callback: () => void) => void;
};

export class InputController {
  private start?: TouchPoint;
  private last?: TouchPoint;
  private lastPinchDistance?: number;
  private moved = false;

  constructor(private readonly runtime: RuntimeCanvas, private readonly callbacks: InputCallbacks) {}

  attach(): void {
    const wx = getWx() as TouchWx | undefined;
    if (this.runtime.isWeChat && wx?.onTouchStart && wx.onTouchMove && wx.onTouchEnd) {
      wx.onTouchStart((event) => this.handleStart(normalizeTouches(event.touches)));
      wx.onTouchMove((event) => this.handleMove(normalizeTouches(event.touches)));
      wx.onTouchEnd((event) => this.handleEnd(normalizeTouches(event.changedTouches)));
      wx.onTouchCancel?.(() => this.reset());
      return;
    }

    this.runtime.canvas.addEventListener('touchstart', (event) => {
      event.preventDefault();
      this.handleStart(normalizeTouches(Array.from(event.touches)));
    });
    this.runtime.canvas.addEventListener('touchmove', (event) => {
      event.preventDefault();
      this.handleMove(normalizeTouches(Array.from(event.touches)));
    });
    this.runtime.canvas.addEventListener('touchend', (event) => {
      event.preventDefault();
      this.handleEnd(normalizeTouches(Array.from(event.changedTouches)));
    });
    this.runtime.canvas.addEventListener('mousedown', (event) => {
      this.handleStart([{ x: event.clientX, y: event.clientY }]);
    });
    this.runtime.canvas.addEventListener('mousemove', (event) => {
      if (event.buttons === 1) {
        this.handleMove([{ x: event.clientX, y: event.clientY }]);
      }
    });
    this.runtime.canvas.addEventListener('mouseup', (event) => {
      this.handleEnd([{ x: event.clientX, y: event.clientY }]);
    });
    this.runtime.canvas.addEventListener('wheel', (event) => {
      event.preventDefault();
      this.callbacks.onPinch(event.deltaY < 0 ? 1.08 : 0.92);
    });
  }

  private handleStart(touches: TouchPoint[]): void {
    this.moved = false;
    if (touches.length >= 2) {
      this.lastPinchDistance = distance(touches[0], touches[1]);
      return;
    }
    this.start = touches[0];
    this.last = touches[0];
  }

  private handleMove(touches: TouchPoint[]): void {
    if (touches.length >= 2) {
      const currentDistance = distance(touches[0], touches[1]);
      if (this.lastPinchDistance && this.lastPinchDistance > 0) {
        this.callbacks.onPinch(currentDistance / this.lastPinchDistance);
      }
      this.lastPinchDistance = currentDistance;
      this.moved = true;
      return;
    }

    const current = touches[0];
    if (!current || !this.last) {
      return;
    }
    const deltaX = current.x - this.last.x;
    const deltaY = current.y - this.last.y;
    if (Math.abs(deltaX) + Math.abs(deltaY) > 1) {
      this.callbacks.onDrag(deltaX, deltaY);
      this.moved = true;
    }
    this.last = current;
  }

  private handleEnd(touches: TouchPoint[]): void {
    const end = touches[0] ?? this.last;
    if (this.start && end && !this.moved && distance(this.start, end) < 12) {
      this.callbacks.onTap(end.x, end.y);
    }
    this.reset();
  }

  private reset(): void {
    this.start = undefined;
    this.last = undefined;
    this.lastPinchDistance = undefined;
    this.moved = false;
  }
}

function normalizeTouches(touches: Array<{ clientX: number; clientY: number }>): TouchPoint[] {
  return touches.map((touch) => ({ x: touch.clientX, y: touch.clientY }));
}

function distance(a: TouchPoint, b: TouchPoint): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

import { cancelRuntimeFrame, requestRuntimeFrame } from '../platform/wx-canvas';

export class FrameLoop {
  private handle = 0;
  private running = false;
  private lastTime = 0;

  start(callback: (deltaSeconds: number, now: number) => void): void {
    if (this.running) {
      return;
    }
    this.running = true;
    this.lastTime = 0;

    const tick = (now: number) => {
      if (!this.running) {
        return;
      }
      const deltaSeconds = this.lastTime === 0 ? 1 / 60 : Math.min(0.1, (now - this.lastTime) / 1000);
      this.lastTime = now;
      callback(deltaSeconds, now);
      this.handle = requestRuntimeFrame(tick);
    };

    this.handle = requestRuntimeFrame(tick);
  }

  stop(): void {
    this.running = false;
    if (this.handle) {
      cancelRuntimeFrame(this.handle);
    }
  }
}

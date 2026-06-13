export class ToastQueue {
  private message = '欢迎来到口袋城市规划师';
  private expiresAt = 0;

  show(message: string, now = nowMs()): void {
    this.message = message;
    this.expiresAt = now + 2600;
  }

  current(now = nowMs()): string | undefined {
    return now <= this.expiresAt ? this.message : undefined;
  }
}

function nowMs(): number {
  return typeof performance === 'undefined' ? Date.now() : performance.now();
}

export class ObjectPool<T> {
  private readonly free: T[] = [];

  constructor(private readonly create: () => T, private readonly reset?: (item: T) => void) {}

  acquire(): T {
    return this.free.pop() ?? this.create();
  }

  release(item: T): void {
    this.reset?.(item);
    this.free.push(item);
  }

  clear(): void {
    this.free.length = 0;
  }
}

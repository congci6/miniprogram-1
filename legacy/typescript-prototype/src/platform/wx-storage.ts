import { getWx } from './wx-canvas';

type StorageWx = {
  getStorageSync?: (key: string) => unknown;
  setStorageSync?: (key: string, value: unknown) => void;
  removeStorageSync?: (key: string) => void;
};

export class LocalStorageAdapter {
  getItem(key: string): string | undefined {
    const wx = getWx() as StorageWx | undefined;
    if (wx?.getStorageSync) {
      const value = wx.getStorageSync(key);
      return typeof value === 'string' ? value : undefined;
    }
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem(key) ?? undefined;
    }
    return undefined;
  }

  setItem(key: string, value: string): void {
    const wx = getWx() as StorageWx | undefined;
    if (wx?.setStorageSync) {
      wx.setStorageSync(key, value);
      return;
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(key, value);
    }
  }

  removeItem(key: string): void {
    const wx = getWx() as StorageWx | undefined;
    if (wx?.removeStorageSync) {
      wx.removeStorageSync(key);
      return;
    }
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem(key);
    }
  }
}

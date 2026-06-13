import { getWx } from './wx-canvas';

type ShareWx = {
  showShareMenu?: (options?: Record<string, unknown>) => void;
  onShareAppMessage?: (callback: () => { title: string; imageUrl?: string }) => void;
};

export function registerShareEntry(): void {
  const wx = getWx() as ShareWx | undefined;
  wx?.showShareMenu?.({ withShareTicket: true });
  wx?.onShareAppMessage?.(() => ({
    title: '来看看我的口袋城市规划',
  }));
}

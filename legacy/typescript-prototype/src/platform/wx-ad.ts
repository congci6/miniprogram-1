export type RewardedAdResult = {
  ok: boolean;
  message: string;
};

export function showReservedRewardedAd(): RewardedAdResult {
  return {
    ok: false,
    message: '广告能力预留在 M4 阶段接入。',
  };
}

export type PlayerProfile = {
  nickname: string;
  avatarUrl?: string;
};

export function getGuestProfile(): PlayerProfile {
  return {
    nickname: '城市规划师',
  };
}

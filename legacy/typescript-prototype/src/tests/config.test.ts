import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

describe('wechat game config', () => {
  it('runs as a landscape game', () => {
    const gameJson = JSON.parse(readFileSync('miniprogram/game.json', 'utf8')) as {
      deviceOrientation?: string;
    };

    expect(gameJson.deviceOrientation).toBe('landscape');
  });
});

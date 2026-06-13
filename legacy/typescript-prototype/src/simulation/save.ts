import { CityState } from './city-state';
import type { SaveGame } from '../types';

export const SAVE_VERSION = 1;

export function createSave(city: CityState, now = Date.now()): SaveGame {
  return {
    version: SAVE_VERSION,
    createdAt: now,
    updatedAt: now,
    city: city.serialize(),
  };
}

export function serializeSave(save: SaveGame): string {
  return JSON.stringify(save);
}

export function deserializeSave(raw: string): CityState {
  const parsed = JSON.parse(raw) as SaveGame;
  if (parsed.version !== SAVE_VERSION) {
    throw new Error(`Unsupported save version: ${parsed.version}`);
  }
  return CityState.deserialize(parsed.city);
}

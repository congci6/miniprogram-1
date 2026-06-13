import { bootGame } from './engine/app';

try {
  bootGame();
} catch (error) {
  console.error('Pocket City Planner failed to boot.', error);
}

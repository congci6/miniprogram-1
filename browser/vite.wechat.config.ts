import { defineConfig } from 'vite';
import path from 'path';

export default defineConfig({
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  build: {
    target: 'es2018',
    outDir: '../miniprogram',
    emptyOutDir: false,
    minify: true,
    lib: {
      entry: path.resolve(__dirname, './src/wechat/main.ts'),
      name: 'PocketCityMiniGame',
      formats: ['iife'],
      fileName: () => 'game.js',
    },
  },
});

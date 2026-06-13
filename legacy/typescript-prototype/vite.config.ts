import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    outDir: 'miniprogram',
    emptyOutDir: false,
    sourcemap: false,
    minify: 'esbuild',
    lib: {
      entry: 'src/main.ts',
      name: 'PocketCityPlanner',
      formats: ['iife'],
      fileName: () => 'game.js',
    },
    rollupOptions: {
      output: {
        inlineDynamicImports: true,
      },
    },
  },
});

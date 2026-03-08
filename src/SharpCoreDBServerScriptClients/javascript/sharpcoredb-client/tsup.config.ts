import { defineConfig } from 'tsup';

export default defineConfig({
  entry: ['src/index.ts'],
  format: ['cjs', 'esm'],
  dts: true,
  splitting: false,
  sourcemap: true,
  clean: true,
  minify: false,
  target: 'es2020',
  external: [
    '@grpc/grpc-js',
    '@grpc/proto-loader',
    'axios',
    'ws',
    'google-protobuf'
  ],
});

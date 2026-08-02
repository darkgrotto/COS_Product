import path from 'path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { execSync } from 'child_process'

// import.meta.dirname (Node 20.11+) replaces __dirname, which Vite 8's native
// config loader no longer supports.
const rootDir = import.meta.dirname

function getGitCommit(): string {
  try {
    return execSync('git rev-parse --short HEAD').toString().trim()
  } catch {
    return 'dev'
  }
}

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(rootDir, './src'),
    },
  },
  define: {
    'import.meta.env.VITE_GIT_COMMIT': JSON.stringify(process.env.VITE_GIT_COMMIT ?? getGitCommit()),
    'import.meta.env.VITE_BUILD_TAG': JSON.stringify(process.env.VITE_BUILD_TAG ?? null),
  },
  server: {
    proxy: {
      '/api': {
        target: process.env.VITE_API_URL ?? 'http://localhost:3000',
        changeOrigin: true
      },
      '/health': {
        target: process.env.VITE_API_URL ?? 'http://localhost:3000',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: '../wwwroot',
    emptyOutDir: true
  }
})

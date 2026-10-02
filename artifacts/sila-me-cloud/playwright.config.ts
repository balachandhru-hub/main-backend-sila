import { defineConfig, devices } from '@playwright/test';

const port = 5174;

export default defineConfig({
  testDir: './src/tests/e2e',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI ? 'line' : 'list',
  use: {
    baseURL: `http://127.0.0.1:${port}`,
    trace: 'retain-on-failure',
    ...devices['Desktop Chrome'],
    launchOptions: {
      executablePath: process.env.CHROMIUM_PATH ?? '/repl/tools/bin/chromium',
      args: ['--no-sandbox'],
    },
  },
  webServer: {
    command: `PORT=${port} BASE_PATH=/ pnpm --filter @workspace/sila-me-cloud run dev`,
    url: `http://127.0.0.1:${port}/`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
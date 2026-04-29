// @ts-check
const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: 'list',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:5000',
    trace: 'on-first-retry',
    actionTimeout: 10000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'dotnet run --project src/ScrumPoker.Web --no-launch-profile --urls http://localhost:5000',
    url: 'http://localhost:5000',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});

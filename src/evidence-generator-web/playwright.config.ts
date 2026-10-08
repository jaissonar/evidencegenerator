import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./e2e",
  outputDir: "../../work/playwright-results",
  workers: 1,
  use: {
    baseURL: "http://127.0.0.1:5173",
    channel: "msedge",
    headless: true,
    viewport: { width: 1440, height: 1050 },
  },
  webServer: [
    {
      command:
        `dotnet run --project ../EvidenceGenerator.Api --no-launch-profile --urls http://127.0.0.1:5080 -- --DataDirectory ../../work/e2e-data/${Date.now()}`,
      url: "http://127.0.0.1:5080/api/health",
      reuseExistingServer: false,
      timeout: 60000,
    },
    {
      command: "npm run dev",
      url: "http://127.0.0.1:5173",
      reuseExistingServer: true,
    },
  ],
});

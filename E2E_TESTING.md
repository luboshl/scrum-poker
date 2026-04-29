# End-to-End Testing Guide

This document explains how to run the Playwright baseline parity suite and how to use it during the backend migration.

## Purpose

The baseline suite locks down the current user-visible behavior of the Scrum Poker application **before** the backend is migrated from Node.js + Socket.io to ASP.NET Core + SignalR.

Running the suite against both implementations confirms behavioral parity at the UI level without coupling the tests to transport-layer implementation details.

## Prerequisites

Node.js 18 or newer is recommended. Install all dependencies with:

```bash
npm install
```

Then install the Playwright browser binaries (only Chromium is required for the baseline):

```bash
npx playwright install chromium --with-deps
```

## Running the Suite

### Headless (default, recommended for CI)

```bash
npm run test:e2e
```

### Headed (watch tests run in a real browser)

```bash
npm run test:e2e:headed
```

### View the HTML report after a run

```bash
npm run test:e2e:report
```

## How the Suite Starts the Application

The Playwright configuration (`playwright.config.js`) uses the `webServer` option to start `node server.js` automatically before the tests begin and shut it down after they finish. You do not need to start the server manually when running the tests.

If the server is already running on port 3000 (e.g. you started `npm start` yourself), Playwright will reuse it when `CI` is not set.

## Scenario Coverage

The baseline suite covers the following migration-critical behaviors:

| # | Scenario |
|---|----------|
| 1 | Create a room and join it from another browser context |
| 2 | Join as observer and verify voting is disabled |
| 3 | Join two users with the same name and verify server-side renaming |
| 4 | Submit votes and verify hidden-vote behavior before reveal |
| 5 | Reveal voting and verify all votes become visible |
| 6 | Statistics appear after reveal and ignore `?` votes |
| 7 | Reset voting and verify votes are cleared for all clients |
| 8 | Remove a participant as observer and verify forced disconnect |
| 9 | Consensus message appears only when more than one equal numeric vote |

## Using the Suite as a Parity Gate During Migration

1. Run `npm run test:e2e` against the **current Node.js implementation** and confirm all 9 scenarios pass. This establishes the green baseline.
2. Implement the C# backend according to `CSHARP_BACKEND_MIGRATION_SPEC.md`.
3. Point the suite at the new backend by either:
   - updating the `webServer.command` in `playwright.config.js` to start the C# application, or
   - starting the C# server manually and setting `baseURL` / `reuseExistingServer: true`.
4. Run `npm run test:e2e` again. All 9 scenarios must still pass for the migration to be considered complete.

No test logic should need to change between the two backend implementations as long as the HTML structure and user-visible behavior are preserved.

## Configuration

`playwright.config.js` at the repository root controls:

- `testDir` – location of test files (`./tests`)
- `use.baseURL` – application base URL (default `http://localhost:3000`)
- `webServer` – automatic server start/stop around the test run
- `workers: 1` – tests run serially to avoid shared-state conflicts between rooms

## Project Structure

```
playwright.config.js         Playwright configuration
tests/
  baseline.spec.js           All 9 baseline parity scenarios
  helpers/
    RoomPage.js              Page-object helper wrapping Scrum Poker interactions
```

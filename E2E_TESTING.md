# End-to-End Testing Guide

This document explains how to run the Playwright baseline parity suite against the ASP.NET Core + SignalR backend.

## Purpose

The baseline suite validates the user-visible behavior of the Scrum Poker application running on the ASP.NET Core + SignalR backend.

## Prerequisites

Node.js 18 or newer is required for Playwright. Install all Node.js dependencies with:

```bash
npm install
```

Then install the Playwright browser binaries (only Chromium is required for the baseline):

```bash
npx playwright install chromium --with-deps
```

The .NET SDK (v10.0 or later) is also required since the tests start the ASP.NET Core backend automatically.

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

The Playwright configuration (`playwright.config.js`) uses the `webServer` option to start the ASP.NET Core application automatically before the tests begin and shut it down after they finish. You do not need to start the server manually when running the tests.

If the server is already running on port 5000 (e.g. you started `dotnet run --project src/ScrumPoker.Web` yourself), Playwright will reuse it when `CI` is not set.

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

## Configuration

`playwright.config.js` at the repository root controls:

- `testDir` – location of test files (`./tests`)
- `use.baseURL` – application base URL (default `http://localhost:5000`)
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

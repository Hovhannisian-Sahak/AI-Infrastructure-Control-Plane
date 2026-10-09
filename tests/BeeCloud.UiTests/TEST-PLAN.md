# BeeCloud UI E2E Test Plan

The suite uses **.NET 8, Microsoft.Playwright, and NUnit** and targets the running BeeCloud UI/API/database stack. Tests are intentionally browser-driven and validate outcomes through the UI rather than calling Redux internals.

## Phase 1 — Core functionality and lifecycle

- Dashboard loads and exposes node creation, networks, incidents, refresh, and navigation.
- Node creation validation and Provisioning → Available.
- Start, restart, stop, detail navigation, and deleted/unknown detail behavior.
- Network creation, activation/deactivation, deletion confirmation, and card-level errors.
- Network attachment/detachment, attached-node exclusion, and inactive-network restrictions.
- Duplicate-name and API-conflict behavior where supported by the real backend contract.

Existing tests in `NodeCreationTests`, `NodeActionTests`, `NodeDeletionTests`, `NetworkTests`, `NodeDetailTests`, and `DashboardTests` remain part of this phase. New attachment tests are in `NetworkAttachmentTests`.

## Phase 2 — Pagination

- Compute nodes (8 per page).
- Networks (6 per page).
- Network attachments (3 per page).
- Incidents (12 per page, API-backed count).
- Health node list (10 per page).
- Active and historical health alerts.
- Cursor-paginated health and metric history on node details.

The initial browser tests cover forward/back navigation for node and network pagination. Additional pagination tests should be added only with enough deterministic records for each list; cursor pagination requires enough historical records to make the API return cursors.

## Phase 3 — Health and incidents

- Health Overview navigation and health table.
- Time-range selection and status filters.
- Node comparison selection and deselection.
- Incident severity, status, node, and date filters; clear filters resets the filter state.
- Health alerts, metrics, current-vs-historical status, and detail/history links.

The browser UI does not expose a fault injection control. Do not write a UI-only test that pretends a user can trigger a simulated fault through a missing button. Use a separate hybrid API/UI fixture after the simulation endpoint request contract and worker timing are verified.

## Phase 4 — Cross-feature workflows

- Create → provision → open detail → start → reload → verify Running → stop → verify Stopped.
- Create network → attach available node → reload → verify attachment → detach.
- Extend to health/incident recovery only when deterministic fault injection is available through a supported test API or simulator contract.

## Phase 5 — Reliability and CI readiness

- API failure handling and recovery/refresh behavior.
- Per-test unique data to reduce collisions.
- Browser context isolation and screenshot attachments on test failure.
- Test categories for selective execution.
- Headless Chromium in CI; visible browser locally with `BEECLOUD_HEADLESS=false`.
- Store NUnit/TRX results and screenshots as CI artifacts.
- Run against a disposable test database, never a personal or production database.

## Run locally

Start PostgreSQL, the API, and the UI first. The defaults are:

- UI: `http://localhost:3001`
- API: `http://localhost:8081`

From the repository root:

```powershell
dotnet build tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj
& "tests/BeeCloud.UiTests/bin/Debug/net8.0/playwright.ps1" install chromium
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj --logger "trx;LogFileName=beecloud-ui-e2e.trx"
```

Select a category:

```powershell
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj --filter "TestCategory=Pagination"
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj --filter "TestCategory=Health"
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj --filter "TestCategory=Reliability"
```

Override URLs with `BEECLOUD_UI_URL` and `BEECLOUD_API_URL`. Use `BEECLOUD_HEADLESS=false` to show the browser.

## CI checklist

1. Provision an isolated PostgreSQL database and apply the same migrations used by the app.
2. Start API and UI, then wait for their health/readiness endpoints.
3. Restore/build the test project and install Playwright Chromium.
4. Run NUnit tests with a TRX logger.
5. Publish TRX and `TestResults/Screenshots` even when tests fail.
6. Fail the job when build or test execution fails.

## Reliability boundaries

- E2E tests mutate real app data. The database must be dedicated to tests.
- These tests do not reset the database and must use unique names.
- Some flows require asynchronous workers; tests should wait on observable state transitions rather than fixed sleeps.
- API fault injection must be scoped to the specific request and removed automatically by the browser context teardown.
- Fault → incident → quarantine → remediation → recovery is not yet a valid pure-UI workflow because the UI currently exposes no fault simulation action.

# BeeCloud UI E2E tests (.NET Playwright + NUnit)

This is a structured test-suite package based on the supplied BeeCloud UI source. It is designed to be merged into the existing `tests/BeeCloud.UiTests` project; review/merge rather than blindly overwrite your existing files.

## Structure

```text
BeeCloud.UiTests/
├── Configuration/
│   └── UiTestSettings.cs
├── Fixtures/
│   └── UiTestBase.cs
├── Helpers/
│   ├── NetworkTestHelper.cs
│   ├── NodeTestHelper.cs
│   └── TestData.cs
├── Pages/
│   ├── DashboardPage.cs
│   ├── HealthPage.cs
│   └── NodeDetailPage.cs
└── Tests/
    ├── DashboardTests.cs
    ├── HealthOverviewTests.cs
    ├── IncidentTests.cs
    ├── NetworkTests.cs
    ├── NodeActionTests.cs
    ├── NodeCreationTests.cs
    ├── NodeDeletionTests.cs
    └── NodeDetailTests.cs
```

## Preconditions

- PostgreSQL and BeeCloud API are running and reachable at `http://localhost:8081`.
- BeeCloud UI is running at `http://localhost:3001`.
- API and UI are connected to the same environment/database.
- Use a test database: these tests create and delete real nodes and networks.
- Install Playwright browser binaries after restoring packages.

## Commands

From the repository root, if merged into the existing project:

```powershell
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj
```

Install the Chromium browser for the test project when needed:

```powershell
pwsh tests/BeeCloud.UiTests/bin/Debug/net8.0/playwright.ps1 install chromium
```

If the script isn't found, build the test project first and check the generated `playwright.ps1` path.

Run with a visible browser:

```powershell
$env:BEECLOUD_HEADLESS = "false"
dotnet test tests/BeeCloud.UiTests/BeeCloud.UiTests.csproj
Remove-Item Env:BEECLOUD_HEADLESS
```

Override URLs:

```powershell
$env:BEECLOUD_UI_URL = "http://localhost:3001"
$env:BEECLOUD_API_URL = "http://localhost:8081"
```

## Design choices

- Page objects centralize stable, user-facing locators.
- Helpers encapsulate unique data and asynchronous node provisioning.
- Wait on meaningful UI states rather than fixed sleeps.
- Each test creates uniquely named resources to reduce collisions.
- Failed tests save a screenshot to `TestResults/Screenshots` and attach it to NUnit results.
- Browser is headless by default for CI and can be shown locally with `BEECLOUD_HEADLESS=false`.

## Current scope and deliberate limitation

The supplied UI has no user-facing fault-simulation control. `GpuFailure` appears in the node model and cards, but there is no simulation button in the UI component tree. Therefore, this suite tests the actual exposed UI workflows and does not pretend a fault can be injected through the UI. A fault → incident → quarantine → remediation → recovery test should be added as a hybrid UI/API E2E test only after confirming the backend simulation endpoint's exact request contract and remediation timing.

## Compatibility note

The current local project already has working tests. Keep its package versions and existing `UiTestBase` if they differ; copy/merge the page objects, helpers, and test cases as appropriate rather than replacing known-good setup code.

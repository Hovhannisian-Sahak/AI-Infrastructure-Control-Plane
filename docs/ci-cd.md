# CI/CD process

## Pull requests and main

The `CI` workflow runs on pull requests into `main` and on every push to
`main`. It separates feedback into four jobs:

- **Frontend checks:** clean npm install, Jest tests, ESLint, and a production
  Next.js build.
- **Backend unit and integration tests:** .NET 8 unit tests and integration
  tests. Integration tests use Testcontainers on the GitHub-hosted Docker
  runner.
- **API and browser end-to-end tests:** starts isolated PostgreSQL and Redis
  services, applies EF migrations, starts the API and worker, then runs API
  contract tests and the Playwright UI suite against that local stack.
- **Container image build:** builds the API, worker, UI, and migration images to
  catch packaging regressions.

Configure branch protection for `main` to require all four CI jobs before
merging. Require pull requests and at least one review, dismiss stale approvals
after new commits, and block force-pushes/deletion of `main`. Keep credentials
out of PR workflows; CI uses an isolated database and a CI-only password.

## Continuous delivery to staging

After the `CI` workflow succeeds for a push to `main`, `Publish and deploy
staging` builds and publishes immutable commit-SHA images for the API, worker,
UI, and migrations to GHCR. It then deploys those exact images to the existing
`beecloud` Kubernetes namespace. Database migrations must complete before the
API, worker, and UI rollouts proceed.

The deployment uses a **self-hosted GitHub Actions runner** labelled
`beecloud-kubernetes`. Register it on a trusted machine that can reach the
cluster's API and that has `kubectl`, PowerShell 7, and a Kubernetes context
configured for the staging namespace. Do not expose this runner to untrusted
pull-request jobs. Use a namespace-scoped Kubernetes identity where possible.

Create a protected GitHub environment named `staging` and configure these
environment secrets:

- `BEE_CLOUD_DATABASE_CONNECTION`: PostgreSQL connection string used by API,
  worker, and migrations.
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`: matching credentials for
  the PostgreSQL deployment. Existing persistent data must use the same
  credentials; changing this secret does not rotate credentials in an
  initialized database.
- `GHCR_USERNAME`, `GHCR_PULL_TOKEN`: a GitHub identity and token with
  read-only access to the four GHCR packages.

Before the first delivery, an administrator must create the `beecloud`
namespace and ensure its storage class supports the PostgreSQL PVC. The workflow
creates/updates the Kubernetes application and registry-pull secrets from the
protected environment values; it does not apply the development secret
manifest. Add required reviewers to the `staging` environment if promotion
should wait for approval.

This deployment is configured for the current local-forwarding setup: the
browser-facing UI is built to call `http://localhost:8081`. After deployment,
run port-forwarding from a machine that can reach the cluster:

```powershell
kubectl port-forward -n beecloud svc/beecloud-api 8081:80
kubectl port-forward -n beecloud svc/beecloud-ui 3001:3001
```

For a remotely hosted UI/API, change the UI API base URL build argument and
configure the API CORS origin to the actual UI origin before using this release
workflow.

## Performance testing

The `Performance tests` workflow is manually dispatched against a protected
GitHub environment named `performance`. Configure its variables:

- `PERFORMANCE_API_BASE_URL`: API URL for a dedicated performance environment.
- `PERFORMANCE_UI_BASE_URL`: UI URL for the same environment.
- `PERFORMANCE_NODE_ID` (optional): node ID for health/metrics history requests.

Protect that GitHub environment with required reviewers. Use a disposable
database for `include_writes`; never point load profiles at production. Start
with `baseline`, then use `load` when the environment is ready. `stress`, `spike`,
and `soak` are explicit, manually selected runs. The runner enforces the
configured p95 and error-rate thresholds and stores the JTL, JMeter log, and HTML
report as a 30-day workflow artifact, even when a run fails.

## Production promotion

Production promotion is intentionally not automated yet. First validate staging
rollouts and performance results, then define a separate protected production
environment and approval policy. Promote the same image digests that passed
staging, and keep rollback tied to the last known-good digest. Never add
production credentials to repository files or expose them to pull-request jobs.

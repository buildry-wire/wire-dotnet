# Releasing Wirepayment

Releases are driven by pushing a `v*` git tag. The `release` workflow builds, tests, packs,
publishes to NuGet.org, and creates a GitHub Release.

## 1. Choose a publishing method

You can publish to NuGet.org with **either** OIDC Trusted Publishing (recommended, no
long-lived secret) **or** a classic API key secret. The release workflow tries OIDC first
and falls back to the API key.

### Option A — Trusted Publishing via OIDC (recommended)

NuGet.org supports GitHub Actions OIDC trusted publishing: the workflow exchanges a
short-lived OIDC token for a temporary NuGet API key, so no API key is ever stored.

1. Sign in to [nuget.org](https://www.nuget.org) with the account that owns (or will own)
   the `Wirepayment` package.
2. Go to **Account settings → Trusted Publishing** and add a new policy:
   - **Repository owner**: `buildry-wire`
   - **Repository**: `wire-dotnet`
   - **Workflow file**: `release.yml`
   - (optionally restrict to the `release` environment / `v*` tags)
3. In the GitHub repo, add a **repository variable** (Settings → Secrets and variables →
   Actions → Variables) named `NUGET_USER` set to your NuGet.org username. The workflow only
   runs the OIDC login step when `NUGET_USER` is set.

The workflow already requests `id-token: write` permission and uses the `NuGet/login`
action to obtain a short-lived key. If the action name/inputs differ in your NuGet/login
version, consult its README — the step is isolated and easy to adjust.

### Option B — API key secret (fallback)

1. On nuget.org, create an API key scoped to push the `Wirepayment` package
   (**Account settings → API Keys**).
2. In the GitHub repo, add a **secret** named `NUGET_API_KEY` with that value
   (Settings → Secrets and variables → Actions → Secrets).
3. Do **not** set the `NUGET_USER` variable (or the OIDC step would run first). The fallback
   step publishes using the `NUGET_API_KEY` secret.

## 2. Cut a release

1. Bump `<Version>` in `src/Wirepayment/Wirepayment.csproj`.
2. Update `CHANGELOG.md`.
3. Commit, then tag and push. The tag (minus the leading `v`) must equal `<Version>`:

   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

The release workflow verifies the tag matches the csproj version before publishing.

## 3. Verify

- Check the `release` workflow run succeeded.
- Confirm the package appears on [nuget.org/packages/Wirepayment](https://www.nuget.org/packages/Wirepayment).
- Confirm the GitHub Release was created with the `.nupkg` attached.

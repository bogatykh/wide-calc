# wide-calc / PrintMeter

Desktop utility for typography workflows: batch-read PDF page sizes, compute linear meters, and group by format (ISO A-series with tolerance).

## Repository layout

- `src/PrintMeter.Core` — domain models, tolerance/rounding defaults, batch analysis
- `src/PrintMeter.Pdf` — `PdfPig` page dimension reader
- `src/PrintMeter.Export` — CSV (`;`, UTF-8 BOM) + XLSX export (`ClosedXML`)
- `src/PrintMeter.App.ViewModels` — MVVM layer (testable on macOS/Linux)
- `src/PrintMeter.App` — WPF UI + hosting + Serilog (Windows only)
- `tests/*` — xUnit + FluentAssertions

## Build & test (Windows)

```bash
dotnet restore PrintMeter.sln
dotnet build PrintMeter.sln -c Release
dotnet test PrintMeter.sln -c Release --no-build
```

This is what PR CI runs (fast quality gate only).

## Publish single-file self-contained `win-x64`

```bash
dotnet publish src/PrintMeter.App/PrintMeter.App.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:PublishTrimmed=false ^
  -p:PublishReadyToRun=true ^
  -o ./artifacts/publish
```

Output: `artifacts/publish/PrintMeter.exe` (plus satellite files if any).

## Build Windows installer (Inno Setup)

Installer script: `installer/PrintMeter.iss`.

On Windows with Inno Setup installed:

```bash
iscc /DMyAppVersion=1.0.0 installer/PrintMeter.iss
```

Output: `artifacts/installer/PrintMeter-Setup-x64.exe`.

Installer behavior:

- installs per-machine into `Program Files`
- creates Start Menu shortcut for **all users** (`{commonprograms}`)
- optional Desktop shortcut for **all users** (`{commondesktop}`)

Silent install examples:

```bat
PrintMeter-Setup-x64.exe /VERYSILENT /NORESTART /SP-
```

```bat
PrintMeter-Setup-x64.exe /VERYSILENT /NORESTART /SP- /TASKS="desktopicon"
```

## Develop on macOS

WPF (`PrintMeter.App`) does not build on macOS. Use the solution filter:

```bash
dotnet restore PrintMeter.Mac.slnf
dotnet build PrintMeter.Mac.slnf -c Release
dotnet test PrintMeter.Mac.slnf -c Release --no-build
```

Windows UI + publish are verified in GitHub Actions (`windows-latest`).

## Configuration

`src/PrintMeter.App/appsettings.json`:

- `PrintMeter:MaxDegreeOfParallelism` — parallel PDF reads (default `4`)
- `PrintMeter:FormatToleranceMm` — ISO match tolerance in mm (default `2`)
- Pricelist-style “billing sheet count” uses built-in nominal long-edge mm per ISO label (`PricelistFormatEquivalence.IsoNominalLongEdgeMm`) and rounding from `PricelistFormatEquivalence.DefaultRounding` — not configured via appsettings unless you extend the code

Logs: `%LocalAppData%/PrintMeter/logs/`.

## Validation

See [docs/VALIDATION.md](docs/VALIDATION.md).

## Automatic versioning (GitHub)

Versioning is automated with **Release Please**:

- workflow: `.github/workflows/versioning.yml`
- config: `release-please-config.json`
- manifest state: `.release-please-manifest.json`
- **Token:** `versioning.yml` uses the default **GITHUB_TOKEN** from `release-please-action@v4` (no `token:` override). Add `token: ${{ secrets.RELEASE_PLEASE_TOKEN }}` to that step **only** if your org forbids releases/PRs from `GITHUB_TOKEN`; use a **classic PAT** with `repo` scope (fine‑grained PATs often need **Contents** + **Pull requests** + **Issues** R/W and **Actions** read-only — see [release-please-action#1048](https://github.com/googleapis/release-please-action/issues/1048)). A bad `RELEASE_PLEASE_TOKEN` repo secret commonly causes **Resource not accessible by integration** — **delete** that secret and rely on GITHUB_TOKEN, or recreate the PAT / authorize SSO for the org account.
- **Stuck merged release PR** (`autorelease: pending` blocking new bumps): run workflow **release-please-unstick** (dispatch) with the merged PR number, or edit labels on that PR manually.
- If **create release** still fails with integration/403 even with **no** PAT secret: set **Settings → Actions → General → Workflow permissions** to **Read and write** for the repository (ensure the org policy is not capped at read‑only).

How it works:

1. Push commits to `main` using Conventional Commit style (`feat:`, `fix:`, `chore:`).
2. Release Please opens/updates a release PR with next SemVer bump and changelog.
3. When that PR is merged, the next run of Release Please on `main` creates the **GitHub Release** and **git tag** `vX.Y.Z`.
4. Job **`publish-release-assets`** builds the installer/zip and **uploads assets** onto that existing release (`softprops/action-gh-release`).
   - `PrintMeter-Setup-x64.exe`
   - `PrintMeter-win-x64.zip`
5. If assets are missing for an existing release (immutable releases), run `versioning` manually with `release_tag` (for example `v0.2.3`) to backfill assets.

Manual release workflow responsibilities:

- `release.yml` is kept as manual fallback (`workflow_dispatch`).

## Commit message policy

- Conventional Commits are required (`feat:`, `fix:`, `chore:`, etc.).
- Workflow `.github/workflows/commitlint.yml` validates commit messages on PRs and pushes to `main`.
- Cursor project rule for this is stored in `.cursor/rules/release-versioning.mdc`.

# PrintMeter Release Checklist

Use this checklist before publishing a production release for the typography workstation.

## 1) Pre-flight

- [ ] All intended changes are merged into `main`.
- [ ] Commit messages follow Conventional Commits (`feat:`, `fix:`, ...).
- [ ] No local-only hacks/debug leftovers.

## 2) Quality gates

- [ ] CI is green on latest `main` (`commitlint`, `ci`, tests).
- [ ] `dotnet test PrintMeter.Mac.slnf -c Release` проходит локально на macOS (без `PrintMeter.App` — там нужен MAUI workload).
- [ ] При необходимости сборки приложения: `dotnet workload restore` и `dotnet build PrintMeter.App.slnf -c Release`.
- [ ] Windows build job produced `PrintMeter.exe` and installer artifact.

## 3) Windows smoke test (real workstation)

- [ ] Install with `PrintMeter-Setup-x64.exe` (admin).
- [ ] Start Menu shortcut appears for all users.
- [ ] Optional desktop shortcut works (if selected).
- [ ] App starts and opens file/folder dialogs correctly.
- [ ] Analyze 5–10 real PDF files from production.
- [ ] Сводка и таблица совпадают с ожидаемыми длинами по форматам.

## 4) Accuracy validation

- [ ] Compare calculated meters vs Acrobat baseline for control files.
- [ ] Difference is within accepted threshold (target: <= 0.5%).
- [ ] If needed, adjust tolerance (`PrintMeter:FormatToleranceMm`) and retest.

## 5) Release/versioning

- [ ] Release Please PR is merged (or ready to merge).
- [ ] Tag/release `vX.Y.Z` exists in GitHub.
- [ ] Release assets are attached:
  - [ ] `PrintMeter-Setup-x64.exe`
  - [ ] `PrintMeter-win-x64.zip`
- [ ] Release notes are clear for operators (what changed and how to update).

## 6) Post-release

- [ ] Keep previous installer available for rollback.
- [ ] Confirm at least one operator can install/update without manual file copy.
- [ ] Record known issues/workarounds in release notes if any.

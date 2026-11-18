# Repository Guidelines

## Project Structure & Module Organization

- `src/Lianxin.Fleet.Core/`: domain models, device bridges, persistence, services, validation, infrastructure, and serialization.
- `src/Lianxin.Fleet.Desktop/`: WPF views, view models, interaction, and resources.
- `src/Lianxin.Fleet.Mac/`: Avalonia 12 macOS workstation (`Views`, `ViewModels`, `Interaction`, `Resources`, plus root `App.axaml`/`App.axaml.cs`/`Program.cs`); uses `~/Library/Application Support/LianxinFleet.Mac/`, shared with the CLI on macOS, and is bundled as `LianxinFleet-Mac.app`.
- `src/Lianxin.Fleet.Cli/`: diagnostics, commands, and Mac iOS signing tools.
- `src/Lianxin.Sms.Api/`: ASP.NET Core SMS service with hosting, routing, persistence, security, validation, and protocol categories.
- `checks/`: executable C# checks; scenarios belong in `Scenarios/`, reusable fixtures in `Infrastructure/`.
- `ios/`: Swift application, models, networking, security, and views. Update Xcode source references when moving files.
- `build/`, `scripts/`, and `docs/`: governance rules, verification/publishing scripts, and operational documentation.

## Build, Test, and Development Commands

Use the .NET 10 SDK selected by `global.json`.

```sh
dotnet build Lianxin.Fleet.sln -c Release
dotnet run --no-build -c Release --project checks/Lianxin.Fleet.Checks
dotnet run --no-build -c Release --project checks/Lianxin.Sms.Checks
dotnet run --project src/Lianxin.Fleet.Cli -- status --data "$(mktemp -d)"
dotnet run --project src/Lianxin.Sms.Api -- --demo
```

These build the solution, run regressions, query fleet status against an empty temporary database, and start the SMS demo API. The fleet apps and CLI have no demo mode; they only drive real devices. On Windows, run `powershell -File scripts/verify.ps1` for governance, formatting, build, and regression checks. WPF execution requires Windows; iOS builds require Mac/Xcode.

## Coding Style & Naming Conventions

Follow `.editorconfig`: four-space C#/XAML/MSBuild indentation, two-space JSON/YAML indentation, UTF-8, LF by default, and CRLF for Windows scripts. Use PascalCase types/methods, camelCase parameters/locals, and file-scoped namespaces matching directories, such as `Lianxin.Fleet.Core.Devices`.

Target 200–300 physical lines per file; 300 is mandatory. Keep cohesive smaller files. Split oversized classes by responsibility, using names such as `FleetStore.Tasks.cs`. Builds enforce `GOV001`–`GOV004`; see `docs/CODE_GOVERNANCE.md`. Keep business/UI implementations in C# and iOS code in Swift; do not reintroduce JavaScript.

## Testing Guidelines

Tests use console-based C# check runners. Name scenario files `*Checks.cs` and register descriptive checks in their runner. Cover nontrivial changes, especially isolation, idempotency, concurrency, recovery, and installation boundaries. SMS checks use real Kestrel and temporary SQLite databases. Run both suites; distinguish simulation from hardware validation. For iOS changes, also build the simulator target without signing.

## Commit & Pull Request Guidelines

This checkout lacks Git metadata, so historical conventions cannot be verified. Use concise imperative subjects, for example `refactor(core): classify device services`. PRs should explain behavior, scope, validation commands/results, and relevant issues. Include screenshots for UI changes and disclose unverified platform/device behavior.

## Security & Configuration

Use temporary databases for verification (`--data` for the CLI, `FLEET_DATA_DIR` for the Mac app; the SMS API keeps its `--demo` mode). Preserve existing business data. Keep credentials, certificates, provisioning profiles, and generated artifacts out of commits. Configure `PYTHON_PATH` and `FLEET_APP_DIR` locally. Smoke-run the Mac app only with `FLEET_DATA_DIR` pointing at a temporary directory; overriding `HOME` does not isolate it on macOS; retain approved-directory installation checks and per-device execution safeguards.

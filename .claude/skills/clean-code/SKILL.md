---
name: clean-code
description: >-
  C# style, formatting, and dependency hygiene for ToxicFishing. Use when writing or refactoring C#,
  resolving Roslynator/analyzer warnings, running dotnet format, or adding/updating NuGet packages.
  Ensures code matches the strict .editorconfig and that dependencies stay on their latest stable,
  compatible versions.
---

# Clean Code & Dependencies

Quality is enforced by **Roslynator analyzers** (`Roslynator.Analyzers` +
`Roslynator.Formatting.Analyzers`, referenced in every project) plus a strict `.editorconfig`.
**A clean build has zero new warnings** — even though the project does not set
`TreatWarningsAsErrors`, treat new warnings as failures.

## Formatting — the analyzer is the source of truth

Run `dotnet format ToxicFishing.sln` before committing, then `dotnet build` and fix every warning.
Don't hand-tune style against the rules below — let the tooling normalize it. The conventions that
matter most here (from `.editorconfig`):

- **`var` always** for local declarations (`roslynator_use_var = always`).
- **Explicit accessibility modifiers** on every declaration.
- **File-scoped namespaces**, with a blank line after the declaration.
- **4-space indentation**, **LF** line endings, **UTF-8**, final newline, no trailing whitespace.
- **Max line length 140** (`roslynator_max_line_length = 140`); wrap long signatures.
- **Trailing commas** in multi-line initializers, enums, and argument lists
  (`roslynator_trailing_comma_style = include`).
- **Pattern-matching null checks** (`is null` / `is not null`), not `== null`.
- **Primary constructors** for services that only store injected dependencies (see `BotController`,
  `FishingSession`, `WowInput`).
- **Collection expressions** (`[]`) where applicable (`ServiceCollection services = [];`,
  `ObservableCollection<…> X { get; } = [];`).
- `internal sealed` for implementation classes, behind `public` interfaces (see `modular-architecture`
  for the `InternalsVisibleTo` model that exposes them to `Test`).

When unsure, open a neighbouring file in the same module and match it.

## Clean-code habits

- Small, single-responsibility methods and classes; clear, intention-revealing names.
- No dead code, commented-out blocks, or leftover TODOs in committed work.
- Fail fast: validate inputs and return early.
- Don't duplicate logic — search first and reuse (`PixelPoint.DistanceTo`, the `AppOptions`
  constants, `IHumanizer` for delays) instead of reinventing it.
- Detection- and input-sensitive code has extra rules — see `vision-and-detection` and
  `input-and-discretion`.

## NuGet dependencies — keep them current

Versions are managed **centrally** through `Directory.Packages.props` at the solution root
(`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`). Each version is declared
**once** there with `<PackageVersion Include="…" Version="…" />`; the per-project `.csproj` files
reference packages **without** a `Version` attribute (`<PackageReference Include="…" />`). Consequences:

- To **add** a package, add a `<PackageVersion>` entry to `Directory.Packages.props` **and** a
  versionless `<PackageReference>` to the consuming project(s).
- To **update** a version, edit the single `<PackageVersion>` entry. **Never** put a `Version` back on a
  `<PackageReference>` — NuGet errors (NU1008) on that under central management.
- **`OpenCvSharp4` and `OpenCvSharp4.runtime.win` must share the exact same version** (the managed
  wrapper and its native runtime are released in lockstep).
- Analyzer packages keep their `PrivateAssets="all"` / `IncludeAssets` metadata on the
  `<PackageReference>` in each project; only the version lives in `Directory.Packages.props`.

Keep dependencies on their **latest stable, compatible** versions. When touching package references
or starting substantial work:

```bash
dotnet list ToxicFishing.sln package --outdated
dotnet list ToxicFishing.sln package --vulnerable --include-transitive
dotnet list ToxicFishing.sln package --deprecated
```

Bump to the newest **stable** release compatible with **.NET 10**; avoid pre-release unless asked.
After any update, `dotnet build` must pass with zero new warnings before the change is done.

Current direct dependencies: `OpenCvSharp4` + `OpenCvSharp4.runtime.win` (`Vision`);
`Microsoft.Extensions.DependencyInjection.Abstractions` (each module that ships an
`Add<Module>Module()` extension); `Microsoft.Extensions.DependencyInjection` (`App` and `Test`, which
build the container); `Roslynator.Analyzers` + `Roslynator.Formatting.Analyzers` (all projects,
analyzers only).

# Repository guide

Paths in this document are relative to the repository root.
Read and follow the [development policy](policies/development.md) alongside this guide.

## About

`VolocyNazad.AssemblyResolver` provides disposable assembly resolution scopes through `ResolveHelper.BeginAssemblyResolveScope`, resolving assemblies from a stack of module directories and managing existing assembly resolution handlers.

## Repository structure

- `Toolkit.AssemblyResolver.slnx` - solution.
- `src/Toolkit.AssemblyResolver/` - library, `ResolveHelper`, and `UnsafeDomainAccessors`.
- `Directory.Build.props` - shared compiler settings and MinVer configuration properties.
- `global.json` - .NET SDK selection.
- `.github/workflows/` - CI and publishing workflows.

## Technology stack

- `Microsoft.NET.Sdk`, targeting `netstandard2.0;netstandard2.1;net8.0;net48`, platform `x64`.
- `global.json` requests SDK `10.0.103`, allows roll-forward within the 10.0 major/minor line, and disables prerelease SDKs.
- Shared properties enable nullable reference types, implicit usings, XML documentation, and build-time code style enforcement; language version is `latest`.
- Runtime-specific assembly resolution uses conditional compilation, reflection, and unsafe accessors for .NET 8 and later.
- The library references MinVer as a private build dependency; other package dependencies are not exposed to consumers.
- No Revit API package reference.

## Documentation layout

- `AGENTS.md` links to the required repository guidance.
- `docs/policies/development.md` contains the development policy.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `CHANGELOG.md` records changes under `Unreleased` before release.

The root solution exposes the documentation files under a `docs` solution folder in Visual Studio, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`). CI and publishing install the SDK from this file. Additional SDK installations may provide older test runtimes. See the [SDK selection policy](policies/development.md#net-sdk-selection).

## Solution items

The root solution exposes repository-level documents and configuration under `solutionItems`, GitHub files and maintenance scripts in matching subfolders, and documentation under `docs/`. The list is explicit, not a filesystem glob; keep links up to date when files change. See the [solution items policy](policies/development.md#solution-items).
## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents,
their navigation links, and complete, valid Solution Items. The
`.github/workflows/repository-policy.yml` workflow runs it for pushes and pull
requests. See the [development policy](policies/development.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. Existing repositories may add stricter C# or analyzer-specific settings. See the [development policy](policies/development.md#formatting-baseline).

## Testing

The automated test suite is in `tests/Toolkit.AssemblyResolver.Tests` and verifies resolver-scope lifecycle behavior.

## Versioning and release tags

The library uses MinVer 8 with stable tags in the `vMAJOR.MINOR.PATCH` format. The tag without its `v` prefix is the NuGet package version. Manual publishing accepts exactly one matching tag at HEAD and verifies the package ID and version before pushing to NuGet.

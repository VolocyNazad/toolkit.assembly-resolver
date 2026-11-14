# Repository guide

Paths in this document are relative to the repository root.
Read and follow the [development policy](policies/development.md) alongside this guide.

## About

`VolocyNazad.AssemblyResolver` provides disposable assembly resolution scopes through `ResolveHelper.BeginAssemblyResolveScope`, resolving assemblies from a stack of module directories and managing existing assembly resolution handlers.

## Repository structure

- `Toolkit.AssemblyResolver.slnx` - solution.
- `src/Toolkit.AssemblyResolver/` - library, `ResolveHelper`, and `UnsafeDomainAccessors`.
- `Directory.Build.props` - shared compiler settings and MinVer configuration properties.
- `Directory.Packages.props` - central package versions for the library.
- `tests/Directory.Packages.props` - test-only package versions, importing the root file.
- `global.json` - .NET SDK selection.
- `.github/workflows/` - CI and publishing workflows.

## Technology stack

- `Microsoft.NET.Sdk`, targeting `netstandard2.0;netstandard2.1;net8.0;net48`, platform `x64`.
- `global.json` requests SDK `10.0.103`, allows roll-forward within the 10.0 major/minor line, and disables prerelease SDKs.
- Shared properties enable nullable reference types, implicit usings, XML documentation, and build-time code style enforcement; language version is `latest`.
- Runtime-specific assembly resolution uses conditional compilation, reflection, and unsafe accessors for .NET 8 and later.
- Package versions use central package management (`Directory.Packages.props`, `ManagePackageVersionsCentrally=true`); test-only versions live in `tests/Directory.Packages.props`, which imports the root file. MinVer remains a private build dependency, and no package dependencies are exposed to consumers.
- No Revit API package reference.

## Documentation layout

- `AGENTS.md` links to the required repository guidance and routes a change to the document that owns it.
- `docs/policies/` contains the repository policies — the rules a change must follow. The
  [policy index](policies/README.md) says what each policy covers.
- `docs/repository.md` describes the project, repository structure, and technology stack.
- `changelog.d/` holds the pending changelog fragments: a consumer-visible change adds one, and the
  release preparation composes them into `CHANGELOG.md`; the rules are in the
  [changelog policy](policies/changelog.md). Git does not track empty directories, so the folder
  exists only while a fragment is pending.
- `scripts/` holds the repository validator, the changelog tool and the release verification.

The root solution exposes the documentation files under a `docs` solution folder in Visual Studio, preserving their subfolder structure. When adding documentation files, also add them as solution items; solution folders do not automatically include new files.

The root `global.json` selects stable .NET SDK 10.0 (minimum `10.0.103`, `rollForward: latestFeature`). CI and publishing install the SDK from this file. Additional SDK installations may provide older test runtimes. See the [SDK selection policy](policies/build.md#sdk-selection).

## Solution items

The root solution exposes repository-level documents and configuration under `solutionItems`, GitHub files and maintenance scripts in matching subfolders, and documentation under `docs/`. The list is explicit, not a filesystem glob; keep links up to date when files change. See the [solution items policy](policies/repository-maintenance.md#solution-items).
## Repository validation

`scripts/Validate-Repository.ps1` enforces the required repository documents,
their navigation links, the complete and valid Solution Items, and the workflow action pins. The
`.github/workflows/repository-policy.yml` workflow runs it, the pending fragment check
(`Update-Changelog.ps1 -Verify`) and the release structure check (`Verify-Release.ps1`) for pushes
and pull requests. See the [repository maintenance policy](policies/repository-maintenance.md#repository-validation).

## Formatting

The root `.editorconfig` defines the portable formatting baseline. Existing repositories may add stricter C# or analyzer-specific settings. See the [repository maintenance policy](policies/repository-maintenance.md#formatting-baseline).

## Continuous integration

`build-and-test` builds the solution in `Release` with warnings as errors and documentation
warnings promoted to errors, runs the headless tests and uploads the package. `repository-policy`
runs the validator, the pending fragment check and the release structure check. `publish-nuget` is
manual and refuses a commit without a single stable tag and a matching changelog section.

## Testing

The automated test suite is in `tests/Toolkit.AssemblyResolver.Tests` and verifies resolver-scope lifecycle behavior.

## Versioning and release tags

The library uses MinVer 8 with stable tags in the `vMAJOR.MINOR.PATCH` format. The tag without its `v` prefix is the NuGet package version. `scripts/Verify-Release.ps1 -FromHeadTag` accepts exactly one matching tag at `HEAD`, and the publish workflow verifies the package ID and version before pushing to NuGet.

The tagged commit must already contain the release section in `CHANGELOG.md` with an empty `Unreleased` section above it and no pending fragment; the publish workflow stops otherwise. See the [release policy](policies/releases.md) and the [changelog policy](policies/changelog.md#release).

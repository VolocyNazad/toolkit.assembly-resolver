# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- Centralize package versions in `Directory.Packages.props` and move test-only versions into `tests/Directory.Packages.props`.

## [1.0.1] - 2026-09-14

### Added

- Add automated tests for repeated scope disposal and nested resolver scopes.
- Validate required repository files, navigation links, and Solution Items in CI.
- Development policy and repository guide under `docs/`, with required reading links in `AGENTS.md` and navigation from `README.md`.

### Changed

- Activate MinVer package versioning and validate tagged packages before publishing.
- Standardize GitHub Actions workflow filenames and display names by responsibility.
- Configure xUnit v3 test execution through Microsoft.Testing.Platform and fail test runs when no tests are discovered.
- Establish a shared EditorConfig baseline and use the repository-policy validator as the single structural CI check.
- Complete solution items for repository documents, configuration, workflows and maintenance scripts; document the shared layout.
- Standardize local and CI SDK selection on stable .NET 10.0 through global.json, restrict roll-forward to that major/minor line, and configure setup-dotnet to read the file.
- Show documentation in Visual Studio Solution Explorer under a `docs` solution folder with matching subfolders.

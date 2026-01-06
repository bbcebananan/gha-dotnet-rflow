# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Initial project structure
- ASP.NET Core Web API with SOAP-style and REST endpoints
- Windows Authentication support
- Anonymous access for external system integration
- Scheduled task management API
- WiX v4 MSI installer with IIS configuration
- GitHub Actions CI/CD pipeline
- Release Please automation
- dotnet-releaser integration
- Pester PowerShell tests
- xUnit unit and integration tests

### Endpoints

- `GET/POST /api/data` - SOAP-style GetData endpoint (Windows Auth)
- `GET/POST /api/auth` - Authentication operations (Windows Auth)
- `GET/POST /api/external` - External system integration (API Key Auth)
- `POST /api/scheduled/run` - Scheduled task trigger (Windows Auth)
- `GET /health` - Health check endpoint

## [1.0.0] - TBD

Initial release.

[Unreleased]: https://github.com/thpham/gha-dotnet-rflow/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/thpham/gha-dotnet-rflow/releases/tag/v1.0.0

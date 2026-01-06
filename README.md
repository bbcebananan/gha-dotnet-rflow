# MyApp - Windows .NET 9 Application CI/CD Pipeline

A Windows-targeted .NET 9 ASP.NET Core Web API with comprehensive CI/CD pipeline using GitHub Actions, Release Please, dotnet-releaser, and WiX Toolset v4 for MSI installers.

## Overview

This project demonstrates a production-ready Windows application with:

- **ASP.NET Core Web API** with SOAP-style and REST endpoints
- **Windows Authentication** (Negotiate) for internal endpoints
- **Anonymous access** with API key authentication for external integrations
- **GitHub Actions** CI/CD pipeline with Windows runners
- **Release Please** for automated version management
- **dotnet-releaser** for multi-architecture builds
- **WiX Toolset v4** for MSI installer creation
- **Pester** PowerShell tests for integration testing

## Project Structure

```
gha-dotnet-rflow/
├── .github/
│   ├── workflows/
│   │   ├── ci.yml              # Build, test, Pester on PRs
│   │   ├── release.yml         # Release Please + dotnet-releaser + MSI
│   │   ├── backport.yml        # Backport automation
│   │   └── cleanup.yml         # Weekly artifact cleanup
│   └── CODEOWNERS
├── src/
│   └── MyApp/
│       ├── Controllers/        # API endpoints
│       ├── Services/           # Business logic
│       ├── Options/            # Configuration classes
│       ├── RestApis/           # External API clients
│       ├── Program.cs          # Application entry point
│       └── MyApp.csproj
├── tests/
│   ├── MyApp.Tests/            # xUnit tests
│   │   ├── Unit/
│   │   └── Integration/
│   └── Scripts/                # Pester PowerShell tests
├── installer/
│   ├── Installer.wixproj       # WiX v4 project
│   ├── Package.wxs
│   ├── Directories.wxs
│   ├── Features.wxs
│   ├── IISConfiguration.wxs
│   └── Assets/
├── Directory.Build.props       # Centralized version management
├── MyApp.sln
├── release-please-config.json
├── .release-please-manifest.json
├── dotnet-releaser.toml
└── CHANGELOG.md
```

## API Endpoints

| Endpoint             | Method    | Auth         | Description                 |
| -------------------- | --------- | ------------ | --------------------------- |
| `/api/data`          | GET, POST | Windows Auth | SOAP-style data retrieval   |
| `/api/auth`          | GET, POST | Windows Auth | Authentication operations   |
| `/api/external`      | GET, POST | API Key      | External system integration |
| `/api/scheduled/run` | POST      | Windows Auth | Trigger scheduled tasks     |
| `/health`            | GET       | Anonymous    | Health check endpoint       |

## Development Setup

### Prerequisites

- .NET 9 SDK (Windows)
- Visual Studio 2022 or VS Code
- IIS with ASP.NET Core Module (for local IIS testing)
- PowerShell 5.1+ with Pester v5

### Building

```bash
# Restore and build
dotnet build MyApp.sln

# Run tests
dotnet test

# Run Pester tests (PowerShell)
./tests/Scripts/Invoke-PesterTests.ps1
```

### Running Locally

```bash
cd src/MyApp
dotnet run
```

The API will be available at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).

## Configuration

### Application Settings

Configuration is managed through `appsettings.json`:

```json
{
  "AppConfig": {
    "ApplicationName": "MyApp",
    "MaxPageSize": 100,
    "DefaultPageSize": 20,
    "EnableDetailedErrors": false,
    "ExternalApiBaseUrl": "https://api.external.example.com",
    "ExternalApiTimeout": 30,
    "AllowedApiKeyHashes": []
  }
}
```

### API Key Authentication

For external endpoints, API keys are validated using DPAPI-protected hashes:

```powershell
# Generate protected API key hash
./tests/Scripts/Protect-ApiKey.ps1 -ApiKey "your-api-key"
```

Add the generated hash to `AllowedApiKeyHashes` in configuration.

## Version Management

This project uses **Release Please** for automated version management:

- Version is centralized in `Directory.Build.props`
- Release Please extracts version using XPath: `//Project/PropertyGroup/Version`
- Changelog follows [Keep a Changelog](https://keepachangelog.com/) format
- Conventional Commits trigger version bumps:
  - `feat:` - Minor version bump
  - `fix:` - Patch version bump
  - `feat!:` or `BREAKING CHANGE:` - Major version bump

## CI/CD Workflows

### CI Workflow (`ci.yml`)

Triggered on pull requests to `main`:

1. **Build** - Compile solution on Windows runner
2. **Test** - Run xUnit tests with code coverage
3. **Pester** - Execute PowerShell integration tests
4. **MSI Preview** - Build installer without publishing

### Release Workflow (`release.yml`)

Triggered on push to `main`:

1. **Release Please** - Create/update release PR
2. **dotnet-releaser** - Build for win-x64 and win-arm64
3. **WiX Build** - Create MSI installers
4. **GitHub Release** - Publish release with artifacts

### Backport Workflow (`backport.yml`)

Automates cherry-picking merged PRs to release branches:

1. Add label `backport release/1.x` to PR
2. When PR merges, backport PR is created automatically

### Cleanup Workflow (`cleanup.yml`)

Weekly scheduled cleanup of old artifacts and workflow runs.

## WiX Installer

The MSI installer is built with WiX Toolset v4 and includes:

- Application files deployed to `Program Files\MyCorp\MyApp`
- IIS Application Pool configuration (Windows Auth enabled)
- IIS Web Site setup with bindings
- Windows service registration (optional)

### Building the Installer

```bash
# Publish the application first
dotnet publish src/MyApp/MyApp.csproj -c Release -r win-x64 -o ./publish

# Build MSI
dotnet build installer/Installer.wixproj -c Release -p:PublishDir=../publish
```

### Installer Features

- **Core Application** - Required, installs application files
- **IIS Integration** - Optional, configures IIS hosting

## Testing

### Unit Tests

Located in `tests/MyApp.Tests/Unit/`:

- `DataServiceTests.cs` - Data service business logic
- `AuthServiceTests.cs` - Authentication processing

### Integration Tests

Located in `tests/MyApp.Tests/Integration/`:

- `ApiIntegrationTests.cs` - End-to-end API testing with WebApplicationFactory

### Pester Tests

Located in `tests/Scripts/`:

- `Test-Endpoints.Tests.ps1` - HTTP endpoint validation
- `Initialize-ExternalFAS.ps1` - External system setup helper

Run Pester tests:

```powershell
Invoke-Pester -Path ./tests/Scripts -Output Detailed
```

## Deployment

### IIS Deployment

1. Install ASP.NET Core Hosting Bundle
2. Run MSI installer
3. Configure application pool identity
4. Set up Windows Authentication in IIS
5. Configure HTTPS bindings

### Configuration for Production

1. Update `appsettings.Production.json` with production values
2. Configure DPAPI-protected API keys
3. Set up logging to Windows Event Log
4. Configure health check monitoring

## Security

- **Windows Authentication** - Uses Negotiate (Kerberos/NTLM)
- **API Key Auth** - DPAPI-protected hashes for external access
- **HTTPS Only** - Enforced in production
- **Request Logging** - Audit trail for all requests

## License

MIT License - See [LICENSE](LICENSE) for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make changes following Conventional Commits
4. Submit a pull request
5. Ensure CI passes

## Support

For issues and feature requests, please use [GitHub Issues](https://github.com/thpham/gha-dotnet-rflow/issues).

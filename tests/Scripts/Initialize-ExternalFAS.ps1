#Requires -Version 5.1

<#
.SYNOPSIS
    Initializes and configures the connection to an external system.

.DESCRIPTION
    This script sets up the necessary configuration for connecting to external
    systems (FAS - Financial Accounting System, or similar). It validates
    connectivity and prepares the environment for integration.

.PARAMETER BaseUrl
    The base URL of the external system API.

.PARAMETER ApiKey
    The API key for authentication. If not provided, will attempt to read
    from environment variable or encrypted storage.

.PARAMETER Timeout
    Connection timeout in seconds. Default: 30

.PARAMETER ValidateConnection
    If specified, performs a connectivity test after configuration.

.PARAMETER Force
    If specified, overwrites existing configuration.

.EXAMPLE
    .\Initialize-ExternalFAS.ps1 -BaseUrl "https://api.external.com" -ValidateConnection

.EXAMPLE
    .\Initialize-ExternalFAS.ps1 -BaseUrl "https://api.external.com" -ApiKey "key123" -Force

.NOTES
    This script is typically run during initial deployment or when
    reconfiguring the external system connection.
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$BaseUrl,

    [Parameter()]
    [string]$ApiKey,

    [Parameter()]
    [ValidateRange(1, 300)]
    [int]$Timeout = 30,

    [Parameter()]
    [switch]$ValidateConnection,

    [Parameter()]
    [switch]$Force
)

# Script constants
$script:ConfigPath = Join-Path $PSScriptRoot "..\..\src\MyApp\appsettings.json"
$script:EnvVarName = "EXTERNAL_API_KEY"

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('Info', 'Warning', 'Error', 'Success')]
        [string]$Level = 'Info'
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        'Info' { 'White' }
        'Warning' { 'Yellow' }
        'Error' { 'Red' }
        'Success' { 'Green' }
    }

    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-ExternalConnection {
    param(
        [string]$Url,
        [string]$Key,
        [int]$TimeoutSeconds
    )

    try {
        Write-Log "Testing connection to $Url..." -Level Info

        $headers = @{}
        if ($Key) {
            $headers['X-Api-Key'] = $Key
        }

        $params = @{
            Uri = "$Url/health"
            Method = 'GET'
            TimeoutSec = $TimeoutSeconds
            Headers = $headers
            ErrorAction = 'Stop'
        }

        # Skip certificate validation for development
        if ($Url -match 'localhost|127\.0\.0\.1') {
            if ($PSVersionTable.PSVersion.Major -ge 6) {
                $params['SkipCertificateCheck'] = $true
            }
        }

        $response = Invoke-RestMethod @params
        Write-Log "Connection successful!" -Level Success
        return $true
    }
    catch {
        Write-Log "Connection failed: $($_.Exception.Message)" -Level Error
        return $false
    }
}

function Get-StoredApiKey {
    # Try environment variable first
    $envKey = [Environment]::GetEnvironmentVariable($script:EnvVarName, 'User')
    if ($envKey) {
        Write-Log "Using API key from environment variable" -Level Info
        return $envKey
    }

    # Try reading from appsettings
    if (Test-Path $script:ConfigPath) {
        try {
            $config = Get-Content $script:ConfigPath -Raw | ConvertFrom-Json
            if ($config.AppConfig.ExternalApiKey) {
                Write-Log "Using API key from appsettings.json" -Level Info
                return $config.AppConfig.ExternalApiKey
            }
        }
        catch {
            Write-Log "Could not read appsettings.json: $($_.Exception.Message)" -Level Warning
        }
    }

    return $null
}

function Update-Configuration {
    param(
        [string]$Url,
        [string]$Key
    )

    if (-not (Test-Path $script:ConfigPath)) {
        Write-Log "Configuration file not found at $script:ConfigPath" -Level Error
        return $false
    }

    try {
        $config = Get-Content $script:ConfigPath -Raw | ConvertFrom-Json

        # Update external API settings
        $config.AppConfig.ExternalApiBaseUrl = $Url

        # Only update key if provided (don't overwrite with empty)
        if ($Key) {
            $config.AppConfig.ExternalApiKey = $Key
        }

        if ($PSCmdlet.ShouldProcess($script:ConfigPath, "Update configuration")) {
            $config | ConvertTo-Json -Depth 10 | Set-Content $script:ConfigPath -Encoding UTF8
            Write-Log "Configuration updated successfully" -Level Success
            return $true
        }
    }
    catch {
        Write-Log "Failed to update configuration: $($_.Exception.Message)" -Level Error
        return $false
    }

    return $false
}

# Main execution
Write-Log "=== External System Initialization ===" -Level Info
Write-Log "Base URL: $BaseUrl" -Level Info

# Normalize URL
$BaseUrl = $BaseUrl.TrimEnd('/')

# Get API key
if (-not $ApiKey) {
    $ApiKey = Get-StoredApiKey
    if (-not $ApiKey) {
        Write-Log "No API key provided or found in storage" -Level Warning
        Write-Log "Set the $script:EnvVarName environment variable or use -ApiKey parameter" -Level Info
    }
}

# Validate connection if requested
if ($ValidateConnection) {
    $connected = Test-ExternalConnection -Url $BaseUrl -Key $ApiKey -TimeoutSeconds $Timeout
    if (-not $connected -and -not $Force) {
        Write-Log "Connection validation failed. Use -Force to continue anyway." -Level Error
        exit 1
    }
}

# Update configuration
$updated = Update-Configuration -Url $BaseUrl -Key $ApiKey

if ($updated) {
    Write-Log "External system initialization complete!" -Level Success
    Write-Log "Configuration file: $script:ConfigPath" -Level Info

    # Output summary
    Write-Host ""
    Write-Host "=== Configuration Summary ===" -ForegroundColor Cyan
    Write-Host "Base URL: $BaseUrl"
    Write-Host "API Key: $(if ($ApiKey) { '****' + $ApiKey.Substring([Math]::Max(0, $ApiKey.Length - 4)) } else { 'Not Set' })"
    Write-Host "Config Path: $script:ConfigPath"
    Write-Host ""
}
else {
    Write-Log "Initialization failed" -Level Error
    exit 1
}

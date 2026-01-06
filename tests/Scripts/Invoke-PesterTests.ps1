#Requires -Modules Pester

<#
.SYNOPSIS
    Executes Pester tests for the MyApp application.

.DESCRIPTION
    This script runs all Pester tests in the Scripts directory and generates
    test results in NUnit XML format for CI/CD pipeline consumption.

.PARAMETER TestPath
    Path to the directory containing test files. Defaults to the script's directory.

.PARAMETER OutputPath
    Path to output directory for test results. Defaults to test-results in the project root.

.PARAMETER Tag
    Optional tags to filter which tests to run.

.PARAMETER ExcludeTag
    Optional tags to exclude from the test run.

.PARAMETER PassThru
    If specified, returns the Pester result object for programmatic use.

.EXAMPLE
    .\Invoke-PesterTests.ps1

.EXAMPLE
    .\Invoke-PesterTests.ps1 -Tag "Integration" -OutputPath "C:\Results"

.NOTES
    Requires Pester 5.0 or later.
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$TestPath = $PSScriptRoot,

    [Parameter()]
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\..\test-results"),

    [Parameter()]
    [string[]]$Tag,

    [Parameter()]
    [string[]]$ExcludeTag,

    [Parameter()]
    [switch]$PassThru
)

# Ensure output directory exists
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Configure Pester
$config = New-PesterConfiguration

# Run settings
$config.Run.Path = $TestPath
$config.Run.Exit = (-not $PassThru)
$config.Run.PassThru = $PassThru

# Tag filtering
if ($Tag) {
    $config.Filter.Tag = $Tag
}
if ($ExcludeTag) {
    $config.Filter.ExcludeTag = $ExcludeTag
}

# Output settings
$config.Output.Verbosity = 'Detailed'
$config.Output.CIFormat = 'GithubActions'
$config.Output.StackTraceVerbosity = 'Full'

# Test result file
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = Join-Path $OutputPath "pester-results.xml"
$config.TestResult.OutputFormat = 'NUnitXml'

# Code coverage (optional - can be enabled)
$config.CodeCoverage.Enabled = $false
$config.CodeCoverage.Path = @(
    (Join-Path $TestPath "..\..\src\MyApp\*.ps1")
)
$config.CodeCoverage.OutputPath = Join-Path $OutputPath "coverage.xml"

Write-Host "=== Pester Test Configuration ===" -ForegroundColor Cyan
Write-Host "Test Path: $TestPath"
Write-Host "Output Path: $OutputPath"
Write-Host "Results File: $($config.TestResult.OutputPath.Value)"
Write-Host ""

# Run tests
$result = Invoke-Pester -Configuration $config

# Summary output
Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests: $($result.TotalCount)"
Write-Host "Passed: $($result.PassedCount)" -ForegroundColor Green
Write-Host "Failed: $($result.FailedCount)" -ForegroundColor $(if ($result.FailedCount -gt 0) { 'Red' } else { 'Green' })
Write-Host "Skipped: $($result.SkippedCount)" -ForegroundColor Yellow
Write-Host "Duration: $($result.Duration)"

if ($PassThru) {
    return $result
}

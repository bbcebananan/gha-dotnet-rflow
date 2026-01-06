#Requires -Modules Pester

<#
.SYNOPSIS
    Pester tests for MyApp API endpoints.

.DESCRIPTION
    These tests validate that the API endpoints are responding correctly.
    They are designed to run against a deployed instance of the application.

.NOTES
    Set the API_BASE_URL environment variable to specify the target server.
    Default: https://localhost:5001
#>

BeforeAll {
    # Configuration
    $script:BaseUrl = $env:API_BASE_URL ?? "https://localhost:5001"
    $script:SkipCertCheck = $env:SKIP_CERT_CHECK -eq 'true' -or $true
    $script:TestApiKey = $env:TEST_API_KEY ?? "demo-api-key-12345"
    $script:Timeout = 30

    # Helper function for web requests
    function Invoke-ApiRequest {
        param(
            [Parameter(Mandatory)]
            [string]$Endpoint,

            [string]$Method = 'GET',

            [hashtable]$Headers = @{},

            [object]$Body,

            [switch]$UseDefaultCredentials,

            [switch]$ExpectError
        )

        $uri = "$script:BaseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            TimeoutSec = $script:Timeout
            ErrorAction = if ($ExpectError) { 'SilentlyContinue' } else { 'Stop' }
        }

        if ($script:SkipCertCheck) {
            $params['SkipCertificateCheck'] = $true
        }

        if ($UseDefaultCredentials) {
            $params['UseDefaultCredentials'] = $true
        }

        if ($Headers.Count -gt 0) {
            $params['Headers'] = $Headers
        }

        if ($Body) {
            $params['Body'] = ($Body | ConvertTo-Json -Depth 10)
            $params['ContentType'] = 'application/json'
        }

        try {
            $response = Invoke-WebRequest @params
            return @{
                StatusCode = $response.StatusCode
                Content = $response.Content
                Headers = $response.Headers
                Success = $true
            }
        }
        catch {
            return @{
                StatusCode = $_.Exception.Response.StatusCode.value__
                Content = $_.ErrorDetails.Message
                Error = $_.Exception.Message
                Success = $false
            }
        }
    }
}

Describe "Health Check Endpoint" -Tag @('Health', 'Integration') {
    It "Should return 200 OK for /health" {
        $response = Invoke-ApiRequest -Endpoint "/health"

        $response.StatusCode | Should -Be 200
        $response.Content | Should -Match "Healthy"
    }
}

Describe "External API Endpoint (Anonymous)" -Tag @('External', 'Integration') {
    Context "GET /api/external - Health Status" {
        It "Should return health status without authentication" {
            $response = Invoke-ApiRequest -Endpoint "/api/external"

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.checkedAt | Should -Not -BeNullOrEmpty
        }
    }

    Context "POST /api/external - Process Request" {
        It "Should reject request without API key" {
            $body = @{
                systemId = "TEST-SYSTEM"
                requestType = "SYNC_DATA"
                payload = @{
                    entityType = "Test"
                    entityId = "123"
                    action = "sync"
                }
            }

            $response = Invoke-ApiRequest -Endpoint "/api/external" -Method POST -Body $body -ExpectError

            $response.StatusCode | Should -Be 401
        }

        It "Should accept request with valid API key" {
            $headers = @{ "X-Api-Key" = $script:TestApiKey }
            $body = @{
                systemId = "TEST-SYSTEM"
                requestType = "SYNC_DATA"
                payload = @{
                    entityType = "Test"
                    entityId = "123"
                    action = "sync"
                }
            }

            $response = Invoke-ApiRequest -Endpoint "/api/external" -Method POST -Headers $headers -Body $body

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
        }

        It "Should process QUERY request type" {
            $headers = @{ "X-Api-Key" = $script:TestApiKey }
            $body = @{
                systemId = "TEST-SYSTEM"
                requestType = "QUERY"
                payload = @{
                    entityType = "Test"
                    entityId = "456"
                    action = "query"
                }
            }

            $response = Invoke-ApiRequest -Endpoint "/api/external" -Method POST -Headers $headers -Body $body

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
            $content.data.action | Should -Be "query"
        }

        It "Should process NOTIFY request type" {
            $headers = @{ "X-Api-Key" = $script:TestApiKey }
            $body = @{
                systemId = "NOTIFICATION-SYSTEM"
                requestType = "NOTIFY"
                payload = @{
                    entityType = "Alert"
                    entityId = "789"
                    action = "alert"
                }
            }

            $response = Invoke-ApiRequest -Endpoint "/api/external" -Method POST -Headers $headers -Body $body

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
        }
    }

    Context "GET /api/external/validate - API Key Validation" {
        It "Should report invalid for missing API key" {
            $response = Invoke-ApiRequest -Endpoint "/api/external/validate"

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.isValid | Should -Be $false
        }

        It "Should report valid for correct API key" {
            $headers = @{ "X-Api-Key" = $script:TestApiKey }

            $response = Invoke-ApiRequest -Endpoint "/api/external/validate" -Headers $headers

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.isValid | Should -Be $true
        }

        It "Should report invalid for incorrect API key" {
            $headers = @{ "X-Api-Key" = "invalid-key-12345" }

            $response = Invoke-ApiRequest -Endpoint "/api/external/validate" -Headers $headers

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.isValid | Should -Be $false
        }
    }

    Context "POST /api/external/webhook - Webhook Endpoint" {
        It "Should process webhook with signature" {
            $headers = @{
                "X-Api-Key" = $script:TestApiKey
                "X-Signature" = "test-signature-hash"
            }
            $body = @{
                systemId = "WEBHOOK-SYSTEM"
                payload = @{
                    entityType = "Event"
                    entityId = "webhook-001"
                    action = "callback"
                }
            }

            $response = Invoke-ApiRequest -Endpoint "/api/external/webhook" -Method POST -Headers $headers -Body $body

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
        }
    }
}

Describe "Data API Endpoint (Windows Auth)" -Tag @('Data', 'Integration', 'WindowsAuth') {
    Context "GET /api/data" {
        It "Should reject unauthenticated requests" -Skip:($env:SKIP_AUTH_TESTS -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/data" -ExpectError

            $response.StatusCode | Should -BeIn @(401, 403)
        }

        It "Should return data for authenticated users" -Skip:($env:CI -eq 'true') {
            # This test requires Windows Authentication
            $response = Invoke-ApiRequest -Endpoint "/api/data" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
            $content.items | Should -Not -BeNullOrEmpty
        }
    }

    Context "GET /api/data/all" {
        It "Should support pagination" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/data/all?page=1&pageSize=5" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.pageNumber | Should -Be 1
            $content.pageSize | Should -Be 5
            $content.items.Count | Should -BeLessOrEqual 5
        }
    }

    Context "Legacy Endpoint" {
        It "Should respond to legacy path /legacy/api/data" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/legacy/api/data" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
        }
    }
}

Describe "Auth API Endpoint (Windows Auth)" -Tag @('Auth', 'Integration', 'WindowsAuth') {
    Context "GET /api/auth - User Profile" {
        It "Should reject unauthenticated requests" -Skip:($env:SKIP_AUTH_TESTS -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/auth" -ExpectError

            $response.StatusCode | Should -BeIn @(401, 403)
        }

        It "Should return user profile for authenticated users" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/auth" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.accountName | Should -Not -BeNullOrEmpty
            $content.isAuthenticated | Should -Be $true
        }
    }

    Context "POST /api/auth - Process Request" {
        It "Should process authenticated requests" -Skip:($env:CI -eq 'true') {
            $body = @{
                operation = "GET_STATUS"
            }

            $response = Invoke-ApiRequest -Endpoint "/api/auth" -Method POST -Body $body -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.success | Should -Be $true
            $content.resultCode | Should -Be "SUCCESS"
        }
    }
}

Describe "Scheduled Task API Endpoint (Windows Auth)" -Tag @('Scheduled', 'Integration', 'WindowsAuth') {
    Context "POST /api/scheduled/run" {
        It "Should reject unauthenticated requests" -Skip:($env:SKIP_AUTH_TESTS -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/scheduled/run" -Method POST -ExpectError

            $response.StatusCode | Should -BeIn @(401, 403)
        }

        It "Should execute task for authenticated users" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/scheduled/run" -Method POST -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.taskName | Should -Not -BeNullOrEmpty
            $content.durationMs | Should -BeGreaterThan 0
        }

        It "Should run specific task by name" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/scheduled/run?taskName=HealthCheck" -Method POST -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content.taskName | Should -Be "HealthCheck"
        }
    }

    Context "GET /api/scheduled/status" {
        It "Should return task statuses" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/scheduled/status" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content | Should -BeOfType [array]
            $content.Count | Should -BeGreaterThan 0
        }
    }

    Context "GET /api/scheduled/tasks" {
        It "Should list available tasks" -Skip:($env:CI -eq 'true') {
            $response = Invoke-ApiRequest -Endpoint "/api/scheduled/tasks" -UseDefaultCredentials

            $response.StatusCode | Should -Be 200
            $content = $response.Content | ConvertFrom-Json
            $content | Should -BeOfType [array]
            $content.name | Should -Contain "DataSync"
            $content.name | Should -Contain "Cleanup"
        }
    }
}

Describe "Swagger/OpenAPI Endpoint" -Tag @('Swagger', 'Integration') {
    It "Should serve Swagger JSON" -Skip:($env:ASPNETCORE_ENVIRONMENT -ne 'Development') {
        $response = Invoke-ApiRequest -Endpoint "/swagger/v1/swagger.json"

        $response.StatusCode | Should -Be 200
        $content = $response.Content | ConvertFrom-Json
        $content.info.title | Should -Be "MyApp API"
    }
}

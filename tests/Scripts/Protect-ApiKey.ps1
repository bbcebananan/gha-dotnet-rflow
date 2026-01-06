#Requires -Version 5.1

<#
.SYNOPSIS
    Encrypts a value using DPAPI for secure storage in configuration files.

.DESCRIPTION
    This script uses the Windows Data Protection API (DPAPI) to encrypt
    sensitive values like API keys. The encrypted value can only be decrypted
    by the same user account on the same machine.

    This is useful for storing sensitive configuration values in appsettings.json
    when running as an IIS Application Pool identity.

.PARAMETER Value
    The plaintext value to encrypt.

.PARAMETER SecureString
    A SecureString containing the value to encrypt. Use this for interactive input.

.PARAMETER Scope
    The DPAPI protection scope.
    - CurrentUser (default): Only the current user can decrypt
    - LocalMachine: Any user on this machine can decrypt

.PARAMETER OutputFormat
    The output format for the encrypted value.
    - Base64 (default): Standard Base64 encoding
    - Hex: Hexadecimal string

.PARAMETER CopyToClipboard
    If specified, copies the encrypted value to the clipboard.

.EXAMPLE
    .\Protect-ApiKey.ps1 -Value "MySecretApiKey"
    Encrypts the API key and outputs the Base64-encoded result.

.EXAMPLE
    .\Protect-ApiKey.ps1 -SecureString (Read-Host -AsSecureString -Prompt "Enter API Key")
    Prompts for the API key securely and encrypts it.

.EXAMPLE
    .\Protect-ApiKey.ps1 -Value "MySecret" -Scope LocalMachine -CopyToClipboard
    Encrypts using LocalMachine scope and copies to clipboard.

.NOTES
    IMPORTANT: This script must be run under the same user account that will
    run the application (typically the IIS Application Pool identity).

    For IIS deployment:
    1. Open cmd as the Application Pool identity using PsExec or similar
    2. Run this script to generate the encrypted value
    3. Place the encrypted value in appsettings.json

    The application code should use DPAPI to decrypt the value at runtime.
#>

[CmdletBinding(DefaultParameterSetName = 'PlainText')]
param(
    [Parameter(Mandatory = $true, ParameterSetName = 'PlainText', Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Value,

    [Parameter(Mandatory = $true, ParameterSetName = 'Secure')]
    [SecureString]$SecureString,

    [Parameter()]
    [ValidateSet('CurrentUser', 'LocalMachine')]
    [string]$Scope = 'CurrentUser',

    [Parameter()]
    [ValidateSet('Base64', 'Hex')]
    [string]$OutputFormat = 'Base64',

    [Parameter()]
    [switch]$CopyToClipboard
)

# Load the required assembly
Add-Type -AssemblyName System.Security

function Convert-SecureStringToPlainText {
    param([SecureString]$SecureString)

    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Protect-Value {
    param(
        [string]$PlainText,
        [System.Security.Cryptography.DataProtectionScope]$ProtectionScope
    )

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($PlainText)
    $encryptedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
        $bytes,
        $null,  # Additional entropy (optional)
        $ProtectionScope
    )

    return $encryptedBytes
}

function Unprotect-Value {
    param(
        [byte[]]$EncryptedBytes,
        [System.Security.Cryptography.DataProtectionScope]$ProtectionScope
    )

    $decryptedBytes = [System.Security.Cryptography.ProtectedData]::Unprotect(
        $EncryptedBytes,
        $null,
        $ProtectionScope
    )

    return [System.Text.Encoding]::UTF8.GetString($decryptedBytes)
}

# Get the plaintext value
if ($PSCmdlet.ParameterSetName -eq 'Secure') {
    $plainText = Convert-SecureStringToPlainText -SecureString $SecureString
}
else {
    $plainText = $Value
}

# Determine protection scope
$protectionScope = switch ($Scope) {
    'CurrentUser' { [System.Security.Cryptography.DataProtectionScope]::CurrentUser }
    'LocalMachine' { [System.Security.Cryptography.DataProtectionScope]::LocalMachine }
}

Write-Host "=== DPAPI Encryption ===" -ForegroundColor Cyan
Write-Host "Scope: $Scope"
Write-Host "Output Format: $OutputFormat"
Write-Host "Running as: $([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)"
Write-Host ""

try {
    # Encrypt the value
    $encryptedBytes = Protect-Value -PlainText $plainText -ProtectionScope $protectionScope

    # Format the output
    $encryptedOutput = switch ($OutputFormat) {
        'Base64' { [Convert]::ToBase64String($encryptedBytes) }
        'Hex' { [BitConverter]::ToString($encryptedBytes) -replace '-', '' }
    }

    # Verify by decrypting
    $decrypted = Unprotect-Value -EncryptedBytes $encryptedBytes -ProtectionScope $protectionScope
    if ($decrypted -ne $plainText) {
        Write-Host "WARNING: Verification failed - decrypted value doesn't match!" -ForegroundColor Red
        exit 1
    }

    Write-Host "Encryption successful! Verification passed." -ForegroundColor Green
    Write-Host ""
    Write-Host "=== Encrypted Value ===" -ForegroundColor Yellow
    Write-Host $encryptedOutput
    Write-Host ""

    # Show how to use in appsettings.json
    Write-Host "=== Usage in appsettings.json ===" -ForegroundColor Cyan
    Write-Host @"
{
  "AppConfig": {
    "ExternalApiKey": "$encryptedOutput"
  }
}
"@
    Write-Host ""

    # Show decryption code
    Write-Host "=== C# Decryption Code ===" -ForegroundColor Cyan
    Write-Host @"
using System.Security.Cryptography;

public static string DecryptValue(string encryptedBase64)
{
    var encryptedBytes = Convert.FromBase64String(encryptedBase64);
    var decryptedBytes = ProtectedData.Unprotect(
        encryptedBytes,
        null,
        DataProtectionScope.$Scope);
    return Encoding.UTF8.GetString(decryptedBytes);
}
"@
    Write-Host ""

    # Copy to clipboard if requested
    if ($CopyToClipboard) {
        $encryptedOutput | Set-Clipboard
        Write-Host "Encrypted value copied to clipboard!" -ForegroundColor Green
    }

    # Return the encrypted value
    return $encryptedOutput
}
catch {
    Write-Host "Encryption failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    # Clear sensitive data from memory
    $plainText = $null
    [System.GC]::Collect()
}

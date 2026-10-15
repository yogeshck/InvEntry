<#
.SYNOPSIS
    Generates and stores the secrets used by DataAccess and FinanceTracker.

.DESCRIPTION
    Machine-level environment variables (read by every process on this PC):
      Jwt__Key                     DataAccess signing key
      InvEntryManagement__ApiKey   shared key: FinanceTracker sends it to DataAccess

    FinanceTracker needs its OWN signing key, different from DataAccess's, so a
    token from one service cannot be presented to the other. The script generates
    it and prints it once. Set it in FinanceTracker's service environment (for
    example NSSM AppEnvironmentExtra: Jwt__Key=<value>), not machine-wide.

    Existing machine values are kept unless -Force is given, so re-running is safe.
    Nothing is written to disk. Run from an elevated PowerShell, then restart the services.

.PARAMETER Force
    Replace existing machine values. Invalidates tokens already issued by DataAccess
    and breaks FinanceTracker until its service value is updated to match the shared key.

.PARAMETER FinanceTrackerKey
    Generates a new FinanceTracker signing key and prints it. Use once per install.

.EXAMPLE
    .\Install-FinanceSecrets.ps1 -WhatIf -FinanceTrackerKey   # preview, changes nothing
    .\Install-FinanceSecrets.ps1 -FinanceTrackerKey           # create missing values
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [switch]$Force,
    [switch]$FinanceTrackerKey
)

function Test-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    return (New-Object Security.Principal.WindowsPrincipal $identity).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}

function New-Secret {
    # 48 random bytes -> 64 Base64 characters, above the 32-byte HMAC minimum.
    $bytes = New-Object byte[] 48
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [Convert]::ToBase64String($bytes)
}

if (-not $WhatIfPreference -and -not (Test-Administrator)) {
    throw 'Run this script from an elevated (Administrator) PowerShell.'
}

$machineVariables = [ordered]@{
    'Jwt__Key'                   = 'DataAccess signing key'
    'InvEntryManagement__ApiKey' = 'shared FinanceTracker -> DataAccess key'
}

$summary = @()
$shown = [ordered]@{}

foreach ($name in $machineVariables.Keys) {
    $current = [Environment]::GetEnvironmentVariable($name, 'Machine')
    if ($current -and -not $Force) {
        $summary += [pscustomobject]@{ Name = $name; Action = 'kept (already set)' }
        continue
    }
    $action = if ($current) { 'replaced (-Force)' } else { 'created' }
    if ($PSCmdlet.ShouldProcess("Machine:$name", "Set ($($machineVariables[$name]))")) {
        $value = New-Secret
        [Environment]::SetEnvironmentVariable($name, $value, 'Machine')
        $shown[$name] = $value
        $summary += [pscustomobject]@{ Name = $name; Action = $action }
    } else {
        $summary += [pscustomobject]@{ Name = $name; Action = "would be $($action -replace ' \(-Force\)', '')" }
    }
}

if ($FinanceTrackerKey) {
    if ($PSCmdlet.ShouldProcess('FinanceTracker', 'Generate signing key (printed once, not stored)')) {
        $shown['FinanceTracker Jwt__Key (service environment only)'] = New-Secret
        $summary += [pscustomobject]@{ Name = 'FinanceTracker Jwt__Key'; Action = 'generated, printed below' }
    } else {
        $summary += [pscustomobject]@{ Name = 'FinanceTracker Jwt__Key'; Action = 'would be generated (WhatIf)' }
    }
}

$summary | Format-Table -AutoSize

if ($shown.Count -gt 0) {
    Write-Host ''
    Write-Host 'Values generated in this run (shown once; store them in your password manager):' -ForegroundColor Yellow
    foreach ($entry in $shown.GetEnumerator()) {
        Write-Host ("  {0} = {1}" -f $entry.Key, $entry.Value)
    }
    Write-Host ''
    Write-Host 'Restart the DataAccess and FinanceTracker services so they read the new values.' -ForegroundColor Cyan
}

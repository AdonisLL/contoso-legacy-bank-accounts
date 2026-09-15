[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$hostExe = Join-Path $root "src\Contoso.LegacyBank.Accounts.ServiceHost\bin\$Configuration\Contoso.LegacyBank.Accounts.ServiceHost.exe"
if (-not (Test-Path $hostExe)) { throw "Build the solution first. Missing: $hostExe" }
& $hostExe --setup
if ($LASTEXITCODE -ne 0) { throw "Database setup failed with exit code $LASTEXITCODE." }

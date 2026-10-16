<#
.SYNOPSIS
    Build brutal-ui on any operating system with PowerShell.

.DESCRIPTION
    Purpose:
      Run dotnet build brutal-ui.sln and show its output on the console. Use
      this script for a quick build. Use scripts\test-windows.ps1 for the full
      Windows test run.

    Preflight checks (in this order):
      1. dotnet exists on the path
      2. dotnet --version starts with 10.

    Output:
      Success: the output of dotnet build, and nothing else.
      Build failure: the output of dotnet build, then this line on standard
      error:
      build: FAIL at build (exit <rc>)
      Check failure: two lines on standard error:
      build: FAIL at preflight: <problem>
      build: fix: <command>

    Exit codes:
      0      the build passed
      1      a preflight check failed, or the script stopped on an error
      other  the exit code of dotnet build

.EXAMPLE
    .\scripts\build.ps1
#>

#Requires -Version 5.1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

# Write-CheckFail <problem> <fix>
# Write the two lines of a failed check to standard error. Exit with code 1.
# The exit ends the script. The finally block of the script still runs.
function Write-CheckFail {
    param(
        [Parameter(Mandatory = $true)] [string] $Problem,
        [Parameter(Mandatory = $true)] [string] $Fix
    )
    [Console]::Error.WriteLine("build: FAIL at preflight: $Problem")
    [Console]::Error.WriteLine("build: fix: $Fix")
    exit 1
}

# Set-Location in a script changes the folder of the caller session. So the
# script uses Push-Location and returns to the old folder at the end.
$savedNoLogo = $env:DOTNET_NOLOGO
Push-Location -LiteralPath $root
try {
    # dotnet writes no welcome text.
    $env:DOTNET_NOLOGO = '1'

    $dotnetFix = 'winget install Microsoft.DotNet.SDK.10'

    # Check 1: dotnet exists.
    if (-not (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue)) {
        Write-CheckFail 'dotnet does not exist' $dotnetFix
    }

    # Check 2: the dotnet version is 10. A failed dotnet --version call must
    # not stop the script, so the error preference is Continue for this call
    # only. The 2>$null is the only PowerShell redirect of a native command.
    $ErrorActionPreference = 'Continue'
    try {
        $versionOutput = & dotnet --version 2>$null
    }
    finally {
        $ErrorActionPreference = 'Stop'
    }
    $dotnetVersion = (@($versionOutput) -join ' ').Trim()
    if ([string]::IsNullOrEmpty($dotnetVersion)) {
        $dotnetVersion = 'unknown'
    }
    if (-not $dotnetVersion.StartsWith('10.', [System.StringComparison]::Ordinal)) {
        Write-CheckFail "dotnet version $dotnetVersion is not version 10" $dotnetFix
    }

    # The build writes to the console. The script does not redirect it.
    & dotnet build brutal-ui.sln
    $rc = $LASTEXITCODE
    if ($rc -ne 0) {
        [Console]::Error.WriteLine("build: FAIL at build (exit $rc)")
        exit $rc
    }
}
finally {
    # A null value removes the variable, so an unset variable stays unset.
    [Environment]::SetEnvironmentVariable('DOTNET_NOLOGO', $savedNoLogo)
    Pop-Location
}

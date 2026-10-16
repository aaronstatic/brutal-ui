<#
.SYNOPSIS
    Build, test, and smoke-run brutal-ui on Windows.

.DESCRIPTION
    Purpose:
      Prove the full path on a Windows machine. The script runs the build and
      the unit tests. Then it runs the HelloPanel sample on the GPU in a
      visible window for a number of frames.

    Steps (the first failure stops the run):
      1. preflight - check the tools, the two variables, and the operating
                     system, then reset the log folder
      2. build     - dotnet build brutal-ui.sln
      3. test      - dotnet test --solution brutal-ui.sln
      4. loader    - check that vulkan-1.dll exists in the system folder
      5. smoke     - run HelloPanel for a number of frames, with a time limit

    Preflight checks (in this order):
      1. dotnet exists on the path
      2. dotnet --version starts with 10.
      3. BRUTAL_UI_SMOKE_TIMEOUT is a whole number above 0
      4. BRUTAL_UI_FRAMES holds only letters, digits, plus signs, and minus
         signs
      5. the operating system is Windows
      6. the script can delete the folder artifacts\test-windows
    A failed check or a failed loader step writes two lines to standard error:
    test-windows: FAIL at <step>: <problem>
    test-windows: fix: <command>

    Output:
      Success: one line on standard output, and nothing on standard error.
      Failure: the failure block on standard error. Each step writes its full
      log to artifacts\test-windows\<step>.log.
      Interrupt: Ctrl+C during the build, test, or smoke step stops the step and its child processes.
      The script then writes this line to standard error:
      test-windows: FAIL at <step>: interrupted
      The exit code is 130.

    Exit codes:
      0      all steps passed
      1      a preflight check or the loader step failed, HelloPanel failed
             at run time, or the script stopped on an error
      2      HelloPanel reported a usage error
      124    the smoke step timed out
      130    Ctrl+C stopped a step
      other  the exit code of dotnet build or dotnet test

    Environment variables:
      BRUTAL_UI_FRAMES          number of frames for the smoke run (default 120)
      BRUTAL_UI_SMOKE_TIMEOUT   time limit of the smoke step in seconds
                                (default 60)
    An empty variable counts as unset.

.EXAMPLE
    .\scripts\test-windows.ps1
#>

#Requires -Version 5.1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$logDir = 'artifacts\test-windows'

# Write-CheckFail <step> <problem> <fix>
# Write the two lines of a failed check to standard error. Exit with code 1.
# The exit ends the script. The finally block of the script still runs.
function Write-CheckFail {
    param(
        [Parameter(Mandatory = $true)] [string] $Step,
        [Parameter(Mandatory = $true)] [string] $Problem,
        [Parameter(Mandatory = $true)] [string] $Fix
    )
    [Console]::Error.WriteLine("test-windows: FAIL at ${Step}: $Problem")
    [Console]::Error.WriteLine("test-windows: fix: $Fix")
    exit 1
}

# Stop-StepTree <process>
# Stop the process and each child process of it. .NET Framework has no
# Process.Kill(true), so taskkill.exe stops the tree. cmd.exe sends the
# output of taskkill.exe to nul, so no text reaches the console. The function
# runs after Ctrl+C in a stopped pipeline. A cmdlet cannot write to the output
# stream of a stopped pipeline, so a .NET call starts cmd.exe, not
# Start-Process. Each other statement here is a .NET call or an assignment.
function Stop-StepTree {
    param(
        [Parameter(Mandatory = $true)] [System.Diagnostics.Process] $Process
    )
    $killInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $killInfo.FileName = $env:ComSpec
    $killInfo.Arguments = "/d /s /c `"taskkill.exe /PID $($Process.Id) /T /F >nul 2>&1`""
    $killInfo.WorkingDirectory = $root
    $killInfo.UseShellExecute = $false
    $killInfo.CreateNoWindow = $true
    $kill = [System.Diagnostics.Process]::Start($killInfo)
    $null = $kill.WaitForExit(30000)
    $kill.Dispose()
    $null = $Process.WaitForExit(10000)
}

# Invoke-Step <name> <command> [<timeout seconds>]
# Run the command in cmd.exe. cmd.exe sends the output to
# artifacts\test-windows\<name>.log. PowerShell never redirects the output of
# a native command, because Windows PowerShell 5.1 turns each standard error
# line into an error record and writes UTF-16 files. On a non-zero exit code,
# write the failure block to standard error and exit with the same code. A
# timeout above 0 sets a time limit. On a time-out, stop the full process tree
# and exit with code 124. After Ctrl+C, write the interrupted line and set the
# exit code 130.
function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)] [string] $Name,
        [Parameter(Mandatory = $true)] [string] $Command,
        [int] $TimeoutSeconds = 0
    )
    $log = "$logDir\$Name.log"
    # The /s option removes only the outer quotes. The command stays as written.
    $p = Start-Process -FilePath $env:ComSpec `
        -ArgumentList "/d /s /c `"$Command > $log 2>&1`"" `
        -WorkingDirectory $root -NoNewWindow -PassThru
    # Read the handle now. Without it, ExitCode can stay empty after the exit.
    # The open handle also keeps Windows from a new use of the process ID.
    $null = $p.Handle
    $timedOut = $false
    $waitDone = $false
    $stepClock = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        # The short wait lets PowerShell react to Ctrl+C between two waits.
        while (-not $p.WaitForExit(200)) {
            if ($TimeoutSeconds -gt 0 -and $stepClock.Elapsed.TotalSeconds -ge $TimeoutSeconds) {
                Stop-StepTree -Process $p
                $timedOut = $true
                break
            }
        }
        $waitDone = $true
    }
    finally {
        # The wait ended early only when Ctrl+C or an error stopped the loop.
        # The script does not catch the stop. PowerShell ends the script after
        # this block. The step process must not stay alive. Ctrl+C can end the
        # step process before this block runs, so the interrupted line does not
        # depend on the state of the process. The exit code and the line come
        # first, so an error in Stop-StepTree cannot hide them. The finally
        # block of the script sets the exit code from $script:stopCode.
        if (-not $waitDone) {
            $script:stopCode = 130
            [Console]::Error.WriteLine("test-windows: FAIL at ${Name}: interrupted")
            if (-not $p.HasExited) {
                Stop-StepTree -Process $p
            }
        }
    }
    # Ctrl+C reaches the step processes too, because they share the console.
    # When they end inside a wait, the loop ends normally. The code
    # -1073741510 (STATUS_CONTROL_C_EXIT, 0xC000013A) then tells of the
    # interrupt.
    if (-not $timedOut -and $p.ExitCode -eq -1073741510) {
        $script:stopCode = 130
        [Console]::Error.WriteLine("test-windows: FAIL at ${Name}: interrupted")
        exit 130
    }
    if ($timedOut) {
        $rc = 124
    }
    else {
        $rc = $p.ExitCode
    }
    if ($rc -ne 0) {
        $header = "test-windows: FAIL at $Name (exit $rc)"
        if ($timedOut) {
            $header = "$header, timed out after $TimeoutSeconds s"
        }
        [Console]::Error.WriteLine($header)
        if (Test-Path -LiteralPath $log) {
            foreach ($line in @(Get-Content -LiteralPath $log -Tail 30)) {
                [Console]::Error.WriteLine($line)
            }
        }
        [Console]::Error.WriteLine("test-windows: full log: $log")
        exit $rc
    }
}

# Set-Location in a script changes the folder of the caller session. So the
# script uses Push-Location and returns to the old folder at the end.
$savedNoLogo = $env:DOTNET_NOLOGO
$savedNodeReuse = $env:MSBUILDDISABLENODEREUSE
Push-Location -LiteralPath $root
# Ctrl+C or an unexpected error ends the script with no exit statement. Then
# the finally block below sets the exit code from these two variables.
# Invoke-Step sets $stopCode to 130 after Ctrl+C. An exit statement sets the
# exit code again after the finally block, so each exit path keeps its code.
$scriptDone = $false
$stopCode = 1
try {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()

    # dotnet writes no welcome text. MSBuild stops its worker processes at the
    # end of the build. A worker process that stays alive keeps a log file
    # open, and the next run then cannot delete the log folder.
    $env:DOTNET_NOLOGO = '1'
    $env:MSBUILDDISABLENODEREUSE = '1'

    $frames = $env:BRUTAL_UI_FRAMES
    if ([string]::IsNullOrEmpty($frames)) {
        $frames = '120'
    }
    $smokeTimeoutText = $env:BRUTAL_UI_SMOKE_TIMEOUT
    if ([string]::IsNullOrEmpty($smokeTimeoutText)) {
        $smokeTimeoutText = '60'
    }

    # Step preflight. The checks run in this order, so a Linux run of the
    # script can test the first four. The first failed check ends the script.
    $dotnetFix = 'winget install Microsoft.DotNet.SDK.10'

    # Check 1: dotnet exists.
    if (-not (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue)) {
        Write-CheckFail preflight 'dotnet does not exist' $dotnetFix
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
        Write-CheckFail preflight "dotnet version $dotnetVersion is not version 10" $dotnetFix
    }

    # Check 3: the time limit is a whole number above 0.
    $smokeTimeout = 0
    if ($smokeTimeoutText -cnotmatch '^[1-9][0-9]*\z' -or -not [int]::TryParse($smokeTimeoutText, [ref]$smokeTimeout)) {
        Write-CheckFail preflight "BRUTAL_UI_SMOKE_TIMEOUT $smokeTimeoutText is not a whole number above 0" 'Set BRUTAL_UI_SMOKE_TIMEOUT to a number of seconds, for example 60.'
    }

    # Check 4: the frames value holds no special character of cmd.exe. The
    # match is case-sensitive, so the Kelvin sign is not a letter. The \z anchor
    # rejects a line feed at the end of the value.
    if ($frames -cnotmatch '^[0-9A-Za-z+-]+\z') {
        Write-CheckFail preflight "BRUTAL_UI_FRAMES $frames holds a character that is not a letter, a digit, a plus sign, or a minus sign" 'Set BRUTAL_UI_FRAMES to a whole number above 0, for example 120.'
    }

    # Check 5: the script runs on Windows only.
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        Write-CheckFail preflight 'this script runs on Windows only' 'Run scripts/test-linux.sh on Linux.'
    }

    # Check 6: the log folder resets. A HelloPanel window or an MSBuild worker
    # process can keep a log file open.
    try {
        if (Test-Path -LiteralPath $logDir) {
            Remove-Item -LiteralPath $logDir -Recurse -Force -ErrorAction Stop
        }
    }
    catch {
        Write-CheckFail preflight 'artifacts\test-windows is in use' 'Close each HelloPanel window. Run dotnet build-server shutdown. Run the script again.'
    }
    $null = New-Item -ItemType Directory -Path $logDir

    Invoke-Step build 'dotnet build brutal-ui.sln'

    Invoke-Step test 'dotnet test --solution brutal-ui.sln'

    # Step loader. Brutal.Vulkan loads the Vulkan loader as vulkan-1 on
    # Windows. The GPU driver installs vulkan-1.dll in the system folder. See
    # docs/features/dev-ops/solution-skeleton/context.md, section "Native
    # Dependencies", and the result of task 1.8 in
    # docs/features/dev-ops/windows-test-path/context.md. The smoke run is the
    # final check of the name.
    $loaderFolder = [Environment]::SystemDirectory
    if (-not (Test-Path -LiteralPath (Join-Path $loaderFolder 'vulkan-1.dll'))) {
        Write-CheckFail loader "the Vulkan loader vulkan-1.dll does not exist in $loaderFolder" 'Install or update the GPU driver. The driver installs vulkan-1.dll.'
    }

    # The smoke run reads the exit code of HelloPanel, not its output. The
    # quotes keep a special character of cmd.exe in the value literal.
    Invoke-Step smoke "dotnet run --project samples/HelloPanel/HelloPanel.csproj --no-build -- --frames `"$frames`"" $smokeTimeout

    [Console]::Out.WriteLine("test-windows: PASS build, test, smoke ($frames frames) in $([math]::Floor($sw.Elapsed.TotalSeconds)) s")
    $scriptDone = $true
}
finally {
    # Ctrl+C can stop the pipeline before this block, so this line uses no
    # cmdlet. The script does not read $global:LASTEXITCODE, because a new
    # session does not have it, and Set-StrictMode then stops the script.
    if (-not $scriptDone) {
        $global:LASTEXITCODE = $stopCode
    }
    # A null value removes the variable, so an unset variable stays unset.
    [Environment]::SetEnvironmentVariable('DOTNET_NOLOGO', $savedNoLogo)
    [Environment]::SetEnvironmentVariable('MSBUILDDISABLENODEREUSE', $savedNodeReuse)
    Pop-Location
}

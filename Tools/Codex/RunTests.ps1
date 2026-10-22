[CmdletBinding()]
param(
    [string]$UnityPath,
    [ValidateSet('EditMode', 'PlayMode')][string]$TestMode = 'EditMode',
    [string]$Filter = 'CodexHarness.Tests',
    [int]$TimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'HarnessCommon.ps1')
if (-not $UnityPath) { $UnityPath = $env:UNITY_EXE }
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    Write-HarnessResult 'Tests' 'NOT VERIFIED' 'Supply a matching -UnityPath or UNITY_EXE.'
    exit 2
}
if (Test-HarnessEditorOpen) {
    Write-HarnessResult 'Tests' 'NOT VERIFIED' 'Project is open; run tests in the connected Editor via MCP/Test Runner.'
    exit 2
}
$root = Get-HarnessRoot
$temp = Join-Path $root 'Temp'
$null = New-Item -Path $temp -ItemType Directory -Force
$id = [Guid]::NewGuid().ToString('N')
$results = Join-Path $temp ("CodexHarness-$TestMode-$id.xml")
$log = Join-Path $temp ("CodexHarness-$TestMode-$id.log")
try {
    $arguments = @('-batchmode', '-nographics', '-projectPath', $root, '-runTests',
        '-testPlatform', $TestMode, '-testResults', $results, '-logFile', $log)
    if ($Filter) { $arguments += @('-testFilter', $Filter) }
    # No -quit: allow the Unity Test Runner to complete and exit.
    $code = Invoke-HarnessUnity $UnityPath $arguments $TimeoutSeconds
    if (-not (Test-Path -LiteralPath $results -PathType Leaf)) {
        Write-HarnessResult 'Tests' 'NOT VERIFIED' "No fresh NUnit results. Unity exit code $code. Log: $log"
        if ($code -ne 0) { exit 1 }
        exit 2
    }
    $summary = Get-HarnessTestResult ([IO.File]::ReadAllText($results))
    if ($code -ne 0) {
        Write-HarnessResult 'Tests' 'FAIL' "Unity exit code $code. Results: $results; log: $log"
        exit 1
    }
    Write-HarnessResult 'Tests' $summary.Status "$TestMode $($summary.Passed)/$($summary.Total) passed, $($summary.Failed) failed. Results: $results"
    exit $summary.ExitCode
} catch {
    Write-HarnessResult 'Tests' 'FAIL' $_.Exception.Message
    exit 1
}

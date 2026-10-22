[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'HarnessCommon.ps1')
$root = Get-HarnessRoot
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-HarnessResult 'GitDiff' 'NOT VERIFIED' 'Git is not available.'
    exit 2
}
$null = & git -C $root rev-parse --is-inside-work-tree 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-HarnessResult 'GitDiff' 'NOT VERIFIED' 'Project is not an accessible Git repository.'
    exit 2
}
& git -C $root status --short
if ($LASTEXITCODE -ne 0) { exit 1 }
& git -C $root diff --check
if ($LASTEXITCODE -ne 0) { Write-HarnessResult 'GitDiff' 'FAIL' 'Unstaged whitespace/conflict-marker check failed.'; exit 1 }
& git -C $root diff --cached --check
if ($LASTEXITCODE -ne 0) { Write-HarnessResult 'GitDiff' 'FAIL' 'Staged whitespace/conflict-marker check failed.'; exit 1 }
Write-HarnessResult 'GitDiff' 'PASS' 'Status and staged/unstaged whitespace checks completed; this is not a semantic code review.'
exit 0

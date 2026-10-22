[CmdletBinding()]
param(
    [string]$UnityPath,
    [switch]$Full,
    [int]$TimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'HarnessCommon.ps1')
$failures = [Collections.Generic.List[string]]::new()
$root = Get-HarnessRoot
try {
    $config = Get-HarnessConfig
    foreach ($relative in (Get-HarnessRequiredFiles $config)) {
        if (-not (Test-Path -LiteralPath (Resolve-HarnessPath $relative) -PathType Leaf)) {
            $failures.Add("Required file is missing: $relative")
        }
    }
    $versionPath = Resolve-HarnessPath 'ProjectSettings/ProjectVersion.txt'
    $version = Get-Content -Raw -LiteralPath $versionPath
    $expected = [regex]::Escape($config.expectedUnityVersion)
    if ($version -notmatch ("(?m)^m_EditorVersion: " + $expected + "\r?$")) {
        $failures.Add('Unity version differs from HarnessConfig.json.')
    }
    $null = Get-Content -Raw -LiteralPath (Resolve-HarnessPath 'Packages/manifest.json') | ConvertFrom-Json
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($source in $config.sourceRoots) {
        $path = Resolve-HarnessPath $source
        if (-not (Test-Path -LiteralPath $path -PathType Container)) {
            $failures.Add("Source root is missing: $source")
            continue
        }
        $files = @(Get-ChildItem -LiteralPath $path -Recurse -File -Filter '*.cs')
        if ($files.Count -eq 0) { $failures.Add("Source root contains no C# scripts: $source") }
        foreach ($file in $files) {
            if (-not $seen.Add($file.FullName)) { continue }
            $lines = [IO.File]::ReadAllLines($file.FullName)
            if ($lines.Length -gt $config.maxScriptLines) {
                $failures.Add("Script exceeds $($config.maxScriptLines) lines ($($lines.Length)): $($file.FullName)")
            }
            for ($i = 0; $i -lt $lines.Length; $i++) {
                if ($lines[$i] -match '^\s*(?:global\s+)?using\s+(?:(?:static\s+|[A-Za-z_]\w*\s*=\s*))?System\.Linq\b') {
                    $failures.Add("Use an explicit loop or installed ZLinq instead of System.Linq: $($file.FullName):$($i + 1)")
                    break
                }
            }
        }
    }
} catch {
    $failures.Add($_.Exception.Message)
}
foreach ($failure in $failures) { Write-HarnessResult 'Static' 'FAIL' $failure }
$exitCode = 0
if ($failures.Count -gt 0) { $exitCode = 1 }
else { Write-HarnessResult 'Static' 'PASS' 'Metadata, harness files, source roots and architecture text rules passed.' }

if (-not $Full) {
    Write-HarnessResult 'Editor' 'NOT VERIFIED' 'Fast mode is source-only. Use the connected Editor/MCP for compile, scenes and tests.'
    exit $exitCode
}
if (-not $UnityPath) { $UnityPath = $env:UNITY_EXE }
if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    Write-HarnessResult 'Full' 'NOT VERIFIED' 'Supply -UnityPath or UNITY_EXE pointing to the matching Unity Editor.'
    if ($exitCode -eq 0) { $exitCode = 2 }
    exit $exitCode
}
if (Test-HarnessEditorOpen) {
    Write-HarnessResult 'Full' 'NOT VERIFIED' 'This project is open in Unity. Use Codex Harness/Validate All and MCP tests; no second Editor is launched.'
    if ($exitCode -eq 0) { $exitCode = 2 }
    exit $exitCode
}
$temp = Join-Path $root 'Temp'
$null = New-Item -Path $temp -ItemType Directory -Force
$log = Join-Path $temp ('CodexHarness-Validate-' + [Guid]::NewGuid().ToString('N') + '.log')
try {
    $code = Invoke-HarnessUnity $UnityPath @('-batchmode', '-nographics', '-projectPath', $root,
        '-executeMethod', 'CodexHarness.CodexHarnessValidation.ValidateAndExit', '-logFile', $log) $TimeoutSeconds
    if ($code -eq 0) { Write-HarnessResult 'EditorValidation' 'PASS' "Project, architecture and enabled build scene checks passed. Log: $log" }
    elseif ($code -eq 2) {
        Write-HarnessResult 'EditorValidation' 'NOT VERIFIED' "No enabled build scenes. Log: $log"
        if ($exitCode -eq 0) { $exitCode = 2 }
    } else { Write-HarnessResult 'EditorValidation' 'FAIL' "Unity exit code $code. Log: $log"; $exitCode = 1 }
    foreach ($mode in @('EditMode', 'PlayMode')) {
        & (Join-Path $PSScriptRoot 'RunTests.ps1') -UnityPath $UnityPath -TestMode $mode -TimeoutSeconds $TimeoutSeconds
        $testCode = $LASTEXITCODE
        if ($testCode -eq 1) { $exitCode = 1 }
        elseif ($testCode -ne 0 -and $exitCode -eq 0) { $exitCode = 2 }
    }
} catch {
    Write-HarnessResult 'Full' 'FAIL' $_.Exception.Message
    $exitCode = 1
}
exit $exitCode

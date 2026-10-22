Set-StrictMode -Version Latest

function Get-HarnessRoot {
    return (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
}

function Resolve-HarnessPath([string]$RelativePath) {
    if ([string]::IsNullOrWhiteSpace($RelativePath) -or [IO.Path]::IsPathRooted($RelativePath)) {
        throw "Expected a project-relative path: $RelativePath"
    }
    $root = (Get-HarnessRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $path = [IO.Path]::GetFullPath((Join-Path $root $RelativePath))
    if (-not $path.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes the project: $RelativePath"
    }
    return $path
}

function Get-HarnessConfig {
    $path = Resolve-HarnessPath 'Tools/Codex/HarnessConfig.json'
    $config = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
    if ($config.schemaVersion -ne 1 -or $config.maxScriptLines -lt 1 -or
        [string]::IsNullOrWhiteSpace($config.expectedUnityVersion) -or
        @($config.sourceRoots).Count -eq 0) {
        throw 'Invalid HarnessConfig.json.'
    }
    $null = Resolve-HarnessPath $config.harnessRoot
    foreach ($source in $config.sourceRoots) { $null = Resolve-HarnessPath $source }
    return $config
}

function Get-HarnessRequiredFiles($Config) {
    return @(
        'AGENTS.md', 'PLANS.md', 'Tools/Codex/HarnessConfig.json',
        'Tools/Codex/HarnessCommon.ps1', 'Tools/Codex/ValidateProject.ps1',
        'Tools/Codex/RunTests.ps1', 'Tools/Codex/VerifyChanges.ps1',
        'docs/architecture/README.md', 'docs/conventions/README.md',
        'docs/workflows/validation.md',
        ($Config.harnessRoot + '/Editor/CodexHarness.Editor.asmdef'),
        ($Config.harnessRoot + '/Tests/Editor/CodexHarness.Editor.Tests.asmdef'),
        '.agents/skills/unity-feature/SKILL.md', '.agents/skills/unity-debug/SKILL.md',
        '.agents/skills/unity-test/SKILL.md', '.agents/skills/unity-code-review/SKILL.md',
        '.agents/skills/unity-scene-validation/SKILL.md',
        '.agents/skills/harness-retrospective/SKILL.md'
    )
}

function Write-HarnessResult([string]$Name, [string]$Status, [string]$Detail) {
    Write-Output ("{0}: {1} - {2}" -f $Name, $Status, $Detail)
}

function Test-HarnessEditorOpen {
    $path = Join-Path (Get-HarnessRoot) 'Temp/UnityLockfile'
    if (-not (Test-Path -LiteralPath $path)) { return $false }
    try {
        $stream = [IO.File]::Open($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        $stream.Dispose()
        return $false
    } catch [IO.IOException] {
        return $true
    }
}

function Invoke-HarnessUnity([string]$UnityPath, [string[]]$Arguments, [int]$TimeoutSeconds = 300) {
    # Arguments are only passed to Unity, never to cmd.exe or another shell.
    $quoted = @($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' })
    $process = Start-Process -FilePath $UnityPath -ArgumentList $quoted -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        $process.WaitForExit()
        throw "Unity batchmode timed out after $TimeoutSeconds seconds."
    }
    return $process.ExitCode
}

function Get-HarnessTestResult([string]$XmlContent) {
    [xml]$xml = $XmlContent
    $run = $xml.SelectSingleNode('/test-run')
    if ($null -eq $run) { throw 'Expected an NUnit test-run report.' }
    $total = 0
    $failed = 0
    $passed = 0
    if (-not [int]::TryParse($run.GetAttribute('total'), [ref]$total) -or
        -not [int]::TryParse($run.GetAttribute('failed'), [ref]$failed) -or
        -not [int]::TryParse($run.GetAttribute('passed'), [ref]$passed)) {
        throw 'Test report is missing valid total/failed/passed counts.'
    }
    if ($total -eq 0 -or ($passed -eq 0 -and $failed -eq 0)) { $status = 'NOT VERIFIED'; $code = 2 }
    elseif ($failed -gt 0 -or $run.GetAttribute('result') -ne 'Passed') { $status = 'FAIL'; $code = 1 }
    else { $status = 'PASS'; $code = 0 }
    return [pscustomobject]@{ Status = $status; ExitCode = $code; Total = $total; Passed = $passed; Failed = $failed }
}

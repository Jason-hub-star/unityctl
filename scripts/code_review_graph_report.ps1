[CmdletBinding()]
param(
    [switch]$Update,
    [switch]$RebuildIfMissing,
    [ValidateRange(1, 100)]
    [int]$Top = 10
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-GraphCommand {
    if (Get-Command "code-review-graph" -ErrorAction SilentlyContinue) {
        return @("code-review-graph")
    }

    if (Get-Command "uvx" -ErrorAction SilentlyContinue) {
        return @("uvx", "--from", "code-review-graph", "code-review-graph")
    }

    throw "Required command not found: code-review-graph (or uvx fallback)"
}

function Invoke-GraphCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$GraphCommand,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $env:PYTHONUTF8 = "1"
    $env:PYTHONIOENCODING = "utf-8"

    & $GraphCommand[0] $GraphCommand[1..($GraphCommand.Length - 1)] $Arguments
    return $LASTEXITCODE
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dbPath = Join-Path $repoRoot ".code-review-graph\graph.db"
$pythonScript = Join-Path $PSScriptRoot "code_review_graph_report.py"

if (-not (Get-Command "python" -ErrorAction SilentlyContinue)) {
    throw "Required command not found: python"
}

$graphCommand = Get-GraphCommand

if (-not (Test-Path $pythonScript)) {
    throw "Python report script not found: $pythonScript"
}

Push-Location $repoRoot
try {
    if ($Update) {
        Write-Host "Updating code-review-graph..."
        $exitCode = Invoke-GraphCommand -GraphCommand $graphCommand -Arguments @("update")
        if ($exitCode -ne 0) {
            throw "code-review-graph update failed with exit code $exitCode"
        }
    }

    if (-not (Test-Path $dbPath) -and $RebuildIfMissing) {
        Write-Host "Graph DB not found. Building code-review-graph..."
        $exitCode = Invoke-GraphCommand -GraphCommand $graphCommand -Arguments @("build")
        if ($exitCode -ne 0) {
            throw "code-review-graph build failed with exit code $exitCode"
        }
    }

    $env:PYTHONUTF8 = "1"
    $env:PYTHONIOENCODING = "utf-8"

    & python $pythonScript --repo-root $repoRoot --db-path $dbPath --top $Top --graph-command "$($graphCommand -join ' ')"
    if ($LASTEXITCODE -ne 0) {
        throw "Python report failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

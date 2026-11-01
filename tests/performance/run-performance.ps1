param(
    [ValidateSet("baseline", "load", "stress", "spike", "soak")]
    [string]$Profile = "baseline",
    [string]$ApiBaseUrl = "http://localhost:8081",
    [string]$UiBaseUrl = "http://localhost:3001",
    [string]$NodeId = "",
    [double]$MaxP95Ms = 1000,
    [double]$MaxErrorRatePercent = 1,
    [switch]$IncludeWrites,
    [switch]$AllowRemoteTarget
)

$ErrorActionPreference = "Stop"

if ($MaxP95Ms -le 0) {
    throw "MaxP95Ms must be greater than zero."
}
if ($MaxErrorRatePercent -lt 0 -or $MaxErrorRatePercent -gt 100) {
    throw "MaxErrorRatePercent must be between 0 and 100."
}

$jmeterCommand = Get-Command jmeter -ErrorAction SilentlyContinue
if (-not $jmeterCommand) {
    throw "Apache JMeter was not found on PATH. Install JMeter 5.6.3 or later and retry."
}

function Get-TargetUri([string]$value, [string]$name) {
    $uri = $null
    if (-not [Uri]::TryCreate($value, [UriKind]::Absolute, [ref]$uri)) {
        throw "$name must be an absolute URL."
    }
    if ($uri.Scheme -notin @("http", "https")) {
        throw "$name must use HTTP or HTTPS."
    }
    return $uri
}

function Test-LocalHost([string]$hostName) {
    return $hostName -in @("localhost", "127.0.0.1", "::1")
}

$apiUri = Get-TargetUri $ApiBaseUrl "ApiBaseUrl"
$uiUri = Get-TargetUri $UiBaseUrl "UiBaseUrl"
if (-not $AllowRemoteTarget -and
    (-not (Test-LocalHost $apiUri.Host) -or -not (Test-LocalHost $uiUri.Host))) {
    throw "Remote hosts are blocked by default. Use a disposable performance environment and pass -AllowRemoteTarget to proceed."
}

if ($NodeId) {
    $parsedNodeId = [Guid]::Empty
    if (-not [Guid]::TryParse($NodeId, [ref]$parsedNodeId)) {
        throw "NodeId must be a GUID."
    }
}

$profiles = @{
    baseline = @{ Threads = 1;   RampUp = 1;   Duration = 60;   ThinkTime = 500 }
    load     = @{ Threads = 10;  RampUp = 30;  Duration = 300;  ThinkTime = 500 }
    stress   = @{ Threads = 50;  RampUp = 60;  Duration = 300;  ThinkTime = 250 }
    spike    = @{ Threads = 100; RampUp = 1;   Duration = 60;   ThinkTime = 250 }
    soak     = @{ Threads = 20;  RampUp = 60;  Duration = 1800; ThinkTime = 750 }
}
$settings = $profiles[$Profile]
$testPlan = Join-Path $PSScriptRoot "beecloud-api.jmx"
$runId = "{0}_{1}" -f (Get-Date -Format "yyyyMMdd_HHmmss_fff"), $Profile
$resultsRoot = Join-Path $PSScriptRoot "results"
$runDirectory = Join-Path $resultsRoot $runId
$reportDirectory = Join-Path $runDirectory "html-report"
$resultFile = Join-Path $runDirectory "results.jtl"

New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$arguments = @(
    "-n",
    "-t", $testPlan,
    "-l", $resultFile,
    "-j", (Join-Path $runDirectory "jmeter.log"),
    "-e",
    "-o", $reportDirectory,
    "-Jthreads=$($settings.Threads)",
    "-Jrampup=$($settings.RampUp)",
    "-Jduration=$($settings.Duration)",
    "-JthinkTime=$($settings.ThinkTime)",
    "-JapiProtocol=$($apiUri.Scheme)",
    "-JapiHost=$($apiUri.Host)",
    "-JapiPort=$($apiUri.Port)",
    "-JuiProtocol=$($uiUri.Scheme)",
    "-JuiHost=$($uiUri.Host)",
    "-JuiPort=$($uiUri.Port)",
    "-JincludeWrites=$($IncludeWrites.IsPresent.ToString().ToLowerInvariant())"
)

if ($NodeId) {
    $arguments += "-JnodeId=$NodeId"
}

Write-Host "Profile: $Profile ($($settings.Threads) users, $($settings.Duration)s)"
Write-Host "API: $($apiUri.GetLeftPart([UriPartial]::Authority))"
Write-Host "UI:  $($uiUri.GetLeftPart([UriPartial]::Authority))"
Write-Host "Writes enabled: $($IncludeWrites.IsPresent)"
Write-Host "Thresholds: p95 <= $MaxP95Ms ms; errors <= $MaxErrorRatePercent%"
Write-Host "Results: $runDirectory"

& $jmeterCommand.Source @arguments
if ($LASTEXITCODE -ne 0) {
    throw "JMeter failed with exit code $LASTEXITCODE. See $resultFile and the JMeter console output."
}

& (Join-Path $PSScriptRoot "analyze-results.ps1") `
    -ResultsPath $resultFile `
    -MaxP95Ms $MaxP95Ms `
    -MaxErrorRatePercent $MaxErrorRatePercent

Write-Host "JMeter HTML report: $reportDirectory\index.html"

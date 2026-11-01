param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsPath,
    [double]$MaxP95Ms = 1000,
    [double]$MaxErrorRatePercent = 1
)

$ErrorActionPreference = "Stop"

if ($MaxP95Ms -le 0) {
    throw "MaxP95Ms must be greater than zero."
}
if ($MaxErrorRatePercent -lt 0 -or $MaxErrorRatePercent -gt 100) {
    throw "MaxErrorRatePercent must be between 0 and 100."
}
if (-not (Test-Path -LiteralPath $ResultsPath -PathType Leaf)) {
    throw "JMeter results file not found: $ResultsPath"
}

$samples = @(Import-Csv -LiteralPath $ResultsPath)
if ($samples.Count -eq 0) {
    throw "JMeter results file contains no samples: $ResultsPath"
}
if (-not ($samples[0].PSObject.Properties.Name -contains "label") -or
    -not ($samples[0].PSObject.Properties.Name -contains "elapsed") -or
    -not ($samples[0].PSObject.Properties.Name -contains "success") -or
    -not ($samples[0].PSObject.Properties.Name -contains "timeStamp")) {
    throw "JMeter results file is missing required CSV columns: label, elapsed, success, timeStamp."
}

$timestamps = @(
    $samples |
        ForEach-Object {
            $timestamp = 0L
            if (-not [long]::TryParse($_.timeStamp, [ref]$timestamp)) {
                throw "Invalid JMeter sample timestamp: $($_.timeStamp)"
            }
            $timestamp
        } |
        Sort-Object
)
$runSeconds = [Math]::Max(1, ($timestamps[-1] - $timestamps[0]) / 1000)

$failedThresholds = [System.Collections.Generic.List[string]]::new()
$groups = $samples | Group-Object -Property label
foreach ($group in $groups) {
    $elapsed = @(
        $group.Group |
            ForEach-Object { [double]$_.elapsed } |
            Sort-Object
    )
    $percentileIndex = [Math]::Max(
        0,
        [Math]::Ceiling($elapsed.Count * 0.95) - 1
    )
    $p95 = $elapsed[$percentileIndex]
    $failedCount = @(
        $group.Group | Where-Object {
            $_.success -notmatch "^(?i:true)$"
        }
    ).Count
    $errorRate = 100 * $failedCount / $group.Count
    $throughput = $group.Count / $runSeconds

    Write-Host (
        "{0}: samples={1}, p95={2:N0} ms, throughput={3:N2}/s, errors={4:N2}%, failures={5}" -f
            $group.Name,
            $group.Count,
            $p95,
            $throughput,
            $errorRate,
            $failedCount
    )

    if ($p95 -gt $MaxP95Ms) {
        $failedThresholds.Add(
            "$($group.Name) p95 $([Math]::Round($p95)) ms > $MaxP95Ms ms"
        )
    }
    if ($errorRate -gt $MaxErrorRatePercent) {
        $failedThresholds.Add(
            "$($group.Name) error rate $([Math]::Round($errorRate, 2))% > $MaxErrorRatePercent%"
        )
    }
}

if ($failedThresholds.Count -gt 0) {
    throw "Performance thresholds failed: $($failedThresholds -join '; ')"
}

Write-Host "Performance thresholds passed."

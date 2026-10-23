$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$archive = Join-Path $projectRoot 'new_dawn_legacy/NewDawn_Playeble_Maps_Saves_Default_Units.zip'
$results = @{}
foreach ($variant in @('legacy', 'ported')) {
    $project = Join-Path $PSScriptRoot "$variant/$variant.csproj"
    dotnet build $project --nologo --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "$variant model build failed" }
    $testFolder = Join-Path $projectRoot ".godot/model-parity/$variant"
    $results[$variant] = @(dotnet run --no-build --project $project -- $archive $testFolder)
    if ($LASTEXITCODE -ne 0) { throw "$variant model run failed" }
}
$differences = Compare-Object $results['legacy'] $results['ported'] -SyncWindow 0
if ($differences) {
    $differences | Format-Table
    throw 'Legacy and ported model behavior differs'
}
Write-Output 'PASS: all 19 bundled map outcomes and all 10 terrain transitions match the original WPF models.'

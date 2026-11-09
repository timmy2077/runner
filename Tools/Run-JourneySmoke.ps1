param([string]$UnityEditorPath = 'C:/Program Files/Unity 2022.3.62f1/Editor', [switch]$RenderScreenshots, [switch]$StandardJourney)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$qaRoot = Join-Path $projectRoot 'Logs/JourneyValidation/SystemQA'
New-Item -ItemType Directory -Path $qaRoot -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings', 'Library/PackageCache')) {
    & robocopy (Join-Path $projectRoot $folder) (Join-Path $qaRoot $folder) /E /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "QA copy failed: $folder" }
}
$qaLog = Join-Path $projectRoot 'Logs/JourneyValidation/systems-smoke.log'
$arguments = @('-batchmode', '-projectPath', ('"' + $qaRoot + '"'),
    '-executeMethod', 'JourneyPrefabValidation.RunSmoke', '-journeySmoke', '-logFile', ('"' + $qaLog + '"'))
if (-not $RenderScreenshots) { $arguments += '-nographics' }
if ($StandardJourney) { $arguments += '-journeyStandard' }
$process = Start-Process -FilePath (Join-Path $UnityEditorPath 'Unity.exe') -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity smoke test started (PID $($process.Id)) in isolated project: $qaRoot"
$deadline = [DateTime]::UtcNow.AddMinutes(12)
while (-not $process.HasExited) {
    if ([DateTime]::UtcNow -gt $deadline) { Stop-Process -Id $process.Id; throw "Unity smoke exceeded 12 minutes. See $qaLog" }
    Start-Sleep -Seconds 2
    $process.Refresh()
}
if ($process.ExitCode -ne 0) { throw "Unity smoke failed ($($process.ExitCode)). See $qaLog" }
if (-not (Select-String -LiteralPath $qaLog -Pattern 'PREFAB RESTORE SMOKE PASSED' -Quiet)) { throw "Unity exited without the smoke success marker. See $qaLog" }
Write-Output 'Unity prefab campaign smoke passed. Scene catalogues and player settings remain owned by the main project.'

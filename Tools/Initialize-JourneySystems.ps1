$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Write-ProjectText($relative, $value) {
    [IO.File]::WriteAllText((Join-Path $projectRoot $relative), $value, $utf8)
}
# Assign stable Unity metadata once. Existing metadata, including every moved script GUID, is preserved.
Get-ChildItem (Join-Path $projectRoot 'Assets/Script'),(Join-Path $projectRoot 'Assets/Editor') -Recurse -Filter *.cs | ForEach-Object {
    if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) {
        [IO.File]::WriteAllText(($_.FullName + '.meta'), "fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`nMonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n", $utf8)
    }
}
Get-ChildItem (Join-Path $projectRoot 'Assets/Script'),(Join-Path $projectRoot 'Assets/Editor') -Recurse -Directory | ForEach-Object {
    if (-not (Test-Path -LiteralPath ($_.FullName + '.meta'))) {
        [IO.File]::WriteAllText(($_.FullName + '.meta'), "fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n", $utf8)
    }
}
function New-SettingsAsset($name, $scriptPath, $body) {
    $assetPath = "Assets/Resources/$name.asset"
    if (Test-Path -LiteralPath (Join-Path $projectRoot $assetPath)) { return }
    $scriptGuid = [regex]::Match([IO.File]::ReadAllText((Join-Path $projectRoot ($scriptPath + '.meta'))), 'guid: ([a-f0-9]+)').Groups[1].Value
    $header = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier:
"@
    Write-ProjectText $assetPath ($header + "`n" + $body + "`n")
    Write-ProjectText ($assetPath + '.meta') "fileFormatVersion: 2`nguid: $([Guid]::NewGuid().ToString('N'))`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n"
}
$demonBody = "  demons:`n"
$definitions = @(
    @(1, '恐惧', '{r: 0.48, g: 0.36, b: 0.65, a: 1}', '看清前方的封路预警。按 J 攻击，也可以停下等待。', '你穿过了恐惧留下的间隙。路还在前方。', '0.7'),
    @(2, '焦虑', '{r: 0.9, g: 0.65, b: 0.3, a: 1}', '先看落点，再迈步。落物不会填满所有空隙。', '你终于找回了自己的步调。', '0.7'),
    @(3, '迷茫', '{r: 0.4, g: 0.65, b: 0.75, a: 1}', '地面的标记会告诉你，移动的障碍将去哪里。', '答案还未齐全，你已经走出了这片雾。', '0.7'),
    @(4, '压力', '{r: 0.78, g: 0.42, b: 0.38, a: 1}', '重量落下前，先离开地面的标记。路口封闭时可以等待。', '这一次，你为自己放下了一点重量。', '0.7'),
    @(5, '死亡', '{r: 0.65, g: 0.73, b: 0.7, a: 1}', '远处的身影没有催促你。带着这一生，慢慢向海边走。', '你依然舍不得。但这一次，你没有再躲开。', '0.9')
)
foreach ($entry in $definitions) {
    $demonBody += @"
  - demon: $($entry[0])
    title: $($entry[1])
    sprite: {fileID: 0}
    tint: $($entry[2])
    startProgress: 0.45
    arenaEndProgress: $($entry[5])
    arrivalSeconds: 2
    warningSeconds: 2
    activeSeconds: 1
    recoverySeconds: 3
    secondPhaseRecovery: 2
    introduction: $($entry[3])
    resolution: $($entry[4])

"@
}
New-SettingsAsset 'JourneyDemonSettings' 'Assets/Script/Runtime/Encounters/JourneyDemonSettings.cs' $demonBody.TrimEnd()
$generationBody = @'
  tuning:
    startDensity: 0.35
    endDensity: 0.7
    recoverySeconds: 1.5
    rules:
    - obstacle: 1
      weight: 3
      stageMask: 30
      fromProgress: 0
      untilProgress: 1
      sprite: {fileID: 0}
    - obstacle: 2
      weight: 2
      stageMask: 30
      fromProgress: 0
      untilProgress: 1
      sprite: {fileID: 0}
    - obstacle: 3
      weight: 2
      stageMask: 63
      fromProgress: 0
      untilProgress: 1
      sprite: {fileID: 0}
    - obstacle: 4
      weight: 1
      stageMask: 30
      fromProgress: 0
      untilProgress: 1
      sprite: {fileID: 0}
'@
New-SettingsAsset 'JourneyGenerationSettings' 'Assets/Script/Runtime/Generation/JourneyGenerationSettings.cs' $generationBody
$settingsPath = Join-Path $projectRoot 'Assets/Resources/LifeJourneySettings.asset'
$settings = [IO.File]::ReadAllText($settingsPath)
foreach ($binding in @(@('demonSettings', 'JourneyDemonSettings'), @('generationSettings', 'JourneyGenerationSettings'))) {
    if ($settings -notmatch ('(?m)^  ' + $binding[0] + ':')) {
        $guid = [regex]::Match([IO.File]::ReadAllText((Join-Path $projectRoot ('Assets/Resources/' + $binding[1] + '.asset.meta'))), 'guid: ([a-f0-9]+)').Groups[1].Value
        $settings += "`n  $($binding[0]): {fileID: 11400000, guid: $guid, type: 2}`n"
    }
}
$settings = [regex]::Replace($settings, '(?s)(  - stage: 5.*?    demon: )0', '${1}5')
[IO.File]::WriteAllText($settingsPath, $settings, $utf8)
Write-Output 'Created and connected mind demon and random generation settings.'

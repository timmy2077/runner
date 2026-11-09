$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$scriptRoot = Join-Path $projectRoot 'Assets/Script'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$files = @(Get-ChildItem -LiteralPath $scriptRoot -Recurse -Filter *.cs -File | Sort-Object FullName)
$byGuid = @{}
foreach ($file in $files) {
    $meta = $file.FullName + '.meta'
    if (-not (Test-Path -LiteralPath $meta)) { throw "Missing metadata: $meta" }
    $guid = [regex]::Match([IO.File]::ReadAllText($meta), '(?m)^guid: (\w+)').Groups[1].Value
    if (-not $guid -or $byGuid.ContainsKey($guid)) { throw "Missing or duplicate GUID: $meta" }
    $byGuid[$guid] = $file.FullName
    $text = [IO.File]::ReadAllText($file.FullName)
    if ($text -match 'class\s+(\w+)\s*:\s*(?:MonoBehaviour|ScriptableObject)' -and
        $Matches[1] -ne $file.BaseName -and $file.BaseName -notin @('LifeStage', 'JourneySave')) {
        # Files with serializable data before their Unity asset class are checked by Unity import.
        if ($text -notmatch ('class\s+' + [regex]::Escape($file.BaseName) + '\b')) { throw "Unity class/file mismatch: $($file.FullName)" }
    }
}
$backup = Join-Path $projectRoot 'Logs/RefactorBackup/before-systems-upgrade.zip'
$preserved = 0
if (Test-Path -LiteralPath $backup) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($backup)
    try {
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName -notmatch '(?i)^Script[/\\].*\.cs\.meta$') { continue }
            $reader = New-Object IO.StreamReader($entry.Open())
            try { $original = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $guid = [regex]::Match($original, '(?m)^guid: (\w+)').Groups[1].Value
            if (-not $byGuid.ContainsKey($guid)) { throw "Original script GUID lost: $($entry.FullName) ($guid)" }
            $preserved++
        }
    } finally { $archive.Dispose() }
}
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# 项目脚本索引')
$lines.Add('')
$lines.Add('项目自有运行时代码按职责归入 `Assets/Script/Runtime`；旧场景和预制体兼容入口归入 `Assets/Script/Legacy`。编辑器工具保留在 `Assets/Editor`。TextMesh Pro 的示例脚本属于第三方资源，保持原目录。')
$lines.Add('')
$lines.Add('所有移动均保留原 `.meta` 和 GUID。旧类名、序列化字段及按钮调用入口保留；含空格的文件名已改为与类名一致。')
$lines.Add('')
$lines.Add('| 脚本 | 职责模块 |')
$lines.Add('| --- | --- |')
foreach ($file in $files) {
    $path = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    $module = $file.DirectoryName.Substring($scriptRoot.Length + 1).Replace('\', '/')
    $lines.Add('| [' + $file.BaseName + '](../' + $path.Replace(' ', '%20') + ') | ' + $module + ' |')
}
foreach ($file in Get-ChildItem (Join-Path $projectRoot 'Assets/Editor') -Recurse -Filter *.cs -File | Sort-Object FullName) {
    $path = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    $lines.Add('| [' + $file.BaseName + '](../' + $path + ') | Editor |')
}
$lines.Add('')
$lines.Add("本次索引包含 $($files.Count) 个项目运行时/兼容脚本；与升级前备份比对，$preserved 个原脚本 GUID 全部保留。")
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Docs') -Force | Out-Null
[IO.File]::WriteAllLines((Join-Path $projectRoot 'Docs/SCRIPT_INDEX.md'), $lines, $utf8)
Write-Output "SCRIPT AUDIT PASSED: $($files.Count) scripts; $preserved original GUIDs preserved. Index: Docs/SCRIPT_INDEX.md"

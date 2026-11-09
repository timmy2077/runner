param([string]$UnityEditorPath = 'C:/Program Files/Unity 2022.3.62f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    $validationFolder = Join-Path $projectRoot 'Logs/JourneyValidation'
    New-Item -ItemType Directory -Path $validationFolder -Force | Out-Null
    $response = Get-ChildItem -LiteralPath Library/Bee -Recurse -Filter Assembly-CSharp.rsp -File | Select-Object -First 1
    if (-not $response) { throw 'Open this project in Unity once to resolve its package assemblies before running this check.' }
    $options = @(Get-Content -LiteralPath $response.FullName | Where-Object {
        $_ -match '^[-/]' -and $_ -notmatch '(^-out:|^-refout:|(?i)WX|WxWasm|wx-runtime|wx-perf|LitJson|FontABTool|additionalfile:|analyzer:)'
    })
    $options += '-out:"Logs/JourneyValidation/Assembly-CSharp.dll"'
    $options += @(Get-ChildItem -LiteralPath Assets/Script -Recurse -Filter *.cs -File | ForEach-Object { '"' + $_.FullName + '"' })
    $options | Set-Content -LiteralPath Logs/JourneyValidation/runtime.rsp -Encoding utf8
    $dotnet = Join-Path $UnityEditorPath 'Data/NetCoreRuntime/dotnet.exe'
    $compiler = Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/csc.dll'
    & $dotnet $compiler '@Logs/JourneyValidation/runtime.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Runtime C# compilation failed.' }
    $editorResponse = Get-ChildItem -LiteralPath Library/Bee -Recurse -Filter Assembly-CSharp-Editor.rsp -File | Select-Object -First 1
    if (-not $editorResponse) { throw 'Unity editor assembly response file is missing.' }
    $editorOptions = @(Get-Content -LiteralPath $editorResponse.FullName | Where-Object {
        $_ -match '^[-/]' -and $_ -notmatch '(^-out:|^-refout:|(?i)WX|WxWasm|wx-runtime|wx-perf|LitJson|FontABTool|additionalfile:|analyzer:|Assembly-CSharp(?:\.ref)?\.dll)'
    })
    $editorOptions += @('-out:"Logs/JourneyValidation/Assembly-CSharp-Editor.dll"', '-r:"Logs/JourneyValidation/Assembly-CSharp.dll"')
    $editorOptions += @(Get-ChildItem -LiteralPath Assets/Editor -Recurse -Filter *.cs -File | ForEach-Object { '"' + $_.FullName + '"' })
    $editorOptions | Set-Content -LiteralPath Logs/JourneyValidation/editor.rsp -Encoding utf8
    & $dotnet $compiler '@Logs/JourneyValidation/editor.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Editor C# compilation failed.' }
    $testOptions = @($options | Where-Object { $_ -match '^[-/]' -and $_ -notmatch '(^-out:|^-target:)' })
    $testOptions += @('-target:exe', '-out:"Logs/JourneyValidation/JourneyLogicTests.exe"', '-r:"Logs/JourneyValidation/Assembly-CSharp.dll"', '"Tests/JourneyLogicTests.cs"', '"Tests/LifeEventLogicTests.cs"', '"Tests/JourneyPrefabLogicTests.cs"')
    $testOptions | Set-Content -LiteralPath Logs/JourneyValidation/tests.rsp -Encoding utf8
    & $dotnet $compiler '@Logs/JourneyValidation/tests.rsp'
    if ($LASTEXITCODE -ne 0) { throw 'Logic test compilation failed.' }
    $previousMonoPath = $env:MONO_PATH
    try {
        $env:MONO_PATH = (Join-Path $UnityEditorPath 'Data/Managed/UnityEngine') + ';' + (Join-Path $projectRoot 'Library/ScriptAssemblies')
        & (Join-Path $UnityEditorPath 'Data/MonoBleedingEdge/bin/mono.exe') 'Logs/JourneyValidation/JourneyLogicTests.exe' 'Assets/Resources/LifeJourneySettings.asset' 'ProjectSettings/EditorBuildSettings.asset'
        if ($LASTEXITCODE -ne 0) { throw 'Campaign logic tests failed.' }
    } finally { $env:MONO_PATH = $previousMonoPath }
} finally { Pop-Location }

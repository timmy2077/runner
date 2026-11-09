param([string]$UnityEditorPath = 'C:/Program Files/Unity 2022.3.62f1/Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Add-Type -Path (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $UnityEditorPath 'Data/DotNetSdkRoslyn/Microsoft.CodeAnalysis.CSharp.dll')
$normalize = [Microsoft.CodeAnalysis.SyntaxNodeExtensions].GetMethods() |
    Where-Object { $_.Name -eq 'NormalizeWhitespace' -and $_.GetParameters().Count -eq 4 } |
    Select-Object -First 1
$formatter = $normalize.MakeGenericMethod([Microsoft.CodeAnalysis.SyntaxNode])
$utf8 = New-Object System.Text.UTF8Encoding($false)
$count = 0
foreach ($file in Get-ChildItem (Join-Path $projectRoot 'Assets/Script'),(Join-Path $projectRoot 'Assets/Editor') -Recurse -Filter *.cs -File) {
    $text = [IO.File]::ReadAllText($file.FullName)
    $text = [regex]::Replace($text, '(?m)^\s*// (?:Start is called before the first frame update|Update is called once per frame)\s*\r?\n', '')
    $root = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($text).GetRoot()
    $formatted = $formatter.Invoke($null, @($root, '    ', "`n", $false)).ToFullString() + "`n"
    $reparsed = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText($formatted).GetRoot()
    if (-not [Microsoft.CodeAnalysis.CSharp.SyntaxFactory]::AreEquivalent($root, $reparsed, $false)) { throw "Formatting changed syntax: $($file.FullName)" }
    [IO.File]::WriteAllText($file.FullName, $formatted, $utf8)
    $count++
}
Write-Output "Formatted $count project C# files with Roslyn; syntax equivalence verified. Third-party code and Unity metadata were untouched."

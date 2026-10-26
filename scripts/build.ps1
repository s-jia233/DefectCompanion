param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildRoot = Join-Path $projectRoot 'build'
$packageRoot = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $projectRoot 'dist\app' }
New-Item -ItemType Directory -Force -Path $buildRoot, $packageRoot | Out-Null
$versionInfo = Get-Content -LiteralPath (Join-Path $projectRoot 'version.json') -Raw | ConvertFrom-Json
if ($versionInfo.version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') { throw 'Invalid release version' }
if ($versionInfo.assemblyVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw 'Invalid assembly version' }
$versionSource = @"
using System.Reflection;
[assembly: AssemblyTitle("故障机器人 · 互动桌宠")]
[assembly: AssemblyProduct("DefectCompanion")]
[assembly: AssemblyDescription("杀戮尖塔2故障机器人互动桌宠")]
[assembly: AssemblyVersion("$($versionInfo.assemblyVersion)")]
[assembly: AssemblyFileVersion("$($versionInfo.assemblyVersion)")]
[assembly: AssemblyInformationalVersion("$($versionInfo.version)")]
"@
$versionSourcePath = Join-Path $buildRoot 'Version.Generated.cs'
[IO.File]::WriteAllText($versionSourcePath, $versionSource, (New-Object Text.UTF8Encoding($false)))
[xml]$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'src\App\app.manifest') -Raw
$manifest.assembly.assemblyIdentity.version = $versionInfo.assemblyVersion
$manifestPath = Join-Path $buildRoot 'app.manifest'
$manifest.Save($manifestPath)
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkRoot 'csc.exe'
$wpfRoot = Join-Path $frameworkRoot 'WPF'
$referencePaths = @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $wpfRoot $_) }
$resources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'assets') -Recurse -File -Filter '*.png' | Where-Object { $_.Name -match '^(mod-|native-|power-|fx-|card-|orb-|particle-|bolt-|lightning-sequence-|cast-fist)' } | Sort-Object Name)
$duplicateNames = @($resources | Group-Object Name | Where-Object Count -gt 1)
if ($duplicateNames.Count) { throw 'Duplicate embedded resource names' }
$assetArgs = @($resources | ForEach-Object { '/resource:' + $_.FullName + ',assets.' + $_.Name })
$assetArgs += '/resource:' + (Join-Path $projectRoot 'assets\attachments\mod-cable-prefabs.bin') + ',assets.mod-cable-prefabs.bin'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Recurse -File -Filter '*.cs' | Sort-Object FullName | ForEach-Object FullName)
$sources += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tests\Wpf') -File -Filter '*.cs' | Sort-Object Name | ForEach-Object FullName)
$sources += $versionSourcePath
$compileArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/codepage:65001',('/out:' + (Join-Path $packageRoot 'DefectCompanion.exe')),('/win32manifest:' + $manifestPath),('/win32icon:' + (Join-Path $projectRoot 'assets\app\pet.ico')),('/resource:' + (Join-Path $projectRoot 'assets\app\pet.ico') + ',pet.ico'),('/resource:' + (Join-Path $projectRoot 'assets\app\pet.png') + ',pet.png'),'/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xaml.dll') + $referencePaths + $assetArgs + $sources
$compileResponse = Join-Path $buildRoot 'app.rsp'
[IO.File]::WriteAllLines($compileResponse, @($compileArgs | ForEach-Object { '"' + $_ + '"' }), (New-Object Text.UTF8Encoding($false)))
& $compilerPath ('@' + $compileResponse)
if ($LASTEXITCODE -ne 0) { throw 'Desktop compilation failed' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App\App.config') -Destination (Join-Path $packageRoot 'DefectCompanion.exe.config') -Force
$testExecutable = Join-Path $buildRoot 'EngineTests.exe'
$testSources = @('src\Core\PetEngine.cs','src\Core\PowerStatus.cs','src\Animation\AnimationTiming.cs','src\Animation\CardAnimation.cs','src\Animation\ModPlayback.cs','tests\EngineTests.cs') | ForEach-Object { Join-Path $projectRoot $_ }
& $compilerPath /nologo /target:exe /codepage:65001 ('/out:' + $testExecutable) /reference:System.Core.dll $testSources
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& $testExecutable
if ($LASTEXITCODE -ne 0) { throw 'Interaction tests failed' }
Write-Output ('Built: ' + (Join-Path $packageRoot 'DefectCompanion.exe'))

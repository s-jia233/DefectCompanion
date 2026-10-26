param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$versionInfo = Get-Content -LiteralPath (Join-Path $sourceRoot 'version.json') -Raw | ConvertFrom-Json
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
$versionSourcePath = Join-Path $sourceRoot 'Version.Generated.cs'
[System.IO.File]::WriteAllText($versionSourcePath, $versionSource, (New-Object System.Text.UTF8Encoding($false)))
[xml]$manifest = Get-Content -LiteralPath (Join-Path $sourceRoot 'app.manifest') -Raw
$manifest.assembly.assemblyIdentity.version = $versionInfo.assemblyVersion
$manifest.Save((Join-Path $sourceRoot 'app.manifest'))
$packageRoot = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $sourceRoot 'release' }
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkRoot 'csc.exe'
$wpfRoot = Join-Path $frameworkRoot 'WPF'
$referencePaths = @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $wpfRoot $_) }
$assetArgs = Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'assets') -Filter '*.png' | Where-Object { $_.Name -match '^(mod-|native-|power-|fx-|card-|orb-|particle-|bolt-|lightning-sequence-|cast-fist)' } | ForEach-Object { '/resource:' + $_.FullName + ',assets.' + $_.Name }
$assetArgs += '/resource:' + (Join-Path $sourceRoot 'assets\mod-cable-prefabs.bin') + ',assets.mod-cable-prefabs.bin'
$compileArgs = @('/nologo','/target:winexe','/platform:anycpu','/optimize+','/codepage:65001',('/out:' + (Join-Path $packageRoot 'DefectCompanion.exe')),('/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')),('/win32icon:' + (Join-Path $sourceRoot 'pet.ico')),('/resource:' + (Join-Path $sourceRoot 'pet.ico') + ',pet.ico'),('/resource:' + (Join-Path $sourceRoot 'pet.png') + ',pet.png'),'/reference:System.dll','/reference:System.Core.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xaml.dll') + $referencePaths + $assetArgs + @((Join-Path $sourceRoot 'PetEngine.cs'),(Join-Path $sourceRoot 'Main.cs'),(Join-Path $sourceRoot 'GameArt.cs'),(Join-Path $sourceRoot 'CardPicker.cs'),(Join-Path $sourceRoot 'UiChecks.cs'))
$compileArgs += (Join-Path $sourceRoot 'CardCatalog.Generated.cs')
$compileArgs += (Join-Path $sourceRoot 'CardEffects.cs')
$compileArgs += (Join-Path $sourceRoot 'CardAnimation.cs'), (Join-Path $sourceRoot 'AdvancedCardEffects.cs')
$compileArgs += (Join-Path $sourceRoot 'ShowcaseExport.cs')
$compileArgs += (Join-Path $sourceRoot 'InstalledModArt.cs'),(Join-Path $sourceRoot 'ModPlayback.cs'),(Join-Path $sourceRoot 'ModVisuals.cs'),(Join-Path $sourceRoot 'ModBindings.Generated.cs'),(Join-Path $sourceRoot 'ModBufferRects.Generated.cs'),(Join-Path $sourceRoot 'ModCableRests.Generated.cs')
$compileArgs += (Join-Path $sourceRoot 'ModIceLance.cs')
$compileArgs += (Join-Path $sourceRoot 'ColdHead.Generated.cs')
$compileArgs += (Join-Path $sourceRoot 'CardPoses.Generated.cs'),(Join-Path $sourceRoot 'CardArtVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'ElementalVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'PressureVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'DualcastVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'LightningStyleVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'StormVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'HandCardVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'MachineryVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'HotCoreArt.cs'),(Join-Path $sourceRoot 'HotCore.Generated.cs')
$compileArgs += (Join-Path $sourceRoot 'LearningHeadArt.cs'),(Join-Path $sourceRoot 'KnowledgeVisuals.cs')
$compileArgs += (Join-Path $sourceRoot 'LearningNeckArt.cs'),(Join-Path $sourceRoot 'LearningNeck.Generated.cs')
$compileArgs += (Join-Path $sourceRoot 'AnimationTiming.cs'), (Join-Path $sourceRoot 'PerformanceCheck.cs')
$compileArgs += (Join-Path $sourceRoot 'PowerStatus.cs'), (Join-Path $sourceRoot 'PowerBar.cs')
$compileArgs += (Join-Path $sourceRoot 'StyleChecks.cs'), (Join-Path $sourceRoot 'NativeAnimation.cs'), (Join-Path $sourceRoot 'NativeClips.Generated.cs')
$compileResponse = Join-Path $sourceRoot '.desktop-build.rsp'
$compileArgs += $versionSourcePath
[System.IO.File]::WriteAllLines($compileResponse, @($compileArgs | ForEach-Object { '"' + $_ + '"' }), (New-Object System.Text.UTF8Encoding($false)))
& $compilerPath ('@' + $compileResponse)
if ($LASTEXITCODE -ne 0) { throw 'Desktop compilation failed' }
Copy-Item -LiteralPath (Join-Path $sourceRoot 'App.config') -Destination (Join-Path $packageRoot 'DefectCompanion.exe.config') -Force
& $compilerPath /nologo /target:exe /codepage:65001 ('/out:' + (Join-Path $sourceRoot 'EngineTests.exe')) /reference:System.Core.dll (Join-Path $sourceRoot 'PetEngine.cs') (Join-Path $sourceRoot 'PowerStatus.cs') (Join-Path $sourceRoot 'AnimationTiming.cs') (Join-Path $sourceRoot 'CardAnimation.cs') (Join-Path $sourceRoot 'ModPlayback.cs') (Join-Path $sourceRoot 'Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& (Join-Path $sourceRoot 'EngineTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Interaction tests failed' }
Write-Output ('Built: ' + (Join-Path $packageRoot 'DefectCompanion.exe'))

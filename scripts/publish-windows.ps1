param([ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK first.' }
& dotnet msbuild src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj '-t:ValidateGovernanceRules,ValidateRepositoryGovernance' -nologo
if ($LASTEXITCODE -ne 0) { throw 'Code governance checks failed.' }
$output = Join-Path (Get-Location) "dist/native-$Runtime"
& dotnet publish src/Lianxin.Fleet.Desktop/Lianxin.Fleet.Desktop.csproj -c Release -r $Runtime --self-contained true -p:RuntimeFrameworkVersion=10.0.15 -p:DebugType=None -p:DebugSymbols=false -o $output
if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }
& dotnet publish src/Lianxin.Sms.Api/Lianxin.Sms.Api.csproj -c Release -r $Runtime --self-contained true -p:RuntimeFrameworkVersion=10.0.15 -p:DebugType=None -p:DebugSymbols=false -o (Join-Path $output 'sms-api')
if ($LASTEXITCODE -ne 0) { throw 'SMS compatibility API publish failed.' }
Copy-Item README.md, requirements-windows.txt, StartFleet.cmd -Destination $output -Force
New-Item -ItemType Directory -Force -Path (Join-Path $output 'docs') | Out-Null
Copy-Item docs/*.md -Destination (Join-Path $output 'docs') -Force
Write-Host "Published native Windows applications to $output"

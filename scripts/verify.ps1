param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 10 SDK first.'
}
function Invoke-DotNet([string[]]$ToolArguments) {
    & dotnet @ToolArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet verification failed: $($ToolArguments -join ' ')"
    }
}
Invoke-DotNet @('msbuild', 'src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj', '-t:ValidateGovernanceRules,ValidateRepositoryGovernance', '-nologo')
Invoke-DotNet @('build', 'Lianxin.Fleet.sln', '-c', $Configuration, '--nologo')
Invoke-DotNet @('format', 'Lianxin.Fleet.sln', 'whitespace', '--verify-no-changes', '--no-restore')
Invoke-DotNet @('format', 'Lianxin.Fleet.sln', 'style', '--diagnostics', 'IDE0005', '--severity', 'info', '--verify-no-changes', '--no-restore')
Invoke-DotNet @('run', '--no-build', '-c', $Configuration, '--project', 'checks/Lianxin.Fleet.Checks')
Invoke-DotNet @('run', '--no-build', '-c', $Configuration, '--project', 'checks/Lianxin.Sms.Checks')

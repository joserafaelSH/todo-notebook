# Builds a distributable copy of Todo Notebook into .\dist (single exe, framework-dependent by default).
#   .\publish.ps1                 -> needs the .NET 10 Desktop Runtime on the target machine
#   .\publish.ps1 -SelfContained  -> bundles the runtime (bigger, runs anywhere on Windows x64)
param(
    [switch]$SelfContained,
    [string]$Output = (Join-Path $PSScriptRoot 'dist')
)

$ErrorActionPreference = 'Stop'

if (Test-Path $Output) { Remove-Item $Output -Recurse -Force }
$sc = if ($SelfContained) { 'true' } else { 'false' }
dotnet publish (Join-Path $PSScriptRoot 'TodoNotebook.csproj') -c Release -r win-x64 `
    --self-contained $sc -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $Output --nologo
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }

Write-Host "Done: $Output"
Write-Host 'To install: copy the folder anywhere and run TodoNotebook.exe once (it registers autostart).'

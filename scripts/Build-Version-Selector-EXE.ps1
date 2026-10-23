[CmdletBinding()]
param([string]$OutputDirectory = "")
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "tools\RevitVersionSelector\RevitVersionSelector.csproj"
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $repoRoot "dist\RevitVersionSelector" }
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCommand) { throw "dotnet CLI not found. Install the .NET 10 SDK and ensure dotnet is on PATH." }
if (-not (Test-Path $project)) { throw "Launcher project not found: $project" }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
& $dotnetCommand.Source publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
$exe = Join-Path $OutputDirectory "RevitVersionSelector.exe"
if (-not (Test-Path $exe)) { throw "Publish completed but EXE was not found: $exe" }
Write-Host "Launcher EXE created: $exe" -ForegroundColor Green

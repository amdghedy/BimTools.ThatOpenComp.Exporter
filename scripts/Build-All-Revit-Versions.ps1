[CmdletBinding()]
param(
 [ValidateSet("2020","2021","2022","2023","2024","2025","2026")]
 [string[]]$Versions=@("2020","2021","2022","2023","2024","2025","2026"),
 [string]$RevitInstallRoot="C:\Program Files\Autodesk",
 [string]$OutputRoot="",
 [switch]$ContinueOnError
)
$ErrorActionPreference="Stop"
$RepoRoot=Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot)) { $OutputRoot=Join-Path $RepoRoot "00-Revit Versions" }
$OutputRoot=[System.IO.Path]::GetFullPath($OutputRoot)
$targets=@{
 "2020"=@{Framework="net472";RevitDir="Revit 2020";Label="Revit2020"}
 "2021"=@{Framework="net48";RevitDir="Revit 2021";Label="Revit2021"}
 "2022"=@{Framework="net48";RevitDir="Revit 2022";Label="Revit2022"}
 "2023"=@{Framework="net48";RevitDir="Revit 2023";Label="Revit2023"}
 "2024"=@{Framework="net48";RevitDir="Revit 2024";Label="Revit2024"}
 "2025"=@{Framework="net8.0-windows";RevitDir="Revit 2025";Label="Revit2025"}
 "2026"=@{Framework="net8.0-windows";RevitDir="Revit 2026";Label="Revit2026"}
}
function Escape-Xml([string]$Value) { return [System.Security.SecurityElement]::Escape($Value) }
$dotnetCommand=Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetCommand) { throw "dotnet CLI not found. Install the .NET SDK and ensure dotnet is on PATH." }
$dotnet=$dotnetCommand.Source
$results=@()
foreach ($version in $Versions) {
 $target=$targets[$version]
 $revitDir=Join-Path $RevitInstallRoot $target.RevitDir
 $api=Join-Path $revitDir "RevitAPI.dll"
 $apiUi=Join-Path $revitDir "RevitAPIUI.dll"
 $versionOutput=Join-Path (Join-Path $OutputRoot ("00-Revit "+$version)) "Build Output"
 Write-Host ""
 Write-Host "=== Revit $version / $($target.Framework) ===" -ForegroundColor Cyan
 if (!(Test-Path $api) -or !(Test-Path $apiUi)) {
  $reason="Revit API assemblies not found: $revitDir"
  Write-Warning "$reason. Install this Revit version or pass -RevitInstallRoot."
  $results += [pscustomobject]@{RevitVersion=$version;Framework=$target.Framework;Status="SKIPPED";Detail=$reason}
  continue
 }
 $buildRoot=Join-Path ([System.IO.Path]::GetTempPath()) ("TOCExporter_Build_"+$version+"_"+[guid]::NewGuid().ToString("N"))
 $sourceRoot=Join-Path $buildRoot "src"
 New-Item -ItemType Directory -Path (Join-Path $sourceRoot "Properties") -Force | Out-Null
 try {
  Copy-Item (Join-Path $RepoRoot "App.cs") $sourceRoot
  Copy-Item (Join-Path $RepoRoot "ExportCommand.cs") $sourceRoot
  Copy-Item (Join-Path $RepoRoot "Properties\AssemblyInfo.cs") (Join-Path $sourceRoot "Properties")
  $csprojPath=Join-Path $sourceRoot "TOCExporter.csproj"
  $framework=$target.Framework
  $apiEsc=Escape-Xml $api
  $apiUiEsc=Escape-Xml $apiUi
  $referenceAssemblies=""
  if ($framework -eq "net472" -or $framework -eq "net48") {
   $referenceAssemblies=@'
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
'@
  }
  $project=@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$framework</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <AssemblyName>TOCExporter</AssemblyName>
    <RootNamespace>TOCExporter</RootNamespace>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
    <Nullable>disable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="RevitAPI"><HintPath>$apiEsc</HintPath><Private>false</Private></Reference>
    <Reference Include="RevitAPIUI"><HintPath>$apiUiEsc</HintPath><Private>false</Private></Reference>
  </ItemGroup>
$referenceAssemblies
</Project>
"@
  Set-Content -LiteralPath $csprojPath -Value $project -Encoding UTF8
  New-Item -ItemType Directory -Path $versionOutput -Force | Out-Null
  & $dotnet build $csprojPath --configuration Release --nologo
  $exitCode=$LASTEXITCODE
  $dll=Get-ChildItem -Path (Join-Path $sourceRoot "bin") -Filter "TOCExporter.dll" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($exitCode -ne 0 -or -not $dll) {
   $reason="dotnet build failed or TOCExporter.dll was not produced (exit code $exitCode)."
   $results += [pscustomobject]@{RevitVersion=$version;Framework=$framework;Status="FAILED";Detail=$reason}
   if (-not $ContinueOnError) { throw $reason }
   continue
  }
  Copy-Item $dll.FullName (Join-Path $versionOutput "TOCExporter.dll") -Force
  $addinId="A120"+$version+"-ABCD-4ABC-8ABC-"+$version+"00000001"
  $assemblyPath=Join-Path $versionOutput "TOCExporter.dll"
  $addin=@"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>TOCExporter</Name>
    <Assembly>$assemblyPath</Assembly>
    <AddInId>$addinId</AddInId>
    <FullClassName>TOCExporter.App</FullClassName>
    <VendorId>TOC</VendorId>
    <VendorDescription>That Open Company Fragment Exporter</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
  Set-Content -LiteralPath (Join-Path $versionOutput "TOCExporter.addin") -Value $addin -Encoding UTF8
  $results += [pscustomobject]@{RevitVersion=$version;Framework=$framework;Status="BUILT";Detail=$versionOutput}
  Write-Host "Built: $versionOutput" -ForegroundColor Green
 } catch {
  if (-not ($results | Where-Object { $_.RevitVersion -eq $version -and $_.Status -eq "FAILED" })) {
   $results += [pscustomobject]@{RevitVersion=$version;Framework=$target.Framework;Status="FAILED";Detail=$_.Exception.Message}
  }
  if (-not $ContinueOnError) { throw }
 } finally {
  if (Test-Path $buildRoot) { Remove-Item $buildRoot -Recurse -Force -ErrorAction SilentlyContinue }
 }
}
$results | Format-Table -AutoSize
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$summaryPath=Join-Path $OutputRoot "Build-Summary.csv"
$results | Export-Csv -Path $summaryPath -NoTypeInformation -Encoding UTF8
Write-Host ""
Write-Host "Build summary: $summaryPath" -ForegroundColor Yellow
if ($results.Status -contains "FAILED") { exit 1 }

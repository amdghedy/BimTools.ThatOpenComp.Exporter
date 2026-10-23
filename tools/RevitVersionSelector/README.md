# Revit Version Selector (Windows EXE)

A Windows desktop launcher lets users select Revit 2020–2027 and run the repository's version-specific build script. It displays build output and the process exit code.

## Build the EXE

From the repository root on Windows, with the .NET 10 SDK installed:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Build-Version-Selector-EXE.ps1"
```

The self-contained Windows x64 EXE is written to `dist\RevitVersionSelector\RevitVersionSelector.exe`.

## Use

1. Run `RevitVersionSelector.exe`.
2. Click **Browse...** and select the cloned repository root containing `scripts\Build-All-Revit-Versions.ps1`.
3. Select the Revit version.
4. Click **Build selected version**.
5. Check the output and `00-Revit Versions\Build-Summary.csv`. A missing API is `SKIPPED`; only `BUILT` indicates that an assembly was produced.

The launcher is self-contained for its .NET runtime, but the actual build still requires the SDK for the selected Revit target, installed matching Revit API DLLs, and possible NuGet restore access. It does not bundle Autodesk API assemblies and does not guarantee runtime compatibility.

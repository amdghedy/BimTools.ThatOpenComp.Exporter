# Revit 2020 build target

- Revit API directory expected by the build script: `C:\Program Files\Autodesk\Revit 2020`
- Target framework: .NET Framework 4.7.2 (`net472`)
- Output: `00-Revit Versions/00-Revit 2020/Build Output/`

Run from the repository root: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-All-Revit-Versions.ps1 -Versions 2020`

Install Revit 2020, Visual Studio Build Tools with the .NET Framework 4.7.2 targeting pack, and Node.js. Build success depends on the matching Revit API assemblies being installed.

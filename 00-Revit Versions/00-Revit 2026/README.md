# Revit 2026 build target

- Revit API directory: `C:\Program Files\Autodesk\Revit 2026`
- Target framework: .NET 8 for Windows (`net8.0-windows`)
- Output: `00-Revit Versions/00-Revit 2026/Build Output/`

Run from the repository root: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-All-Revit-Versions.ps1 -Versions 2026`

Install Revit 2026 and a current .NET 8 SDK. The script will skip this target if the required Revit API assemblies are not found.

# Revit 2025 build target

- Revit API directory: `C:\Program Files\Autodesk\Revit 2025`
- Target framework: .NET 8 for Windows (`net8.0-windows`)
- Output: `00-Revit Versions/00-Revit 2025/Build Output/`

Run from the repository root: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-All-Revit-Versions.ps1 -Versions 2025`

Revit 2025 moved to .NET 8. Use a current .NET 8 SDK and install Revit 2025 locally; its API assemblies are not included in this repository.

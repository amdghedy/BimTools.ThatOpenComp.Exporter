# Revit 2027 build target

- Revit API directory: `C:\\Program Files\\Autodesk\\Revit 2027`
- Target framework: .NET 10 for Windows (`net10.0-windows`)
- Output: `00-Revit Versions/00-Revit 2027/Build Output/`

Run from the repository root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\\scripts\\Build-All-Revit-Versions.ps1" -Versions 2027
```

## Prerequisites

- Autodesk Revit 2027 installed, including `RevitAPI.dll` and `RevitAPIUI.dll` in the directory above.
- .NET 10 SDK. Autodesk's Revit 2027 migration guidance specifies SDK `10.0.100`.
- Node.js and the repository's npm dependencies configured for the IFC-to-Fragments converter.

The build script compiles against the installed Revit API assemblies and marks the target `SKIPPED` if they are missing. A successful compile does not by itself prove runtime compatibility; test the add-in in Revit 2027 with a copy of a representative project and linked models. The source still contains a machine-specific converter path that must be configured before export.

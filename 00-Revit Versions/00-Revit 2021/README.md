# Revit 2021 build target

- Revit API directory: `C:\Program Files\Autodesk\Revit 2021`
- Target framework: .NET Framework 4.8 (`net48`)
- Output: `00-Revit Versions/00-Revit 2021/Build Output/`

Run from the repository root: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-All-Revit-Versions.ps1 -Versions 2021`

Install Revit 2021 and the .NET Framework 4.8 targeting pack. The build script checks for the API DLLs before compiling.

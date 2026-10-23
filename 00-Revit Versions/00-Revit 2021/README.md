# Revit 2021 build target

- Revit API directory: `C:\Program Files\Autodesk\Revit 2021`
- Target framework: .NET Framework 4.8 (`net48`)
- Output: `artifacts/Revit2021/`

Run from the repository root: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-All-Revit-Versions.ps1 -Versions 2021`

Install Revit 2021 and the .NET Framework 4.8 targeting pack. The build script checks for the API DLLs before compiling.

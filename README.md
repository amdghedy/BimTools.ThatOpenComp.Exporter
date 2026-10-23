# That Open Company Revit Fragment Exporter

Convert a Revit project and its accessible linked Revit models into **That Open Company `.frag` files**, then inspect the exported models in a browser-based 3D viewer built with Three.js and That Open Company Fragments.

> **Project status:** This repository contains a Revit add-in, an IFC-to-Fragments Node.js converter, and two browser viewer entry points. Review the setup notes below before first use: the Revit command currently contains a machine-specific converter path, and the viewer entry points have different capabilities.

## What it does

- Adds an **Export to Fragments** button to the Revit ribbon under **BIM TOOLS → TOCExporter**.
- Exports the active host model to IFC 4 with base quantities and wall/column splitting enabled.
- Finds accessible, file-based Revit links, removes duplicate paths, and attempts to export each linked model separately.
- Opens each link as a temporary copy, detaching and discarding worksets on that copy so it can be processed as a primary document.
- Converts each generated IFC file to a compressed `.frag` file with `@thatopen/fragments`.
- Includes browser-based 3D viewers for opening exported `.frag` files.
- Displays export errors and warnings for linked files that could not be processed.

## Components and tools in this repository

| File / component | Purpose |
|---|---|
| `App.cs` | Registers the Revit ribbon tab, panel, and export button. |
| `ExportCommand.cs` | Main Revit export workflow: save destination, host IFC export, link discovery, temporary link copies, IFC conversion, and result messages. |
| `TOCExporter.addin` | Revit add-in manifest that tells Revit how to load the add-in. |
| `TOCExporter.csproj` / `TOCExporter.sln` | Visual Studio project and solution for building the add-in. |
| `index.js` | Command-line IFC → `.frag` converter using `IfcImporter` from `@thatopen/fragments`. |
| `package.json` / `package-lock.json` | Node.js dependencies and scripts for the converter/viewer. |
| `serve.js` | Small local Node.js static server, available at `http://localhost:3000`; serves `viewer.html` by default. |
| `viewer.html` | Viewer with multi-file selection, allowing several `.frag` files (for example, host plus links) to be loaded together. |
| `index.html` + `src/main.ts` + `src/style.css` | Vite-based viewer entry point and its TypeScript/CSS source. The current `src/main.ts` file accepts one `.frag` file at a time. |
| `vite.config.ts` | Vite development-server settings, including cross-origin isolation headers used by WebAssembly-related features. |
| `tsconfig.json` | TypeScript compiler configuration. |
| `public/favicon.svg`, `public/icons.svg`, `src/assets/` | Browser UI assets. |

## Supported options and current behavior

### Revit export

| Option / behavior | Current implementation |
|---|---|
| Output format | `.frag` |
| Output selection | Save File dialog; choose the host model's output `.frag` path |
| IFC intermediate format | IFC 4 |
| Export base quantities | Enabled |
| Wall and column splitting | Enabled |
| Host model | Exported first |
| Revit links | Attempts to discover unique, accessible file-based linked RVT paths |
| Workshared link handling | Copies each linked RVT to a temporary location and opens the copy with `DetachAndDiscardWorksets` |
| Linked model output | Separate `.frag` files written beside the chosen host output |
| File names | Derived from the Revit document or link file name; invalid filename characters are replaced |
| Family documents | Not supported; command exits with an error |
| Link failures | A warning is shown and processing continues with other links |
| Conversion failures | A partial-failure dialog reports failed output names and error text |
| Temporary data | Temporary RVT copies are deleted after each link attempt; the temporary working folder is created under the system temp directory |
| Converter path | Currently hard-coded in `ExportCommand.cs`; it must be changed for your machine (see Setup) |

The add-in does **not** currently expose a settings dialog for IFC options. IFC 4, base quantities, and wall/column splitting are hard-coded in `ExportCommand.cs`.

### Browser viewer controls

**`viewer.html` (served by `node serve.js`)**
- Select one or multiple `.frag` files in the file picker.
- Load host and linked-model fragments together.
- Automatic camera framing around the loaded models.
- Loading status and per-file error messages.
- Mouse controls: left-drag to orbit, right-drag to pan, scroll wheel to zoom.
- Grid, ambient light, directional light, and a dark scene background.

**Vite viewer (`index.html` / `src/main.ts`)**
- Select one `.frag` file at a time.
- Loads the model through That Open Company Components/Fragments.
- Shows a status message and fragment count.
- Automatically frames the camera around the loaded model.
- Uses a scene grid, ambient light, and directional light.

The viewer code currently does not provide a model tree, element/property inspector, visibility toggles, measurement tools, clipping planes, saved viewpoints, or a model-comparison interface. Those should be treated as future enhancements, not existing features.

## Requirements

### Revit add-in
- Autodesk Revit **2020**, matching the references and debug start path in the current Visual Studio project.
- Visual Studio 2022 with the **.NET Framework 4.7.2 Developer Pack/Targeting Pack**.
- Revit API assemblies available at the paths configured in `TOCExporter.csproj`:
  - `C:\Program Files\Autodesk\Revit 2020\RevitAPI.dll`
  - `C:\Program Files\Autodesk\Revit 2020\RevitAPIUI.dll`
- Node.js installed and available as `node` on PATH.
- NPM dependencies installed for the converter.

> The current project targets Revit 2020 APIs and .NET Framework 4.7.2. Other Revit versions may require API reference and compatibility changes; they are not confirmed by this repository configuration.

### Browser viewer / converter
- A current Node.js installation compatible with the package versions in `package.json`.
- NPM access for the initial dependency installation.
- A modern browser such as Chrome or Edge with WebGL support.
- Enough RAM and graphics resources for the size of the model.

## Setup

### 1. Clone the repository

```bash
git clone https://github.com/amdghedy/BimTools.ThatOpenComp.Exporter.git
cd BimTools.ThatOpenComp.Exporter
```

### 2. Install Node.js dependencies

Run this in the repository root, where `package.json` is located:

```bash
npm install
```

This installs the packages used by the IFC converter and browser viewer, including `@thatopen/components`, `@thatopen/fragments`, `three`, and `web-ifc`.

### 3. Configure the converter path in the Revit add-in

Before building, open `ExportCommand.cs` and find the `converterPath` assignment. The committed source currently points to a developer-specific path under a user profile. Change it to the full path to this repository's `index.js`, for example:

```csharp
string converterPath = @"D:\\path\\to\\BimTools.ThatOpenComp.Exporter\\index.js";
```

Replace the example with the actual folder on your computer. The converter uses packages from its own `node_modules` folder, so make sure `npm install` has been run in the directory containing `index.js`. Also ensure the Windows account running Revit can read the RVT links and write to the selected output folder.

### 4. Build the Revit add-in

1. Open `TOCExporter.sln` in Visual Studio.
2. Confirm that the Revit API references point to your installed Revit version.
3. Select **Debug** or **Release** and build the `TOCExporter` project.
4. Locate the generated `TOCExporter.dll` in the build output folder.

### 5. Configure the add-in manifest

Open `TOCExporter.addin` and update its `<Assembly>` value to the full path of the built `TOCExporter.dll`. The committed manifest also contains a developer-specific path.

For Revit 2020, copy the manifest into:

```text
%AppData%\\Autodesk\\Revit\\Addins\\2020\\
```

The DLL path inside the manifest must exist and be readable. Do not leave the sample/developer path unchanged.

### 6. Run the export

1. Start Revit 2020 and open a **project** model (not an RFA family document).
2. Confirm the required linked RVT files are accessible from your machine. Links that cannot be resolved to accessible file paths may be skipped.
3. Open **BIM TOOLS → TOCExporter → Export to Fragments**.
4. Choose the destination and filename for the host `.frag` file.
5. Allow Revit to export IFC files and the Node.js converter to generate the fragment files.
6. Check the completion dialog. The host fragment uses the filename you selected; linked model fragments are written into the same output directory.

**Important:** exporting large models can take time and create large temporary IFC files. Do not close Revit while the command is running. Ensure the output and temporary folders have enough free disk space.

## Open the exported model

### Recommended: multi-file viewer

The repository's `serve.js` serves `viewer.html`, which supports multiple fragment files.

1. Open a terminal in the repository root.
2. Run:

   ```bash
   node serve.js
   ```

3. Open [http://localhost:3000](http://localhost:3000) in Chrome or Edge.
4. Use the file picker to select one or more exported `.frag` files. Use Ctrl/Shift in the file dialog to select multiple files.
5. Orbit with left mouse drag, pan with right mouse drag, and zoom with the mouse wheel.
6. Stop the server with **Ctrl+C** in the terminal.

The server binds to `127.0.0.1`, so it is intended for local use on the same computer.

### Alternative: Vite development viewer

The repository also includes Vite scripts:

```bash
npm run dev
```

Use the local URL printed by Vite. This entry point is based on `index.html` and `src/main.ts` and currently loads one fragment at a time.

To check a production build:

```bash
npm run build
npm run preview
```

The build script runs TypeScript checking and Vite bundling. The current `serve.js` serves the repository files directly and is a separate path from Vite's development/preview workflow.

## Troubleshooting

| Symptom | Checks |
|---|---|
| Revit does not show the ribbon button | Confirm the `.addin` file is in the correct Revit-version folder, its DLL path exists, and the manifest's `FullClassName` is `TOCExporter.App`. Restart Revit after changing the manifest. |
| Build fails with missing Revit references | Update the `RevitAPI.dll` and `RevitAPIUI.dll` reference paths to your installed Revit version and install the matching .NET Framework targeting pack. |
| “Converter script not found” | Update the hard-coded `converterPath` in `ExportCommand.cs` to the actual `index.js` location. Rebuild and redeploy the DLL. |
| Node cannot be started | Install Node.js and ensure `node --version` works in a terminal and in the environment used to launch Revit. |
| Converter reports missing package/module | Run `npm install` in the directory that contains the converter's `index.js`; confirm that directory has `node_modules`. |
| A linked RVT is skipped | Confirm the link is loaded/resolved and points to a file accessible to the current user. Cloud/workshared links without a directly accessible local file path may not be processed by this implementation. |
| Some models export but others fail | Read the partial-failure dialog, check file permissions and free disk space, and test the failing RVT independently. |
| Viewer returns 404 or module errors | Start `node serve.js` from the repository root, verify dependencies exist, and open `http://localhost:3000`. |
| Model appears too large or small | Use the viewer's scroll-wheel zoom and orbit/pan controls. The current viewer automatically frames the loaded model but does not include a dedicated fit/reset button. |
| Vite build fails | Run `npm install`, then `npm run build`; review the first TypeScript/compiler error before proceeding. |

## Important implementation notes

- The Revit export command uses transactions around IFC export calls. Validate the workflow on a **copy** of a project before using it on critical production files.
- Link discovery uses the linked model's external file reference and skips paths that cannot be resolved to existing files.
- Temporary link copies are opened with worksets detached/discarded. This is intended to avoid operating on the in-memory linked document, but test this behavior with your team's worksharing standards.
- The converter path and the add-in assembly path in the checked-in manifest are machine-specific and should be made configurable before distributing the add-in to other users.
- No automated test suite or CI workflow is currently documented in the repository. Build and validate with a representative host model and linked models before production use.


## Multi-version Revit build targets (2020–2027)

Version-specific target notes are organized under **`00-Revit Versions/`**. Each folder starts with `00-` and documents its expected Revit API directory, .NET target, and build command.

| Revit version | Target framework | Default API directory |
|---|---|---|
| 2020 | .NET Framework 4.7.2 (`net472`) | `C:\\Program Files\\Autodesk\\Revit 2020` |
| 2021 | .NET Framework 4.8 (`net48`) | `C:\\Program Files\\Autodesk\\Revit 2021` |
| 2022 | .NET Framework 4.8 (`net48`) | `C:\\Program Files\\Autodesk\\Revit 2022` |
| 2023 | .NET Framework 4.8 (`net48`) | `C:\\Program Files\\Autodesk\\Revit 2023` |
| 2024 | .NET Framework 4.8 (`net48`) | `C:\\Program Files\\Autodesk\\Revit 2024` |
| 2025 | .NET 8 for Windows (`net8.0-windows`) | `C:\\Program Files\\Autodesk\\Revit 2025` |
| 2026 | .NET 8 for Windows (`net8.0-windows`) | `C:\\Program Files\\Autodesk\\Revit 2026` |
| 2027 | .NET 10 for Windows (`net10.0-windows`) | `C:\\Program Files\\Autodesk\\Revit 2027` |

### Build every installed version

Use Windows PowerShell from a clone of this repository:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\\scripts\\Build-All-Revit-Versions.ps1 -ContinueOnError
```

Build one target only:

```powershell
.\\scripts\\Build-All-Revit-Versions.ps1 -Versions 2024
```

If Revit is installed in a non-default location, provide its common parent directory:

```powershell
.\\scripts\\Build-All-Revit-Versions.ps1 -RevitInstallRoot "D:\\Autodesk" -ContinueOnError
```

The script checks for that version's `RevitAPI.dll` and `RevitAPIUI.dll`, generates a temporary SDK-style project using the appropriate framework, builds the add-in, and writes version-specific outputs under `00-Revit Versions/00-Revit 2020/Build Output` through `00-Revit Versions/00-Revit 2027/Build Output`. Successful outputs include `TOCExporter.dll` and a generated `TOCExporter.addin` manifest. It also writes `00-Revit Versions/Build-Summary.csv`; versions whose API assemblies are missing are marked **SKIPPED** rather than falsely reported as built.

**Prerequisites:** install each Revit version you intend to build against, the .NET SDK, and (for Revit 2020–2024) the .NET Framework reference assemblies/targeting packs. The script may need internet access to restore the reference-assemblies NuGet package. Revit 2025 and 2026 require a .NET 8 SDK; Revit 2027 requires the .NET 10 SDK (Autodesk documents SDK 10.0.100 for Revit 2027). Revit API DLLs are not bundled in this repository.

**Important:** this is a multi-version build scaffold, not proof that every version compiles or runs. The original exporter code was authored against Revit 2020 API references; API changes, runtime behavior, and IFC export behavior must be verified separately in each Revit version. The generated manifests point to the build output location, so copy/update the manifest and assembly paths for your final deployment location. The exporter also still requires the converter path in `ExportCommand.cs` to be configured for your machine.


## Repository

- **Source:** https://github.com/amdghedy/BimTools.ThatOpenComp.Exporter
- **Issues / feature requests:** use the repository's GitHub Issues.

## License

No license file was present in the repository when this README was updated. Unless a license is added, do not assume that the source is licensed for unrestricted redistribution or commercial reuse.


## Windows GUI: choose a Revit version

A Windows x64 desktop selector is included under `tools/RevitVersionSelector/`. It lets users choose Revit 2020–2027, select their local repository folder, and run the version-specific build script with live output.

Build the single-file, self-contained launcher on Windows with the .NET 10 SDK:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\scripts\Build-Version-Selector-EXE.ps1"
```

The executable is created at `dist\RevitVersionSelector\RevitVersionSelector.exe`. This repository stores the source and publishing script; the EXE is not yet a checked-in or prebuilt binary. The launcher does not bundle Revit API assemblies or the SDK needed to compile the selected add-in. See [the selector README](tools/RevitVersionSelector/README.md).

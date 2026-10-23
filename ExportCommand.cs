using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace TOCExporter
{
    [Transaction(TransactionMode.Manual)]
    public class ExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument    uidoc = uiapp.ActiveUIDocument;
            Document      doc   = uidoc.Document;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;

            if (doc.IsFamilyDocument)
            {
                TaskDialog.Show("Error", "Cannot export a family document.");
                return Result.Failed;
            }

            try
            {
                // ── 1. Ask user where to save the host .frag output ──────────
                string docName = doc.Title;
                if (docName.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
                    docName = docName.Substring(0, docName.Length - 4);

                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter   = "Fragment Files|*.frag";
                saveDialog.Title    = "Save Fragment File";
                saveDialog.FileName = docName + ".frag";

                if (saveDialog.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                string outputFragPath = saveDialog.FileName;
                string outputDir      = Path.GetDirectoryName(outputFragPath);

                // Temp folder for all IFC files
                string tempFolder = Path.Combine(
                    Path.GetTempPath(), "TOCExport_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempFolder);

                // ── 2. Build shared IFC options ───────────────────────────────
                IFCExportOptions ifcOptions = new IFCExportOptions();
                ifcOptions.FileVersion            = IFCVersion.IFC4;
                ifcOptions.WallAndColumnSplitting = true;
                ifcOptions.ExportBaseQuantities   = true;

                // ── 3. Export HOST document (Transaction is valid here) ────────
                var ifcFiles = new List<string>();
                string hostIfcName = MakeSafeFileName(docName) + ".ifc";
                string hostIfcPath = Path.Combine(tempFolder, hostIfcName);

                using (Transaction tx = new Transaction(doc, "TOC Export Host IFC"))
                {
                    tx.Start();
                    doc.Export(tempFolder, hostIfcName, ifcOptions);
                    tx.Commit();
                }

                if (File.Exists(hostIfcPath))
                    ifcFiles.Add(hostIfcPath);

                // ── 4. Collect linked RVT file paths ──────────────────────────
                // Linked docs are READ-ONLY — we MUST NOT open a Transaction on them.
                // Instead, we re-open each linked file as a standalone primary document.
                var linkedRvtPaths = new List<string>();

                FilteredElementCollector linkCollector =
                    new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance));

                foreach (RevitLinkInstance linkInst in linkCollector)
                {
                    try
                    {
                        // Get the RevitLinkType to obtain the external file reference
                        RevitLinkType linkType =
                            doc.GetElement(linkInst.GetTypeId()) as RevitLinkType;

                        if (linkType == null) continue;

                        ExternalFileReference extRef = linkType.GetExternalFileReference();
                        if (extRef == null) continue;

                        // Convert ModelPath to a user-visible file path string
                        ModelPath modelPath    = extRef.GetAbsolutePath();
                        string    rvtFilePath  =
                            ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);

                        if (!File.Exists(rvtFilePath)) continue;
                        if (linkedRvtPaths.Contains(rvtFilePath)) continue; // avoid duplicates

                        linkedRvtPaths.Add(rvtFilePath);
                    }
                    catch
                    {
                        // If the link path cannot be resolved, skip it silently
                    }
                }

                // ── 5. Open each linked RVT as a COPY so Revit treats it as
                //       a fresh primary document (not the in-memory linked one) ──
                foreach (string rvtPath in linkedRvtPaths)
                {
                    Document linkedDoc    = null;
                    string   tempCopyPath = null;

                    try
                    {
                        // Copy the linked RVT to a temp path.
                        // Revit has no in-memory record of the copy as a linked doc,
                        // so it opens as a TRUE primary document → Transactions work.
                        string copyName = MakeSafeFileName(
                            Path.GetFileNameWithoutExtension(rvtPath)) + "_tocopy.rvt";
                        tempCopyPath = Path.Combine(tempFolder, copyName);
                        File.Copy(rvtPath, tempCopyPath, overwrite: true);

                        OpenOptions openOpts = new OpenOptions();
                        // DetachAndDiscardWorksets handles both workshared and local files
                        openOpts.DetachFromCentralOption =
                            DetachFromCentralOption.DetachAndDiscardWorksets;

                        ModelPath copyModelPath =
                            ModelPathUtils.ConvertUserVisiblePathToModelPath(tempCopyPath);

                        linkedDoc = app.OpenDocumentFile(copyModelPath, openOpts);

                        string linkBaseName = MakeSafeFileName(
                            Path.GetFileNameWithoutExtension(rvtPath));
                        string linkIfcName  = linkBaseName + ".ifc";
                        string linkIfcPath  = Path.Combine(tempFolder, linkIfcName);

                        // Transaction is now valid — copy is a fresh primary document
                        using (Transaction tx = new Transaction(linkedDoc, "TOC Export Link IFC"))
                        {
                            tx.Start();
                            linkedDoc.Export(tempFolder, linkIfcName, ifcOptions);
                            tx.Commit();
                        }

                        if (File.Exists(linkIfcPath))
                            ifcFiles.Add(linkIfcPath);
                    }
                    catch (Exception linkEx)
                    {
                        TaskDialog.Show("Warning",
                            $"Could not export linked file:\n{rvtPath}\n\nReason: {linkEx.Message}\n\nSkipping.");
                    }
                    finally
                    {
                        // Close the temp copy (do NOT save changes)
                        if (linkedDoc != null)
                            try { linkedDoc.Close(false); } catch { }

                        // Clean up the temporary RVT copy
                        if (tempCopyPath != null && File.Exists(tempCopyPath))
                            try { File.Delete(tempCopyPath); } catch { }
                    }
                }

                if (ifcFiles.Count == 0)
                {
                    TaskDialog.Show("Error", "No IFC files were exported.");
                    return Result.Failed;
                }

                // ── 6. Verify converter script ────────────────────────────────
                string converterPath =
                    @"C:\Users\abdelrehem.mohamed\.gemini\antigravity\scratch\TOC_RevitExporter\Converter\index.js";

                if (!File.Exists(converterPath))
                {
                    TaskDialog.Show("Error", "Converter script not found at:\n" + converterPath);
                    return Result.Failed;
                }

                // ── 7. Run Node.js converter for every IFC file ───────────────
                bool   anyFailed = false;
                string failLog   = "";

                for (int i = 0; i < ifcFiles.Count; i++)
                {
                    string ifcPath  = ifcFiles[i];
                    string fragName = Path.GetFileNameWithoutExtension(ifcPath) + ".frag";
                    string fragDest = (i == 0) ? outputFragPath
                                               : Path.Combine(outputDir, fragName);

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName               = "node";
                    psi.Arguments              = $"\"{converterPath}\" \"{ifcPath}\" \"{fragDest}\"";
                    psi.UseShellExecute        = false;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError  = true;
                    psi.CreateNoWindow         = true;

                    using (Process p = Process.Start(psi))
                    {
                        p.WaitForExit();
                        string err = p.StandardError.ReadToEnd();
                        if (p.ExitCode != 0)
                        {
                            anyFailed = true;
                            failLog  += $"\n[{fragName}]\n{err}\n";
                        }
                    }
                }

                // ── 8. Result ─────────────────────────────────────────────────
                if (anyFailed)
                    TaskDialog.Show("Partial Failure", "Some files failed:\n" + failLog);
                else
                    TaskDialog.Show("Success",
                        $"Export complete!\n\n" +
                        $"Host  → {outputFragPath}\n" +
                        $"Links → {linkedRvtPaths.Count} file(s) saved to:\n{outputDir}\n\n" +
                        "Load each .frag in the web viewer.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}

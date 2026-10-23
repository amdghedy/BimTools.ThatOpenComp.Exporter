using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace TOCExporter
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Create a custom ribbon tab
            string tabName = "BIM TOOLS";
            try 
            {
                application.CreateRibbonTab(tabName);
            } 
            catch 
            {
                // Tab might already exist
            }

            // Add a new ribbon panel
            RibbonPanel ribbonPanel = application.CreateRibbonPanel(tabName, "TOCExporter");

            // Create a push button to trigger the command
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("cmdTOCExport",
                "Export to\nFragments", thisAssemblyPath, "TOCExporter.ExportCommand");

            PushButton pushButton = ribbonPanel.AddItem(buttonData) as PushButton;
            pushButton.ToolTip = "Exports the active document to That Open Company Fragments format.";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

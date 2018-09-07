using Autodesk.Revit.UI;

namespace Revit2WebGlExporter
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class RevitExternalApplication : IExternalApplication
    {
        private ExportEventForm exportEventForm;

        public Autodesk.Revit.UI.Result OnStartup(Autodesk.Revit.UI.UIControlledApplication application)
        {
            ExportEventHandler handle = new ExportEventHandler();
            ExternalEvent extEvent = ExternalEvent.Create(handle);
            exportEventForm = new ExportEventForm(extEvent);
            exportEventForm.Show();
            exportEventForm.Visible = false;

            Log.Initial();
            Log.WriteLog("-------------------Revit2WebGlExporter加载成功-------------------");

            return Result.Succeeded;
        }

        public Autodesk.Revit.UI.Result OnShutdown(Autodesk.Revit.UI.UIControlledApplication application)
        {
            exportEventForm.Close();

            Log.WriteLog("----------------------------Revit关闭----------------------------");

            return Result.Succeeded;
        }
    }
}

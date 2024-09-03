using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Project1.Revit.FbxNwcExportor {
  [Transaction(TransactionMode.Manual)]
  public class CmdNavisworksExport : IExternalCommand {
    public Result Execute(ExternalCommandData commandData,
        ref string message, ElementSet elements) {
      var uidoc = commandData.Application.ActiveUIDocument;

      var view = new NavisworksExportView();
      view.Owner = App.Current.MainWindow;
      view.VM.GetDocument(uidoc);
      view.ShowDialog();

      return Result.Succeeded;
    }
  }
}

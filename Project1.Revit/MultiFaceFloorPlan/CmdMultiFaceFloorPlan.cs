using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Project1.Revit.MultiFaceFloorPlan {
  [Transaction(TransactionMode.Manual)]
  internal class CmdMultiFaceFloorPlan : IExternalCommand {
    public Result Execute(ExternalCommandData commandData,
   ref string message, ElementSet elements) {
      var uiapp = commandData.Application;

      var view = new MultiFaceFloorPlanView {
        Owner = App.Current.MainWindow
      };
      view.VM.FamilyQc(uiapp);
      view.ShowDialog();

      return Result.Succeeded;
    }
  }
}

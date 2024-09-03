using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Project1.Revit.AlignPipe {
  [Transaction(TransactionMode.Manual)]
  class CmdAlignPipe : IExternalCommand {
    public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements) {
      var uiDoc = commandData.Application.ActiveUIDocument;
      var viewType = uiDoc.ActiveView.ViewType;

      if (IsUsableView(viewType)) {
        TaskDialog.Show("알림", "현재 뷰에서 사용 불가능합니다.");
        return Result.Failed;
      }

      var view = new AlignPipeOptionView();
      view.Owner = App.Current.MainWindow;
      view.VM.Initialize(uiDoc);
      view.ShowDialog();
      return Result.Succeeded;
    }

    private bool IsUsableView(ViewType viewType) {
      if (viewType == ViewType.FloorPlan) { return false; }
      if (viewType == ViewType.CeilingPlan) { return false; }
      if (viewType == ViewType.EngineeringPlan) { return false; }
      if (viewType == ViewType.AreaPlan) { return false; }
      if (viewType == ViewType.Section) { return false; }
      if (viewType == ViewType.Detail) { return false; }
      if (viewType == ViewType.Elevation) { return false; }
      return true;
    }
  }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Project1.Revit.IsoPipeDimension {
  [Transaction(TransactionMode.Manual)]
  class CmdPipeDimension : IExternalCommand {
    public Result Execute(ExternalCommandData commandData,
        ref string message, ElementSet elements) {
      var uiDoc = commandData.Application?.ActiveUIDocument;
      if (uiDoc == null) { return Result.Failed; }

      if (uiDoc.ActiveGraphicalView is View3D == false) {
        TaskDialog.Show("알림", "3D뷰에서만 사용 가능합니다.");
        return Result.Failed;
      }

      var view = new IsoNameView();

      var canLock = view.VM.CanLockView(commandData.Application,
          out var isContinue);
      if (!isContinue) {
        TaskDialog.Show("알림", "치수 입력할 배관이 없습니다.");
        return Result.Cancelled;
      }
      if (canLock) {
        view.VM.Initialize();
      } else {
        view.Owner = App.Current.MainWindow;
        view.ShowDialog();
      }
      return Result.Succeeded;
    }
  }
}

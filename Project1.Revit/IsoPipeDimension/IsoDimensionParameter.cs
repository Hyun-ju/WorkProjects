using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using static Project1.Revit.Common.ParameterUtils;

namespace Project1.Revit.IsoPipeDimension {
  public class IsoDimensionParameter {
    public static readonly string TextNoteIdParam = "Dimension_TextNote_ID";
    public static readonly string TargetPipeIdParam = "Target_Pipe_ID";

    /// <summary>
    /// Iso 작성 텍스트와 관련된 파라미터가 있는지 확인 후 없으면 생성
    /// </summary>
    /// <param name="uiApp"></param>
    /// <param name="pipe"></param>
    /// <param name="fitting"></param>
    public static void CheckIsoParamter(UIApplication uiApp, Element pipe, Element fitting) {
      var doc = uiApp.ActiveUIDocument.Document;

      var pipeParam = pipe?.GetParameter(TextNoteIdParam);
      var fittingParam = fitting?.GetParameter(TextNoteIdParam);

      if (pipeParam != null && fittingParam != null) { return; }

      if (pipeParam == null && fittingParam == null) {
        CreateParameter(uiApp);
      }
      else {
        // Pipe/Elbow 둘 중 하나에 파라미터가 존재시
        // 파라미터에 카테고리 추가
        var tempParam = pipeParam != null ? pipeParam : fittingParam;
        CategorySet catSet = new CategorySet();
        catSet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
        catSet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeFitting));

        InsertCategoryInParameter(uiApp, tempParam.Definition, catSet);
      }
    }

    private static void CreateParameter(UIApplication uiApp) {
      var app = uiApp.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      CategorySet catSet = new CategorySet();
      catSet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));
      catSet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeFitting));

      var paramOption = new CreateParameterOption();
      paramOption.ParameterType = ParameterType.Integer;
      paramOption.IsVisible = false;
      paramOption.IsUserModifiable = false;
      paramOption.ParameterGroup = BuiltInParameterGroup.PG_IDENTITY_DATA;
      CreateParameters(uiApp, catSet, paramOption, TextNoteIdParam, TargetPipeIdParam);

      var bmItr = doc.ParameterBindings.ForwardIterator();
      while (bmItr.MoveNext()) {
        if (bmItr.Key.Name != TargetPipeIdParam) { continue; }

        catSet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_TextNotes));
        InsertCategoryInParameter(uiApp, bmItr.Key, catSet);
        break;
      }
    }
  }
}

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Project1.Revit.Common;
using System;
using System.IO;
using System.Linq;
using static Project1.Revit.Common.ParameterUtils;

namespace Project1.Revit.PipeLength {
  [Transaction(TransactionMode.Manual)]
  class CmdSumBending : IExternalCommand {
    UIDocument _UiDoc = null;
    Document _Doc = null;

    private static readonly string _SchduleName = "배관 총 길이";
    private static readonly string _ParamName
          = ProjectParameterList.TKDefinitionNames.SumBendingLength;


    public Result Execute(ExternalCommandData commandData,
            ref string message, ElementSet elements) {
      Initialize(commandData.Application);
      return Result.Succeeded;
    }

    public void Initialize(UIApplication uiApp) {
      _UiDoc = uiApp.ActiveUIDocument;
      _Doc = _UiDoc.Document;

      var app = uiApp.Application;

      // 배관 및 벤딩 엘보가 있는지 확인
      var checkElem = _Doc.TypeElements(typeof(Pipe)).FirstOrDefault();
      if (checkElem == null) {
        var fittingCol = _Doc.CategoryElements(BuiltInCategory.OST_PipeFitting);
        foreach (var fitting in fittingCol) {
          var param = fitting.GetParameter(_ParamName);
          if (param == null) { continue; }
          checkElem = fitting;
        }
      }

      if (checkElem == null) {
        TaskDialog.Show("알림", "합산할 배관과 벤딩 엘보가 없습니다.");
        return;
      }
      View view = null;
      using (var tx = new Transaction(_Doc,
           "배관 총 길이 일람표 생성")) {
        try {
          tx.Start();

          // 파라미터 생성 및 값 설정
          if (checkElem.GetParameter(_ParamName) == null) {

            // Project_Length 파라미터 생성할 카테고리 설정
            var catSet = app.Create.NewCategorySet();
            var pipeCate = Category.GetCategory(_Doc, BuiltInCategory.OST_PipeCurves);
            var fittCate = Category.GetCategory(_Doc, BuiltInCategory.OST_PipeFitting);
            catSet.Insert(pipeCate);
            catSet.Insert(fittCate);

            var paramOption = new CreateParameterOption();
            paramOption.ParameterType = ParameterType.Length;
            paramOption.IsVisible = false;
            paramOption.IsUserModifiable = false;
            paramOption.ParameterGroup = BuiltInParameterGroup.PG_GEOMETRY;
            CreateParameters(uiApp, catSet, paramOption, _ParamName);
          }
          SetParameterValue();

          // 동일 이름의 일람표 있는지 확인
          var schedules = _Doc.CategoryElements(BuiltInCategory.OST_Schedules);
          foreach (var sche in schedules) {
            if (sche.Name == _SchduleName) {
              TaskDialog.Show("알림", "길이 업데이트 완료");
              tx.Commit();
              return;
            }
          }

          // 일람표 생성
          var result = CreateSchedule(checkElem, out var schdule);
          view = schdule;
          if (result) {
            TaskDialog.Show("알림", "일람표 생성 성공");
          }
          else {
            TaskDialog.Show("알림", "일람표 생성 실패");
          }
          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", tx.GetName());
        }
      }
      if (view != null) {
        _UiDoc.ActiveView = view;
      }
    }

    private bool CreateSchedule(Element elem, out View view) {
      view = null;
      var typeParam = elem.GetParameter(
          ProjectParameterList.DefinitionNames.Material);
      var sizeParam = elem.GetParameter(
          ProjectParameterList.DefinitionNames.Size);
      var lengthParam = elem.GetParameter(_ParamName);
      if (typeParam == null || sizeParam == null || lengthParam == null) {
        // 특정 파라미터가 없을 시 실행 불가
        return false;
      }

      var schedule = ViewSchedule.CreateSchedule(_Doc,
          ElementId.InvalidElementId);
      schedule.Name = _SchduleName;

      // 일람표에 열 추가
      var fieldIds = schedule.Definition.GetSchedulableFields();
      SchedulableField typeAbleField = null;
      SchedulableField sizeAbleField = null;
      SchedulableField lengthAbleField = null;
      foreach (var fieldId in fieldIds) {
        var field = fieldId.ParameterId.IntegerValue;
        if (typeParam.Id == fieldId.ParameterId) {
          typeAbleField = fieldId;
        }
        else if (lengthParam.Id == fieldId.ParameterId) {
          lengthAbleField = fieldId;
        }
        else if (sizeParam.Id == fieldId.ParameterId) {
          sizeAbleField = fieldId;
        }
      }

      var typeField = schedule.Definition.AddField(typeAbleField);
      var sizeField = schedule.Definition.AddField(sizeAbleField);

      var lengthField = schedule.Definition.AddField(lengthAbleField);
      lengthField.DisplayType = ScheduleFieldDisplayType.Totals;

      // 정렬
      schedule.Definition.AddSortGroupField(
          new ScheduleSortGroupField(typeField.FieldId));
      schedule.Definition.AddSortGroupField(
          new ScheduleSortGroupField(sizeField.FieldId));
      schedule.Definition.IsItemized = false; // 그룹화

      // 필터
      ScheduleFilter filter = new ScheduleFilter(lengthField.FieldId,
          ScheduleFilterType.HasValue);
      schedule.Definition.AddFilter(filter);

      view = schedule;
      return true;
    }

    private void SetParameterValue() {
      var elements = _Doc.TypeElements(typeof(Pipe)).ToList();
      var fittings = _Doc.CategoryElements(BuiltInCategory.OST_PipeFitting);
      elements.AddRange(fittings.ToList());

      SumPipeLengthHelper.UpdateParameter(elements);
    }
  }
}

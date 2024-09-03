using Autodesk.Revit.DB;
using Project1.Revit.Common;
using System;
using System.Collections.Generic;

namespace Project1.Revit.PipeLength {
  public class LengthParameterUpdater : IUpdater {
    // 파이프 길이에 변화 생길 시
    UpdaterId _UpdaterId;
    private static readonly string _ParamName = ProjectParameterList.TKDefinitionNames.SumBendingLength;


    public LengthParameterUpdater(AddInId id) {
      var guid = new Guid("C2CC4C08-0E62-481E-AEB4-5A7A565D7380");
      _UpdaterId = new UpdaterId(id, guid);
    }

    public void Execute(UpdaterData data) {
      var addElems = data.GetAddedElementIds();
      var modElems = data.GetModifiedElementIds();

      if (addElems.Count == 0 && modElems.Count == 0) { return; }
      var elemIds = new List<ElementId>();
      elemIds.AddRange(addElems);
      elemIds.AddRange(modElems);

      var doc = data.GetDocument();

      var elems = new List<Element>();
      foreach (var elemId in elemIds) {
        var elem = doc.GetElement(elemId);

        var param = elem.GetParameter(_ParamName);
        if (param == null) { return; }
        elems.Add(elem);
      }
      if (elems.Count == 0) { return; }

      using (var tx = new SubTransaction(doc)) {
        try {
          tx.Start();

          SumPipeLengthHelper.UpdateParameter(elems);

          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", "Length Updateer");
        }
      }
    }

    public string GetAdditionalInformation() {
      return "배관 및 벤딩 엘보에 변화가 생길 시 길이 파라미터를 업데이트 합니다.";
    }

    public ChangePriority GetChangePriority() {
      return ChangePriority.FreeStandingComponents;
    }

    public UpdaterId GetUpdaterId() {
      return _UpdaterId;
    }

    public string GetUpdaterName() {
      return typeof(LengthParameterUpdater).Name;
    }
  }
}

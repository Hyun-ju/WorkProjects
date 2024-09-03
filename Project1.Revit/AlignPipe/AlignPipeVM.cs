using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Project1.Revit.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Project1.Revit.AlignPipe {
  class AlignPipeVM : ViewModelBase {
    UIDocument _UiDoc = null;

    private bool _IsCenterBasis;
    private string _Distance;

    public bool IsCenterBasis {
      get { return _IsCenterBasis; }
      set { Set(ref _IsCenterBasis, value); }
    }
    public string Distance {
      get { return _Distance; }
      set { Set(ref _Distance, value); }
    }
    public RelayCommand<Window> CmdApply { get; set; }


    public AlignPipeVM() {
      _IsCenterBasis = true;
      CmdApply = new RelayCommand<Window>(Apply);
    }


    public void Initialize(UIDocument uiDoc) {
      _UiDoc = uiDoc;
    }

    private void Apply(Window window) {
      window.Close();

      if (_UiDoc.Document.ActiveView is ViewSchedule) {
        return;
      }

      var targetElems = SelectTargetPipes();
      if (targetElems == null) { return; }
      if (targetElems.Count == 1) {
        TaskDialog.Show("알림", "간격 조정할 배관이 없습니다");
        return;
      }
      var basisElem = SelectBasisPipe(targetElems);
      if (basisElem == null) { return; }

      var unitTypeId = basisElem.get_Parameter(
          BuiltInParameter.CURVE_ELEM_LENGTH).GetUnitTypeId();
      double.TryParse(Distance, out var distance);
      distance = UnitUtils.ConvertToInternalUnits(distance, unitTypeId);
      AdjustPipeSpacing(targetElems, basisElem, distance);
    }

    private IList<Element> SelectTargetPipes() {
      var elems = new List<Element>();
      var doc = _UiDoc.Document;

      var preSelect = _UiDoc.Selection.GetElementIds();
      if (preSelect.Count > 1) {
        foreach (var elemId in preSelect) {
          var elem = doc.GetElement(elemId);
          if (elem is Pipe) { elems.Add(elem); }
        }
      } else {
        try {
          var selectFilter = new Filters.PipeSelectionFilter();
          var refes = _UiDoc.Selection.PickObjects(ObjectType.Element,
              selectFilter, "간격 조정할 배관들을 선택하세요.");
          if (refes.Count == 0) { return null; }
          foreach (var refe in refes) {
            elems.Add(doc.GetElement(refe));
          }
        } catch {
          return null;
        }
      }
      return elems;
    }

    private Element SelectBasisPipe(IList<Element> elems) {
      var doc = _UiDoc.Document;
      try {
        var selectFilter = new BasisPipeSelectionFilter(elems);
        var refe = _UiDoc.Selection.PickObject(ObjectType.Element,
            selectFilter, "기준 배관을 선택하세요.");
        if (refe == null) { return null; }
        return doc.GetElement(refe);
      } catch {
        return null;
      }
    }

    private void AdjustPipeSpacing(IList<Element> tarElems, Element srcElem,
          double distance) {
      var doc = _UiDoc.Document;
      var view = doc.ActiveView;
      var viewDir = view.ViewDirection;

      Line srcLine = null;
      if (srcElem.GetLocationCurve() is Line line) {
        srcLine = line;
      }
      if (srcLine == null) { return; }
      XYZ vector = srcLine.Direction.Normalize();
      XYZ srcOrigin = srcLine.Origin;

      // 기준 배관 벡터의 cross 벡터
      var cross = vector.CrossProduct(viewDir).Normalize();

      var elemsDic = new Dictionary<Element, XYZ>();
      for (int i = 0; i < tarElems.Count; i++) {
        var pipe = tarElems[i];
        if (pipe.GetLocationCurve() is Line tarLine) {
          var tarVec = tarLine.Direction.Normalize();

          // 기준 배관 벡터와 같은지 확인
          if (!tarVec.IsSameDirection(vector, 1e-2)) {
            TaskDialog.Show("알림",
                "선택한 배관들 중 기준 배관과 방향이 일치하지 않는 배관이 있습니다.");
            _UiDoc.RefreshActiveView();
            return;
          }

          elemsDic.Add(pipe, tarLine.Origin);
        }
      }

      // DotProduct를 통해 순서 정렬
      var temp = elemsDic.OrderBy(e => cross.DotProduct(e.Value - srcOrigin));
      var elemsList = new List<(Element element, XYZ xyz)>();
      int basisInx = -1;
      int tempInx = -1;
      foreach (var item in temp) {
        elemsList.Add((item.Key, item.Value));
        tempInx++;
        if (item.Key.Id == srcElem.Id) { basisInx = tempInx; }
      }

      using (var tx = new Transaction(doc,
           "배관 간격 일괄 조정")) {
        try {
          tx.Start();

          for (int i = 0; i < elemsList.Count; i++) {
            var pipeKvp = elemsList[i];
            var pipe = pipeKvp.Item1;

            if (i == basisInx) { continue; }
            double outDia = 0;

            if (!IsCenterBasis) {
              // 외경 기준일 경우 반지름 및 지름 합산
              int startInd = 0;
              int endInd = 0;
              if (i < basisInx) {
                startInd = i;
                endInd = basisInx;
              } else {
                startInd = basisInx;
                endInd = i;
              }

              for (int j = startInd; j <= endInd; j++) {
                var tempPipe = elemsList[j].element;
                if (j == startInd || j == endInd) {
                  // 기준 배관 및 현재 타겟 배관 일 경우 반지름 합산
                  var diameter = tempPipe.get_Parameter(
                      BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                  outDia += diameter.AsDouble() / 2;
                } else {
                  // 그 외 배관 일 경우 지름 합산
                  var dia = tempPipe.get_Parameter(
                      BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                  outDia += dia.AsDouble();
                }
              }
              outDia /= Math.Abs(basisInx - i);
            }

            // 기준 배관의 cross벡터를 옮겨질 거리만큼 곱하여 배관의 origin 설정
            var moveDis = cross * (distance + outDia) * (i - basisInx);
            var movePoint = srcOrigin + moveDis;

            // 배관의 선에 기준 배관 origin의 직교하는 점 (translation 구하기 위함)
            var unboundLine = (Line)pipeKvp.element.GetLocationCurve();
            unboundLine.MakeUnbound();
            var pnt = unboundLine.Project(srcOrigin).XYZPoint;
            var moveOrigin = pnt;

            var dif2Pnt = moveOrigin - srcOrigin;
            // 같은 vector이지만, 높이가 다를때 (뷰의 기준에 따라)
            if (!viewDir.X.IsAlmostEqualTo(0)) {
              movePoint = movePoint.Add(new XYZ(dif2Pnt.X, 0, 0));
            }
            if (!viewDir.Y.IsAlmostEqualTo(0)) {
              movePoint = movePoint.Add(new XYZ(0, dif2Pnt.Y, 0));
            }
            if (!viewDir.Z.IsAlmostEqualTo(0)) {
              movePoint = movePoint.Add(new XYZ(0, 0, dif2Pnt.Z));
            }

            ElementTransformUtils.MoveElement(doc, pipe.Id, movePoint - moveOrigin);
          }

          tx.Commit();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", tx.GetName());
        }
      }
      _UiDoc.RefreshActiveView();
    }
  }
}

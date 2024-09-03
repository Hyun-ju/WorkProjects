using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Project1.Revit.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Project1.Revit.IsoPipeDimension {
  class PipeDimensionVM : ViewModelBase {
    public class ElbowToFittingInfo {
      public Element Elbow { get; set; }
      public Element ConnFitting { get; set; }


      public ElbowToFittingInfo(Element elbow, Element connFitting) {
        Elbow = elbow;
        ConnFitting = connFitting;
      }
    }

    UIApplication _UiApp = null;
    UIDocument _UiDoc = null;
    Document _Doc = null;

    private string _TextNoteIdParam;
    private string _TargetPipeIdParam;
    private string _ViewName = string.Empty;
    private bool _IsCreated = false;
    private IList<Element> _Pipes = new List<Element>();
    private IList<ElbowToFittingInfo> _ElbowToFittingInfos =
        new List<ElbowToFittingInfo>();

    public string ViewName {
      get { return _ViewName; }
      set { Set(ref _ViewName, value); }
    }
    public RelayCommand<Window> CmdApply { get; set; }


    public PipeDimensionVM() {
      CmdApply = new RelayCommand<Window>(Apply);

      _TextNoteIdParam = IsoDimensionParameter.TextNoteIdParam;
      _TargetPipeIdParam = IsoDimensionParameter.TargetPipeIdParam;
    }


    public bool CanLockView(UIApplication uiApp, out bool isContinue) {
      _UiApp = uiApp;
      _UiDoc = uiApp.ActiveUIDocument;
      _Doc = _UiDoc.Document;

      _Pipes = GetAllPipes();
      _ElbowToFittingInfos = GetAllElbowToFittingInfos();

      if (_Pipes.Count < 1 && _ElbowToFittingInfos.Count < 1) {
        isContinue = false;
      }
      else {
        isContinue = true;
      }

      var view = (View3D)_UiDoc.ActiveGraphicalView;
      return view.CanSaveOrientation();
    }

    public void Apply(Window window) {
      if (string.IsNullOrEmpty(ViewName) || string.IsNullOrWhiteSpace(ViewName)) {
        TaskDialog.Show("알림", "뷰 이름을 입력해주세요");
        return;
      }
      if (!NamingUtils.IsValidName(ViewName)) {
        TaskDialog.Show("알림",
            "사용 불가능한 특수문자가 포함되어 있습니다.");
        return;
      }
      ViewName = ViewName.Trim();
      var doc = _Doc;
      var viewCol = doc.TypeElements(typeof(View3D));
      foreach (var view in viewCol) {
        if (view.Name.Equals(ViewName)) {
          TaskDialog.Show("알림", "같은 3D 뷰 이름이 존재합니다");
          return;
        }
      }
      _IsCreated = true;
      Initialize();

      window.Close();
    }

    public void Initialize() {
      var doc = _Doc;

      View3D view3D = null;
      if (_UiDoc.ActiveGraphicalView is View3D view) {
        view3D = view;
      }

      var lengthParam = _Pipes.First().get_Parameter(
          BuiltInParameter.CURVE_ELEM_LENGTH);
      var unitTypeId = lengthParam.GetUnitTypeId();

      using (var tx = new Transaction(doc,
           "ISO 치수 입력")) {
        try {
          tx.Start();

          if (_IsCreated) {
            // 새로 생성해야 할 경우
            var viewId = _UiDoc.ActiveGraphicalView.Duplicate(
                ViewDuplicateOption.WithDetailing);
            view3D = (View3D)doc.GetElement(viewId);
            view3D.Name = ViewName;
            view3D.Scale = 10;
          }
          if (!view3D.IsLocked) {
            // 뷰가 잠겨있지 않을 경우 잠금
            view3D.SaveOrientationAndLock();
          }

          var textParams = _Pipes.FirstOrDefault().GetParameters(_TextNoteIdParam);
          if (textParams.Count > 1) {
            foreach (var item in textParams) {
              var bindings = doc.ParameterBindings;
              if (item.IsReadOnly) {
                bindings.Remove(item.Definition);
              }
              if (!item.HasValue) {
                bindings.Remove(item.Definition);
              }
            }
          }
          doc.Regenerate();

          IsoDimensionParameter.CheckIsoParamter(_UiApp,
              _Pipes.FirstOrDefault(), _ElbowToFittingInfos.FirstOrDefault()?.Elbow);

          //RemoveTextNoteNotInView(view3D);
          CreatePipeDimension(view3D, _Pipes, unitTypeId);
          CreateElbowToFittingDimension(view3D, _ElbowToFittingInfos);

          var res = tx.Commit();
          if (res == TransactionStatus.Committed) {
            TaskDialog.Show("알림", "ISO 치수 입력 성공");
            _UiDoc.ActiveView = view3D;
          }
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", tx.GetName());
          TaskDialog.Show("알림", "ISO 치수 입력 실패");
          _UiDoc.ActiveView = view3D;
        }
      }
      _UiDoc.RefreshActiveView();
    }

    private IList<Element> GetAllPipes() {
      var elems = new List<Element>();
      var elemCol = _Doc.ElementCollector(_UiDoc.ActiveGraphicalView.Id);

      foreach (var element in elemCol) {
        var cate = element.Category;
        if (cate == null) { continue; }
        if (cate.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves) {
          elems.Add(element);
        }
      }
      return elems;
    }

    private List<ElbowToFittingInfo> GetAllElbowToFittingInfos() {
      var elbowToFittingInfo = new List<ElbowToFittingInfo>();
      var fittingCol = _Doc.CategoryElements(_UiDoc.ActiveGraphicalView.Id, BuiltInCategory.OST_PipeFitting);
      foreach (var item in fittingCol) {
        var mepFitting = item.GetMechanicalFitting();
        {//
          if (mepFitting?.PartType == PartType.Elbow &&
              mepFitting?.ConnectorManager != null) {
            var connectors =
                mepFitting.ConnectorManager.Connectors;
            foreach (Connector connector in connectors) {
              var part = connector.GetConnectedPart();
              if (part.GetMechanicalFitting() != null) {
                elbowToFittingInfo.Add(new ElbowToFittingInfo(item, part));
              }
            }
          }
        }//
      }
      return elbowToFittingInfo;
    }

    private void CreatePipeDimension(View3D view3D,
        ICollection<Element> pipes, ForgeTypeId unitTypeId) {
      var doc = _Doc;
      var orientation = view3D.GetOrientation();

      // 작업평면 설정
      Plane plane = Plane.CreateByNormalAndOrigin(
          orientation.ForwardDirection, XYZ.Zero);

      // TextNoteType 가져오기
      var defaultText = doc.GetDefaultElementTypeId(
          ElementTypeGroup.TextNoteType);
      var textNoteType = doc.GetElement(defaultText) as TextNoteType;
      textNoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);

      foreach (Pipe pipe in pipes) {
        var pipeParma = pipe.GetParameter(_TextNoteIdParam);
        if (pipeParma?.AsInteger() != null) {
          var textnoteId = new ElementId((int)pipeParma?.AsInteger());
          var textnote = doc.GetElement(textnoteId);
          if (textnote != null && textnote.OwnerViewId == view3D.Id) {
            // 파이프의 textnote가 현재 뷰에 있는 textnote가 아닐때
            continue;
          }
        }

        var points = PipeDimensionHelper.GetPipeDimensionPoints(pipe);
        if (points == null) { continue; }

        var position = (points[0] + points[1]) / 2;

        // 배관 vector의 Rotation값
        var dist0 = plane.GetDistancePlaneToPoint(points[0]);
        var dist1 = plane.GetDistancePlaneToPoint(points[1]);
        var planePoint0 = points[0] - (dist0 * orientation.ForwardDirection);
        var planePoint1 = points[1] - (dist1 * orientation.ForwardDirection);

        var vector0 = planePoint0 - planePoint1;
        var rotation = vector0.AngleOnPlaneTo(view3D.RightDirection,
            orientation.ForwardDirection);

        var dimension = UnitUtils.ConvertFromInternalUnits(
            points[0].DistanceTo(points[1]), unitTypeId);
        dimension = MathUtils.RoundDoubleToInt(dimension);

        // 치수 문자 작성
        var textOptions = new TextNoteOptions {
          TypeId = textNoteType.Id,
          HorizontalAlignment = HorizontalTextAlignment.Center,
          VerticalAlignment = VerticalTextAlignment.Middle,
          Rotation = rotation,
          KeepRotatedTextReadable = true
        };
        var textNote = TextNote.Create(doc, view3D.Id,
            position, dimension.ToString(), textOptions);

        // pipe의 파라미터에 textnote의 id 입력
        // textnote의 파라미터에 pipe의 id 입력
        pipeParma.Set(textNote.Id.IntegerValue);
        //var textnoteParam = textNote.LookupParameter(_TargetPipeIdParam);
        var textnoteParam = textNote.GetParameter(_TargetPipeIdParam);
        if (textnoteParam != null) {
          textnoteParam.Set(pipe.Id.IntegerValue);
        }
      }
    }

    private void CreateElbowToFittingDimension(View3D view3D,
        IList<ElbowToFittingInfo> infos) {
      var doc = _Doc;
      var orientation = view3D.GetOrientation();

      // 작업평면 설정
      Plane plane = Plane.CreateByNormalAndOrigin(
          orientation.ForwardDirection, XYZ.Zero);

      // TextNoteType 가져오기
      var defaultText = doc.GetDefaultElementTypeId(
          ElementTypeGroup.TextNoteType);
      var textNoteType = doc.GetElement(defaultText) as TextNoteType;
      textNoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);

      foreach (var info in infos) {
        var elbowParam = info.Elbow.GetParameter(_TextNoteIdParam);
        if (elbowParam?.AsInteger() != null) {
          var textnoteId = new ElementId((int)elbowParam?.AsInteger());
          var textnote = doc.GetElement(textnoteId);
          if (textnote != null && textnote.OwnerViewId == view3D.Id) {
            continue;
          }
        }

        var elbow = (FamilyInstance)info.Elbow;
        var elbowPt = elbow.GetLocationPoint();
        var connFittingPt = info.ConnFitting.GetLocationPoint();
        var centerPt = (elbowPt + connFittingPt) / 2;

        var elbowDist = plane.GetDistancePlaneToPoint(elbowPt);
        var connFittingDist = plane.GetDistancePlaneToPoint(connFittingPt);
        var elbowPlanePt = elbowPt - (elbowDist * orientation.ForwardDirection);
        var connFittingPlanePt =
            connFittingPt - (connFittingDist * orientation.ForwardDirection);

        var vector0 = elbowPlanePt - connFittingPlanePt;
        var rotation = vector0.AngleOnPlaneTo(view3D.RightDirection,
            orientation.ForwardDirection);

        var dimension = UnitUtils.ConvertFromInternalUnits(
            elbowPt.DistanceTo(connFittingPt), UnitTypeId.Millimeters);
        dimension = MathUtils.RoundDoubleToInt(dimension);

        // 치수 문자 작성
        var textOptions = new TextNoteOptions {
          TypeId = textNoteType.Id,
          HorizontalAlignment = HorizontalTextAlignment.Center,
          VerticalAlignment = VerticalTextAlignment.Middle,
          Rotation = rotation,
          KeepRotatedTextReadable = true
        };
        var textNote = TextNote.Create(doc, view3D.Id,
            centerPt, dimension.ToString(), textOptions);

        // elbow의 파라미터에 textnote의 id 입력
        // textnote의 파라미터에 elbow의 id 입력
        elbowParam.Set(textNote.Id.IntegerValue);
        var textnoteParam = textNote.GetParameter(_TargetPipeIdParam);
        textnoteParam.Set(elbow.Id.IntegerValue);
      }
    }
  }
}

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Project1.Revit.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace Project1.Revit.MultiFaceFloorPlan {
  static class ObjectEx {
    public static bool IsDefault<T>(this T obj) {
      return EqualityComparer<T>.Default.Equals(obj, default(T));
    }
  }
  public class MultiFaceFloorPlanVM : ViewModelBase {
    class TextInfo {
      public TextNote TextNote { get; set; }
      public XYZ TextPosition { get; set; }
      public XYZ ConnecorOrigin { get; set; }
      public LeaderDirection LeaderDirection { get; set; }
      public LeaderDirection ViewDirection { get; set; }


      public TextInfo(XYZ position, LeaderDirection direction) {
        TextPosition = position;
        LeaderDirection = direction;
      }
    }

    class ConnectorInfo : Common.ConnectorInfo {
      public Connector Connector { get; private set; }

      public ConnectorInfo(ConnectorInfo other) : base(other) {
        Connector = other.Connector;
      }
      public ConnectorInfo(Connector connector) : base(connector) {
        Connector = connector;
      }
    }

    enum LeaderDirection {
      PX,
      NX,
      PY,
      NY,
      PZ,
      NZ
    }

    private UIApplication _UiApp = null;
    private Application _App = null;
    private FamilyInstance _Instance = null;
    private string _ScaleText;
    private double _Scale = 1;

    private readonly string _Top = "Top";
    private readonly string _Bottom = "Bottom";
    private readonly string _Left = "Left";
    private readonly string _Front = "Front";
    private readonly string _Right = "Right";
    private readonly string _Rear = "Rear";

    private IList<InfoByView> _FileInfoByView;

    public string ScaleText {
      get { return _ScaleText; }
      set { Set(ref _ScaleText, value); }
    }

    public IList<InfoByView> FileInfoByView {
      get { return _FileInfoByView; }
      set { Set(ref _FileInfoByView, value); }
    }
    private InfoByView _FamilyFilePath;
    public InfoByView FamilyFilePath {
      get { return _FamilyFilePath; }
      set { Set(ref _FamilyFilePath, value); }
    }

    public RelayCommand CmdOverloadCadFile { get; set; }
    public RelayCommand<System.Windows.Window> CmdApply { get; set; }
    public RelayCommand CmdExportExcel { get; set; }


    public MultiFaceFloorPlanVM() {
      FamilyFilePath = new InfoByView();

      FileInfoByView = new List<InfoByView> {
        new InfoByView(_Top, false),
        new InfoByView(_Left, true),
        new InfoByView(_Front, true),
        new InfoByView(_Right, true),
        new InfoByView(_Rear, true),
        new InfoByView(_Bottom, false),
      };
      _ScaleText = 1.ToString();

      CmdOverloadCadFile = new RelayCommand(LoadFiles);
      CmdApply = new RelayCommand<System.Windows.Window>(Apply);
      //CmdExportExcel = new RelayCommand(ExportExcel);
    }


    public void FamilyQc(UIApplication uiApp) {
      _UiApp = uiApp;
      _App = _UiApp.Application;
    }

    private void LoadFiles() {
      var dialog = new System.Windows.Forms.OpenFileDialog() {
        Filter = "Revit Family Files(*.rfa)|*.rfa",
        Title = FamilyFilePath.ViewDir,
      };
      if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
        return;
      }
      var path = dialog.FileName;
      FamilyFilePath.FileFullPath = path;

      // CAD 파일 유무 확인
      var directory = Path.GetDirectoryName(path);
      var files = Directory.GetFiles(directory);
      // 기존 값 초기화
      foreach (var cadFile in FileInfoByView) {
        cadFile.FileFullPath = null;
        cadFile.IsChecked = false;
      }
      foreach (var file in files) {
        if (!Path.GetExtension(file).Equals(".dwg")) { continue; }
        foreach (var cadFile in FileInfoByView) {
          if (cadFile.ViewDir.ToLower().Equals(
              Path.GetFileNameWithoutExtension(file).ToLower())) {
            cadFile.FileFullPath = file;
            cadFile.IsChecked = true;
          }
        }
      }
    }

    private void Apply(System.Windows.Window window) {
      if (string.IsNullOrEmpty(FamilyFilePath.FileFullPath) ||
          !File.Exists(FamilyFilePath.FileFullPath)) {
        TaskDialog.Show("알림", "선택된 패밀리 파일이 없습니다.");
        FamilyFilePath.FileFullPath = string.Empty;
        return;
      }

      // CAD 파일 확인
      var directory = Path.GetDirectoryName(FamilyFilePath.FileFullPath);
      var files = Directory.GetFiles(directory);
      foreach (var cadFile in FileInfoByView) {
        cadFile.FileFullPath = null;
      }
      foreach (var file in files) {
        if (!Path.GetExtension(file).Equals(".dwg")) { continue; }
        foreach (var cadFile in FileInfoByView) {
          if (cadFile.ViewDir.ToLower().Equals(
              Path.GetFileNameWithoutExtension(file).ToLower())) {
            cadFile.FileFullPath = file;
          }
        }
      }

      foreach (var cadFile in FileInfoByView) {
        var isExist = File.Exists(cadFile.FileFullPath);
        if (cadFile.IsChecked) {
          if (string.IsNullOrEmpty(cadFile.FileFullPath) || !isExist) {
            TaskDialog.Show("알림",
                $"해당 폴더에 {cadFile.ViewDir} CAD 파일이 없습니다.");
            return;
          }
        }
      }

      if (string.IsNullOrEmpty(_ScaleText) || string.IsNullOrWhiteSpace(_ScaleText)) {
        TaskDialog.Show("알림", "Scale을 작성해주세요.");
        return;
      }
      double.TryParse(_ScaleText, out var scale);
      if (scale <= 0) {
        TaskDialog.Show("알림", "0 이상의 숫자를 넣어주세요.");
        return;
      }

      _Scale = scale;
      Verify();
      window.Close();
    }


    private void Verify() {
      // 새문서 생성 및 열기
      var familyPath = FamilyFilePath.FileFullPath;
      var direc = Path.GetDirectoryName(familyPath);
      var familyName = Path.GetFileNameWithoutExtension(familyPath);
      var newPath = Path.Combine(direc, $"{familyName}_PocData_{DateTime.Now:yyyyMMddHHmmss}");
      newPath = Path.ChangeExtension(newPath, "rvt");

      var doc = _App.NewProjectDocument(UnitSystem.Metric);
      var saveAsOptions = new SaveAsOptions() { OverwriteExistingFile = true };
      doc.SaveAs(newPath, saveAsOptions);

      var newUiDoc = _UiApp.OpenAndActivateDocument(newPath);
      doc = newUiDoc.Document;

      using (var tx = new Transaction(doc, "Create Insance")) {
        try {
          tx.Start();

          var loadRe = doc.LoadFamily(FamilyFilePath.FileFullPath, out var targetFamily);
          var symbolIds = targetFamily.GetFamilySymbolIds();
          var symbol = doc.GetElement(symbolIds.First()) as FamilySymbol;
          var level = doc.GetSortedLevels().FirstOrDefault();
          if (level.IsDefault()) {
            throw new Exception("Level Not Found");
          }

          if (!symbol.IsActive) { symbol.Activate(); }
          var instance = doc.Create.NewFamilyInstance(XYZ.Zero, symbol,
              StructuralType.NonStructural);
          _Instance = instance;
          doc.Regenerate();

          var systems = doc.TypeElements(typeof(PipingSystemType));
          var system = systems.FirstOrDefault();
          var pipetypes = doc.TypeElements(typeof(PipeType));
          var pipetype = pipetypes.FirstOrDefault();

          var connectorInfos = new List<ConnectorInfo>();
          var connectors = instance.GetConnectorSet();
          foreach (Connector connector in connectors) {
            connectorInfos.Add(new ConnectorInfo(connector));
          }

          // 요청 기준에 따른 ID 값 생성
          connectorInfos.Sort(Common.ConnectorInfo.SortEquipmentFamilyConnector);
          for (int i = 0; i < connectorInfos.Count; i++) {
            connectorInfos[i].Sequence = i + 1;
          }

          int maxLength = 0;
          foreach (var itr in connectorInfos) {
            var origin = itr.Connector.Origin;
            var endPnt = origin + itr.Connector.CoordinateSystem.BasisZ / 100;
            if (itr.Connector.Shape == ConnectorProfileType.Round) {
              var idStr = string.Format("{0:00}", itr.Sequence);
              var pipePlaceholder = Pipe.CreatePlaceholder(doc,
                  system.Id, pipetype.Id, level.Id, origin, endPnt);
              pipePlaceholder
                  .get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)
                  .Set(itr.Connector.Radius);
              pipePlaceholder
                  .get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
                  .Set(idStr);
              var conInfo = new ConnectorInfo(itr.Connector);
              pipePlaceholder
                   .get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)
                   .Set(conInfo.InfoText);

              var descLength = itr.Connector.Description.Length;
              maxLength = maxLength > descLength ? maxLength : descLength;
            }
          }
          var cateId = new ElementId(BuiltInCategory.OST_PlaceHolderPipes);
          doc.ActiveView.HideCategoryTemporary(cateId);
          doc.ActiveView.ConvertTemporaryHideIsolateToPermanent();

          var bb = instance.get_BoundingBox(doc.ActiveView);


          // 평면뷰 생성
          var floorPlanVFT = doc.TypeElements(typeof(ViewFamilyType))
              .Cast<ViewFamilyType>()
              .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan);
          if (floorPlanVFT.IsDefault()) {
            throw new Exception("FloorPlan ViewFamilyType Not Found");
          }
          var floorPlanView = ViewPlan.Create(doc,
              floorPlanVFT.Id, level.Id);
          {
            floorPlanView.Name = $"{_Top} View";
            var planViewRange = floorPlanView.GetViewRange();
            planViewRange.SetLevelId(PlanViewPlane.TopClipPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.CutPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.BottomClipPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.UnderlayBottom, level.Id);

            var viewMargin = 1;
            var viewTop = (bb.Max.Z + viewMargin) - level.Elevation;
            var viewBot = (bb.Min.Z - viewMargin) - level.Elevation;

            planViewRange.SetOffset(PlanViewPlane.TopClipPlane, viewTop);
            planViewRange.SetOffset(PlanViewPlane.CutPlane, viewBot);
            planViewRange.SetOffset(PlanViewPlane.BottomClipPlane, viewBot);
            planViewRange.SetOffset(PlanViewPlane.ViewDepthPlane, viewBot - 1);

            floorPlanView.SetViewRange(planViewRange);
          }

          // 천장뷰 생성
          var ceilingPlanVFT = doc.TypeElements(typeof(ViewFamilyType))
              .Cast<ViewFamilyType>()
              .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.CeilingPlan);
          if (ceilingPlanVFT.IsDefault()) {
            throw new Exception("CeilingPlan ViewFamilyType Not Found");
          }
          var ceilingPlanView = ViewPlan.Create(doc,
          ceilingPlanVFT.Id, level.Id);
          {
            ceilingPlanView.Name = "Test Ceiling Plan";
            var planViewRange = ceilingPlanView.GetViewRange();
            planViewRange.SetLevelId(PlanViewPlane.TopClipPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.CutPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.BottomClipPlane, level.Id);
            planViewRange.SetLevelId(PlanViewPlane.UnderlayBottom, level.Id);

            var viewMargin = 1;
            var viewTop = (bb.Max.Z + viewMargin) - level.Elevation;
            var viewBot = (bb.Min.Z - viewMargin) - level.Elevation;

            planViewRange.SetOffset(PlanViewPlane.TopClipPlane, viewTop);
            planViewRange.SetOffset(PlanViewPlane.CutPlane, viewBot);
            planViewRange.SetOffset(PlanViewPlane.BottomClipPlane, viewBot);
            planViewRange.SetOffset(PlanViewPlane.ViewDepthPlane, viewBot - 1);

            ceilingPlanView.SetViewRange(planViewRange);
          }

          // 색션뷰(입면도)뷰 생성
          var sectionVFT = doc.TypeElements(typeof(ViewFamilyType))
           .Cast<ViewFamilyType>()
           .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Section);
          if (sectionVFT.IsDefault()) {
            throw new Exception("Section ViewFamilyType Not Found");
          }

          var sectionView = ViewSection.CreateSection(doc, sectionVFT.Id, bb);
          sectionView.Name = $"{_Bottom} View";
          var viewer = doc.CategoryElements(sectionView.Id, BuiltInCategory.OST_Viewers)
              .FirstOrDefault();
          var viewerBb = viewer.get_BoundingBox(sectionView);
          var viewerCen = (viewerBb.Min + viewerBb.Max) / 2;
          var axis = Line.CreateBound(viewerCen, viewerCen + sectionView.ViewDirection);
          ElementTransformUtils.RotateElement(doc, viewer.Id, axis, Math.PI);

          // 입면도 생성
          var elevationVFT = doc.TypeElements(typeof(ViewFamilyType))
              .Cast<ViewFamilyType>()
              .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Elevation);
          if (elevationVFT.IsDefault()) {
            throw new Exception("Elevation ViewFamilyType Not Found");
          }

          var middPnt = (bb.Min + bb.Max) / 2;
          var leftPnt = new XYZ(bb.Min.X, middPnt.Y, middPnt.Z);
          var leftElevMarker = ElevationMarker.CreateElevationMarker(doc,
              elevationVFT.Id, leftPnt, 100);
          var leftView = leftElevMarker.CreateElevation(doc, floorPlanView.Id, 0);
          leftView.Name = $"{_Left} View";
          var bounding = leftElevMarker.get_BoundingBox(floorPlanView);
          var centerPnt = (bounding.Min + bounding.Max) / 2;
          var line = Line.CreateBound(centerPnt, centerPnt + XYZ.BasisZ);
          ElementTransformUtils.RotateElement(doc,
              leftElevMarker.Id, line, Math.PI * 1.5);

          var frontPnt = new XYZ(middPnt.X, bb.Min.Y, middPnt.Z);
          var frontElevMarker = ElevationMarker.CreateElevationMarker(doc,
              elevationVFT.Id, frontPnt, 100);
          var frontView = frontElevMarker.CreateElevation(doc, floorPlanView.Id, 0);
          frontView.Name = $"{_Front} View";

          var rightPnt = new XYZ(bb.Max.X, middPnt.Y, middPnt.Z);
          var rightElevMarker = ElevationMarker.CreateElevationMarker(doc,
              elevationVFT.Id, rightPnt, 100);
          var rightView = rightElevMarker.CreateElevation(doc, floorPlanView.Id, 0);
          rightView.Name = $"{_Right} View";
          bounding = rightElevMarker.get_BoundingBox(floorPlanView);
          centerPnt = (bounding.Min + bounding.Max) / 2;
          line = Line.CreateBound(centerPnt, centerPnt + XYZ.BasisZ);
          ElementTransformUtils.RotateElement(doc,
              rightElevMarker.Id, line, Math.PI / 2);

          var rearPnt = new XYZ(middPnt.X, bb.Max.Y, middPnt.Z);
          var rearElevMarker = ElevationMarker.CreateElevationMarker(doc,
              elevationVFT.Id, rearPnt, 100);
          var rearView = rearElevMarker.CreateElevation(doc, floorPlanView.Id, 0);
          rearView.Name = $"{_Rear} View";
          bounding = rearElevMarker.get_BoundingBox(floorPlanView);
          centerPnt = (bounding.Min + bounding.Max) / 2;
          line = Line.CreateBound(centerPnt, centerPnt + XYZ.BasisZ);
          ElementTransformUtils.RotateElement(doc,
              rearElevMarker.Id, line, Math.PI);

          ceilingPlanView.GetViewRange();

          var textnoteType = (TextNoteType)doc.TypeElements(typeof(TextNoteType))?.FirstOrDefault();
          textnoteType.get_Parameter(BuiltInParameter.TEXT_BACKGROUND).Set(1);
          textnoteType.get_Parameter(BuiltInParameter.LEADER_OFFSET_SHEET).Set(0.0);
          var textSizeParam = textnoteType.get_Parameter(BuiltInParameter.TEXT_SIZE);
          textSizeParam.Set(textSizeParam.AsDouble() * _Scale);

          var views = new List<View> {
          sectionView,
          floorPlanView,
          leftView,
          frontView,
          rightView,
          rearView
          };
          foreach (var fileView in FileInfoByView) {
            //if (!fileView.IsChecked) { continue; }
            foreach (var view in views) {
              if (view.Name.Contains(fileView.ViewDir)) {
                fileView.View = view;
                break;
              }
            }
          }

          var textInfos = new List<TextInfo>();
          var queue = new Queue();
          var dupNoteType = (TextNoteType)textnoteType.Duplicate("PoC No");
          dupNoteType.get_Parameter(BuiltInParameter.LINE_COLOR).Set(255);
          foreach (var importView in FileInfoByView) {
            //if (!importView.IsChecked) { continue; }
            var view = importView.View;
            view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION).Set(0);

            var elems = doc.ElementCollector(view.Id);
            var hideElem = new List<ElementId>();

            var viewDir = view.ViewDirection.Normalize();
            foreach (var elem in elems) {
              if (elem.Id == instance.Id) {
                TextNote text = null;
                foreach (var itr in connectorInfos) {
                  var idStr = string.Format("{0:00}", itr.Sequence);
                  var position = itr.Connector.Origin;

                  var connDir = itr.Connector.CoordinateSystem.BasisZ;
                  if (!viewDir.IsAlmostEqualTo(connDir)) { continue; }

                  // Id Num 작성
                  var textInfo = GetTextPosition(viewDir, bb, position);
                  if (viewDir == XYZ.BasisX) {
                    textInfo.ViewDirection = LeaderDirection.PX;
                  }
                  else if (viewDir == -XYZ.BasisX) {
                    textInfo.ViewDirection = LeaderDirection.NX;
                  }
                  else if (viewDir == XYZ.BasisY) {
                    textInfo.ViewDirection = LeaderDirection.PY;
                  }
                  else if (viewDir == -XYZ.BasisY) {
                    textInfo.ViewDirection = LeaderDirection.NY;
                  }
                  else if (viewDir == XYZ.BasisZ) {
                    textInfo.ViewDirection = LeaderDirection.PZ;
                  }
                  else if (viewDir == -XYZ.BasisZ) {
                    textInfo.ViewDirection = LeaderDirection.NZ;
                  }

                  text = TextNote.Create(doc, view.Id,
                      textInfo.TextPosition, idStr, dupNoteType.Id);

                  textInfo.TextNote = text;
                  textInfo.ConnecorOrigin = position;
                  textInfos.Add(textInfo);
                  queue.Enqueue(textInfo);

                  text.HorizontalAlignment = HorizontalTextAlignment.Center;
                  text.VerticalAlignment = VerticalTextAlignment.Middle;
                  switch (textInfo.LeaderDirection) {
                    case LeaderDirection.PX:
                      text.HorizontalAlignment = HorizontalTextAlignment.Left;
                      break;
                    case LeaderDirection.NX:
                      text.HorizontalAlignment = HorizontalTextAlignment.Right;
                      break;
                    case LeaderDirection.PY:
                      text.VerticalAlignment = VerticalTextAlignment.Bottom;
                      break;
                    case LeaderDirection.NY:
                      text.VerticalAlignment = VerticalTextAlignment.Top;
                      break;
                    case LeaderDirection.PZ:
                      text.VerticalAlignment = VerticalTextAlignment.Bottom;
                      break;
                    case LeaderDirection.NZ:
                      text.VerticalAlignment = VerticalTextAlignment.Top;
                      break;
                  }
                }
                continue;
              }
              hideElem.Add(elem.Id);
            }
            // 패밀리, 텍스트 노트 이외 숨기기
            view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE).Set(0);
            view.HideElements(hideElem);
            view.ConvertTemporaryHideIsolateToPermanent();
            view.Scale = 20;

            if (importView.IsChecked
              && view.Name.Contains(importView.ViewDir)) {
              OverloadCadFile(importView);
            }
          }

          doc.Regenerate();
          foreach (var textInfo in textInfos) {
            var text = textInfo.TextNote;
            var coord = text.Coord;
            var moveDis = textInfo.TextNote.Text.Length * textInfo.TextNote.Height;
            switch (textInfo.LeaderDirection) {
              case LeaderDirection.PX:
                coord += moveDis * XYZ.BasisX;
                break;
              case LeaderDirection.NX:
                coord += moveDis * -XYZ.BasisX;
                break;
              case LeaderDirection.PY:
                coord += moveDis * XYZ.BasisY;
                break;
              case LeaderDirection.NY:
                coord += moveDis * -XYZ.BasisY;
                break;
              case LeaderDirection.PZ:
                coord += moveDis * XYZ.BasisZ;
                break;
              case LeaderDirection.NZ:
                coord += moveDis * -XYZ.BasisZ;
                break;
            }
          }

          Console.WriteLine();
          // 겹치는 텍스트노트 찾아 위치 이동
          while (queue.Count > 0) {
            var textinfo1 = (TextInfo)queue.Dequeue();
            var text1 = textinfo1.TextNote;
            var view = (View)doc.GetElement(text1.OwnerViewId);
            var viewScale = view.Scale;

            var hei1 = text1.Height * viewScale;
            var wid1 = text1.Width * viewScale;

            var text1coord = text1.Coord;
            var coord1 = new XYZ(text1coord.X, text1coord.Y, 0);
            Line line1 = null;
            switch (textinfo1.LeaderDirection) {
              case LeaderDirection.PX:
              case LeaderDirection.NX:
                line1 = Line.CreateBound(coord1 - new XYZ(0, hei1 / 2, 0),
                    coord1 + new XYZ(0, hei1 / 2, 0));
                break;
              case LeaderDirection.PY:
              case LeaderDirection.NY:
                line1 = Line.CreateBound(coord1 - new XYZ(wid1 / 2, 0, 0),
                    coord1 + new XYZ(wid1 / 2, 0, 0));
                break;
              case LeaderDirection.PZ:
              case LeaderDirection.NZ:
                continue;
                break;
            }
            foreach (var textinfo2 in textInfos) {
              var text2 = textinfo2.TextNote;
              if (text1.Id.Equals(text2.Id)) { continue; }
              if (!text1.OwnerViewId.Equals(text2.OwnerViewId)) { continue; }
              if (textinfo1.LeaderDirection != textinfo2.LeaderDirection) { continue; }

              var hei2 = text2.Height * viewScale;
              var wid2 = text2.Width * viewScale;

              var text2coord = text2.Coord;
              var coord2 = new XYZ(text2coord.X, text2coord.Y, 0);
              Line line2 = null;
              switch (textinfo2.LeaderDirection) {
                case LeaderDirection.PX:
                case LeaderDirection.NX:
                  line2 = Line.CreateBound(coord2 - new XYZ(0, hei2 / 2, 0),
                      coord2 + new XYZ(0, hei2 / 2, 0));
                  break;
                case LeaderDirection.PY:
                case LeaderDirection.NY:
                  line2 = Line.CreateBound(coord2 - new XYZ(wid2 / 2, 0, 0),
                      coord2 + new XYZ(wid2 / 2, 0, 0));
                  break;
                case LeaderDirection.PZ:
                case LeaderDirection.NZ:
                  continue;
              }

              var intersect = line1.Intersect(line2);
              if (intersect != SetComparisonResult.Disjoint) {
                double right = 0;
                double left = 0;
                switch (textinfo1.LeaderDirection) {
                  case LeaderDirection.PX:
                  case LeaderDirection.NX:
                    var topY = text1coord.Y > text2coord.Y ? text2coord.Y : text1coord.Y;
                    topY += hei2 / 2;
                    var bottomY = text1coord.Y > text2coord.Y ? text1coord.Y : text2coord.Y;
                    bottomY -= hei2 / 2;
                    var overlapDisY = topY - bottomY;
                    if (overlapDisY.IsAlmostEqualTo(0)) { continue; }

                    if (text1coord.Y > text2coord.Y) {
                      text1.Coord += new XYZ(0, overlapDisY * 0.55, 0);
                      text2.Coord -= new XYZ(0, overlapDisY * 0.55, 0);
                    }
                    else {
                      text1.Coord -= new XYZ(0, overlapDisY * 0.55, 0);
                      text2.Coord += new XYZ(0, overlapDisY * 0.55, 0);
                    }
                    break;
                  case LeaderDirection.PY:
                  case LeaderDirection.NY:
                    var rightX = text1coord.X > text2coord.X ? text2coord.X : text1coord.X;
                    rightX += wid2 / 2;
                    var leftX = text1coord.X > text2coord.X ? text1coord.X : text2coord.X;
                    leftX -= wid2 / 2;
                    var overlapDisX = rightX - leftX;
                    if (overlapDisX.IsAlmostEqualTo(0)) { continue; }

                    if (text1coord.X > text2coord.X) {
                      text1.Coord += new XYZ(overlapDisX * 0.55, 0, 0);
                      text2.Coord -= new XYZ(overlapDisX * 0.55, 0, 0);
                    }
                    else {
                      text1.Coord -= new XYZ(overlapDisX * 0.55, 0, 0);
                      text2.Coord += new XYZ(overlapDisX * 0.55, 0, 0);
                    }
                    break;
                  case LeaderDirection.PZ:
                  case LeaderDirection.NZ:
                    if (!view.ViewDirection.Y.IsAlmostEqualTo(0)) {
                      right = text1coord.X > text2coord.X ? text2coord.X : text1coord.X;
                      left = text1coord.X > text2coord.X ? text1coord.X : text2coord.X;
                      var overlapDis = (right + wid2 / 2) - (left - wid2 / 2);
                      if (overlapDis.IsAlmostEqualTo(0)) { continue; }

                      if (text1coord.X > text2coord.X) {
                        text1.Coord += new XYZ(overlapDis * 0.55, 0, 0);
                        text2.Coord -= new XYZ(overlapDis * 0.55, 0, 0);
                      }
                      else {
                        text1.Coord -= new XYZ(overlapDis * 0.55, 0, 0);
                        text2.Coord += new XYZ(overlapDis * 0.55, 0, 0);
                      }
                    }
                    else if (!view.ViewDirection.X.IsAlmostEqualTo(0)) {
                      right = text1coord.Y > text2coord.Y ? text2coord.Y : text1coord.Y;
                      left = text1coord.Y > text2coord.Y ? text1coord.Y : text2coord.Y;
                      var overlapDis = (right + wid2 / 2) - (left - wid2 / 2);
                      if (overlapDis.IsAlmostEqualTo(0)) { continue; }

                      if (text1coord.Y > text2coord.Y) {
                        text1.Coord += new XYZ(0, overlapDis * 0.55, 0);
                        text2.Coord -= new XYZ(0, overlapDis * 0.55, 0);
                      }
                      else {
                        text1.Coord -= new XYZ(0, overlapDis * 0.55, 0);
                        text2.Coord += new XYZ(0, overlapDis * 0.55, 0);
                      }
                    }
                    break;
                }

                if (!queue.Contains(textinfo1)) {
                  queue.Enqueue(textinfo1);
                }
                if (!queue.Contains(textinfo2)) {
                  queue.Enqueue(textinfo2);
                }
              }
            }
          }

          // 텍스트 노트 지시선 추가
          // 지시선 추가 후 텍스트 노트 위치 변경시 시지선도 같이 움직임
          foreach (var textInfo in textInfos) {
            var text = textInfo.TextNote;
            var leader = text.AddLeader(TextNoteLeaderTypes.TNLT_STRAIGHT_L);
            leader.End = textInfo.ConnecorOrigin;

            var coord = text.Coord;
            var center = (textInfo.ConnecorOrigin + coord) / 2;
            switch (textInfo.LeaderDirection) {
              case LeaderDirection.PX:
              case LeaderDirection.NX:
                leader.Elbow = new XYZ(center.X, coord.Y, center.Z);
                break;
              case LeaderDirection.PY:
              case LeaderDirection.NY:
                leader.Elbow = new XYZ(coord.X, center.Y, center.Z);
                break;
              case LeaderDirection.PZ:
              case LeaderDirection.NZ:
                leader.Elbow = new XYZ(center.X, center.Y, coord.Z);
                break;
            }
          }


          // View Sheet 생성 (육면도 생성)
          var viewsheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
          viewsheet.Name = $"{familyName} Sheet";

          Viewport bottomVp = null, topVp = null;
          double maxHeight = 0.0;
          double maxWidth = 0.0;
          foreach (var viewInfo in FileInfoByView) {
            //if (!viewInfo.IsChecked) { continue; }
            var vp = Viewport.Create(doc, viewsheet.Id, viewInfo.View.Id, XYZ.Zero);
            viewInfo.ViewPort = vp;
            if (viewInfo.IsSideView) {
              var vpHeight = vp.GetBoxOutline().MaximumPoint.Y
                  - vp.GetBoxOutline().MinimumPoint.Y;
              var vpWidth = vp.GetBoxOutline().MaximumPoint.X
                  - vp.GetBoxOutline().MinimumPoint.X;
              maxHeight = Math.Max(vpHeight, maxHeight);
              maxWidth = Math.Max(vpWidth, maxWidth);
            }

            if (viewInfo.ViewDir == _Bottom) {
              bottomVp = vp;
            }
            else if (viewInfo.ViewDir == _Top) {
              topVp = vp;
            }
          }

          var centerPoint = XYZ.Zero;
          Outline tempOutLine = null;
          XYZ tempLen = XYZ.Zero;

          Viewport preVp = null;
          XYZ vpLabelPnt = XYZ.Zero;
          TextNote textnote = null;
          double frontXpnt = 0.0;
          double botYpnt = 0.0;
          double padding = Math.Min(maxHeight, maxWidth) * 0.15;

          if (bottomVp != null) {
            tempOutLine = bottomVp.GetBoxOutline();
            tempLen = tempOutLine.MaximumPoint - tempOutLine.MinimumPoint;
            botYpnt = tempLen.Y;
          }

          foreach (var viewInfo in FileInfoByView) {
            //if (!viewInfo.IsChecked) { continue; }

            var viewport = viewInfo.ViewPort;
            tempOutLine = viewport.GetBoxOutline();
            tempLen = (tempOutLine.MaximumPoint - tempOutLine.MinimumPoint) / 2.0;
            if (preVp == null) {
              centerPoint = new XYZ(tempLen.X, botYpnt + maxHeight / 2 + padding, 0);
            }
            else {
              var preBox = preVp.GetBoxCenter();
              var preBoxOL = preVp.GetBoxOutline();
              centerPoint = new XYZ(preBox.X + tempLen.X + padding +
                  (preBoxOL.MaximumPoint.X - preBoxOL.MinimumPoint.X) / 2.0,
                  preBox.Y, 0);
            }
            viewport.SetBoxCenter(centerPoint);

            // side 뷰 일때 같은 높이에 위치하도록 설정
            if (viewInfo.IsSideView) {
              vpLabelPnt = (viewport.GetLabelOutline().MinimumPoint
                + viewport.GetLabelOutline().MaximumPoint) / 2;
              textnote = TextNote.Create(doc, viewsheet.Id,
              vpLabelPnt, viewInfo.ViewDir.ToUpper(), textnoteType.Id);
              textnote.HorizontalAlignment = HorizontalTextAlignment.Center;
            }

            if (viewInfo.ViewDir == _Front) {
              frontXpnt = viewport.GetBoxOutline().MinimumPoint.X;
            }
            if (viewInfo.IsSideView) {
              preVp = viewport;
            }
          }

          double bottomMaxY = 0.0;
          if (bottomVp != null) {
            tempOutLine = bottomVp.GetBoxOutline();
            tempLen = (tempOutLine.MaximumPoint - tempOutLine.MinimumPoint) / 2.0;
            centerPoint = new XYZ(frontXpnt + tempLen.X, tempLen.Y, 0);
            bottomVp.SetBoxCenter(centerPoint);
            vpLabelPnt = (bottomVp.GetLabelOutline().MinimumPoint
                + bottomVp.GetLabelOutline().MaximumPoint) / 2;
            textnote = TextNote.Create(doc, viewsheet.Id,
                vpLabelPnt, _Bottom.ToUpper(), textnoteType.Id);
            textnote.HorizontalAlignment = HorizontalTextAlignment.Center;
            bottomMaxY = bottomVp.GetBoxOutline().MaximumPoint.Y;
          }

          if (topVp != null) {
            tempOutLine = topVp.GetBoxOutline();
            tempLen = (tempOutLine.MaximumPoint - tempOutLine.MinimumPoint) / 2.0;
            centerPoint = new XYZ(frontXpnt + tempLen.X,
                 bottomMaxY + padding + maxHeight + padding + tempLen.Y, 0);
            topVp.SetBoxCenter(centerPoint);
            vpLabelPnt = (topVp.GetLabelOutline().MinimumPoint
                + topVp.GetLabelOutline().MaximumPoint) / 2;
            textnote = TextNote.Create(doc, viewsheet.Id,
                vpLabelPnt, _Top.ToUpper(), textnoteType.Id);
            textnote.HorizontalAlignment = HorizontalTextAlignment.Center;
          }

          double vpMaxXpnt = 0.0;
          double vpMaxYpnt = 0.0;
          foreach (var viewInfo in FileInfoByView) {
            //if (!viewInfo.IsChecked) { continue; }
            var vpOutline = viewInfo.ViewPort.GetBoxOutline().MaximumPoint;

            vpMaxXpnt = vpMaxXpnt > vpOutline.X ? vpMaxXpnt : vpOutline.X;
            vpMaxYpnt = vpMaxYpnt > vpOutline.Y ? vpMaxYpnt : vpOutline.Y;
          }

          var textSize = textnoteType
              .get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble();
          var schedule = CreateSchedule(doc, textnoteType, maxLength);
          centerPnt = new XYZ(vpMaxXpnt, vpMaxYpnt, 0);
          var scheduleSheet = ScheduleSheetInstance.Create(doc,
               viewsheet.Id, schedule.Id, centerPnt);

          tx.Commit();
          _UiApp.ActiveUIDocument.ActiveView = viewsheet;

          doc.Save();
          ExportExcel(schedule);
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", tx.GetName());
        }
      }
    }

    /// <summary>
    /// 선형 보간
    /// </summary>
    /// <param name="s">시작 값</param>
    /// <param name="e">끝 값</param>
    /// <param name="d">보간 위지 0~1</param>
    /// <returns></returns>
    private double LinearInterpolation(double s, double e, double d) {
      return (1 - d) * s + d * e;
    }

    private TextInfo GetTextPosition(XYZ viewDirection, BoundingBoxXYZ equipBB, XYZ connPt) {
      var equipBBMin = equipBB.Min;
      var equipBBMax = equipBB.Max;
      var d = 0.7; // 보간 값

      var distList = new List<double>();
      double distMinX = Math.Abs(equipBBMin.X - connPt.X);
      double distMaxX = Math.Abs(equipBBMax.X - connPt.X);
      double distMinY = Math.Abs(equipBBMin.Y - connPt.Y);
      double distMaxY = Math.Abs(equipBBMax.Y - connPt.Y);
      double distMinZ = Math.Abs(equipBBMin.Z - connPt.Z);
      double distMaxZ = Math.Abs(equipBBMax.Z - connPt.Z);

      if (!viewDirection.X.IsAlmostEqualTo(0)) {
        distList.Add(distMinY);
        distList.Add(distMaxY);
        distList.Add(distMinZ);
        distList.Add(distMaxZ);
      }
      else if (!viewDirection.Y.IsAlmostEqualTo(0)) {
        distList.Add(distMinX);
        distList.Add(distMaxX);
        distList.Add(distMinZ);
        distList.Add(distMaxZ);
      }
      else if (!viewDirection.Z.IsAlmostEqualTo(0)) {
        distList.Add(distMinX);
        distList.Add(distMaxX);
        distList.Add(distMinY);
        distList.Add(distMaxY);
      }
      var distMin = distList.Min();

      if (distMin.Equals(distMinX)) {
        var x = LinearInterpolation(equipBBMin.X, connPt.X, d);
        var point = new XYZ(x, connPt.Y, connPt.Z);
        return new TextInfo(point, LeaderDirection.NX);
      }
      else if (distMin.Equals(distMaxX)) {
        var x = LinearInterpolation(connPt.X, equipBBMax.X, d);
        var point = new XYZ(x, connPt.Y, connPt.Z);
        return new TextInfo(point, LeaderDirection.PX);
      }
      else if (distMin.Equals(distMinY)) {
        var y = LinearInterpolation(equipBBMin.Y, connPt.Y, d);
        var point = new XYZ(connPt.X, y, connPt.Z);
        return new TextInfo(point, LeaderDirection.NY);
      }
      else if (distMin.Equals(distMaxY)) {
        var y = LinearInterpolation(connPt.Y, equipBBMax.Y, d);
        var point = new XYZ(connPt.X, y, connPt.Z);
        return new TextInfo(point, LeaderDirection.PY);
      }
      else if (distMin.Equals(distMinZ)) {
        var z = LinearInterpolation(equipBBMin.Z, connPt.Z, d);
        var point = new XYZ(connPt.X, connPt.Y, z);
        return new TextInfo(point, LeaderDirection.NZ);
      }
      else if (distMin.Equals(distMaxZ)) {
        var z = LinearInterpolation(connPt.Z, equipBBMax.Z, d);
        var point = new XYZ(connPt.X, connPt.Y, z);
        return new TextInfo(point, LeaderDirection.PZ);
      }
      return null;
    }

    private ViewSchedule CreateSchedule(Document doc, TextNoteType textnoteType,
            int descriptionLength) {
      var schedule = ViewSchedule.CreateSchedule(doc,
          Category.GetCategory(doc, BuiltInCategory.OST_PlaceHolderPipes).Id);
      var fileName = Path.GetFileNameWithoutExtension(FamilyFilePath.FileFullPath);
      schedule.Name = fileName;

      var textSize = textnoteType.get_Parameter(BuiltInParameter.TEXT_SIZE).AsDouble();

      var fieldIds = schedule.Definition.GetSchedulableFields();
      SchedulableField idAbleField = null;
      SchedulableField descriptionAbleField = null;
      foreach (var fieldId in fieldIds) {
        var field = fieldId.ParameterId.IntegerValue;
        if ((int)BuiltInParameter.ALL_MODEL_MARK == fieldId.ParameterId.IntegerValue) {
          idAbleField = fieldId;
        }
        if ((int)BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS == fieldId.ParameterId.IntegerValue) {
          descriptionAbleField = fieldId;
        }
      }

      var idField = schedule.Definition.AddField(idAbleField);
      idField.ColumnHeading = "ID";
      idField.HorizontalAlignment = ScheduleHorizontalAlignment.Center;
      idField.SheetColumnWidth = 3 * textSize;

      var descriptionField = schedule.Definition.AddField(descriptionAbleField);
      descriptionField.ColumnHeading = "POC NAME";
      descriptionField.HorizontalAlignment = ScheduleHorizontalAlignment.Left;

      var titleLen = fileName.Length;
      var descLen = 3 + descriptionLength < titleLen ? titleLen : descriptionLength;
      descriptionField.SheetColumnWidth = descLen * textSize;

      ScheduleFilter filter = new ScheduleFilter(idField.FieldId,
          ScheduleFilterType.HasValue);
      schedule.Definition.AddFilter(filter);

      var sort = new ScheduleSortGroupField(idField.FieldId, ScheduleSortOrder.Ascending);
      schedule.Definition.AddSortGroupField(sort);

      schedule.BodyTextTypeId = textnoteType.Id;
      schedule.HeaderTextTypeId = textnoteType.Id;

      return schedule;
    }

    private void OverloadCadFile(InfoByView cadFilePath) {
      var options = new DWGImportOptions {
        ThisViewOnly = true,
        CustomScale = _Scale,
        Placement = ImportPlacement.Origin,
      };

      var view = cadFilePath.View;
      var doc = view.Document;

      var viewDir = view.ViewDirection.Normalize();
      var res = doc.Import(cadFilePath.FileFullPath, options, view, out var dwgId);
      if (!res) { return; }

      var dwgFile = (ImportInstance)doc.GetElement(dwgId);
      dwgFile.Pinned = false;
      dwgFile.get_Parameter(BuiltInParameter.IMPORT_BACKGROUND).Set(0);
      dwgFile.Category.LineColor = new Color(0, 0, 255);
      var subCate = dwgFile.Category.SubCategories;
      foreach (Category item in subCate) {
        item.LineColor = new Color(0, 0, 255);
      }

      #region CAD 파일의 Center Point와 Family Bounding의 Center Point를 맞춰 배치코드
      //var insBb = _Instance.get_BoundingBox(view);
      //var insCenter = (insBb.Min + insBb.Max) / 2;
      //if (!viewDir.X.IsAlmostEqualTo(0)) {
      //  insCenter = new XYZ(insBb.Min.X, insCenter.Y, insCenter.Z);
      //}
      //if (!viewDir.Y.IsAlmostEqualTo(0)) {
      //  insCenter = new XYZ(insCenter.X, insBb.Min.Y, insCenter.Z);
      //}
      //if (!viewDir.Z.IsAlmostEqualTo(0)) {
      //  insCenter = new XYZ(insCenter.X, insCenter.Y, insBb.Min.Z);
      //}

      //var dwgBb = dwgFile.get_BoundingBox(view);
      //var center = (dwgBb.Min + dwgBb.Max) / 2;
      //var movePnt = insCenter - center;
      //ElementTransformUtils.MoveElement(doc, dwgId, movePnt);
      #endregion
    }

    private void ExportExcel(ViewSchedule schedule) {
      var directoryPath = Path.GetDirectoryName(FamilyFilePath.FileFullPath);
      var FamilyName = Path.GetFileNameWithoutExtension(FamilyFilePath.FileFullPath);
      var path = Path.Combine(directoryPath,
          $"{FamilyName}_정보_{DateTime.Now:yyyyMMddHHmmss}");
      path = Path.ChangeExtension(path, ".xlsx");

      var doc = schedule.Document;

      try {
        var excelApp = new Excel.Application();
        Excel.Workbook wb = excelApp.Workbooks.Add();
        Excel.Worksheet ws = wb.Worksheets[1];

        var table = schedule.GetTableData();
        var body = table.GetSectionData(SectionType.Body);

        ((Excel.Range)ws.Rows.Cells[1, 1]).Value = FamilyName;
        Excel.Range range = ws.Range[ws.Cells[1, 1], ws.Cells[1, body.NumberOfColumns]];
        range.Merge();

        for (int row = body.FirstRowNumber; row <= body.LastRowNumber; row++) {
          for (int col = body.FirstColumnNumber; col <= body.LastColumnNumber; col++) {
            var cellText = schedule.GetCellText(SectionType.Body, row, col);
            ((Excel.Range)ws.Rows.Cells[row + 2, col + 1]).Value = cellText;
          }
        }

        ws.Columns.AutoFit();
        wb.SaveAs(path);
        wb.Close();
        excelApp.Quit();

      } catch (Exception ex) {
        TaskDialog.Show("알림", "엑셀 생성 실패");
      }
      TaskDialog.Show("알림", "엑셀 생성 성공");

      return;
    }
  }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Project1.Revit.Common;
using Project1.Revit.Exportor.IPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Project1.Revit.Common.ClassificationCriteria;

namespace Project1.Revit.FbxNwcExportor {
  // FBX 파일명 기준
  // 건설 BIM : (_기준 2번째까지)_층_분할구역기둥열_대분류_중분류.fbx
  // H-up BIM : ProjectCode_FileCode_EqId_층_분할구역기둥열_대분류.fbx

  public class FbxExportOptionVM : ViewModelBase {
    private Document _Doc = null;
    private View3D _View = null;
    private BoundingBoxXYZ _ViewBoundBox = null;
    private ExportorObject _ExportorObject = null;

    private List<FbxExportInfo> _ExportInfos;
    private const string _NA = "NA";

    private static readonly List<string> _FileCodes = new List<string>() {
      "FileCode1",
      "FileCode2",
      "FileCode3",
      "FileCode4",
      "FileCode5",
      "FileCode6",
      "FileCode7",
      "FileCode8",
      "FileCode9",
      "FileCode10",
    };
    private string _FileCode = _NA;

    private const string _Roof = "ROOF FL";
    private static readonly List<string> _LevelNaming = new List<string>() {
      "st FL",
      "nd FL",
      "rd FL",
      "th FL",
      _Roof
    };


    #region 바인딩 목록
    private string _ExportSaveDirectory;
    private bool _IsGridMode;
    private bool _IsToolMode;
    private bool _IsCropGrid;
    private bool _IsExportFbxAndCsv;
    private bool _IsExportFbx;
    private bool _IsExportCsv;

    private Grid _SelectedXGrid0;
    private Grid _SelectedXGrid1;
    private Grid _SelectedYGrid0;
    private Grid _SelectedYGrid1;
    private List<Grid> _XGridList;
    private List<Grid> _YGridList;
    private List<Level> _Levels;
    private Level _StartLevel;
    private Level _EndLevel;
    private double _Offset;
    private string _ProjectCode;
    private bool _IsSingleTool;
    private string _ToolName;

    public string ExportSaveDirectory {
      get { return _ExportSaveDirectory; }
      set { Set(ref _ExportSaveDirectory, value); }
    }
    public bool IsGridMode {
      get { return _IsGridMode; }
      set { Set(ref _IsGridMode, value); }
    }
    public bool IsToolMode {
      get { return _IsToolMode; }
      set { Set(ref _IsToolMode, value); }
    }
    public bool IsCropGrid {
      get { return _IsCropGrid; }
      set { Set(ref _IsCropGrid, value); }
    }
    public bool IsExportFbxAndCsv {
      get { return _IsExportFbxAndCsv; }
      set { Set(ref _IsExportFbxAndCsv, value); }
    }
    public bool IsExportFbx {
      get { return _IsExportFbx; }
      set { Set(ref _IsExportFbx, value); }
    }
    public bool IsExportCsv {
      get { return _IsExportCsv; }
      set { Set(ref _IsExportCsv, value); }
    }

    public Grid StartXGrid {
      get { return _SelectedXGrid0; }
      set { Set(ref _SelectedXGrid0, value); }
    }
    public Grid EndXGrid {
      get { return _SelectedXGrid1; }
      set { Set(ref _SelectedXGrid1, value); }
    }
    public Grid StartYGrid {
      get { return _SelectedYGrid0; }
      set { Set(ref _SelectedYGrid0, value); }
    }
    public Grid EndYGrid {
      get { return _SelectedYGrid1; }
      set { Set(ref _SelectedYGrid1, value); }
    }
    public List<Grid> XGridList {
      get { return _YGridList; }
      set { Set(ref _YGridList, value); }
    }
    public List<Grid> YGridList {
      get { return _XGridList; }
      set { Set(ref _XGridList, value); }
    }
    public List<Level> Levels {
      get { return _Levels; }
      set { Set(ref _Levels, value); }
    }
    public Level StartLevel {
      get { return _StartLevel; }
      set { Set(ref _StartLevel, value); }
    }
    public Level EndLevel {
      get { return _EndLevel; }
      set { Set(ref _EndLevel, value); }
    }

    public double Offset {
      get { return _Offset; }
      set { Set(ref _Offset, value); }
    }
    public string ProjectCode {
      get { return _ProjectCode; }
      set { Set(ref _ProjectCode, value); }
    }
    public bool IsSingleTool {
      get { return _IsSingleTool; }
      set { Set(ref _IsSingleTool, value); }
    }
    public string ToolName {
      get { return _ToolName; }
      set { Set(ref _ToolName, value); }
    }

    public RelayCommand CmdSelectDirectory { get; set; }
    public RelayCommand CmdFbxExport { get; set; }
    #endregion


    public FbxExportOptionVM() {
      YGridList = new List<Grid>();
      XGridList = new List<Grid>();
      Levels = new List<Level>();

      IsGridMode = true;
      IsToolMode = false;
      IsExportFbxAndCsv = true;

      Offset = 700;

      CmdSelectDirectory = new RelayCommand(SelectSaveDirectory);
      CmdFbxExport = new RelayCommand(RunButtonFbxExport);
    }

    /// <summary>
    /// document에 있는 기본 정보들을 로딩
    /// </summary>
    public void GetBasicDoumentInfos(Document doc) {
      _Doc = doc;
      if (doc.ActiveView is View3D view3D) { _View = view3D; }

      var grids = doc.TypeElements(typeof(Grid));
      foreach (Grid grid in grids) {
        var name = grid.Name;
        if (!name.First().Equals('X') && !name.First().Equals('Y')) { continue; }
        var gridInx = name.Remove(0, 1);
        if (!int.TryParse(gridInx, out var inx)) { continue; }

        if (grid.Curve is Line == false) { continue; }
        var line = (Line)grid.Curve;
        var direc = line.Direction.Normalize();
        if (direc.IsSameDirection(XYZ.BasisX)) {
          YGridList.Add(grid);
        } else if (direc.IsSameDirection(XYZ.BasisY)) {
          XGridList.Add(grid);
        }
      }

      for (int i = 0; i < XGridList.Count - 1; i++) {
        var grid0 = XGridList[i];
        for (int j = i + 1; j < XGridList.Count; j++) {
          var grid1 = XGridList[j];

          if (grid0.Curve is Line line0 && grid1.Curve is Line line1) {
            if (line0.Origin.X > line1.Origin.X) {
              var temp = grid0;
              XGridList[i] = grid1;
              XGridList[j] = temp;
              grid0 = grid1;
            }
          }
        }
      }
      for (int i = 0; i < YGridList.Count - 1; i++) {
        var grid0 = YGridList[i];
        for (int j = i + 1; j < YGridList.Count; j++) {
          var grid1 = YGridList[j];

          if (grid0.Curve is Line line0 && grid1.Curve is Line line1) {
            if (line0.Origin.Y > line1.Origin.Y) {
              var temp = grid0;
              YGridList[i] = grid1;
              YGridList[j] = temp;
              grid0 = grid1;
            }
          }
        }
      }
      if (XGridList.Count > 1) {
        StartXGrid = XGridList[0];
        EndXGrid = XGridList[1];
      }
      if (YGridList.Count > 1) {
        StartYGrid = YGridList[0];
        EndYGrid = YGridList[1];
      }

      var levels = doc.TypeElements(typeof(Level));
      foreach (Level level in levels) {
        var levelName = level.Name.ToUpper();

        foreach (var item in _LevelNaming) {
          if (levelName.Contains(item.ToUpper())) {
            Levels.Add(level);
            break;
          }
        }
      }
      Levels.Sort((a, b) => a.Elevation.CompareTo(b.Elevation));
      if (Levels.Count > 1) {
        StartLevel = Levels[0];
        EndLevel = Levels[1];
      }

      ExportSaveDirectory = Path.GetDirectoryName(doc.PathName);

      foreach (var naming in _FileCodes) {
        if (_Doc.Title.ToUpper().Contains(naming.ToUpper())) {
          _FileCode = naming;
        }
      }
      return;
    }

    public Result RunIpcFbxExport(Document doc) {
      _ExportorObject = App.Current.ExportorObject;
      _Doc = doc;

      var exportView = ExportorCommonMethod.GetView3D(doc);
      if (exportView == null) { return Result.Cancelled; }
      GetBasicDoumentInfos(doc);

      StartXGrid = null;
      EndXGrid = null;
      StartYGrid = null;
      EndYGrid = null;

      IsGridMode = _ExportorObject.IsGridMode;
      IsToolMode = _ExportorObject.IsToolMode;
      IsCropGrid = _ExportorObject.IsCropGrid;
      IsExportFbxAndCsv = _ExportorObject.IsExportFbxAndCsv;
      IsExportFbx = _ExportorObject.IsExportFbx;
      IsExportCsv = _ExportorObject.IsExportCsv;
      if (IsGridMode || IsCropGrid) {
        foreach (var item in XGridList) {
          var name = item.Name.ToUpper();
          if (_ExportorObject.StartXGrid.ToUpper().Equals(name)) {
            StartXGrid = item;
          }
          if (_ExportorObject.EndXGrid.ToUpper().Equals(name)) {
            EndXGrid = item;
          }
        }
        foreach (var item in YGridList) {
          var name = item.Name.ToUpper();
          if (_ExportorObject.StartYGrid.ToUpper().Equals(name)) {
            StartYGrid = item;
          }
          if (_ExportorObject.EndYGrid.ToUpper().Equals(name)) {
            EndYGrid = item;
          }
        }

        if (StartXGrid == null || EndXGrid == null ||
            StartYGrid == null || EndYGrid == null) {
          if (StartXGrid == null || StartYGrid == null) {
            var notExist = string.Empty;
            if (StartXGrid == null) { notExist = $"{_ExportorObject.StartXGrid}"; }
            if (StartYGrid == null) {
              if (!string.IsNullOrEmpty(notExist)) { notExist += $", "; }
              notExist += $"{_ExportorObject.StartYGrid}";
            }
            ExportorCommonMethod.WriteLogMessage($"선택한 시작 그리드[{notExist}]가 없습니다");
          }
          if (EndXGrid == null || EndYGrid == null) {
            var notExist = string.Empty;
            if (EndXGrid == null) { notExist = $"{_ExportorObject.EndXGrid}"; }
            if (EndYGrid == null) {
              if (!string.IsNullOrEmpty(notExist)) { notExist += $", "; }
              notExist += $"{_ExportorObject.EndYGrid}";
            }
            ExportorCommonMethod.WriteLogMessage($"선택한 종료 그리드[{notExist}]가 없습니다");
          }
          return Result.Failed;
        }
      }
      if (IsToolMode) {
        ProjectCode = _ExportorObject.ProjectCode;
        IsSingleTool = _ExportorObject.IsSingleTool;
        ToolName = _ExportorObject.ToolName;
      }

      StartLevel = null;
      EndLevel = null;
      foreach (var item in Levels) {
        var name = item.Name.ToUpper();
        if (_ExportorObject.StartLevel.ToUpper().Equals(name)) {
          StartLevel = item;
        }
        if (_ExportorObject.EndLevel.ToUpper().Equals(name)) {
          EndLevel = item;
        }
      }
      if (_ExportorObject.EndLevel.ToUpper().Equals(_Roof.ToUpper()) && EndLevel == null) {
        EndLevel = Levels[Levels.Count - 1];
      }
      if (StartLevel == null || EndLevel == null) {
        if (StartLevel == null) {
          ExportorCommonMethod.WriteLogMessage($"선택한 시작 Level[{_ExportorObject.StartLevel}]이 없습니다");
        }
        if (EndLevel == null) {
          ExportorCommonMethod.WriteLogMessage($"선택한 종료 Level[{_ExportorObject.EndLevel}]이 없습니다");
        }
        return Result.Failed;
      }

      _View = exportView;
      Offset = _ExportorObject.Offset;

      using (var tx = new Transaction(doc,
          "FBX Export")) {
        try {
          tx.Start();
          if (_View.IsSectionBoxActive) {
            _View.IsSectionBoxActive = false;
            doc.Regenerate();
          }
          ExportByOptions();
          tx.RollBack();
        } catch (Exception ex) {
          tx.RollBack();
          ExportorCommonMethod.WriteLogMessage($"{ex}");
          return Result.Failed;
        }
      }

      return Result.Succeeded;
    }

    private void RunButtonFbxExport() {
      using (var tx = new Transaction(_Doc,
           "FBX Export")) {
        try {
          tx.Start();
          ExportByOptions();
          tx.RollBack();
        } catch (Exception ex) {
          tx.RollBack();
          var exMsg = string.Format(
              "Err Transaction Name => {0}", tx.GetName());
        }
      }
    }

    private void SelectSaveDirectory() {
      var dlg = new FolderBrowserDialog {
        Description = "내보내기 파일 저장할 폴더를 선택해주세요",
        SelectedPath = ExportSaveDirectory,
      };
      var show = dlg.ShowDialog();
      if (show == DialogResult.OK) {
        ExportSaveDirectory = dlg.SelectedPath;
      }
    }

    private static bool IsExportElement(Element elem) {
      if (elem.GetMEPModel() != null) {
        return true;
      } else if (elem is MEPCurve) {
        return true;
      } else if (elem.GetBuiltInCategory() == BuiltInCategory.OST_GenericModel) {
        return true;
      }

      return false;
    }

    private void ExportByOptions() {
      if (StartLevel == null || EndLevel == null) { return; }

      if (IsGridMode) { GetLinkFiles(); }
      var allElems = _Doc.ElementCollector(_View.Id);
      var hideCates = new HashSet<ElementId>();
      var hideElems = new HashSet<ElementId>();
      foreach (var item in allElems) {
        if (IsToolMode && IsExportElement(item)) { continue; }

        var cateId = item?.Category?.Id;
        if (cateId == null) { continue; }

        if (IsGridMode) {
          if (cateId.IntegerValue == ((int)BuiltInCategory.OST_RvtLinks)) {
            continue;
          }
        }

        if (!item.CanBeHidden(_View)) { continue; }
        hideCates.Add(cateId);
        hideElems.Add(item.Id);
      }
      if (IsGridMode && hideElems.Any()) {
        _View.HideElements(hideElems);
      } else if (IsToolMode && hideCates.Any()) {
        _View.HideCategoriesTemporary(hideCates);
      }
      _View.ConvertTemporaryHideIsolateToPermanent();

      _View.IsSectionBoxActive = true;
      var sb = _View.GetSectionBox();
      var ori = sb.Transform.Origin;
      var min = ori + sb.Min;
      var max = ori + sb.Max;
      var bb = new BoundingBoxXYZ {
        Min = min,
        Max = max
      };
      _ViewBoundBox = bb;
      _View.IsSectionBoxActive = false;

      if (StartLevel.Elevation > EndLevel.Elevation) {
        var tempLevel = StartLevel;
        StartLevel = EndLevel;
        EndLevel = tempLevel;
      }

      CsvFileExport._Doc = _Doc;
      CsvFileExport._ExportSaveDirectory = _ExportSaveDirectory;
      CsvFileExport._IsGridMode = _IsGridMode;
      CsvFileExport._IsToolMode = _IsToolMode;
      CsvFileExport._View = _View;

      if (IsGridMode) { IterateGrid(); } else if (IsToolMode) { GetToolByEqId(); }
    }

    private void GetToolByEqId() {
      var elems = _Doc.ElementCollector(_View.Id);
      var exportInfos = new List<FbxExportInfo>();

      foreach (var item in elems) {
        var toolName = ProjectParameterList.Read(item).EqId;

        if (string.IsNullOrEmpty(toolName) && item.IsMechanicalEquipment()) {
          toolName = item.Name;
        }
        if (string.IsNullOrEmpty(toolName)) { continue; }

        var temp = toolName.Split('_');
        if (!temp.Any()) { continue; }
        toolName = temp.First();

        // 단일로 추출시
        if (IsSingleTool && !toolName.Equals(toolName)) { continue; }

        exportInfos.AddItem(toolName, item);
      }

      Offset = 0;
      if (IsCropGrid) {
        StartXGrid = XGridList[0];
        EndXGrid = XGridList[XGridList.Count - 1];
        StartYGrid = YGridList[0];
        EndYGrid = YGridList[YGridList.Count - 1];
        IterateGrid();
      } else if (IsToolMode) {
        var min = _ViewBoundBox.Min;
        var max = _ViewBoundBox.Max;
        CropSectionBox(min.X, min.Y, max.X, max.Y, string.Empty);
      }
    }

    private void GetLinkFiles() {
      var doc = _Doc;
      _ExportInfos = new List<FbxExportInfo>();

      var linkInstances = doc.TypeElements(typeof(RevitLinkInstance));
      foreach (RevitLinkInstance link in linkInstances) {
        var typeId = link.GetTypeId();
        var type = doc.GetElement(typeId);
        if (type is RevitLinkType linkType) {
          if (linkType.GetLinkedFileStatus() != LinkedFileStatus.Loaded) { continue; }
        }

        var linkName = link.Name;
        var splitNames = linkName.Split('_');
        GetConstructionCategory(splitNames, out var mainCate, out var subCate);

        var categories = $"{mainCate}_{subCate}";
        var names = string.Empty;
        if (splitNames.Count() > 0) {
          names = $"{splitNames[0]}_";
          if (splitNames.Count() > 1) {
            var sec = splitNames[1];
            if (sec.Length > 3) { sec = sec.Remove(3); }
            names += sec;
          } else { names += _NA; }
        } else { names = $"{_NA}_{_NA}"; }

        categories = $"{names}_{categories}";
        _ExportInfos.AddItem(categories, link);
      }
    }


    private void IterateGrid() {
      var xInx0 = XGridList.IndexOf(StartXGrid);
      var xInx1 = XGridList.IndexOf(EndXGrid);
      var startX = xInx0 < xInx1 ? xInx0 : xInx1;
      var endX = xInx0 > xInx1 ? xInx0 : xInx1;

      var yInx0 = YGridList.IndexOf(StartYGrid);
      var yInx1 = YGridList.IndexOf(EndYGrid);
      var startY = yInx0 < yInx1 ? yInx0 : yInx1;
      var endY = yInx0 > yInx1 ? yInx0 : yInx1;

      for (int x = startX; x < endX; x++) {
        var xGrid0 = XGridList[x];
        var xGrid1 = XGridList[x + 1];
        var xCurve0 = ((Line)xGrid0.Curve).Origin;
        var xCurve1 = ((Line)xGrid1.Curve).Origin;
        for (int y = startY; y < endY; y++) {
          var yGrid0 = YGridList[y];
          var yGrid1 = YGridList[y + 1];
          var yCurve0 = ((Line)yGrid0.Curve).Origin;
          var yCurve1 = ((Line)yGrid1.Curve).Origin;

          var minX = xCurve0.X < xCurve1.X ? xCurve0.X : xCurve1.X;
          var maxX = xCurve0.X > xCurve1.X ? xCurve0.X : xCurve1.X;
          var minY = yCurve0.Y < yCurve1.Y ? yCurve0.Y : yCurve1.Y;
          var maxY = yCurve0.Y > yCurve1.Y ? yCurve0.Y : yCurve1.Y;

          var gridInfo = $"{xGrid0.Name}{yGrid0.Name}";
          CropSectionBox(minX, minY, maxX, maxY, gridInfo);
        }
      }
    }

    private void CropSectionBox(double minX, double minY, double maxX, double maxY, string gridInfo) {
      var startInx = Levels.IndexOf(StartLevel);
      var endInx = Levels.IndexOf(EndLevel);
      for (int levelInx = startInx; levelInx <= endInx; levelInx++) {
        double minZ, maxZ;
        var level0 = Levels[levelInx];
        var elevation0 = level0.Elevation;

        if (levelInx != Levels.Count() - 1) {
          var level1 = Levels[levelInx + 1];
          var elevation1 = level1.Elevation;
          minZ = Math.Min(elevation0, elevation1);
          maxZ = Math.Max(elevation0, elevation1);
        } else {
          minZ = elevation0;
          maxZ = _ViewBoundBox.Max.Z;
          if (minZ > maxZ) {
            System.Diagnostics.Debug.WriteLine($"{level0.Name}");
            return;
          }
        }

        var levelForge = level0.get_Parameter(BuiltInParameter.LEVEL_ELEV)?.GetUnitTypeId();
        var offset = UnitUtils.ConvertToInternalUnits(Offset, levelForge);
        var minPnt = new XYZ(minX - offset, minY - offset, minZ - offset);
        var maxPnt = new XYZ(maxX + offset, maxY + offset, maxZ + offset);
        var bb = new BoundingBoxXYZ {
          Min = minPnt,
          Max = maxPnt
        };
        _View.SetSectionBox(bb);
        _Doc.Regenerate();

        ExportInSectionBox(level0, gridInfo);
      }
    }


    private void ExportInSectionBox(Level level, string gridInfo) {
      var levelName = string.Empty;
      foreach (var item in level.Name) {
        if (!char.IsDigit(item)) { break; }
        levelName += item;
      }
      if (string.IsNullOrEmpty(levelName)) { levelName = level.Name; } else { levelName += "F"; }

      if (IsGridMode) {
        if (_ExportInfos != null && _ExportInfos.Any()) {
          BasisLinkFileName($"{levelName}_{gridInfo}");
        }
      } else if (IsToolMode) {
        var doc = _Doc;

        foreach (var info in _ExportInfos) {
          var ToolName = info.CategoryName;

          var exportElems = new List<Element>();
          var viewedElems = doc.ElementCollector(_View.Id).ToList();
          var ids = new HashSet<ElementId>();
          var elems = info.Elements;
          foreach (var item in elems) { ids.Add(item.Id); }

          foreach (var item in viewedElems) {
            if (ids.Contains(item.Id)) { exportElems.Add(item); }
          }

          if (!IsCropGrid) {
            Element equip = null;
            foreach (var item in elems) {
              if (item.IsMechanicalEquipment()) {
                equip = item;
              }
            }
            var pnt = equip.GetLocationPoint();
            if (pnt != null) { gridInfo = GetGridInfo(pnt); }
            if (string.IsNullOrEmpty(gridInfo)) { gridInfo = _NA; }
          }


          if (!exportElems.Any()) { continue; }
          var name = $"{ProjectCode}_{_FileCode}_{ToolName}_{levelName}_{gridInfo}";
          IsolateByWorkType(name, exportElems);
        }
      }
    }

    /// <summary>
    /// XYZ 좌표를 통하여 어느 그리드 범주에 있는지 파악
    /// </summary>
    /// <param name="pnt"></param>
    /// <returns></returns>
    private string GetGridInfo(XYZ pnt) {
      var gridInfo = string.Empty;
      for (int i = 0; i < XGridList.Count - 1; i++) {
        var xGrid0 = XGridList[i];
        var xGrid1 = XGridList[i + 1];
        if (xGrid0.Curve is Line line0 && xGrid1.Curve is Line line1) {
          if (pnt.X > line0.Origin.X && pnt.X < line1.Origin.X) {
            gridInfo += xGrid0.Name;
            break;
          }
        }
      }
      for (int i = 0; i < YGridList.Count - 1; i++) {
        var yGrid0 = YGridList[i];
        var yGrid1 = YGridList[i + 1];
        if (yGrid0.Curve is Line line0 && yGrid1.Curve is Line line1) {
          if (pnt.Y > line0.Origin.Y && pnt.Y < line1.Origin.Y) {
            gridInfo += yGrid0.Name;
            break;
          }
        }
      }

      if (string.IsNullOrEmpty(gridInfo)) { return _NA; }
      return gridInfo;
    }


    private void IsolateByWorkType(string name, List<Element> elems) {
      var infos = new List<FbxExportInfo>();
      foreach (var item in elems) {
        if (item.Location == null) { continue; }

        var category = GetCategoryName(item);
        infos.AddItem(category, item);
      }

      name = ExportorCommonMethod.RemoveInvalidChars(name);
      foreach (var info in infos) {
        var fbxName = $"{name}_{info.CategoryName}";
        var ids = new HashSet<ElementId>();
        var infoElems = info.Elements;
        foreach (var item in infoElems) {
          ids.Add(item.Id);
        }
        if (!ids.Any()) { continue; }

        ExoortFbx(fbxName, ids.ToList(), null);
      }

      if (!IsExportFbx) { CsvFileExport.ExportCsvInfo(name, infos); }
    }

    private string GetCategoryName(Element element) {
      var category = Utilites.Util_NA;

      var value = ProjectParameterList.Read(element).UtilityCategory;
      if (string.IsNullOrEmpty(value)) {
        switch (element.GetBuiltInCategory()) {
          case BuiltInCategory.OST_GenericModel:
            return Utilites.Util_ICase;
          case BuiltInCategory.OST_CableTray:
          case BuiltInCategory.OST_CableTrayFitting:
            return Utilites.Util_JCase;
          case BuiltInCategory.OST_MechanicalEquipment:
            return Utilites.Util_GCase;
        }
      } else {
        foreach (var kvp in Utilites.UtilityDictionary) {
          foreach (var cate in kvp.Value) {
            if (cate.ToUpper().Equals(value.ToUpper())) {
              return kvp.Key;
            }
          }
        }
      }

      return category;
    }

    private void GetConstructionCategory(string[] splitNames,
        out string mainCatecory, out string subCatecory) {
      mainCatecory = Constructions.ConstructionSub_NA;
      subCatecory = Constructions.ConstructionSub_NA;

      foreach (var kvp in Constructions.ConstructionBim) {
        for (int i = 0; i < splitNames.Count(); i++) {
          var splited = splitNames[i];
          if (splited.Equals(kvp.Key)) {
            mainCatecory = splited;

            for (int j = i + 1; j < splitNames.Count(); j++) {
              var splitedMid = splitNames[j];
              foreach (var item in kvp.Value) {
                if (splitedMid.Contains(item)) {
                  subCatecory = item;
                  return;
                }
              }
            }
          }
        }
      }
      return;
    }

    private void BasisLinkFileName(string levelGrid) {
      var categoryInfos = new List<FbxExportInfo>();
      foreach (var info in _ExportInfos) {
        var splits = info.CategoryName.Split('_');
        var fileName = $"{splits[0]}_{splits[1]}";
        var category = $"{splits[2]}_{splits[3]}";
        var fbxName = $"{fileName}_{levelGrid}_{category}";

        var elemInLinkFile = new List<Element>();
        var viewedIds = new List<ElementId>();
        var hideIds = new List<ElementId>();

        foreach (var item1 in _ExportInfos) {
          var links = item1.LinkInstances;
          if (!info.CategoryName.Equals(item1.CategoryName)) {
            foreach (var link in item1.LinkInstances) {
              hideIds.Add(link.Id);
            }
            continue;
          }
          foreach (var link in links) {
            viewedIds.Add(link.Id);
            View3D view = null;
            var linkViews = link.GetLinkDocument().TypeElements(typeof(View3D));
            foreach (View3D linkView in linkViews) {
              if (view == null) { view = linkView; }
              if (!linkView.IsTemplate) { view = linkView; }
            }

            var section = _View.GetSectionBox();
            var outline = new Outline(section.Min, section.Max);
            var boundingFilter = new BoundingBoxIntersectsFilter(outline);
            var linkElems = link.GetLinkDocument().ElementCollector(view.Id)
                              .WherePasses(boundingFilter);
            elemInLinkFile.AddRange(linkElems.ToList());
          }
        }
        if (!elemInLinkFile.Any()) { continue; }

        var mainInfo = FbxExportInfoMethod.GetDefaultExportInfo(categoryInfos, fileName);
        mainInfo.SubCategoryInfos.Add(new FbxExportInfo(category, elemInLinkFile));
      }

      if (!IsExportFbx) {
        foreach (var info in categoryInfos) {
          CsvFileExport.ExportCsvInfo($"{info.CategoryName}_{levelGrid}", info.SubCategoryInfos);
        }
      }

      return;
    }

    private void ExoortFbx(string fbxName, List<ElementId> viewedIds, List<ElementId> hideIds) {
      if (IsExportCsv || viewedIds == null || !viewedIds.Any()) { return; }
      var doc = _Doc;
      fbxName = ExportorCommonMethod.RemoveInvalidChars(fbxName);

      using (var tx = new SubTransaction(doc)) {
        try {
          tx.Start();

          if (hideIds != null && hideIds.Any()) {
            _View.HideElements(hideIds);
          }
          _View.UnhideElements(viewedIds);
          _View.IsolateElementsTemporary(viewedIds);
          _View.ConvertTemporaryHideIsolateToPermanent();
          doc.Regenerate();

          var viewSet = new ViewSet();
          viewSet.Insert(_View);

          var directory = ExportSaveDirectory;
          if (_ExportorObject != null) { directory = _ExportorObject.ExportSaveDirectory; }
          doc.Export(directory, fbxName, viewSet, new FBXExportOptions());

          tx.RollBack();
        } catch (Exception ex) {
          var exMsg = string.Format(
              "Err Transaction Name => SubTx");
          if (_ExportorObject != null) {
            ExportorCommonMethod.WriteLogMessage($"{ex}");
          }
          tx.RollBack();
        }
      }
    }
  }
}

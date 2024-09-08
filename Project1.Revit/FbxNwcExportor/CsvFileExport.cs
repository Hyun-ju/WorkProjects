using Autodesk.Revit.DB;
using CsvHelper;
using Project1.Revit.Common;
using System.Collections.Generic;
using System.IO;

namespace Project1.Revit.FbxNwcExportor {
  public class CsvFileExport {
    public class CsvHeaderNames {
      public const string FileName = "FILENAME"; // FBX File Name
      public const string VizId = "VIZ_ID"; // FBX 파일명 + Element ID
      /// <summary>
      /// 장비: 장비_CODE / 배관: EQ_ID
      /// </summary>
      public const string ToolCode = "Tool_CODE";
      public const string VizFbxId = "VIZ_FBX_ID"; // Element Id
      public const string VizRevitId = "VIZ_REVIT_ID"; // Element Id
      public const string VizRevitFileCode = "VIZ_REVIT_FILE_CODE";
      public const string VizFamilyName = "VIZ_FAMILY_NAME";
      public const string VizFamilyType = "VIZ_FAMILY_TYPE";
      //public const string EquipmentType = "EQUIPMENT_TPYE"; // MODULE
      public const string EquipmentType = "EQUIPMENT_TYPE"; // MODULE
      public const string EquipmentName = "EQUIPMENT_NAME"; // Family Name
      public const string AreaName = "AREA_NAME"; // AREA
      public const string Marker = "MAKER"; // MAKER
      public const string ModelName = "MODEL_NAME"; // MODEL
      public const string FairGroupName = "FAIRGROUP_NAME";
      public const string ModuleName = "MODULE_NAME"; //EQ_MODEULE
      public const string SubMake = "SUB_MAKE";
      public const string SubModelCode = "SUB_MODEL_CODE";
      public const string Name = "NAME"; // Family and Type
      public const string Template = "TEMPLATE"; // Family and Type
      public const string SubType = "SUBTYPE";
      public const string Area = "AREA";
      public const string PartSize = "PART_SIZE"; // SIZE
      public const string Bom = "BOM"; // Length
      public const string BomUnit = "BOM_UNIT";
      public const string ConnectionId = "CONNECTIONID";
      public const string Materials = "MATERIALS"; // MATERIAL
      public const string Utilites = "UTILTIES";  //UTILITY
      public const string BoundingMinX = "BOUNDING_MIN_X";
      public const string BoundingMinY = "BOUNDING_MIN_Y";
      public const string BoundingMinZ = "BOUNDING_MIN_Z";
      public const string BoundingMaxX = "BOUNDING_MAX_X";
      public const string BoundingMaxY = "BOUNDING_MAX_Y";
      public const string BoundingMaxZ = "BOUNDING_MAX_Z";
      public const string Sizes = "SIZES"; // PoC_SIZE
      public const string Groups = "GROUPS"; // EQ_ID
      public const string PartCode = "PART_CODE"; // CODE
      public const string PartItem = "PART_ITEM"; //ITEM
      public const string PartConnectedType = "PART_CONNECTED_TYPE"; // Connection Type
      public const string PartMaterial = "PART_MATERIAL"; // MATERIAL
      public const string ObjectMinX = "OBJECT_MIN_X";
      public const string ObjectMinY = "OBJECT_MIN_Y";
      public const string ObjectMinZ = "OBJECT_MIN_Z";
      public const string ObjectMaxX = "OBJECT_MAX_X";
      public const string ObjectMaxY = "OBJECT_MAX_Y";
      public const string ObjectMaxZ = "OBJECT_MAX_Z";
      public const string InsulationsThickness = "INSULATIONS_THICKNESS";
      public const string Util = "UTIL";
      public const string Mate = "MATE";
      public const string Rev = "REV";
      public const string Item = "ITEM";
      public const string EqCode = "EQCODE";
      public const string RevDate = "REVDATE";
      public const string Phase = "PHASE";
      public const string Sc = "SC";
      public const string ConDate = "CONDATE";
      public const string BdCheck = "BD_CHECK";
      public const string Shop = "SHOP";
      public const string ScanDate = "SCANDATE";
      public const string Status = "STATUS";
      public const string NozzleNo = "NOZZLE_NO";
    }

    public static Document _Doc = null;
    public static View3D _View = null;
    public static string _ExportSaveDirectory;
    public static bool _IsGridMode;
    public static bool _IsToolMode;

    public static void ExportCsvInfo(string fbxName,
        List<FbxExportInfo> infos) {
      if (App.Current.ExportorObject != null) {
        _ExportSaveDirectory = App.Current.ExportorObject.ExportSaveDirectory;
      }

      var type = string.Empty;
      if (_IsGridMode) { type = "BIM"; } else if (_IsToolMode) { type = "5D"; }
      var path = Path.Combine(_ExportSaveDirectory, $"{fbxName}_{type}.csv");

      //using (var file = new StreamWriter(path, false, System.Text.Encoding.GetEncoding("euc-kr"))) {
      //  WriteCsvHeader(file);
      //  foreach (var kvp in workTypeDic) {
      //    foreach (var item in kvp.Value) {
      //      if (_IsGridMode) {
      //        ConstructionCsv($"{fbxName}_{kvp.Key}", item, file);
      //      }
      //      else if (_IsToolMode) {
      //        HookupCsv($"{fbxName}_{kvp.Key}", item, file);
      //      }
      //    }
      //  }
      //  file.Close();
      //}

      using (var file = new StreamWriter(path, false, System.Text.Encoding.UTF8))
      using (var csv = new CsvWriter(file, System.Globalization.CultureInfo.CurrentCulture)) {
        WriteCsvHeader(csv);
        foreach (var info in infos) {
          var elems = info.Elements;
          var name = info.CategoryName;
          foreach (var item in elems) {
            if (_IsGridMode) {
              ConstructionCsv($"{fbxName}_{name}", item, csv);
            } else if (_IsToolMode) {
              HookupCsv($"{fbxName}_{name}", item, csv);
            }
          }
        }
        csv.Flush();
      }
    }

    public static void ExportCsvInfo(string fbxName,
        Dictionary<string, List<Element>> workTypeDic) {
      if (App.Current.ExportorObject != null) {
        _ExportSaveDirectory = App.Current.ExportorObject.ExportSaveDirectory;
      }

      var type = string.Empty;
      if (_IsGridMode) { type = "BIM"; } else if (_IsToolMode) { type = "5D"; }
      var path = Path.Combine(_ExportSaveDirectory, $"{fbxName}_{type}.csv");

      //using (var file = new StreamWriter(path, false, System.Text.Encoding.GetEncoding("euc-kr"))) {
      //  WriteCsvHeader(file);
      //  foreach (var kvp in workTypeDic) {
      //    foreach (var item in kvp.Value) {
      //      if (_IsGridMode) {
      //        ConstructionCsv($"{fbxName}_{kvp.Key}", item, file);
      //      }
      //      else if (_IsToolMode) {
      //        HookupCsv($"{fbxName}_{kvp.Key}", item, file);
      //      }
      //    }
      //  }
      //  file.Close();
      //}

      using (var file = new StreamWriter(path, false, System.Text.Encoding.UTF8))
      using (var csv = new CsvWriter(file, System.Globalization.CultureInfo.CurrentCulture)) {
        WriteCsvHeader(csv);
        foreach (var kvp in workTypeDic) {
          foreach (var item in kvp.Value) {
            if (_IsGridMode) {
              ConstructionCsv($"{fbxName}_{kvp.Key}", item, csv);
            } else if (_IsToolMode) {
              HookupCsv($"{fbxName}_{kvp.Key}", item, csv);
            }
          }
        }
        csv.Flush();
      }
    }


    private static void WriteCsvHeader(CsvWriter csv) {
      if (_IsGridMode) {
        csv.WriteField(CsvHeaderNames.FileName);
        csv.WriteField(CsvHeaderNames.VizId);
        csv.WriteField(CsvHeaderNames.VizRevitId);
        csv.WriteField(CsvHeaderNames.VizRevitFileCode);
        csv.WriteField(CsvHeaderNames.VizFamilyName);
        csv.WriteField(CsvHeaderNames.VizFamilyType);
        csv.WriteField(CsvHeaderNames.InsulationsThickness);
        csv.WriteField(CsvHeaderNames.Util);
        csv.WriteField(CsvHeaderNames.Mate);
        csv.WriteField(CsvHeaderNames.Rev);
        csv.WriteField(CsvHeaderNames.Item);
        csv.WriteField(CsvHeaderNames.PartSize);
        csv.WriteField(CsvHeaderNames.EqCode);
        csv.WriteField(CsvHeaderNames.RevDate);
        csv.WriteField(CsvHeaderNames.Phase);
        csv.WriteField(CsvHeaderNames.Sc);
        csv.WriteField(CsvHeaderNames.ConDate);
        csv.WriteField(CsvHeaderNames.BdCheck);
        csv.WriteField(CsvHeaderNames.Shop);
        csv.WriteField(CsvHeaderNames.ScanDate);
        csv.WriteField(CsvHeaderNames.Status);
        csv.WriteField(CsvHeaderNames.NozzleNo);
        csv.WriteField(CsvHeaderNames.ObjectMinX);
        csv.WriteField(CsvHeaderNames.ObjectMinY);
        csv.WriteField(CsvHeaderNames.ObjectMinZ);
        csv.WriteField(CsvHeaderNames.ObjectMaxX);
        csv.WriteField(CsvHeaderNames.ObjectMaxY);
        csv.WriteField(CsvHeaderNames.ObjectMaxZ);
        csv.NextRecord();
      } else if (_IsToolMode) {
        csv.WriteField(CsvHeaderNames.FileName);
        csv.WriteField(CsvHeaderNames.VizId);
        csv.WriteField(CsvHeaderNames.ToolCode);
        csv.WriteField(CsvHeaderNames.VizFbxId);
        csv.WriteField(CsvHeaderNames.EquipmentType);
        csv.WriteField(CsvHeaderNames.EquipmentName);
        csv.WriteField(CsvHeaderNames.AreaName);
        csv.WriteField(CsvHeaderNames.Marker);
        csv.WriteField(CsvHeaderNames.ModelName);
        csv.WriteField(CsvHeaderNames.FairGroupName);
        csv.WriteField(CsvHeaderNames.ModuleName);
        csv.WriteField(CsvHeaderNames.SubMake);
        csv.WriteField(CsvHeaderNames.SubModelCode);
        csv.WriteField(CsvHeaderNames.Name);
        csv.WriteField(CsvHeaderNames.Template);
        csv.WriteField(CsvHeaderNames.SubType);
        csv.WriteField(CsvHeaderNames.Area);
        csv.WriteField(CsvHeaderNames.PartSize);
        csv.WriteField(CsvHeaderNames.Bom);
        csv.WriteField(CsvHeaderNames.BomUnit);
        csv.WriteField(CsvHeaderNames.ConnectionId);
        csv.WriteField(CsvHeaderNames.Materials);
        csv.WriteField(CsvHeaderNames.Utilites);
        csv.WriteField(CsvHeaderNames.BoundingMinX);
        csv.WriteField(CsvHeaderNames.BoundingMinY);
        csv.WriteField(CsvHeaderNames.BoundingMinZ);
        csv.WriteField(CsvHeaderNames.BoundingMaxX);
        csv.WriteField(CsvHeaderNames.BoundingMaxY);
        csv.WriteField(CsvHeaderNames.BoundingMaxZ);
        csv.WriteField(CsvHeaderNames.Sizes);
        csv.WriteField(CsvHeaderNames.Groups);
        csv.WriteField(CsvHeaderNames.PartCode);
        csv.WriteField(CsvHeaderNames.PartItem);
        csv.WriteField(CsvHeaderNames.PartConnectedType);
        csv.WriteField(CsvHeaderNames.PartMaterial);
        csv.WriteField(CsvHeaderNames.ObjectMinX);
        csv.WriteField(CsvHeaderNames.ObjectMinY);
        csv.WriteField(CsvHeaderNames.ObjectMinZ);
        csv.WriteField(CsvHeaderNames.ObjectMaxX);
        csv.WriteField(CsvHeaderNames.ObjectMaxY);
        csv.WriteField(CsvHeaderNames.ObjectMaxZ);
        csv.NextRecord();
      }
    }


    private static void HookupCsv(string fbxName, Element elem, CsvWriter csv) {
      var valueNAN = "N/A";

      var projectParams = ProjectParameterList.Read(elem);
      var familyType = elem
          .get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM)?.AsValueString();
      // 파라미터가 없을 시 null / 값이 없을 N/A
      if (elem.GetBuiltInCategory() == BuiltInCategory.OST_MechanicalEquipment) {

        csv.WriteField($"{CommaCheck(fbxName)}");
        csv.WriteField($"{CommaCheck(fbxName)}_{elem.Id}");
        csv.WriteField($"{(projectParams?.ToolCode != null ? CommaCheck(projectParams.ToolCode) : valueNAN)}");
        csv.WriteField($"{elem.Id.IntegerValue}");
        csv.WriteField($"{(projectParams?.Module != null ? CommaCheck(projectParams.Module) : valueNAN)}");
        var familyName = elem is FamilyInstance instance ? instance.Symbol.FamilyName : valueNAN;
        csv.WriteField($"{CommaCheck(familyName)}");
        csv.WriteField($"{(projectParams?.Area != null ? CommaCheck(projectParams.Area) : valueNAN)}");
        csv.WriteField($"{(projectParams?.Maker != null ? CommaCheck(projectParams.Maker) : valueNAN)}");
        csv.WriteField($"{(projectParams?.Model != null ? CommaCheck(projectParams.Model) : valueNAN)}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(familyType)}");

        csv.WriteField($"{CommaCheck(familyType)}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");

        var bb = elem.get_BoundingBox(_View);
        csv.WriteField(ConvertUnit(bb?.Min?.X));
        csv.WriteField(ConvertUnit(bb?.Min?.Y));
        csv.WriteField(ConvertUnit(bb?.Min?.Z));
        csv.WriteField(ConvertUnit(bb?.Max?.X));
        csv.WriteField(ConvertUnit(bb?.Max?.Y));

        csv.WriteField(ConvertUnit(bb?.Max?.Z));
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
      } else {
        csv.WriteField($"{CommaCheck(fbxName)}");
        csv.WriteField($"{CommaCheck(fbxName)}_{elem.Id}");
        csv.WriteField($"{CommaCheck(projectParams?.EqId)}");
        csv.WriteField($"{elem.Id.IntegerValue}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(projectParams?.EqModule)}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(familyType)}");

        csv.WriteField($"{CommaCheck(familyType)}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(projectParams?.Size)}");
        csv.WriteField($"{CommaCheck(elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)?.AsValueString())}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(projectParams?.Material)}");
        csv.WriteField($"{CommaCheck(projectParams?.Utility)}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{string.Empty}");

        csv.WriteField($"{string.Empty}");
        csv.WriteField($"{CommaCheck(projectParams?.PoCSize)}");
        csv.WriteField($"{CommaCheck(projectParams?.EqId)}");
        csv.WriteField($"{CommaCheck(projectParams?.Code)}");
        csv.WriteField($"{CommaCheck(projectParams?.Item)}");
        csv.WriteField($"{CommaCheck(projectParams?.ConType)}");
        csv.WriteField($"{CommaCheck(projectParams?.Material)}");

        var bb = elem.get_BoundingBox(_View);
        csv.WriteField(ConvertUnit(bb?.Min?.X));
        csv.WriteField(ConvertUnit(bb?.Min?.Y));
        csv.WriteField(ConvertUnit(bb?.Min?.Z));
        csv.WriteField(ConvertUnit(bb?.Max?.X));
        csv.WriteField(ConvertUnit(bb?.Max?.Y));
        csv.WriteField(ConvertUnit(bb?.Max?.Z));
      }
      csv.NextRecord();
    }
    private static void ConstructionCsv(string fbxName, Element elem, CsvWriter csv) {
      var doc = elem.Document;
      var type = doc.GetElement(elem.GetTypeId());

      var projectParams = ProjectParameterList.Read(elem);
      csv.WriteField($"{CommaCheck(fbxName)}");
      csv.WriteField($"{CommaCheck(fbxName)}_{elem.Id}");
      csv.WriteField($"{elem.Id.IntegerValue}");
      csv.WriteField($"{string.Empty}"); // VIZ_REVIT_FILE_CODE
      var familyName = elem
          .get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)?
          .AsValueString();
      csv.WriteField($"{CommaCheck(familyName)}"); // VIZ_FAMILY_NAME
      csv.WriteField($"{CommaCheck(type?.Name)}"); // VIZ_FAMILY_TYPE
      var insuThick = elem
          .get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS)?
          .AsValueString();
      csv.WriteField($"{CommaCheck(insuThick)}"); // INSULATIONS_THICKNESS
      csv.WriteField($"{CommaCheck(projectParams.Utility)}"); // UTIL
      csv.WriteField($"{CommaCheck(projectParams.Material)}"); // MATE
      csv.WriteField($"{CommaCheck(projectParams.Rev)}"); // REV
      csv.WriteField($"{CommaCheck(projectParams.Item)}"); // ITEM
      csv.WriteField($"{CommaCheck(projectParams.Size)}"); // PART_SIZE
      csv.WriteField($"{CommaCheck(projectParams.ToolCode)}"); // EQCODE
      csv.WriteField($"{CommaCheck(projectParams.RevDate)}"); // REVDATE

      csv.WriteField($"{CommaCheck(projectParams.Phase)}"); // PHASE
      csv.WriteField($"{CommaCheck(projectParams.ScCheck)}"); // SC
      csv.WriteField($"{CommaCheck(projectParams.ConDate)}"); // CONDATE
      csv.WriteField($"{CommaCheck(projectParams.BdCheck)}"); // BD_CHECK
      csv.WriteField($"{CommaCheck(projectParams.Shop)}"); // SHOP
      csv.WriteField($"{CommaCheck(projectParams.ScanDate)}"); // SCANDATE
      csv.WriteField($"{CommaCheck(projectParams.Status)}"); // STATUS
      csv.WriteField($"{CommaCheck(projectParams.C5NozzleNo)}"); // NOZZLE_NO

      var bb = elem.get_BoundingBox(_View);
      csv.WriteField(ConvertUnit(bb?.Min?.X)); // EQUIPMENT_MIN_X
      csv.WriteField(ConvertUnit(bb?.Min?.Y)); // EQUIPMENT_MIN_Y
      csv.WriteField(ConvertUnit(bb?.Min?.Z)); // EQUIPMENT_MIN_Z
      csv.WriteField(ConvertUnit(bb?.Max?.X)); // EQUIPMENT_MAX_X
      csv.WriteField(ConvertUnit(bb?.Max?.Y)); // EQUIPMENT_MAX_Y
      csv.WriteField(ConvertUnit(bb?.Max?.Z)); // EQUIPMENT_MAX_Z

      csv.NextRecord();
    }

    private static string ConvertUnit(double? value) {
      if (value == null) { return string.Empty; }

      var convertValue = value.Value;
      if (convertValue.IsAlmostEqualTo(0)) { convertValue = 0; }
      convertValue = UnitUtils
          .ConvertFromInternalUnits(convertValue, UnitTypeId.Millimeters);
      return convertValue.ToString();
    }

    private static string CommaCheck(string value) {
      //if (value == null) { return value; }
      //var newValue = value;

      //if (newValue.Contains(",")) {
      //  newValue = newValue.Replace(",", "_");
      //}

      //return newValue;
      return value;
    }
  }
}

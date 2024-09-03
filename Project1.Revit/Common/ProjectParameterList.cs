using Autodesk.Revit.DB;

namespace Project1.Revit.Common {
  public class ProjectParameterList {
    public static class DefinitionNames {
      public const string Code = "CODE";
      public const string Item = "ITEM";
      public const string Size = "SIZE";
      public const string ConType = "CON_TYPE";
      public const string Material = "MATERIAL";
      public const string Utility = "UTILITY";
      public const string No = "No";
      public const string EqId = "EQ_ID";
      public const string UtilityCategory = "UTILITY_CATEGORY";
      public const string ReqId = "REQ_ID";
      public const string Floor = "FLOOR";
      public const string Module = "MODULE";
      public const string PoCSize = "PoC_SIZE";
      public const string TappingNo = "Tapping_No";
    }

    public static class CsvDefinitionNames {
      public const string Rev = "REV";
      public const string EqCode = "EQCODE";
      public const string RevDate = "REVDATE";
      public const string Phase = "PHASE";
      public const string ScCheck = "SC_CHECK";
      public const string ConDate = "CONDATE";
      public const string BdCheck = "BD_CHECK";
      public const string Shop = "SHOP";
      public const string ScanDate = "SCANDATE";
      public const string Status = "STATUS";
    }

    /// <summary>
    /// 길이 산출 기능에서 추가한 파라미터
    /// </summary>
    public static class TKDefinitionNames {
      public const string SumBendingLength = "Project_Length";
    }

    /// <summary>
    /// 파라미터 쓰기 여부
    /// </summary>
    public struct Options {
      /// <summary>
      /// 기본 값 (모두 복사: 1차 배관 파라미터 제외)
      /// </summary>
      public static Options Default = new Options {
        Code = true,
        Item = true,
        Size = true,
        ConType = true,
        Material = true,
        Utility = true,
        PoCNo = true,
        EqId = true,
        UtilityCategory = true,
        ReqId = true,
        Floor = true,
        EqModule = true,
        PoCSize = true,
        TappingNo = true,
      };


      public bool Code;
      public bool Item;
      public bool Size;
      public bool ConType;
      public bool Material;
      public bool Utility;
      public bool PoCNo;
      public bool EqId;
      public bool UtilityCategory;
      public bool ReqId;
      public bool Floor;
      public bool EqModule;
      public bool PoCSize;
      public bool TappingNo;
    }


    public static ProjectParameterList Read(Element elem) {
      var result = new ProjectParameterList();

      foreach (Parameter param in elem.ParametersMap) {
        switch (param.Definition.Name) {
          case DefinitionNames.Code:
            result.Code = param.AsString();
            break;
          case DefinitionNames.Item:
            result.Item = param.AsString();
            break;
          case DefinitionNames.Size:
            result.Size = param.AsString();
            break;
          case DefinitionNames.ConType:
            result.ConType = param.AsString();
            break;
          case DefinitionNames.Material:
            result.Material = param.AsString();
            break;
          case DefinitionNames.Utility:
            result.Utility = param.AsString();
            break;
          case DefinitionNames.No:
            result.PoCNo = param.AsString();
            break;
          case DefinitionNames.EqId:
            result.EqId = param.AsString();
            break;
          case DefinitionNames.UtilityCategory:
            result.UtilityCategory = param.AsString();
            break;
          case DefinitionNames.ReqId:
            result.ReqId = param.AsString();
            break;
          case DefinitionNames.Floor:
            result.Floor = param.AsString();
            break;
          case DefinitionNames.Module:
            result.EqModule = param.AsString();
            break;
          case DefinitionNames.PoCSize:
            result.PoCSize = param.AsString();
            break;
          case DefinitionNames.TappingNo:
            result.TappingValveNo1 = param.AsString();
            break;

          case CsvDefinitionNames.Rev:
            result.Rev = param.AsString();
            break;
          case CsvDefinitionNames.EqCode:
            result.EqCode = param.AsString();
            break;
          case CsvDefinitionNames.RevDate:
            result.RevDate = param.AsString();
            break;
          case CsvDefinitionNames.Phase:
            result.Phase = param.AsString();
            break;
          case CsvDefinitionNames.ScCheck:
            result.ScCheck = param.AsString();
            break;
          case CsvDefinitionNames.ConDate:
            result.ConDate = param.AsString();
            break;
          case CsvDefinitionNames.BdCheck:
            result.BdCheck = param.AsString();
            break;
          case CsvDefinitionNames.Shop:
            result.Shop = param.AsString();
            break;
          case CsvDefinitionNames.ScanDate:
            result.ScanDate = param.AsString();
            break;
          case CsvDefinitionNames.Status:
            result.Status = param.AsString();
            break;
        }
      }

      return result;
    }

    public static void Write(Element elem,
        ProjectParameterList parameters) {
      Write(elem, parameters, Options.Default);
    }

    public static void Write(Element elem,
        ProjectParameterList parameters, Options options) {
      foreach (Parameter param in elem.ParametersMap) {
        if (param.IsReadOnly) { continue; }
        switch (param.Definition.Name) {
          case DefinitionNames.Code:
            if (options.Code) { param.Set(parameters.Code); }
            break;
          case DefinitionNames.Item:
            if (options.Item) { param.Set(parameters.Item); }
            break;
          case DefinitionNames.Size:
            if (options.Size) { param.Set(parameters.Size); }
            break;
          case DefinitionNames.ConType:
            if (options.ConType) { param.Set(parameters.ConType); }
            break;
          case DefinitionNames.Material:
            if (options.Material) { param.Set(parameters.Material); }
            break;
          case DefinitionNames.Utility:
            if (options.Utility) { param.Set(parameters.Utility); }
            break;
          case DefinitionNames.No:
            if (options.PoCNo) { param.Set(parameters.PoCNo); }
            break;
          case DefinitionNames.EqId:
            if (options.EqId) { param.Set(parameters.EqId); }
            break;
          case DefinitionNames.UtilityCategory:
            if (options.UtilityCategory) { param.Set(parameters.UtilityCategory); }
            break;
          case DefinitionNames.ReqId:
            if (options.ReqId) { param.Set(parameters.ReqId); }
            break;
          case DefinitionNames.Floor:
            if (options.Floor) { param.Set(parameters.Floor); }
            break;
          case DefinitionNames.PoCSize:
            if (options.PoCSize) { param.Set(parameters.PoCSize); }
            break;
          case DefinitionNames.Module:
            if (options.EqModule) { param.Set(parameters.EqModule); }
            break;
          case DefinitionNames.TappingNo:
            if (options.TappingNo) { param.Set(parameters.TappingValveNo1); }
            break;
        }
      }
    }

    public static void Copy(Element src, Element dest) {
      Copy(src, dest, Options.Default);
    }

    public static void Copy(Element src, Element dest, Options options) {
      var parameters = Read(src);
      Write(dest, parameters, options);
    }


    public string Code { get; set; }
    public string Item { get; set; }
    public string Size { get; set; }
    public string ConType { get; set; }
    public string Material { get; set; }
    public string Utility { get; set; }
    public string PoCNo { get; set; }
    public string EqId { get; set; }
    public string UtilityCategory { get; set; }
    public string ReqId { get; set; }
    public string Floor { get; set; }
    public string EqModule { get; set; }
    public string PoCSize { get; set; }
    public string TappingValveNo1 { get; set; }
    public string C5NozzleNo { get; set; }

    public string ToolCode { get; set; }
    public string Area { get; set; }
    public string Module { get; set; }
    public string Maker { get; set; }
    public string Model { get; set; }

    public string Rev { get; set; }
    public string EqCode { get; set; }
    public string RevDate { get; set; }
    public string Phase { get; set; }
    public string ScCheck { get; set; }
    public string ConDate { get; set; }
    public string BdCheck { get; set; }
    public string Shop { get; set; }
    public string ScanDate { get; set; }
    public string Status { get; set; }
  }
}

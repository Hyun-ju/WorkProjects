using Autodesk.Revit.DB;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Project1.Revit.Common {
  public class ConnectorInfo : ObservableObject {
    public static class ColumnNames {
      public const string Number = "번호";
      public const string PocNo = "PoC_No";
      public const string UTILITY = "UTILITY";
      public const string SIZE = "SIZE";
      public const string MATERIAL = "MATERIAL";
      public const string Type = "유형";
      public const string Diameter = "직경";
      public const string Direction = "방향";
      public const string Connected = "연결여부";
      public const string CON_TYPE = "연결유형";
      public const string SystemClassification = "시스템 분류";
    }
    private static readonly string[] _MiddleBasis = new string[] {
        "System1",
        "System2",
        "System3",
        "System4",
        "System5",
        "System6",
        "System7",
        "System8",
        "System9",
        "System10",
      };

    public static int SortByPoCNoAscending(ConnectorInfo a, ConnectorInfo b) {
      if (a.IsStick && b.IsStick) {
        return a.StickNo.CompareTo(b.StickNo);
      } else if (a.IsStick) {
        return 1;
      } else if (b.IsStick) {
        return -1;
      }

      var pocNoA = a.Num;
      var pocNoB = b.Num;
      try {
        if (pocNoA == null && pocNoB == null) {
          return 0;
        } else if (pocNoA == null) {
          return 1;
        } else if (pocNoB == null) {
          return -1;
        }
        var codeA = pocNoA.Substring(0, 1);
        var codeB = pocNoB.Substring(0, 1);
        var rst = string.Compare(codeA, codeB);
        if (rst != 0) { return rst; }

        if (!int.TryParse(pocNoA.Substring(1), out var noA)) {
          for (int i = 1; i < pocNoA.Length; i++) {
            if (!char.IsDigit(pocNoA[i])) {
              if (i > 1) {
                noA = int.Parse(pocNoA.Substring(1, i - 1));
                break;
              } else {
                noA = int.MaxValue;
              }
            }
          }
        }
        if (!int.TryParse(pocNoB.Substring(1), out var noB)) {
          for (int i = 1; i < pocNoB.Length; i++) {
            if (!char.IsDigit(pocNoB[i])) {
              if (i > 1) {
                noB = int.Parse(pocNoB.Substring(1, i - 1));
                break;
              } else {
                noB = int.MaxValue;
              }
            }
          }
        }
        rst = noA.CompareTo(noB);
        if (rst != 0) { return rst; }
      } catch (Exception e) {
      }
      return pocNoA.CompareTo(pocNoB);
    }

    public static int SortEquipmentFamilyConnector(ConnectorInfo a, ConnectorInfo b) {
      for (int i = 0; i < _MiddleBasis.Length; i++) {
        if (a.SystemTypeName == null) { return 1; }
        if (a.SystemTypeName.Contains(_MiddleBasis[i])) {
          for (int j = 0; j < _MiddleBasis.Length; j++) {
            if (b.SystemTypeName == null) { return -1; }
            if (b.SystemTypeName.Contains(_MiddleBasis[j])) {
              if (i == j) {
                var aPocNo = Regex.Replace(a.Num, @"[a-zA-Z]", string.Empty);
                var aIntRes = int.TryParse(aPocNo, out var aInt);
                var bPocNo = Regex.Replace(b.Num, @"[a-zA-Z]", string.Empty);
                var bIntRes = int.TryParse(bPocNo, out var bInt);
                if (aIntRes && bIntRes) {
                  return aInt - bInt;
                }

                return a.Num.CompareTo(b.Num);
              } else {
                return i - j;
              }
            }
          }
        }
      }
      return SortByPoCNoAscending(a, b);
    }

    public static Dictionary<string, List<ConnectorInfo>> CreateConnectorInfo(
        Document familyDoc) {
      var result = new Dictionary<string, List<ConnectorInfo>>();
      if (!familyDoc.IsFamilyDocument) { return result; }
      var mgr = familyDoc.FamilyManager;
      if (mgr == null) { return result; }

      var conElems = familyDoc.TypeElements(
          typeof(ConnectorElement)).ToElements();

      var familyParamsByName = new Dictionary<string, FamilyParameter>();
      foreach (FamilyParameter fp in mgr.Parameters) {
        familyParamsByName.Add(fp.Definition.Name, fp);
      }


      foreach (FamilyType type in mgr.Types) {
        var conInfos = new List<ConnectorInfo>();
        result.Add(type.Name, conInfos);

        foreach (ConnectorElement conElem in conElems) {
          var p = conElem.get_Parameter(BuiltInParameter.RBS_CONNECTOR_DESCRIPTION);
          if (p == null || !p.HasValue) { continue; }
          var paramName = p.AsString();

          if (!familyParamsByName.TryGetValue(paramName, out var fp)) {
            continue;
          }
          var pocDesc = type.AsString(fp);
          var info = new ConnectorInfo(conElem, pocDesc);
          conInfos.Add(info);
        }
      }
      return result;
    }

    private static readonly string _PipeName = "배관";
    private static readonly string _DuctName = "덕트";

    private bool _IsConnected;
    private bool _IsCheck;
    private bool _IsValid;

    public int Sequence { get; set; }
    public int Idx { get; set; }
    public string Type { get; set; }
    public string SystemTypeName { get; set; }
    public string Diameter { get; set; }
    public string Direction { get; set; }
    public string Number { get; set; }
    public string Num { get; set; }
    public string Size { get; set; }
    public string Utility { get; set; }
    public string UtitltyType { get; set; }
    public string Material { get; set; }
    public string ConType { get; set; }
    public string SystemClassification { get; set; }
    /// <summary>
    /// 커넥터 설명에 입력된 내용
    /// </summary>
    public string ConnectorDescription { get; set; }
    /// <summary>
    /// 커넥터의 정보 입력한 원본 텍스트
    /// </summary>
    public string InfoText { get; set; }

    /// <summary>
    /// 스틱 여부
    /// </summary>
    public bool IsStick { get; set; }
    /// <summary>
    /// 스틱 번호
    /// </summary>
    public string StickNo { get; set; }

    public bool IsConnected {
      get { return _IsConnected; }
      set { Set(ref _IsConnected, value); }
    }
    public bool IsCheck {
      get { return _IsCheck; }
      set { Set(ref _IsCheck, value); }
    }
    public bool IsValid {
      get { return _IsValid; }
      set { Set(ref _IsValid, value); }
    }


    private ConnectorInfo() {
      Sequence = -1;
      Idx = -1;
      Type = string.Empty;
      SystemTypeName = string.Empty;
      Diameter = string.Empty;
      Direction = string.Empty;
      Number = string.Empty;
      Size = string.Empty;
      Utility = string.Empty;
      UtitltyType = string.Empty;
      Material = string.Empty;
      ConType = string.Empty;
      IsConnected = false;
      IsCheck = false;
      IsValid = false;

      IsStick = false;
      StickNo = string.Empty;
    }

    public ConnectorInfo(ConnectorInfo other) {
      Sequence = other.Sequence;
      Idx = other.Idx;
      Type = other.Type;
      SystemTypeName = other.SystemTypeName;
      Diameter = other.Diameter;
      Direction = other.Direction;
      Number = other.Number;
      Size = other.Size;
      Utility = other.Utility;
      UtitltyType = other.UtitltyType;
      Material = other.Material;
      ConType = other.ConType;
      IsConnected = other.IsConnected;
      IsCheck = other.IsCheck;
      IsValid = other.IsValid;

      IsStick = other.IsStick;
      StickNo = other.StickNo;
    }

    public ConnectorInfo(Connector connector) {
      // 커넥터의 Direction이 없는 경우 예외 처리
      if (connector.Domain == Domain.DomainHvac ||
          connector.Domain == Domain.DomainPiping) {
        Direction = connector.Direction.ToString();
      } else {
        Direction = string.Empty;
      }
      //Direction = connector.Direction.ToString();
      Idx = connector.Id;
      IsConnected = connector.IsConnected;

      if (connector.Domain == Domain.DomainPiping) {
        Type = _PipeName;
        if (connector.Shape == ConnectorProfileType.Round) {
          var diameter = UnitUtils.ConvertFromInternalUnits(
            connector.Radius * 2, UnitTypeId.Millimeters);
          Diameter = diameter.ToString();
        } else {
          var msg = $"Connector Shape Warring. Owner ID:{connector.Owner.Id}, ConnId:{Idx}";
        }
      } else if (connector.Domain == Domain.DomainHvac) {
        Type = _DuctName;
        switch (connector.Shape) {
          case ConnectorProfileType.Round:
            var diameter = UnitUtils.ConvertFromInternalUnits(
                connector.Radius * 2, UnitTypeId.Millimeters);
            Diameter = diameter.ToString();
            break;
          case ConnectorProfileType.Rectangular:
            var width = UnitUtils.ConvertFromInternalUnits(
                connector.Width, UnitTypeId.Millimeters);
            var height = UnitUtils.ConvertFromInternalUnits(
                connector.Height, UnitTypeId.Millimeters);
            Diameter = $"{width} x {height}";
            break;
          case ConnectorProfileType.Oval:
            break;
          case ConnectorProfileType.Invalid:
            break;
        }
      }

      ReadConnectorDescriptions(connector);
    }

    public ConnectorInfo(ConnectorElement conElem, string pocDesc) {
      Direction = conElem.Direction.ToString();
      ConnectorDescription = pocDesc;
      Idx = conElem.Id.IntegerValue;
      IsConnected = false;

      if (conElem.Domain == Domain.DomainPiping) {
        Type = _PipeName;
        if (conElem.Shape == ConnectorProfileType.Round) {
          var diameter = UnitUtils.ConvertFromInternalUnits(
            conElem.Radius * 2, UnitTypeId.Millimeters);
          Diameter = diameter.ToString();
        } else {
          var msg = $"Connector Shape Warring. Document Title:{conElem.Document.Title}, Id:{conElem.Id.IntegerValue}";
        }
      } else if (conElem.Domain == Domain.DomainHvac) {
        Type = _DuctName;
        switch (conElem.Shape) {
          case ConnectorProfileType.Round:
            var diameter = UnitUtils.ConvertFromInternalUnits(
                conElem.Radius * 2, UnitTypeId.Millimeters);
            Diameter = diameter.ToString();
            break;
          case ConnectorProfileType.Rectangular:
            var width = UnitUtils.ConvertFromInternalUnits(
                conElem.Width, UnitTypeId.Millimeters);
            var height = UnitUtils.ConvertFromInternalUnits(
                conElem.Height, UnitTypeId.Millimeters);
            Diameter = $"{width} x {height}";
            break;
          case ConnectorProfileType.Oval:
            // 타원형 어떻게 구현할지...
            break;
          case ConnectorProfileType.Invalid:
            var msg = $"Connector Shape Warring. Document Title:{conElem.Document.Title}, Id:{conElem.Id.IntegerValue}";
            break;
        }
      }

      ParseNoInfo(pocDesc);
    }

    void ReadConnectorDescriptions(Connector connector) {
      var connDesc = connector.Description;
      ConnectorDescription = connDesc;
      if (string.IsNullOrEmpty(connDesc)) { return; }

      // 커넥터 설명에 구분자가 있는 경우
      if (connDesc.Contains(",")) {
        ParseNoInfo(connDesc);
        return;
      }

      // 커넥터 설명에 개체의 파라미터 이름 쓰여진 경우
      var elem = connector.Owner;
      if (elem == null) { return; }
      var doc = elem.Document;
      if (doc == null) { return; }

      if (doc.IsFamilyDocument) {
        Console.WriteLine();
      } else {
        Parameter p = null;
        if (elem is FamilyInstance fi) {
          // 패밀리인 경우 심볼에 값이 있음.
          p = fi.Symbol.GetParameter(connDesc);
        } else {
          p = elem.GetParameter(connDesc);
        }
        if (p != null && p.HasValue) {
          var pocInfo = p.AsString();
          ParseNoInfo(pocInfo);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connDesc"></param>
    void ParseNoInfo(string connDesc) {
      InfoText = connDesc;

      if (string.IsNullOrEmpty(connDesc)) { return; }

      Num = null;
      var tmp = connDesc.Split(',');
      var tmpLen = tmp.Length;
      switch (tmpLen) {
        case 3:
          Num = tmp[0];
          int idx = tmp[1].LastIndexOf('_');
          SystemTypeName = tmp[1].Substring(0, idx);
          Size = tmp[1].Substring(idx + 1);
          Material = tmp[2];
          break;
        case 4:
          Num = tmp[0];
          SystemTypeName = tmp[1];
          Size = tmp[2];
          Material = tmp[3];
          break;
        case 5:
          Num = tmp[0];
          SystemTypeName = tmp[1];
          Size = tmp[2];
          Material = tmp[3];
          ConType = tmp[4];
          break;
        case 6:
          Num = tmp[0];
          SystemTypeName = tmp[1];
          Size = tmp[2];
          Material = tmp[3];
          ConType = tmp[4];
          StickNo = tmp[5];
          break;
        default:
          return;
      }

      tmp = SystemTypeName.Split('_');
      if (tmp.Length == 2) {
        UtitltyType = tmp[0];
        Utility = tmp[1];
        IsValid = true;
      }

      IsStick = Num.Equals("stick", StringComparison.OrdinalIgnoreCase);

      if (IsStick) {
        Number = $"Stick {StickNo}";
      } else {
        Number = $"No {Num}_{Utility}_{Size}_{ConType}";
      }
    }
  }
}

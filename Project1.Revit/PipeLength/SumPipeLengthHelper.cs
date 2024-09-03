using Autodesk.Revit.DB;
using Project1.Revit.Common;
using System;
using System.Collections.Generic;

namespace Project1.Revit.PipeLength {
  internal static class SumPipeLengthHelper {
    private static readonly string ParamName = ProjectParameterList.TKDefinitionNames.SumBendingLength;
    private static readonly string PipeName1 = "Exception Pipe Name1";
    private static readonly string PipeName2 = "Exception Pipe Name2";

    internal static void UpdateParameter(IList<Element> elems) {
      // 만든 Project_Length 파라미터 값 변경
      foreach (var elem in elems) {
        var category = elem.Category;
        if (category == null) { continue; }
        var builtInCate = elem.GetBuiltInCategory();

        if (builtInCate == BuiltInCategory.OST_PipeCurves) {
          var length = elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
          elem.GetParameter(ParamName).Set(length.AsDouble());
          continue;
        }
        else if (builtInCate == BuiltInCategory.OST_PipeFitting) {
          /// 피팅류의 경우
          /// 8A: 6.3, 10A: 9.5, 15A: 12.7, 20A: 19.05, 25A : 25.4
          /// [Exception Pipe Name1]의 경우
          /// 반지름 : 파라미터 R 사용
          /// 길이 = 반지름 * 2 * Pi * (2 * Pi / radian)
          var instance = elem as FamilyInstance;
          if (instance == null) { continue; }
          if (instance.GetPartType() != PartType.Elbow) {
            continue;
          }

          var minSize = elem.get_Parameter(
              BuiltInParameter.RBS_PIPE_SIZE_MINIMUM);
          var maxSize = elem.get_Parameter(
              BuiltInParameter.RBS_PIPE_SIZE_MAXIMUM);
          var midSize = (minSize.AsDouble() + maxSize.AsDouble()) / 2;
          // todo ..
          var size = UnitUtils.ConvertFromInternalUnits(
              midSize, minSize.GetUnitTypeId());
          if (elem.Name.ToUpper().Contains(PipeName1)) {
            if (size != 15) { continue; }
          }
          else if (elem.Name.ToUpper().Contains(PipeName2.ToUpper())) {
            if (size != 25 && size != 40) { continue; }
          }
          else if (size < 8 || size > 25) {
            continue;
          }

          var connectors = instance.GetConnectorSet();
          if (connectors.Size != 2) {
            continue;
          }

          // 배관 크기 별 반지름
          double length = 2 * Math.PI;
          if (elem.Name.ToUpper().Contains(PipeName2.ToUpper())) {
            var param = elem.GetParameter("R");
            var doubleParma = UnitUtils.ConvertFromInternalUnits(
                param.AsDouble(), param.GetUnitTypeId());
            length *= doubleParma;
          }
          else {
            var convertSize = Convert(size);
            if (convertSize == 0) { continue; }
            length *= convertSize;
          }

          // 각도 계산 (라디안)
          var connVec = new List<XYZ>();

          var cen = instance.GetLocationPoint();
          foreach (Connector conn in connectors) {
            var vec = cen - conn.Origin;
            connVec.Add(vec);
          }

          var radian = Math.PI - connVec[0].AngleTo(connVec[1]);
          length *= radian / (2 * Math.PI);
          length = UnitUtils.ConvertToInternalUnits(length,
              minSize.GetUnitTypeId());

          elem.GetParameter(ParamName).Set(length);
        }
      }
    }

    private static double Convert(double size) {
      switch (Math.Round(size)) {
        case 8:
          return 6.3;
        case 10:
          return 9.5;
        case 15:
          return 12.7;
        case 20:
          return 19.05;
        case 25:
          return 25.4;
        default:
          return 0;
      }
    }
  }
}

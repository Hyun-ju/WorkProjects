using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Project1.Revit.Common;
using System.Collections.Generic;

namespace Project1.Revit.IsoPipeDimension {
  public static class PipeDimensionHelper {
    public static List<XYZ> GetPipeDimensionPoints(Pipe pipe) {
      var points = new List<XYZ>();
      var connectors = pipe.ConnectorManager.Connectors;
      foreach (Connector connector in connectors) {
        if (!connector.IsConnected) {
          points.Add(connector.Origin);
          continue;
        }
        var part = connector.GetConnectedPart();
        if (part is FamilyInstance instance) {
          var fitting = instance.GetMechanicalFitting();
          if (fitting != null) {
            if (fitting.PartType == PartType.Elbow) {
              points.Add(instance.GetLocationPoint());
            }
            else {
              points.Add(connector.Origin);
            }
          }
          else {
            var isFlange = IsFlangePart(instance);
            if (isFlange) {
              var point = GetPointElbowConnectedToFlange(instance, connector.Origin);
              points.Add(point);
            }
            else {
              points.Add(connector.Origin);
            }
          }
        }
      }
      if (points.Count < 2) {
        return null;
      }
      return points;
    }

    private static bool IsFlangePart(FamilyInstance instance) {
      var doc = instance.Document;
      var subComponentIds = instance.GetSubComponentIds();
      foreach (var subComponentId in subComponentIds) {
        var element = doc.GetElement(subComponentId);
        if (element.GetPartType() == PartType.PipeFlange) {
          return true;
        }
      }
      return false;
    }

    private static XYZ GetPointElbowConnectedToFlange(
        FamilyInstance familyInstance, XYZ srcOrigin) {
      var connectors = familyInstance.MEPModel.ConnectorManager.Connectors;

      foreach (Connector connector in connectors) {
        var part = connector.GetConnectedPart();
        if (part.GetPartType() == PartType.Elbow) {
          return part.GetLocationPoint();
        }
      }

      return srcOrigin;
    }
  }
}

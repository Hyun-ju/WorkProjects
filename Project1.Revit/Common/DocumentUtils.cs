using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Project1.Revit.Common {
  public static class DocumentUtils {
    public static BuiltInCategory GetBuiltInCategory(this Element element) {
      var cateId = element?.Category?.Id?.IntegerValue;
      if (cateId.Equals(ElementId.InvalidElementId.IntegerValue)) { return BuiltInCategory.INVALID; }

      return (BuiltInCategory)cateId;
    }

    public static List<Level> GetSortedLevels(this Document document) {
      var levels = new List<Level>();
      var elems = document.TypeElements(typeof(Level)).ToElements();

      foreach (var item in elems) {
        if (item is Level level)
          levels.Add(level);
      }
      
      levels.Sort((a, b) => a.Elevation.CompareTo(b.Elevation));

      return levels;
    }


    public static double GetDistancePlaneToPoint(
       this Plane plane, XYZ point) {
      var toPoint = point - plane.Origin;
      return plane.Normal.DotProduct(toPoint);
    }


    public static XYZ GetLocationPoint(this Element e) {
      if (e.Location != null
          && e.Location is LocationPoint locationPoint) {
        return locationPoint.Point;
      }
      return null;
    }

    public static Curve GetLocationCurve(this Element e) {
      if (e.Location != null
          && e.Location is LocationCurve locationCurve) {
        return locationCurve.Curve;
      }
      return null;
    }
  }
}

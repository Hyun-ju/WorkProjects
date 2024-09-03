using Autodesk.Revit.DB;
using System;

namespace Project1.Revit.Common {
  public static class FilteredElementCollectors {
    public static FilteredElementCollector ElementCollector(this Document document) {
      return new FilteredElementCollector(document);
    }

    public static FilteredElementCollector ElementCollector(this Document document, ElementId viewId) {
      return new FilteredElementCollector(document, viewId);
    }


    public static FilteredElementCollector TypeElements(this Document document, Type type) {
      return ElementCollector(document).OfClass(type);
    }

    public static FilteredElementCollector TypeElements(this Document document, ElementId viewId, Type type) {
      return ElementCollector(document, viewId).OfClass(type);
    }


    public static FilteredElementCollector CategoryElements(this Document document, BuiltInCategory category) {
      return ElementCollector(document).OfCategory(category);
    }

    public static FilteredElementCollector CategoryElements(this Document document, ElementId viewId, BuiltInCategory category) {
      return ElementCollector(document, viewId).OfCategory(category);
    }
  }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI.Selection;

namespace Project1.Revit.Filters {
  public class PipeSelectionFilter : ISelectionFilter {
    public static readonly PipeSelectionFilter Instance = new PipeSelectionFilter();
    public bool AllowElement(Element elem) {
      if (elem is Pipe) { return true; }
      return false;
    }

    public bool AllowReference(Reference reference, XYZ position) {
      return false;
    }
  }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace Project1.Revit.AlignPipe {
  public class BasisPipeSelectionFilter : ISelectionFilter {
    private IList<Element> _SelectedElems = null;


    public BasisPipeSelectionFilter(IList<Element> selectedElems) {
      _SelectedElems = selectedElems;
    }


    public bool AllowElement(Element elem) {
      if (elem == null) { return false; }

      // 기준 배관 선택시 정렬될 배관들 중에 선택
      foreach (var selecedElem in _SelectedElems) {
        if (elem.Id == selecedElem.Id) { return true; }
      }
      return false;
    }

    public bool AllowReference(Reference reference, XYZ position) {
      return false;
    }
  }
}

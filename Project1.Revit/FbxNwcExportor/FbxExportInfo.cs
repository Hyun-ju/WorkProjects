using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Project1.Revit.FbxNwcExportor {
  public class FbxExportInfo {
    public string CategoryName { get; set; } = string.Empty;

    public List<FbxExportInfo> SubCategoryInfos { get; set; } = new List<FbxExportInfo>();

    public List<Element> Elements { get; set; } = new List<Element>();

    public List<RevitLinkInstance> LinkInstances { get; set; } = new List<RevitLinkInstance>();


    public FbxExportInfo() {
    }

    public FbxExportInfo(string categoryName) {
      CategoryName = categoryName;
    }

    public FbxExportInfo(string categoryName, Element element) {
      CategoryName = categoryName;
      Elements.Add(element);
    }

    public FbxExportInfo(string categoryName, List<Element> elements) {
      CategoryName = categoryName;
      Elements.AddRange(elements);
    }
  }


  public static class FbxExportInfoMethod {
    public static FbxExportInfo GetExportInfo(this List<FbxExportInfo> list,
                                              string categoryName) {
      return list.Find(a => a.CategoryName.Equals(categoryName));
    }

    public static FbxExportInfo GetDefaultExportInfo(this List<FbxExportInfo> list,
                                                     string categoryName) {
      var findInfo = list.GetExportInfo(categoryName);
      if (findInfo == null) {
        findInfo = new FbxExportInfo(categoryName);
        list.Add(findInfo);
      }
      return findInfo;
    }



    public static FbxExportInfo AddItem(this List<FbxExportInfo> list,
                                string categoryName, Element elem) {
      var findInfo = list.GetDefaultExportInfo(categoryName);
      findInfo.Elements.Add(elem);
      return findInfo;
    }

    public static FbxExportInfo AddItem(this List<FbxExportInfo> list,
                                string categoryName, RevitLinkInstance instance) {
      var findInfo = list.GetDefaultExportInfo(categoryName);
      findInfo.LinkInstances.Add(instance);
      return findInfo;
    }
  }
}

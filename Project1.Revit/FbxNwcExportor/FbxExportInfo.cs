using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Project1.Revit.FbxNwcExportor {
  public class FbxExportInfo {
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;

    public Dictionary<string, FbxExportInfo> Dictionary { get; set; }
        = new Dictionary<string, FbxExportInfo>();

    public List<Element> Elements { get; set; } = new List<Element>();


    public FbxExportInfo() {
    }

    public FbxExportInfo(string firstName) {
      FirstName = firstName;
    }

    public FbxExportInfo(string firstName, string secondName) {
      FirstName = firstName;
      SecondName = secondName;
    }
  }
}

using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace Project1.Revit.Common {
  internal static class ConnectionUtils {
    static readonly Options _Options =
      new Options { DetailLevel = ViewDetailLevel.Fine };

    /// <summary>
    /// 커넉터에 연결된 개체 목록 조회
    /// </summary>
    /// <param name="connector"></param>
    /// <returns></returns>
    internal static List<Element> GetConnectedParts(this Connector connector) {
      if (connector == null || connector.IsConnected == false) {
        return null;
      }
      var elems = new List<Element>();
      foreach (Connector itr in connector.AllRefs) {
        if (itr.ConnectorType == ConnectorType.Logical) { continue; }
        if (itr.Owner.Id == connector.Owner.Id) { continue; }
        elems.Add(itr.Owner);
      }
      return elems;
    }

    /// <summary>
    /// 커넥터에 연결된 개체 조회
    /// </summary>
    /// <param name="connector"></param>
    /// <returns>연결 개체가 하나일때만 값 있음.</returns>
    internal static Element GetConnectedPart(this Connector connector) {
      var elems = GetConnectedParts(connector);
      if (elems != null && elems.Count == 1) {
        return elems[0];
      }
      return null;
    } 

  }
}

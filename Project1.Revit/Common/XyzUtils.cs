using Autodesk.Revit.DB;

namespace Project1.Revit.Common {
  internal static class XyzUtils {
    /// <summary>
    /// 좌표가 일치하는지 판단하는 메소드
    /// </summary>
    /// <param name="xyz1"></param>
    /// <param name="xyz2"></param>
    /// <returns>두 xyz좌표가 일치 여부</returns>
    /// <remarks>XYZ의 IsAlmostEqualTo 매소드는 벡터가 같은지 판단</remarks>
    public static bool IsSameXYZ(this XYZ xyz1, XYZ xyz2) {
      if (!xyz1.X.IsAlmostEqualTo(xyz2.X)) { return false; }
      if (!xyz1.Y.IsAlmostEqualTo(xyz2.Y)) { return false; }
      if (!xyz1.Z.IsAlmostEqualTo(xyz2.Z)) { return false; }
      return true;
    }

    /// <summary>
    /// 양방향으로 같은 방향인지 판단
    /// </summary>
    public static bool IsSameDirection(this XYZ vector1, XYZ vector2) {
      if (vector1.IsAlmostEqualTo(vector2)) { return true; }
      if (vector1.IsAlmostEqualTo(vector2.Negate())) { return true; }
      return false;
    }


    /// <summary>
    /// 공차 범위내 양방향으로 같은 방향인지 판단
    /// </summary>
    public static bool IsSameDirection(this XYZ vector1, XYZ vector2, double tolerance) {
      if (vector1.IsAlmostEqualTo(vector2, tolerance)) { return true; }
      if (vector1.IsAlmostEqualTo(vector2.Negate(), tolerance)) { return true; }
      return false;
    }
  }
}

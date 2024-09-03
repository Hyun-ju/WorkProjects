using System;

namespace Project1.Revit.Common {
  public static class MathUtils {
    public const double Tolerance = 1e-5;
    public static readonly double PI = Math.PI;
    public static readonly double HalfPI = Math.PI * 0.5;
    public static readonly double QuarterPI = Math.PI * 0.25;

    /// <summary>
    /// 각도 도에서 라디안으로 변환
    /// </summary>
    /// <param name="angle">각도 도 값</param>
    /// <returns></returns>
    public static double DegreeToRadian(this double angle) {
      return angle * PI / 180.0;
    }
    /// <summary>
    /// 각도 라디안에서 도로 변환
    /// </summary>
    /// <param name="angle">각도 라디안 값</param>
    /// <returns></returns>
    public static double RadianToDegree(this double angle) {
      return angle * 180.0 / PI;
    }

    /// <summary>
    /// 공차를 사용한 값 비교
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance">기본 값 1e-5</param>
    /// <returns></returns>
    public static bool IsAlmostEqualTo(this double a, double b, double tolerance = Tolerance) {
      return Math.Abs(a - b) < tolerance;
    }

    /// <summary>
    /// 공차를 사용한 값 비교
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="tolerance">기본 값 1e-5</param>
    /// <returns></returns>
    public static bool IsAlmostEqualTo(this float a, double b, double tolerance = Tolerance) {
      return Math.Abs(a - b) < tolerance;
    }


    public static int RoundDoubleToInt(double value) {
      value = Math.Floor(Math.Round(value));
      return (int)value;
    }
  }
}

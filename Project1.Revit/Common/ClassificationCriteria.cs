using System.Collections.Generic;
using StrList = System.Collections.Generic.List<string>;

namespace Project1.Revit.Common {
  public class ClassificationCriteria {
    // 건설 
    public static class Constructions {
      #region 건설 대분류 
      // Example
      public const string ConstructionMain_1 = "Example_Main1";
      public const string ConstructionMain_2 = "Example_Main2";
      public const string ConstructionMain_3 = "Example_Main3";
      public const string ConstructionMain_4 = "Example_Main4";
      public const string ConstructionMain_5 = "Example_Main5";
      public const string ConstructionMain_6 = "Example_Main6";
      public const string ConstructionMain_7 = "Example_Main7";
      public const string ConstructionMain_8 = "Example_Main8";
      #endregion
      #region 건설 중분류
      public const string ConstructionSub_NA = "NA";
      public const string ConstructionSub_1 = "Example_Sub_A";
      public const string ConstructionSub_2 = "Example_Sub_B";
      public const string ConstructionSub_3 = "Example_Sub_C";
      public const string ConstructionSub_4 = "Example_Sub_D";
      public const string ConstructionSub_5 = "Example_Sub_E";
      public const string ConstructionSub_6 = "Example_Sub_F";
      public const string ConstructionSub_7 = "Example_Sub_G";
      public const string ConstructionSub_8 = "Example_Sub_H";
      public const string ConstructionSub_9 = "Example_Sub_I";
      public const string ConstructionSub_10 = "Example_Sub_J";
      public const string ConstructionSub_11 = "Example_Sub_K";
      public const string ConstructionSub_12 = "Example_Sub_L";
      public const string ConstructionSub_13 = "Example_Sub_M";
      public const string ConstructionSub_14 = "Example_Sub_N";
      public const string ConstructionSub_15 = "Example_Sub_O";
      #endregion

      public static StrList Construction_Main1s = new StrList() {
        ConstructionSub_1,
        ConstructionSub_2,
      };
      public static StrList Construction_Main2s = new StrList() {
        ConstructionSub_3,
        ConstructionSub_4,
      };
      public static StrList Construction_Main3s = new StrList() {
        ConstructionSub_5,
        ConstructionSub_6,
        ConstructionSub_7,
      };
      public static StrList Construction_Main4s = new StrList() {
        ConstructionSub_NA,
      };
      public static StrList Construction_Main5s = new StrList() {
        ConstructionSub_NA,
      };
      public static StrList Construction_Main6s = new StrList() {
        ConstructionSub_6,
      };
      public static StrList Construction_Main7s = new StrList() {
        ConstructionSub_5,
        ConstructionSub_8,
        ConstructionSub_9,
        ConstructionSub_10,
        ConstructionSub_11,
        ConstructionSub_12,
        ConstructionSub_13,
      };
      public static StrList Construction_Main8s = new StrList() {
        ConstructionSub_14,
        ConstructionSub_15,
      };
      public static Dictionary<string, StrList> ConstructionBim
          = new Dictionary<string, StrList> {
            { ConstructionMain_1, Construction_Main1s },
            { ConstructionMain_2, Construction_Main2s },
            { ConstructionMain_3, Construction_Main3s },
            { ConstructionMain_4, Construction_Main4s },
            { ConstructionMain_5, Construction_Main5s },
            { ConstructionMain_6, Construction_Main6s },
            { ConstructionMain_7, Construction_Main7s },
            { ConstructionMain_8, Construction_Main8s },
          };
    }

    // Utility
    public static class Utilites {
      public const string Util_NA = "NA";
      public const string Util_ACase = "ACase";
      public const string Util_A_Case = "A Case";
      public const string Util_BCase = "BCase";
      public const string Util_CCase = "CCase";
      public const string Util_CCase_1 = "CCASE 1";
      public const string Util_CCase_2 = "CCASE 2";
      public const string Util_CCase_3 = "CCASE 3";
      public const string Util_DCase = "DCase";
      public const string Util_ECase = "ECase";
      public const string Util_FCase = "FCase";
      public const string Util_F_Case = "F Case";
      public const string Util_FCase_Kor = "F케이스";
      public const string Util_GCase = "GCase";
      public const string Util_HCase = "HCase";
      public const string Util_H_Case = "H Case";
      public const string Util_ICase = "ICase";
      public const string Util_JCase = "JCase";
      public const string Util_J_Case = "J Case";

      public static Dictionary<string, StrList> UtilityDictionary
                                       = new Dictionary<string, StrList>() {
        { Util_NA, new StrList() { Util_NA, } },
        { Util_ACase, new StrList() {
            Util_ACase, Util_A_Case,
          } },
        { Util_BCase, new StrList() { Util_BCase, } },
        { Util_CCase, new StrList() {
            Util_CCase, Util_CCase_1, Util_CCase_2, Util_CCase_3
          } },
        { Util_DCase, new StrList() { Util_DCase, } },
        { Util_ECase, new StrList() { Util_ECase, } },
        { Util_FCase, new StrList() {
            Util_FCase, Util_F_Case, Util_FCase_Kor
          } },
        { Util_GCase, new StrList() { Util_GCase, } },
        { Util_HCase, new StrList() {
            Util_HCase, Util_H_Case,
          } },
        { Util_ICase, new StrList() { Util_ICase, } },
        { Util_JCase, new StrList() {
            Util_JCase, Util_J_Case
          } },
      };
    }
  }
}

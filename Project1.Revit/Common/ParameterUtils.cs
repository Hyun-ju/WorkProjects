using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;

namespace Project1.Revit.Common {
  public static class ParameterUtils {
    /// <summary>
    /// 파라미터 생성시 필요한 일부 설정값 받는 옵션 함수
    /// </summary>
    public class CreateParameterOption {
      public ParameterType ParameterType { get; set; }
      public bool IsVisible { get; set; }
      public bool IsUserModifiable { get; set; }

      public BuiltInParameterGroup ParameterGroup { get; set; }
    }


    /// <summary>
    /// 파라미터 생성
    /// </summary>
    /// <param name="uiApp"></param>
    /// <param name="categorySet"></param>
    /// <param name="option">파라미터 생성에 필요한 옵션들</param>
    /// <param name="paramterNames">파라미터 이름들</param>
    public static void CreateParameters(UIApplication uiApp, CategorySet categorySet,
       CreateParameterOption option, params string[] paramterNames) {
      var app = uiApp.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      string sharedFile = app.SharedParametersFilename;
      string tempFile = Path.GetTempFileName() + ".txt";
      using (File.Create(tempFile)) { }
      app.SharedParametersFilename = tempFile;

      var dic = new Dictionary<string, ExternalDefinition>();
      foreach (var paramName in paramterNames) {
        var options = new ExternalDefinitionCreationOptions(
            paramName, option.ParameterType) {
          Visible = option.IsVisible,
          UserModifiable = option.IsUserModifiable,
        };
        var externalDefinition = (ExternalDefinition)app.OpenSharedParameterFile().
            Groups.Create("TempGroup").Definitions.Create(options);

        dic.Add(paramName, externalDefinition);
      }

      app.SharedParametersFilename = sharedFile;
      File.Delete(tempFile);

      //instance에 바인딩
      var bindingMap = new UIApplication(app).
          ActiveUIDocument.Document.ParameterBindings;

      foreach (var kvp in dic) {
        var paramName = kvp.Key;
        var externalDefinition = kvp.Value;

        bool isContains = false;
        Definition definition = null;
        var bmItr = doc.ParameterBindings.ForwardIterator();
        while (bmItr.MoveNext()) {
          if (bmItr.Key.Name != paramName) { continue; }
          definition = bmItr.Key;
          var insBinding = bindingMap.get_Item(definition) as InstanceBinding;
          foreach (var item in insBinding.Categories) {
            if (!categorySet.Contains(item as Category)) {
              categorySet.Insert(item as Category);
            }
          }
          isContains = true;
          break;
        }

        var binding = app.Create.NewInstanceBinding(categorySet);
        if (!isContains) {
          bindingMap.Insert(externalDefinition, binding, option.ParameterGroup);
        }
        else {
          bindingMap.ReInsert(definition, binding, option.ParameterGroup);
        }
      }
    }

    /// <summary>
    /// 생성된 파라미터에 카테고리 추가
    /// </summary>
    /// <param name="uiApp"></param>
    /// <param name="definition"></param>
    /// <param name="catgorySet"></param>
    public static void InsertCategoryInParameter(UIApplication uiApp, Definition definition, CategorySet catgorySet) {
      var app = uiApp.Application;
      var doc = uiApp.ActiveUIDocument.Document;

      var bindings = doc.ParameterBindings;

      var binding = bindings.get_Item(definition) as InstanceBinding;
      if (binding == null) {
        binding = app.Create.NewInstanceBinding(catgorySet);
      }
      else {
        foreach (Category item in catgorySet) {
          if (!binding.Categories.Contains(item)) {
            binding.Categories.Insert(item);
          }
        }
      }
      doc.ParameterBindings.ReInsert(definition, binding,
          definition.ParameterGroup);
    }

    /// <summary>
    /// 이름으로 파라미터 검색
    /// </summary>
    /// <param name="elem"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Parameter GetParameter(this Element elem, string name) {
      // 같은 이름의 여러개의 파리미터가 있는 경우
      // 읽기 전용이 아닌 파라미터 검색하여 리턴
      var parameters = elem.GetParameters(name);
      foreach (var parameter in parameters) {
        if (parameter.IsReadOnly) { continue; }
        return parameter;
      }
      return null;
    }

    /// <summary>
    /// 내부 단위를 Parameter의 단위로 변환
    /// </summary>
    /// <param name="param"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static double ConvertFromInternal(this Parameter param, double value) {
      return UnitUtils.ConvertFromInternalUnits(value, param.GetUnitTypeId());
    }

    /// <summary>
    /// Parameter의 단위를 내부 단위로 변환
    /// </summary>
    /// <param name="param"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static double ConvertToInternal(this Parameter param, double value) {
      return UnitUtils.ConvertToInternalUnits(value, param.GetUnitTypeId());
    }
  }
}

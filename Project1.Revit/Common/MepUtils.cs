using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;

namespace Project1.Revit.Common {
  public static class MepUtils {
    public static bool IsMechanicalEquipment(this Element element) {
      if (element is FamilyInstance instance) {
        return instance.IsMechanicalEquipment();
      }
      return false;
    }

    public static bool IsMechanicalEquipment(this Element element, out FamilyInstance instance) {
      instance = null;
      if (element is FamilyInstance familyInstance) {
        var isEquip = familyInstance.IsMechanicalEquipment();
        if (isEquip) {
          instance = familyInstance;
          return isEquip;
        }
      }
      return false;
    }

    public static bool IsMechanicalEquipment(this FamilyInstance fi) {
      if (fi == null ||
          !fi.IsValidObject ||
          fi.Category == null) {
        return false;
      }

      var catId = fi.Category.Id.IntegerValue;
      if (catId != (int)BuiltInCategory.OST_MechanicalEquipment) {
        return false;
      }

      if (fi.MEPModel is MechanicalEquipment == false ||
          fi?.MEPModel?.ConnectorManager?.Connectors == null) {
        return false;
      }
      return true;
    }


    public static ConnectorSet GetConnectorSet(this Element element) {
      return element.GetConnectorManager()?.Connectors;
    }

    public static ConnectorManager GetConnectorManager(this Element element) {
      if (element is Pipe pipe) { return pipe.ConnectorManager; }
      return element.GetMEPModel()?.ConnectorManager;
    }


    public static MEPModel GetMEPModel(this FamilyInstance instance) {
      return instance.MEPModel;
    }

    public static MEPModel GetMEPModel(this Element element) {
      if (element is FamilyInstance instance) {
        return instance.GetMEPModel();
      }
      return null;
    }


    public static MechanicalFitting GetMechanicalFitting(this FamilyInstance instance) {
      var mepModel = instance.GetMEPModel();
      if (mepModel is MechanicalFitting fitting) { return fitting; }
      return null;
    }

    public static MechanicalFitting GetMechanicalFitting(this Element element) {
      if (element is FamilyInstance instance) {
        return instance.GetMechanicalFitting();
      }
      return null;
    }


    public static PartType GetPartType(this MechanicalFitting fitting) {
      return fitting.PartType;
    }

    public static PartType GetPartType(this FamilyInstance instance) {
      var fitting = instance.GetMechanicalFitting();
      if (fitting != null) { return fitting.GetPartType(); }
      return PartType.Undefined;
    }

    public static PartType GetPartType(this Element element) {
      if (element is FamilyInstance instance) {
        return instance.GetPartType();
      }
      return PartType.Undefined;
    }
  }
}

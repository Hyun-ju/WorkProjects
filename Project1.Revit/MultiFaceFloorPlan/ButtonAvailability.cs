using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Project1.Revit.MultiFaceFloorPlan {
  internal class ButtonAvailability : IExternalCommandAvailability {
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) {
      return true;
    }
  }
}

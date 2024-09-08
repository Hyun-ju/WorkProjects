using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using adwin = Autodesk.Windows;

namespace Project1.Revit {
  public sealed class RibbonBuilder {
    internal static class Utils {
      public static BitmapSource ToBitmapSource(Bitmap bmp) {
        bmp.SetResolution(96, 96);

        return System.Windows.Interop.Imaging
        .CreateBitmapSourceFromHBitmap(
            bmp.GetHbitmap(),
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
      }

      public static PushButtonData CreatePushButtonData(
          Type commandClass, string text,
          string tooltip, string description,
          Bitmap img16, Bitmap img32) {
        var cmdClassFullName = commandClass.FullName;
        var dataName = $"Button.Data.{cmdClassFullName}";
        return new PushButtonData(
            dataName, text,
            commandClass.Assembly.Location,
            cmdClassFullName) {
          ToolTip = tooltip,
          LongDescription = description,
          Image = ToBitmapSource(img16),
          LargeImage = ToBitmapSource(img32),
        };
      }

      public static ToggleButtonData CreateToggleButtonData(
          Type commandClass, string text,
          string tooltip, string description,
          Bitmap img16, Bitmap img32) {
        var cmdClassFullName = commandClass.FullName;
        var dataName = $"ToggleButton.Data.{cmdClassFullName}";
        return new ToggleButtonData(
            dataName, text,
            commandClass.Assembly.Location,
            cmdClassFullName) {
          ToolTip = tooltip,
          LongDescription = description,
          Image = ToBitmapSource(img16),
          LargeImage = ToBitmapSource(img32),
        };
      }

      public static PushButton CreatePushButton(
          RibbonPanel panel, PushButtonData data,
          string tooltip = null) {
        var button = (PushButton)panel.AddItem(data);
        button.ToolTip = tooltip ?? data.ToolTip;
        return button;
      }

      public static PushButton CreatePushButton(
          RibbonPanel panel, Type commandClass,
          string text, string tooltip, string description,
          Bitmap img16, Bitmap img32) {
        var data = CreatePushButtonData(
            commandClass, text, tooltip,
            description, img16, img32);
        return CreatePushButton(panel, data);
      }
    }

    UIControlledApplication _UICtrlApp = null;

    private static readonly string _TabName = "KHJ Personal Develop Util";

    public RibbonBuilder(UIControlledApplication uiCtrlApp) {
      _UICtrlApp = uiCtrlApp;
    }

    public void CreateHookUpTab() {
      var newTab = CreateRibbonTab(_TabName);
      BuildTestPanel(_TabName);
      var panel = _UICtrlApp.CreateRibbonPanel(_TabName, "도구");
      var _BendingLength = Utils.CreatePushButton(
          panel, typeof(PipeLength.CmdSumBending),
          "벤딩 엘보\n길이",
          null,
          null,
          Properties.Resources.Icon_BendingLength_16,
          Properties.Resources.Icon_BendingLength_32);

      var _IsoDemsion = Utils.CreatePushButton(
          panel, typeof(IsoPipeDimension.CmdPipeDimension),
          "ISO 치수\n입력",
          null,
          null,
          Properties.Resources.Icon_ISO_16,
          Properties.Resources.Icon_ISO_32);

      var _AlignPipe = Utils.CreatePushButton(
          panel, typeof(AlignPipe.CmdAlignPipe),
          "배관 간격\n조정",
          null,
          null,
          Properties.Resources.Icon_PipeAlign_16,
          Properties.Resources.Icon_PipeAlign_32);

      var _FamilyQc = Utils.CreatePushButton(
          panel, typeof(MultiFaceFloorPlan.CmdMultiFaceFloorPlan),
          "6면 도면\nQC",
          null,
          null,
          Properties.Resources.Icon_MultiFacePlan_16,
          Properties.Resources.Icon_MultiFacePlane_32);
      _FamilyQc.AvailabilityClassName = typeof(MultiFaceFloorPlan.ButtonAvailability).FullName;
    }

    //[Conditional("DEBUG")]
    private void BuildTestPanel(string tabName) {
      var panel = _UICtrlApp.CreateRibbonPanel(tabName, "Export");
      var _EbxExportButton = Utils.CreatePushButton(
          panel, typeof(FbxNwcExportor.CmdFbxExport),
          "FBX Export",
          null,
          null,
          Properties.Resources.Icon_FbxExport_16,
          Properties.Resources.Icon_FbxExport_32);
      var _NwcExportButton = Utils.CreatePushButton(
          panel, typeof(FbxNwcExportor.CmdNavisworksExport),
          "NWC Export",
          null,
          null,
          Properties.Resources.Icon_NwcExport_16,
          Properties.Resources.Icon_NwcExport_32);
    }

    private adwin.RibbonTab CreateRibbonTab(string newTabName) {
      var tabs = adwin.ComponentManager.Ribbon.Tabs;
      foreach (var tab in tabs) {
        if (tab.AutomationName.Equals(newTabName)) { return null; }
      }
      var uiCtrlApp = App.Current.UIControlledApplication;
      uiCtrlApp.CreateRibbonTab(newTabName);

      tabs = adwin.ComponentManager.Ribbon.Tabs;
      adwin.RibbonTab newTab = null;
      foreach (var tab in tabs) {
        if (tab.AutomationName.Equals(newTabName)) {
          newTab = tab;
          break;
        }
      }
      return newTab;
    }
  }
}

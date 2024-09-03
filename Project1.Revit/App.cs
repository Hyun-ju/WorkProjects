using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Project1.Revit.Exportor.IPC;
using Project1.Revit.IsoPipeDimension;
using Project1.Revit.PipeLength;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using UIFramework;
using Events = Autodesk.Revit.UI.Events;

namespace Project1.Revit {
  public class App : IExternalApplication {
    public static App Current { get; private set; }


    public MainWindow MainWindow { get; private set; }
    public UIControlledApplication UIControlledApplication { get; private set; }
    public UIApplication UIApplication { get; private set; }
    public RibbonBuilder RibbonBuilder { get; private set; }

    public ExportorObject ExportorObject { get; private set; }


    public Result OnShutdown(UIControlledApplication application) {
      return Result.Succeeded;
    }

    public Result OnStartup(UIControlledApplication application) {
      LoadDlls();

      InitializeFields(application);

      var app = application.ControlledApplication;

      #region ISO Updater
      var isoDimensionUpdater = new IsoDimensionUpdater(app.ActiveAddInId);
      UpdaterRegistry.RegisterUpdater(isoDimensionUpdater);

      var pipeCate = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);
      var isoUpdaterId = isoDimensionUpdater.GetUpdaterId();
      // Element Change(Parameter, property, length etc..) Update
      UpdaterRegistry.AddTrigger(isoUpdaterId, pipeCate, Element.GetChangeTypeAny());
      // Element Add Update
      UpdaterRegistry.AddTrigger(isoUpdaterId, pipeCate, Element.GetChangeTypeElementAddition());
      // Element Delete Update
      UpdaterRegistry.AddTrigger(isoUpdaterId, pipeCate, Element.GetChangeTypeElementDeletion());
      #endregion

      #region Length Updater
      var lengthParamupdater = new LengthParameterUpdater(app.ActiveAddInId);
      UpdaterRegistry.RegisterUpdater(lengthParamupdater);

      var filerCate = new List<BuiltInCategory>() {
          BuiltInCategory.OST_PipeCurves,
          BuiltInCategory.OST_PipeFitting};
      var elemFiler = new ElementMulticategoryFilter(filerCate);

      var lengthUpdaterId = lengthParamupdater.GetUpdaterId();
      UpdaterRegistry.AddTrigger(lengthUpdaterId, elemFiler, Element.GetChangeTypeAny());
      UpdaterRegistry.AddTrigger(lengthUpdaterId, elemFiler, Element.GetChangeTypeElementAddition());
      #endregion

      RibbonBuilder = new RibbonBuilder(UIControlledApplication);
      RibbonBuilder.CreateHookUpTab();
      UIControlledApplication.DialogBoxShowing += Application_DialogBoxShowing;

      var client = Utils.CreateClient();
      if (client) {
        application.Idling += Application_Idling;
      }

      return Result.Succeeded;
    }

    private void Application_DialogBoxShowing(object sender, Events.DialogBoxShowingEventArgs e) {
      if (ExportorObject != null) {
        // Exportor 작동 일때만 작동하도록 구현
        if (e.DialogId.Equals("Dialog_Revit_DocWarnDialog")) {
          e.OverrideResult((int)TaskDialogResult.Cancel);
        }
      }
    }

    private void Application_Idling(object sender, Events.IdlingEventArgs e) {
      ExportInfos tarInfo = null;
      try {
        ExportorObject = Utils.GetShareObject();

        tarInfo = ExportorObject.TargetInfo;
        tarInfo.State = ProgressStateEnum.InProgress;
        ExportorObject.TargetInfo = tarInfo;
      } catch (Exception ex) {
        if (sender is UIApplication uiApp) {
          uiApp.Idling -= Application_Idling;
          ExportorObject = null;
          return;
        }
      }

      Document doc = null;
      try {
        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        Result result = Result.Failed;
        #region uidoc
        //var uiDoc = Current.UIApplication.
        //    OpenAndActivateDocument(_ExportorObject.TargetInfo.FullPath);
        //doc = uiDoc.Document;
        //if (_ExportorObject.IsFbxExportor) {
        //  var export = new FbxNwcExportor.FbxExportOptionVM();
        //  result = export.RunIpcFbxExport(doc);
        //}
        //else if (_ExportorObject.IsNwcExportor) {
        //  var export = new FbxNwcExportor.NavisworksExportVM();
        //  result = export.RunIpcNwcExport(doc);
        //}
        //doc.SaveAs(_ExportorObject.TargetInfo.TempSavePath);
        #endregion

        #region doc
        var app = Current.UIApplication.Application;
        var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(ExportorObject.TargetInfo.FullPath);
        var openOptions = new OpenOptions() {
          DetachFromCentralOption = DetachFromCentralOption.DetachAndDiscardWorksets
        };
        doc = app.OpenDocumentFile(modelPath, openOptions);
        //doc = app.OpenDocumentFile(_ExportorObject.TargetInfo.FullPath);
        if (ExportorObject.IsFbxExportor) {
          var export = new FbxNwcExportor.FbxExportOptionVM();
          result = export.RunIpcFbxExport(doc);
        } else if (ExportorObject.IsNwcExportor) {
          var export = new FbxNwcExportor.NavisworksExportVM();
          result = export.RunIpcNwcExport(doc);
        }
        #endregion

        sw.Stop();

        tarInfo = ExportorObject.TargetInfo;
        tarInfo.ElapsedTime = sw.Elapsed;
        if (result == Result.Succeeded) {
          tarInfo.State = ProgressStateEnum.Success;
        } else if (result == Result.Cancelled) {
          tarInfo.State = ProgressStateEnum.Pass;
        } else {
          tarInfo.State = ProgressStateEnum.Fail;
        }
        ExportorObject.TargetInfo = tarInfo;
      } catch (Exception ex) {
        //var path = doc.PathName;
        //path = Path.ChangeExtension(path, null);
        //path += _ExportorObject.TempGuid + ".rvt";
        //doc.SaveAs(path);

        tarInfo = ExportorObject.TargetInfo;
        tarInfo.State = ProgressStateEnum.Fail;
        ExportorObject.TargetInfo = tarInfo;

        var str = $"[{ProgressStateEnum.Fail}] {ExportorObject.TargetInfo.FileName} => {ex}";
        File.AppendAllText(ExportorObject.ResultFilePath, str + Environment.NewLine);
      }
      SendMessage(UIControlledApplication.MainWindowHandle,
            0x10, IntPtr.Zero, IntPtr.Zero);
      return;
    }

    private void LoadDlls() {
      AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
      {
        var directory = Path.GetDirectoryName(
            this.GetType().Assembly.Location);
        // 로드 실패가 발생하는 DLL들 강제로 로드
        Assembly.LoadFrom(
            Path.Combine(directory, "GalaSoft.MvvmLight.dll"));
        Assembly.LoadFrom(
            Path.Combine(directory, "GalaSoft.MvvmLight.Extras.dll"));
        Assembly.LoadFrom(
            Path.Combine(directory, "GalaSoft.MvvmLight.Platform.dll"));
      }
    }

    private void InitializeFields(UIControlledApplication application) {
      Current = this;
      UIControlledApplication = application;

      {
        var fieldName = "m_uiapplication";
        var fi = UIControlledApplication.GetType().GetField(
            fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        UIApplication = (UIApplication)fi.GetValue(UIControlledApplication);
      }

      {
        var hwndSrc = HwndSource.FromHwnd(
            UIControlledApplication.MainWindowHandle);
        MainWindow = hwndSrc.RootVisual as MainWindow;
      }
    }

    private Assembly CurrentDomain_AssemblyResolve(object sender,
        ResolveEventArgs args) {
      // 리소스 관련은 넘어감.
      if (args.Name.Contains(".resources")) { return null; }

      if (args.Name.Contains("GalaSoft.MvvmLight.Platform")) {
        string assemblyFile = "GalaSoft.MvvmLight.Platform.dll";
        if (File.Exists(assemblyFile)) {
          return Assembly.LoadFrom(assemblyFile);
        }
      }
      return null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg,
        IntPtr wParam, IntPtr lParam);
  }
}

using Autodesk.Revit.DB;
using GalaSoft.MvvmLight;
using System.IO;

namespace Project1.Revit.MultiFaceFloorPlan {
  public class InfoByView : ObservableObject {
    private string _ViewDir;
    private View _View;
    private string _FileFullPath;
    private string _FileName;
    private bool _IsChecked;
    private bool _IsSideView;
    private Viewport _ViewPort;

    public string ViewDir {
      get { return _ViewDir; }
      set { Set(ref _ViewDir, value); }
    }
    public View View {
      get { return _View; }
      set { Set(ref _View, value); }
    }
    public string FileFullPath {
      get { return _FileFullPath; }
      set {
        if (Set(ref _FileFullPath, value)) {
          if (!string.IsNullOrEmpty(_FileFullPath)) {
            FileName = Path.GetFileName(_FileFullPath);
          }
          else {
            FileName = string.Empty;
          }
        }
      }
    }
    public string FileName {
      get { return _FileName; }
      protected set { Set(ref _FileName, value); }
    }
    public bool IsChecked {
      get { return _IsChecked; }
      set { Set(ref _IsChecked, value); }
    }
    public bool IsSideView {
      get { return _IsSideView; }
      set { Set(ref _IsSideView, value); }
    }
    public Viewport ViewPort {
      get { return _ViewPort; }
      set { Set(ref _ViewPort, value); }
    }


    public InfoByView() : this(string.Empty, false) { }

    public InfoByView(string viewDir, bool isSideView) {
      ViewDir = viewDir;
      FileFullPath = string.Empty;
      FileName = string.Empty;
      IsChecked = false;
      IsSideView = isSideView;
    }
  }
}

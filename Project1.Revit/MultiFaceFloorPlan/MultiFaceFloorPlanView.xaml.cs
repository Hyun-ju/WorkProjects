using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace Project1.Revit.MultiFaceFloorPlan {
  /// <summary>
  /// MultiFaceFloorPlanView.xaml에 대한 상호 작용 논리
  /// </summary>
  public partial class MultiFaceFloorPlanView : Window {
    private readonly Regex regex = new Regex("[.0-9]+");

    public MultiFaceFloorPlanView() {
      InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      var button = (Button)sender;
      var data = (InfoByView)button.DataContext;

      var dialog = new OpenFileDialog() {
        Filter = "CAD Files(*.dwg)|*.dwg",
        Title = data.ViewDir,
      };
      if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        data.FileFullPath = dialog.FileName;
        //data.FileName = dialog.SafeFileName;
      }
    }

    private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
      var textbox = (TextBox)sender;
      if (e.Text.Equals(".")) {
        e.Handled = textbox.Text.Contains(".");
        return;
      }
      e.Handled = !regex.IsMatch(e.Text);
    }

    private void TextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
      if (e.Key == System.Windows.Input.Key.Space) {
        e.Handled = true;
      }
    }

    private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {

    }

  }
}

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project1.Revit.AlignPipe {
  /// <summary>
  /// AlignPipeOptionView.xaml에 대한 상호 작용 논리
  /// </summary>
  public partial class AlignPipeOptionView : Window {
    private readonly Regex _NumberRegex = new Regex("[0-9]+");
    private readonly string _Dot = ".";

    public AlignPipeOptionView() {
      InitializeComponent();
    }

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
      // Enter, Tab, Esc 키 검사 안함
      if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape) {
        return;
      }
      if (e.Key == Key.Decimal || e.Key == Key.OemPeriod) {
        var textBox = (TextBox)sender;
        e.Handled = textBox.Text.Contains(_Dot);
        return;
      }
      e.Handled =
        e.Key != Key.Delete &&
        e.Key != Key.Back &&
        !_NumberRegex.IsMatch(e.Key.ToString());
    }
  }
}

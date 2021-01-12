using System.Windows;

namespace ArduinoRGBController
{
    /// <summary>
    /// Interaction logic for StaticColorPicker.xaml
    /// </summary>
    public partial class StaticColorPicker : Window
    {
        public StaticColorPicker()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}

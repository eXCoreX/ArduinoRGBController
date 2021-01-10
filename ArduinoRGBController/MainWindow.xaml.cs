using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;

namespace ArduinoRGBController
{
    class UnminimizeWindowCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            (parameter as MainWindow).WindowState = WindowState.Normal;
            (parameter as MainWindow).Activate();
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string ArduinoCOM { get; set; } = "";

        SerialPort ArduinoSerial { get; set; }

        private bool ActionDone { get; set; } = true;
        private bool MsgRepeat { get; set; } = false;

        private struct ActionFired
        {
            public object sender;
            public EventArgs e;
            public ActionFired(object o, EventArgs e)
            {
                sender = o;
                this.e = e;
            }
        }
        private ActionFired lastFired, lastSent;

        private Thread serialThread;

        public MainWindow()
        {
            InitializeComponent();

            if(FindArduino())
            {
                InitSerialConnection();
            }
        }

        private bool InitSerialConnection()
        {
            ArduinoSerial = new SerialPort(ArduinoCOM, 115200);
            ArduinoSerial.ReadBufferSize = 1024;
            ArduinoSerial.WriteBufferSize = 1024;
            if (!ArduinoSerial.IsOpen)
            {
                try
                {
                    ArduinoSerial.Open();
                    serialThread = new Thread(ArduinoSerial_DataReceived);
                    serialThread.IsBackground = true;
                    serialThread.Start();
                    return true;
                }
                catch (Exception)
                {
                    mainText.Content = "Arduino is busy";
                }
            }
            return false;
        }

        private void ArduinoSerial_DataReceived()
        {
            while (true)
            {
                try
                {
                    var tmp = ArduinoSerial.ReadLine();
                    Debug.WriteLine(tmp);
                    if (tmp.Contains("R"))
                    {
                        if (true)
                        {
                            if (MsgRepeat || !ActionDone)
                            {
                                if (lastFired.sender == null)
                                {
                                    lastFired = lastSent;
                                }
                                MsgRepeat = false;
                            }
                            if (lastFired.sender != null)
                            {
                                ActionDone = false;
                                Dispatcher.Invoke(() =>
                                {
                                    if (lastFired.sender is Button)
                                    {
                                        ToggleAnimReal(lastFired.sender, (MouseButtonEventArgs)lastFired.e);
                                    }
                                    else if (lastFired.sender is Slider)
                                    {
                                        if ((lastFired.sender as Slider).Name == "brightness")
                                        {
                                            brightness_ValueChangedReal(lastFired.sender, (RoutedPropertyChangedEventArgs<double>)lastFired.e);
                                        }
                                        else
                                        {
                                            animSpeed_ValueChangedReal(lastFired.sender, (RoutedPropertyChangedEventArgs<double>)lastFired.e);
                                        }
                                    }
                                    else
                                    {
                                        ColorPicker_SelectedColorChangedReal(lastFired.sender, (RoutedPropertyChangedEventArgs<Color?>)lastFired.e);
                                    }
                                });
                                lastSent = lastFired;
                                lastFired.sender = null;
                            }
                        }
                    }
                    else if (tmp.Contains("done"))
                    {
                        ActionDone = true;
                    }
                    else if (tmp.Contains("rep"))
                    {
                        MsgRepeat = true;
                    }
                    else if (tmp.Contains("H"))
                    {
                        if (MsgRepeat || lastFired.sender != null)
                        {
                            ArduinoSerial.Write("Y");
                            Debug.WriteLine("Y");
                        }
                        else
                        {
                            ArduinoSerial.Write("N");
                            Debug.WriteLine("N");
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception)
                {

                }
            }
        }

        private string AutodetectArduinoPort()
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%COM%'");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();

                    if (desc.Contains("CH340"))
                    {
                        return item["Name"].ToString();
                    }
                }
            }
            catch (ManagementException e)
            {
                /* Do Nothing */
            }

            return null;
        }

        bool FindArduino()
        {
            var port = AutodetectArduinoPort();
            if (!string.IsNullOrEmpty(port))
            {
                string com = port.Split('(')[1].Trim();
                com = com.Substring(0, com.Length - 1);
                ArduinoCOM = com;
                mainText.Content = com;
                return true;
            }
            else
            {
                var ports = SerialPort.GetPortNames();
                mainText.Content = string.Join("\n", ports);
            }
            return false;
        }


        bool CheckConnection(object sender, EventArgs e)
        {
            if (ArduinoSerial == null)
            {
                return false;
            }
            if (ArduinoSerial.IsOpen)
            {
                lastFired = new ActionFired(sender, e);
            }
            else
            {
                if (FindArduino())
                {
                    if (InitSerialConnection())
                    {
                        lastFired = new ActionFired(sender, e);
                    }
                }
            }

            return false;
        }


        private void ToggleAnim(object sender, MouseButtonEventArgs e)
        {
            CheckConnection(sender, e);
        }

        private void ToggleAnimReal(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button)
            {
                var btn = sender as Button;
                switch (btn.Name)
                {
                    case "toggleRainbow":
                        ArduinoSerial.Write("$1^");
                        Debug.WriteLine("$1^");
                        break;
                    case "toggleStatic":
                        ArduinoSerial.Write("$2^");
                        Debug.WriteLine("$2^");
                        break;
                    default:
                        break;
                }
            }
        }

        ~MainWindow()
        {
            if (ArduinoSerial != null)
            {
                ArduinoSerial.Close();
                ArduinoSerial.Dispose();
            }
            if (serialThread != null)
            {
                serialThread.Abort();
            }
        }

        private void brightness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CheckConnection(sender, e);
        }

        private void brightness_ValueChangedReal(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte val = (byte)Math.Round(e.NewValue * 255);
            ArduinoSerial.Write($"$3, {val}^");
            Debug.WriteLine($"$3, {val}^");
        }

        private void animSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CheckConnection(sender, e);
        }

        private void animSpeed_ValueChangedReal(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte val = (byte)Math.Round(e.NewValue * 10);
            ArduinoSerial.Write($"$4, {val}^");
            Debug.WriteLine($"$4, {val}^");
        }

        private void Button_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var picker = new StaticColorPicker();
            picker.Owner = this;
            picker.colorPicker.SelectedColorChanged += ColorPicker_SelectedColorChanged;
            if (picker.ShowDialog() == true)
            {

            }
            picker.colorPicker.SelectedColorChanged -= ColorPicker_SelectedColorChanged;
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (!e.NewValue.HasValue)
            {
                return;
            }
            CheckConnection(sender, e);
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    notifyIcon.Visibility = Visibility.Hidden;
                    ShowInTaskbar = true;
                    break;
                case WindowState.Minimized:
                    notifyIcon.Visibility = Visibility.Visible;
                    ShowInTaskbar = false;
                    break;
                case WindowState.Maximized:
                    break;
                default:
                    break;
            }
        }


        private void ColorPicker_SelectedColorChangedReal(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var color = e.NewValue.Value;
            ArduinoSerial.Write($"$5, {color.R}, {color.G}, {color.B}^");
            Debug.WriteLine($"$5, {color.R}, {color.G}, {color.B}^");
        }
    }
}

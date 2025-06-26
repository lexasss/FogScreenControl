using System.ComponentModel;
using System.Windows;

namespace FogControlWithKinect
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
                StatusText = _isReady ? "Kinect is available." : "Kinect is not available.";
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();

            StatusText = "Kinect is not available";
        }

        // Internal

        readonly Services.LowPassFilter _pointSmoother = new Services.LowPassFilter(200, 33);
        readonly Services.LowPassFilter _depthSmoother = new Services.LowPassFilter(70, 33);

        Services.HandTipService _handTipService = null;
        Services.MouseController _mouseController = null;

        bool _isReady = false;
        bool _isRunning = false;
        string _statusText = "Kinect is not available";

        double _distanceToScreen = 2.15; // meters, distance from Kinect to the screen


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _handTipService = new Services.HandTipService();
            if (_handTipService.IsAvailable)
            {
                IsReady = true;
            }

            _handTipService.IsAvailableChanged += (s, args) => Dispatcher.Invoke(() =>
            {
                IsReady = args.IsAvailable;
            });

            _handTipService.TipLocationChanged += (s, args) => Dispatcher.Invoke(() =>
            {
                double depth = _depthSmoother.Filter(args.Location.Z);
                _mouseController?.SetPosition(
                    args.Location.X,
                    args.Location.Y,
                    depth
                );
            });
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _handTipService?.Dispose();
        }

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = !IsRunning;

            if (IsRunning)
            {
                var mapper = new Services.MappingService("last-calib.txt");
                if (!mapper.IsReady)
                {
                    MessageBox.Show("Mapping service is not ready. Please calibrate the device first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsRunning = false;
                    return;
                }

                _mouseController = new Services.MouseController(rdbMethodTouch.IsChecked == true ?
                    Services.InterationMethod.Touch : Services.InterationMethod.Tap,
                    _distanceToScreen,
                    mapper,
                    _pointSmoother);

                _handTipService?.Start(rdbHandLeft.IsChecked == true ? Services.Hand.Left : Services.Hand.Right);

                btnStartStop.Content = "Stop";
            }
            else
            {
                _handTipService?.Stop();
                _mouseController = null;

                btnStartStop.Content = "Start";
            }
        }
    }
}
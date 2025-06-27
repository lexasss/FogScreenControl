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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsReady)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
            }
        }

        public string StatusText => _isReady ? "Kinect is connected." : "Kinect is not available.";

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartInteractionButtonText)));
            }
        }

        public string StartInteractionButtonText => _isRunning ? "Stop interaction" : "Start interaction";

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Internal

        readonly Services.LowPassFilter _pointSmoother = new Services.LowPassFilter(200, 33);
        readonly Services.LowPassFilter _depthSmoother = new Services.LowPassFilter(70, 33);

        Services.HandTipService _handTipService = null;
        Services.MouseController _mouseController = null;

        bool _isReady = false;
        bool _isRunning = false;

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

        private void StartStopInteraction_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = !IsRunning;

            if (IsRunning)
            {
                var mapper = new Services.MappingService(App.CalibrationFileName);
                if (!mapper.IsReady)
                {
                    MessageBox.Show("Kinect-to-screen mapping service is not ready. Please calibrate the device first.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsRunning = false;
                    return;
                }

                var interactionMethod = chkMouseDrag.IsChecked == true ? Services.InterationMethod.Tap : Services.InterationMethod.Touch;
                var hand = chkHand.IsChecked == true ? Services.Hand.Right: Services.Hand.Left;

                _mouseController = new Services.MouseController(
                    interactionMethod,
                    mapper,
                    _pointSmoother
                );

                _handTipService?.Start(hand);
            }
            else
            {
                _handTipService?.Stop();
                _mouseController = null;
            }
        }

        private void Calibrate_Click(object sender, RoutedEventArgs e)
        {
            var calibrationWindow = new CalibrationWindow(_handTipService);
            calibrationWindow.ShowDialog();
        }
    }
}
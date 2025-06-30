using DevExpress.Mvvm;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToggleInteractionButtonText)));
            }
        }

        public string ToggleInteractionButtonText => _isRunning ? "Stop interaction" : "Start interaction";

        public ICommand ToggleInteractionCommand => new DelegateCommand(() =>
        {
            IsRunning = !IsRunning;

            if (IsRunning)
            {
                var mapper = new Services.MappingService(App.CalibrationFileName);
                if (!mapper.IsReady)
                {
                    MessageBox.Show("Kinect-to-screen mapping is not available. Please calibrate the device first.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsRunning = false;
                    return;
                }

                var interactionMethod = chkMouseDrag.IsChecked == true ? Services.InterationMethod.Tap : Services.InterationMethod.Touch;
                var hand = chkHand.IsChecked == true ? Services.Hand.Right : Services.Hand.Left;

                _mouseController = new Services.MouseController(
                    interactionMethod,
                    mapper,
                    App.PointSmoother
                );

                _handTipService?.Start(hand);
            }
            else
            {
                _handTipService?.Stop();
                _skeletonPainter?.Clear();
                _mouseController = null;
            }
        });

        public ICommand CalibrateCommand => new DelegateCommand(() =>
        {
            var calibrationWindow = new CalibrationWindow(_handTipService);

            _isCalibrating = true;
            calibrationWindow.ShowDialog();
            _isCalibrating = false;
        });

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Internal

        Services.HandTipService _handTipService = null;
        Services.MouseController _mouseController = null;
        Services.SkeletonPainter _skeletonPainter = null;

        bool _isReady = false;
        bool _isRunning = false;
        bool _isCalibrating = false;

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
                if (_isCalibrating)
                    return;

                double depth = App.DepthSmoother.Filter(args.Location.Z);
                _mouseController?.SetPosition(
                    args.Location.X,
                    args.Location.Y,
                    depth
                );
            });

            _handTipService.FrameArrived += (s, args) => Dispatcher.Invoke(() =>
            {
                if (_isCalibrating)
                    return;

                _skeletonPainter?.Draw(args.Bodies, (pt) => _handTipService.MapPoint(pt));
            });

            var frameDescription = _handTipService.FrameDescription;
            _skeletonPainter = new Services.SkeletonPainter(frameDescription.Width, frameDescription.Height);

            imgSkeleton.Source = _skeletonPainter.ImageSource;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _handTipService?.Dispose();
        }
    }
}
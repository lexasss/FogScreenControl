using FogControlWithKinect.Services;
using System.ComponentModel;
using System.Windows;

namespace FogControlWithKinect
{
    public partial class CalibrationWindow : Window, INotifyPropertyChanged
    {
        public bool IsCalibrating
        {
            get => _isCalibrating;
            private set
            {
                _isCalibrating = value;
                if (_isCalibrating)
                {
                    ResizeMode = ResizeMode.NoResize;
                    WindowState = WindowState.Maximized;
                    WindowStyle = WindowStyle.None;
                }
                else
                {
                    CalibrationPoint = CalibrationPoint.Undefined;

                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                    ResizeMode = ResizeMode.CanResize;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCalibrating)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CalibrationButtonText)));
            }
        }

        public string CalibrationButtonText => _isCalibrating ? "Interrupt" : "Calibrate";

        public CalibrationPoint CalibrationPoint
        {
            get => _calibrationPoint;
            private set
            {
                _calibrationPoint = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CalibrationPoint)));
            }
        }

        public double DistanceToScreen
        {
            get => _distanceToScreen;
            set
            {
                _distanceToScreen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistanceToScreen)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CalibrationWindow(HandTipService handTipService)
        {
            InitializeComponent();

            _handTipService = handTipService;
            _handTipService.TipLocationChanged += OnHandData;

            MappingService mapper = new Services.MappingService(App.CalibrationFileName);
            if (mapper.IsReady)
            {
                DistanceToScreen = mapper.DistanceToScreen;
            }
        }

        // Internal

        readonly CalibrationPoint[] _calibrationPoints = new[]
        {
            CalibrationPoint.TopLeft,
            CalibrationPoint.TopRight,
            CalibrationPoint.BottomRight,
            CalibrationPoint.BottomLeft,
        };

        readonly HandTipService _handTipService;

        CalibrationService _calibrationService = null;
        CalibrationPoint _calibrationPoint = CalibrationPoint.Undefined;
        
        bool _isCalibrating = false;
        int _calibPointIndex = -1;
        double _distanceToScreen = 2.15; // Default distance to screen in meters

        private void OnHandData(object sender, HandTipService.TipLocationChangedEventArgs e)
        {
            if (_calibrationService == null)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var cameraPoint = e.Location;
                var result = _calibrationService?.Feed(cameraPoint);
                if (result == CalibrationService.Event.PointStart)
                {
                }
                else if (result == CalibrationService.Event.PointEnd)
                {
                    CalibrationPoint = ++_calibPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT
                        ? _calibrationPoints[_calibPointIndex]
                        : CalibrationPoint.Undefined;
                }
                else if (result == CalibrationService.Event.Finished)
                {
                    IsCalibrating = false;
                    _handTipService.Stop();

                    try
                    {
                        _calibrationService?.SaveToFile(App.CalibrationFileName);
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show($"Failed to save calibration data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    _calibrationService = null;
                }
            });
        }

        private void StartStopCalibration_Click(object sender, RoutedEventArgs e)
        {
            IsCalibrating = !IsCalibrating;

            if (IsCalibrating)
            {
                _calibrationService = new CalibrationService(_distanceToScreen);
                _handTipService.Start(Hand.Right);

                _calibPointIndex = 0;
                CalibrationPoint = _calibrationPoints[_calibPointIndex];
            }
            else
            {
                _handTipService.Stop();
                _calibrationService = null;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            _handTipService.Stop();
            _calibrationService = null;
        }
    }
}

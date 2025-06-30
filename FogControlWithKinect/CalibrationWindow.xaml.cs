using DevExpress.Mvvm;
using FogControlWithKinect.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

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
                if (_skeletonPainter != null)
                {
                    _skeletonPainter.DistanceToScreen = _distanceToScreen;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistanceToScreen)));
            }
        }

        public ICommand ToggleCalibrationCommand => new DelegateCommand(() =>
        {
            IsCalibrating = !IsCalibrating;

            if (IsCalibrating)
            {
                _calibrationService = new CalibrationService(_distanceToScreen);
                _handTipService.Start(_hand);

                _calibPointIndex = 0;
                CalibrationPoint = _calibrationPoints[_calibPointIndex];
            }
            else
            {
                _handTipService.Stop();
                _calibrationService = null;
            }
        });

        public ICommand CloseCommand => new DelegateCommand(() =>
        {
            _handTipService.Stop();
            _calibrationService = null;

            DialogResult = false;
        });

        public event PropertyChangedEventHandler PropertyChanged;

        public CalibrationWindow(HandTipService handTipService, Hand hand)
        {
            InitializeComponent();

            _handTipService = handTipService;
            _hand = hand;

            MappingService mapper = new MappingService(App.CalibrationFileName);
            if (mapper.IsReady)
            {
                DistanceToScreen = mapper.DistanceToScreen;
            }

            var frameDescription = _handTipService.FrameDescription;
            _skeletonPainter = new SkeletonPainter(frameDescription.Width, frameDescription.Height, _hand, _distanceToScreen);

            imgSkeleton.Source = _skeletonPainter.ImageSource;

            _handTipService.FrameArrived += HandTipService_FrameArrived;
            _handTipService.TipLocationChanged += HandTipService_TipLocationChanged;
        }

        private void HandTipService_FrameArrived(object sender, HandTipService.FrameArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _skeletonPainter.Draw(e.Bodies, (pt) => _handTipService.MapPoint(pt));
            });
        }

        // Internal

        readonly CalibrationPoint[] _calibrationPoints = new[]
        {
            CalibrationPoint.TopLeft,
            CalibrationPoint.TopRight,
            CalibrationPoint.BottomLeft,
            CalibrationPoint.BottomRight,
        };

        readonly HandTipService _handTipService;
        readonly SkeletonPainter _skeletonPainter;
        readonly Hand _hand;

        MouseController _mouseController = null;
        CalibrationService _calibrationService = null;
        CalibrationPoint _calibrationPoint = CalibrationPoint.Undefined;

        bool _isCalibrating = false;
        bool _isVerifying = false;
        int _calibPointIndex = -1;
        double _distanceToScreen = 2.15; // Default distance to screen in meters

        public bool VerifyCalibration()
        {
            _mouseController = new MouseController(
                InterationMethod.Touch,
                new MappingService(_calibrationService),
                App.PointSmoother
            );

            _isVerifying = true;

            var result = MessageBox.Show("Is the calibration accurate?", "Calibration Verification", MessageBoxButton.YesNo, MessageBoxImage.Question);

            _isVerifying = false;

            return result == MessageBoxResult.Yes;
        }

        private void HandTipService_TipLocationChanged(object sender, HandTipService.TipLocationChangedEventArgs e)
        {
            if (_calibrationService == null)
                return;

            Dispatcher.Invoke(() =>
            {
                if (_isVerifying)
                {
                    double depth = App.DepthSmoother.Filter(e.Location.Z);
                    _mouseController?.SetPosition(
                        e.Location.X,
                        e.Location.Y,
                        depth
                    );
                }
                else
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
                        try
                        {
                            if (VerifyCalibration())
                            {
                                _calibrationService?.SaveToFile(App.CalibrationFileName);
                                MessageBox.Show("Calibration data saved scucessfully.", "Calibration Verification", MessageBoxButton.OK, MessageBoxImage.Information);
                                DialogResult = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to save calibration data: {ex.Message}", "Calibration Verification", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        IsCalibrating = false;

                        _handTipService.Stop();

                        _calibrationService = null;
                    }
                }
            });

            System.Diagnostics.Debug.WriteLine($"Calibration point: {CalibrationPoint} at {e.Location.X}, {e.Location.Y}, {e.Location.Z}"); 
        }
    }
}

using DevExpress.Mvvm;
using FogControlWithKinect.Enums;
using FogControlWithKinect.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FogControlWithKinect
{
    public partial class CalibrationWindow : Window, INotifyPropertyChanged
    {
        public IEnumerable<int> CalibrationPointCounts => CalibrationService.CalibPointCounts;

        public int CalibrationPointCount
        {
            get => Properties.Settings.Default.CalibrationPointCount;
            set
            {
                Properties.Settings.Default.CalibrationPointCount = value;
                Properties.Settings.Default.Save();

                _calibrationPoints = CalibrationService.GetCalibPoints(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CalibrationPointCount)));
            }
        }

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
            get => _mappingService?.DistanceToScreen ?? 0;
            set
            {
                _mappingService.DistanceToScreen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistanceToScreen)));
            }
        }

        public ICommand ToggleCalibrationCommand => new DelegateCommand(() =>
        {
            IsCalibrating = !IsCalibrating;

            if (IsCalibrating)
            {
                _calibrationService = new CalibrationService(_mappingService, CalibrationPointCount);
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

        public CalibrationWindow(HandTipService handTipService, Hand hand, MappingMethod mappingMethod)
        {
            InitializeComponent();

            if (!CalibrationService.CalibPointCounts.Contains(CalibrationPointCount))
            {
                CalibrationPointCount = CalibrationService.CalibPointCounts[0];
            }
            else
            {
                _calibrationPoints = CalibrationService.GetCalibPoints(CalibrationPointCount);
            }

            _mappingService = new MappingService(mappingMethod, App.CalibrationFileName);
            _handTipService = handTipService;
            _hand = hand;

            var frameDescription = _handTipService.FrameDescription;
            _skeletonPainter = new SkeletonPainter(frameDescription.Width, frameDescription.Height, _hand)
            {
                MappingService = _mappingService,
            };

            imgSkeleton.Source = _skeletonPainter.ImageSource;

            _handTipService.FrameArrived += HandTipService_FrameArrived;
            _handTipService.TipLocationChanged += HandTipService_TipLocationChanged;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DistanceToScreen)));
        }

        private void HandTipService_FrameArrived(object sender, HandTipService.FrameArrivedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _skeletonPainter.Draw(e.Bodies, (pt) => _handTipService.SpaceToPlane(pt));
            });
        }

        // Internal

        readonly MappingService _mappingService;
        readonly HandTipService _handTipService;
        readonly SkeletonPainter _skeletonPainter;
        readonly Hand _hand;

        MouseController _mouseController = null;
        CalibrationService _calibrationService = null;

        CalibrationPoint _calibrationPoint = CalibrationPoint.Undefined;
        CalibrationPoint[] _calibrationPoints;

        bool _isCalibrating = false;
        bool _isVerifying = false;
        int _calibPointIndex = -1;

        public bool VerifyCalibration()
        {
            _mouseController = new MouseController(
                InterationMethod.Move,
                new MappingService(_mappingService.Method, _calibrationService.SpacePoints, _mappingService.DistanceToScreen));

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
                    _mouseController?.SetPosition(e.Location);
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
                        Utils.Sounds.CalibPointDone.Play();

                        CalibrationPoint = ++_calibPointIndex < CalibrationPointCount
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

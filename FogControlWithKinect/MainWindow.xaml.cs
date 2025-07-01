using DevExpress.Mvvm;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace FogControlWithKinect
{

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public bool IsHandRight
        {
            get => _hand == Enums.Hand.Right;
            set
            {
                _hand = value ? Enums.Hand.Right : Enums.Hand.Left;
                if (_skeletonPainter != null)
                {
                    _skeletonPainter.Hand = _hand;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsHandRight)));
            }
        }
        public bool IsClickAndDrag
        {
            get => Properties.Settings.Default.UseClickAndDrop;
            set
            {
                Properties.Settings.Default.UseClickAndDrop = value;
                Properties.Settings.Default.Save();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsClickAndDrag)));
            }
        }
        public bool IsMouseEventSoundEnabled
        {
            get => Properties.Settings.Default.UseMouseEventSounds;
            set
            {
                Properties.Settings.Default.UseMouseEventSounds = value;
                Properties.Settings.Default.Save();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMouseEventSoundEnabled)));
            }
        }
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
                    MessageBox.Show("Sensor-to-fogscreen mapping is not available. Please calibrate the sensor.",
                        Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    IsRunning = false;
                    return;
                }

                var interactionMethod = IsClickAndDrag ? Enums.InterationMethod.ClickAndDrag : Enums.InterationMethod.Move;

                _mouseController = new Services.MouseController(interactionMethod, mapper)
                {
                    IsPlayingSoundOnMouseEvents = IsMouseEventSoundEnabled
                };

                _skeletonPainter.MappingService = mapper;

                _handTipService?.Start(_hand);
            }
            else
            {
                _handTipService?.Stop();
                _skeletonPainter?.Clear();
                _skeletonPainter.MappingService = null;
                _mouseController = null;
            }
        });

        public ICommand CalibrateCommand => new DelegateCommand(() =>
        {
            var calibrationWindow = new CalibrationWindow(_handTipService, _hand);

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

        Enums.Hand _hand = Enums.Hand.Right;

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

                _mouseController?.SetPosition(args.Location);
            });

            _handTipService.FrameArrived += (s, args) => Dispatcher.Invoke(() =>
            {
                if (_isCalibrating)
                    return;

                _skeletonPainter?.Draw(args.Bodies, (pt) => _handTipService.SpaceToPlane(pt));
            });

            var frameDescription = _handTipService.FrameDescription;
            _skeletonPainter = new Services.SkeletonPainter(frameDescription.Width, frameDescription.Height, _hand);

            imgSkeleton.Source = _skeletonPainter.ImageSource;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            _handTipService?.Dispose();
        }
    }
}
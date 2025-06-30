using System;
using System.IO;

namespace FogControlWithKinect.Services
{
    public class MappingService
    {
        public bool IsReady { get; private set; } = false;
        public double DistanceToScreen { get; private set; } = 2.15;

        private MappingService()
        {
            _mapper = new Mappers.NaiveMapper();
        }

        public MappingService(string calibFileName) : this()
        {
            try
            {
                LoadFromFile(calibFileName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load calibration file: {ex.Message}");
                return;
            }
        }

        public MappingService(CalibrationService calibrationService) : this()
        {
            DistanceToScreen = calibrationService.DistanceToScreen;

            var screenPoints = ConstructScreenPoints();
            var calibPoints = calibrationService.TrackerPoints;
            _mapper.Configure(screenPoints, calibPoints);

            IsReady = true;
        }

        /// <summary>
        /// Maps Kinect coordinates to screen coordinates.
        /// </summary>
        /// <param name="x">Kinect hand tip joint X coordinate</param>
        /// <param name="y">Kinect hand tip joint Y coordinate</param>
        /// <return>The screen point</return>
        public System.Windows.Point Map(double x, double y)
        {
            if (!IsReady)
            {
                return new System.Windows.Point(x, y);
            }

            return _mapper.Map(x, y);
        }

        // Internal

        readonly Mappers.IMapper _mapper;

        private void LoadFromFile(string calibFileName)
        {
            double[,] calibPoints = new double[CalibrationService.CALIBRATOR_POINT_COUNT, 3];
            double[,] screenPoints = new double[CalibrationService.CALIBRATOR_POINT_COUNT, 2];

            int calibPointIndex = 0;
            int screenPointIndex = 0;

            using (var calibFile = new StreamReader(calibFileName))
            {
                while (!calibFile.EndOfStream)
                {
                    var line = calibFile.ReadLine();
                    var p = line?.Split(' ');
                    if (p?.Length == 3 && calibPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT) // tracker points
                    {
                        calibPoints[calibPointIndex, 0] = double.Parse(p[0]);
                        calibPoints[calibPointIndex, 1] = double.Parse(p[1]);
                        calibPoints[calibPointIndex, 2] = double.Parse(p[2]);
                        calibPointIndex++;
                    }
                    else if (p?.Length == 2 && screenPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT)    // screen points
                    {
                        screenPoints[screenPointIndex, 0] = double.Parse(p[0]);
                        screenPoints[screenPointIndex, 1] = double.Parse(p[1]);
                        screenPointIndex++;
                    }
                    else if (line.Length > 0 && double.TryParse(line, out double distanceToScreen))
                    {
                        DistanceToScreen = distanceToScreen;
                    }
                }
            }

            if (screenPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT)
            {
                screenPoints = ConstructScreenPoints();
            }
            if (calibPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT)
            {
                return;
            }

            _mapper.Configure(screenPoints, calibPoints);

            IsReady = true;
        }

        private double[,] ConstructScreenPoints()
        {
            var screenPoints = new double[CalibrationService.CALIBRATOR_POINT_COUNT, 2];

            var screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            var screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);

            screenPoints[0, 0] = 0;
            screenPoints[0, 1] = 0;
            screenPoints[1, 0] = screenWidth;
            screenPoints[1, 1] = 0;
            screenPoints[2, 0] = 0;
            screenPoints[2, 1] = screenHeight;
            screenPoints[3, 0] = screenWidth;
            screenPoints[3, 1] = screenHeight;

            return screenPoints;
        }
    }
}
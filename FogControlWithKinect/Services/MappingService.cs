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
            _screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);
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
            _calibPoints = calibrationService.Points;
            DistanceToScreen = calibrationService.DistanceToScreen;

            IsReady = true;
            Configure();
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

            return new System.Windows.Point(
                (x - _offsetX) * _scaleX,
                (y - _offsetY) * _scaleY
            );
        }

        // Internal

        readonly int _screenWidth;
        readonly int _screenHeight;

        readonly double[,] _calibPoints = new double[CalibrationService.CALIBRATOR_POINT_COUNT, 3];

        double _scaleX = 1;
        double _scaleY = 1;
        double _offsetX = 0;
        double _offsetY = 0;

        private void LoadFromFile(string calibFileName)
        {
            using (var calibFile = new StreamReader(calibFileName))
            {
                int pointIndex = 0;

                while (!calibFile.EndOfStream)
                {
                    var line = calibFile.ReadLine();
                    var p = line?.Split(' ');
                    if (p?.Length == 3)
                    {
                        _calibPoints[pointIndex, 0] = double.Parse(p[0]);
                        _calibPoints[pointIndex, 1] = double.Parse(p[1]);
                        _calibPoints[pointIndex, 2] = double.Parse(p[2]);
                        pointIndex++;
                    }
                    else if (line.Length > 0 && double.TryParse(line, out double distanceToScreen))
                    {
                        DistanceToScreen = distanceToScreen;
                    }
                }

                if (pointIndex == CalibrationService.CALIBRATOR_POINT_COUNT)
                {
                    IsReady = true;
                    Configure();
                }
            }
        }

        private void Configure()
        {
            _offsetX = _calibPoints[0, 0];
            _offsetY = _calibPoints[0, 1];

            _scaleX = _screenWidth / (((_calibPoints[1, 0] - _calibPoints[0, 0]) + (_calibPoints[3, 0] - _calibPoints[2, 0])) / 2);
            _scaleY = _screenHeight / (((_calibPoints[2, 1] - _calibPoints[0, 1]) + (_calibPoints[3, 1] - _calibPoints[1, 1])) / 2);
        }
    }
}
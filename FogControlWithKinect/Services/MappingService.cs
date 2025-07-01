using FogControlWithKinect.Models;
using System;
using System.IO;

namespace FogControlWithKinect.Services
{
    public class MappingService
    {
        public bool IsReady { get; private set; } = false;
        /// <summary>
        /// Distance between the tracking camera and the screen in meters.
        /// </summary>
        public double DistanceToScreen { get; set; } = 2.15;

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

        public MappingService(SpacePoint[] spacePoints, double distanceToScreen) : this()
        {
            DistanceToScreen = distanceToScreen;

            var screenPoints = ConstructScreenPoints();
            var calibPoints = spacePoints;
            _mapper.Configure(screenPoints, calibPoints);

            IsReady = true;
        }

        public ScreenPoint Map(SpacePoint cameraPoint)
        {
            if (!IsReady)
            {
                return ScreenPoint.Zero;
            }

            return _mapper.Map(cameraPoint);
        }

        public bool IsInFog(SpacePoint point) => _mapper.IsInFog(point, DistanceToScreen);

        public double GetDistanceFromScreen(SpacePoint point) => _mapper.GetDistanceFromScreen(point, DistanceToScreen);

        public ScreenPoint[] GetScreenPoints() => ConstructScreenPoints();

        // Internal

        readonly Mappers.IMapper _mapper;

        private void LoadFromFile(string calibFileName)
        {
            SpacePoint[] calibPoints = new SpacePoint[CalibrationService.CALIBRATOR_POINT_COUNT];
            ScreenPoint[] screenPoints = new ScreenPoint[CalibrationService.CALIBRATOR_POINT_COUNT];

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
                        calibPoints[calibPointIndex++] = new SpacePoint(double.Parse(p[0]), double.Parse(p[1]), double.Parse(p[2]));
                    }
                    else if (p?.Length == 2 && screenPointIndex < CalibrationService.CALIBRATOR_POINT_COUNT)    // screen points
                    {
                        screenPoints[screenPointIndex++] = new ScreenPoint(double.Parse(p[0]), double.Parse(p[1]));
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

        private ScreenPoint[] ConstructScreenPoints()
        {
            var screenPoints = new ScreenPoint[CalibrationService.CALIBRATOR_POINT_COUNT];

            var screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            var screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);

            screenPoints[0] = new ScreenPoint(0, 0);
            screenPoints[1] = new ScreenPoint(screenWidth, 0);
            screenPoints[2] = new ScreenPoint(0, screenHeight);
            screenPoints[3] = new ScreenPoint(screenWidth, screenHeight);

            return screenPoints;
        }
    }
}
using FogControlWithKinect.Models;
using System;
using System.Collections.Generic;
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
            _mapper = new Mappers.Projective2D();
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
            _mapper.Configure(screenPoints, spacePoints);

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

        public bool IsInFog(SpacePoint spacePoint) => _mapper.GetDistanceFromScreen(spacePoint, DistanceToScreen) < 0;

        public double GetDistanceFromScreen(SpacePoint point) => _mapper.GetDistanceFromScreen(point, DistanceToScreen);

        public ScreenPoint[] GetScreenPoints() => ConstructScreenPoints();

        // Internal

        readonly Mappers.IMapper _mapper;

        private void LoadFromFile(string calibFileName)
        {
            var spacePoints = new List<SpacePoint>();
            var screenPoints = new List<ScreenPoint>();

            using (var calibFile = new StreamReader(calibFileName))
            {
                while (!calibFile.EndOfStream)
                {
                    var line = calibFile.ReadLine();
                    var p = line?.Split(' ');
                    if (p?.Length == 3) // tracker points
                    {
                        spacePoints.Add(new SpacePoint(double.Parse(p[0]), double.Parse(p[1]), double.Parse(p[2])));
                    }
                    else if (p?.Length == 2)    // screen points
                    {
                        screenPoints.Add(new ScreenPoint(double.Parse(p[0]), double.Parse(p[1])));
                    }
                    else if (line.Length > 0 && double.TryParse(line, out double distanceToScreen))
                    {
                        DistanceToScreen = distanceToScreen;
                    }
                }
            }

            if (spacePoints.Count < 4)
            {
                return;
            }

            var screenPointArray = screenPoints.ToArray();
            if (screenPointArray.Length < 4)
            {
                screenPointArray = ConstructScreenPoints();
            }

            _mapper.Configure(screenPointArray, spacePoints.ToArray());

            IsReady = true;
        }

        private ScreenPoint[] ConstructScreenPoints()
        {
            var screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            var screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);

            return new ScreenPoint[] {
                new ScreenPoint(0, 0),
                new ScreenPoint(screenWidth, 0),
                new ScreenPoint(0, screenHeight),
                new ScreenPoint(screenWidth, screenHeight)
            };
        }
    }
}
using FogScreenControl.Enums;
using FogScreenControl.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace FogScreenControl.Services
{
    public class MappingService
    {
        public bool IsReady { get; private set; } = false;

        /// <summary>
        /// Distance between the tracking camera and the fog screen in meters.
        /// </summary>
        public double DistanceToScreen { get; set; } = 2.15;

        public MappingMethod Method => _mapper.Method;

        private MappingService(MappingMethod method)
        {
            if (method == MappingMethod.Naive)
                _mapper = new Mappers.Naive();
            else if (method == MappingMethod.Linear2D)
                _mapper = new Mappers.Projective2D();
            else if (method == MappingMethod.Linear3D)
                _mapper = new Mappers.Projective3D();
            else
                throw new ArgumentException("Invalid mapping method specified.", nameof(method));
        }

        public MappingService(MappingMethod method, string calibFileName) : this(method)
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

        public MappingService(MappingMethod method, SpacePoint[] spacePoints, double distanceToScreen) : this(method)
        {
            DistanceToScreen = distanceToScreen;

            var screenPoints = CalibrationService.GetScreenPoints(spacePoints.Length);
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
                    else if (line.Length > 0 && double.TryParse(line, out double distanceToScreen)) // distance to screen
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
                screenPointArray = CalibrationService.GetScreenPoints(screenPoints.Count);
            }

            _mapper.Configure(screenPointArray, spacePoints.ToArray());

            IsReady = true;
        }
    }
}
using FogScreenControl.Enums;
using FogScreenControl.Models;
using System;

namespace FogScreenControl.Services
{
    public class MappingService
    {
        public bool IsReady { get; private set; } = false;

        /// <summary>
        /// Distance between the tracking camera and the fog screen in meters.
        /// </summary>
        public double TrackerToScreenDistance { get; set; } = 2.15;

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
                CalibrationData calibData = CalibrationService.LoadFromFile(calibFileName);
                if (calibData.IsValid)
                {
                    _mapper.Configure(calibData.ScreenPoints, calibData.SpacePoints);
                    TrackerToScreenDistance = calibData.TrackerToScreenDistance;
                    IsReady = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load calibration file: {ex.Message}");
                return;
            }
        }

        public MappingService(MappingMethod method, SpacePoint[] spacePoints, double trackerToScreenDistance) : this(method)
        {
            TrackerToScreenDistance = trackerToScreenDistance;

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

        public bool IsHandInsideFog(SpacePoint spacePoint) => GetHandToScreenDistance(spacePoint) < 0;

        public double GetHandToScreenDistance(SpacePoint spacePoint) => spacePoint.Z - TrackerToScreenDistance;


        // Internal

        readonly Mappers.IMapper _mapper;
    }
}
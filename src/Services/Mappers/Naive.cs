using FogScreenControl.Models;
using System;

namespace FogScreenControl.Services.Mappers
{
    internal class Naive : IMapper
    {
        public Enums.MappingMethod Method => Enums.MappingMethod.Naive;

        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            if (spacePoints.Length < 4 || spacePoints.Length != screenPoints.Length)
                throw new ArgumentException("At least 4 calibration points are required.");

            var screenWidth = screenPoints[1].X - screenPoints[0].X;
            var screenHeight = screenPoints[2].Y - screenPoints[0].Y;

            _offsetX = spacePoints[0].X;
            _offsetY = spacePoints[0].Y;

            _scaleX = screenWidth / (((spacePoints[1].X - spacePoints[0].X) + (spacePoints[3].X - spacePoints[2].X)) / 2);
            _scaleY = screenHeight / (((spacePoints[2].Y - spacePoints[0].Y) + (spacePoints[3].Y - spacePoints[1].Y)) / 2);
        }

        public ScreenPoint Map(SpacePoint spacePoint) => new ScreenPoint(
            (spacePoint.X - _offsetX) * _scaleX,
            (spacePoint.Y - _offsetY) * _scaleY
        );

        // Internal

        double _scaleX = 1;
        double _scaleY = 1;
        double _offsetX = 0;
        double _offsetY = 0;
    }
}

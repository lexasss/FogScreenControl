using FogControlWithKinect.Models;

namespace FogControlWithKinect.Services.Mappers
{
    internal class NaiveMapper : IMapper
    {
        public NaiveMapper()
        {
            _screenWidth = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CXSCREEN);
            _screenHeight = Utils.WinAPI.GetSystemMetrics(Utils.WinAPI.SystemMetric.SM_CYSCREEN);
        }

        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            _screenWidth = screenPoints[1].X - screenPoints[0].X;
            _screenHeight = screenPoints[2].Y - screenPoints[0].Y;

            _offsetX = spacePoints[0].X;
            _offsetY = spacePoints[0].Y;

            _scaleX = _screenWidth / (((spacePoints[1].X - spacePoints[0].X) + (spacePoints[3].X - spacePoints[2].X)) / 2);
            _scaleY = _screenHeight / (((spacePoints[2].Y - spacePoints[0].Y) + (spacePoints[3].Y - spacePoints[1].Y)) / 2);
        }

        public ScreenPoint Map(SpacePoint spacePoint) => new ScreenPoint(
            (spacePoint.X - _offsetX) * _scaleX,
            (spacePoint.Y - _offsetY) * _scaleY
        );

        public bool IsInFog(SpacePoint spacePoint, double distanceToScreen) => 
            spacePoint.Z < distanceToScreen;

        public double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen) =>
            spacePoint.Z - distanceToScreen;

        // Internal

        double _screenWidth;
        double _screenHeight;

        double _scaleX = 1;
        double _scaleY = 1;
        double _offsetX = 0;
        double _offsetY = 0;
    }
}

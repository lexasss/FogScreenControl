using FogControlWithKinect.Models;

namespace FogControlWithKinect.Services.Mappers
{
    internal interface IMapper
    {
        void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints);
        ScreenPoint Map(SpacePoint spacePoint);
        double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen);
    }
}

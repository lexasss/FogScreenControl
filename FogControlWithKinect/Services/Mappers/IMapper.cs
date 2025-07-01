using FogControlWithKinect.Models;

namespace FogControlWithKinect.Services.Mappers
{
    internal interface IMapper
    {
        void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints);
        ScreenPoint Map(SpacePoint spacePoint);
        bool IsInFog(SpacePoint spacePoint, double distanceToScreen);
        double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen);
    }
}

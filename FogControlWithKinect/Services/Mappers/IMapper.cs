using FogScreenControl.Models;

namespace FogScreenControl.Services.Mappers
{
    internal interface IMapper
    {
        Enums.MappingMethod Method { get; }
        void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints);
        ScreenPoint Map(SpacePoint spacePoint);
        double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen);
    }
}

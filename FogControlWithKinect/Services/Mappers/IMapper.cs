namespace FogControlWithKinect.Services.Mappers
{
    internal interface IMapper
    {
        void Configure(double[,] screenPoints, double[,] trackerPoints);
        System.Windows.Point Map(double x, double y);
    }
}

using System.Windows;

namespace FogControlWithKinect
{
    public partial class App : Application
    {
        public static string CalibrationFileName { get; } = "last-calib.txt";

        public static Services.LowPassFilter PointSmoother { get; set; } = new Services.LowPassFilter(200, 33);
        public static Services.LowPassFilter DepthSmoother { get; set; } = new Services.LowPassFilter(70, 33);
    }
}

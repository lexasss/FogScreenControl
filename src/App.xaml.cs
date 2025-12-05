using System.Windows;

namespace FogScreenControl
{
    public partial class App : Application
    {
        public static string CalibrationFileName { get; } = "calibration.json";

        public static Services.LowPassFilter PointSmoother { get; set; } = new Services.LowPassFilter(200, 33);
        public static Services.LowPassFilter DepthSmoother { get; set; } = new Services.LowPassFilter(70, 33);
    }
}

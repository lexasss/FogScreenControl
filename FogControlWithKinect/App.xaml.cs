using System;
using System.Media;
using System.Windows;

namespace FogControlWithKinect
{
    public partial class App : Application
    {
        public static string CalibrationFileName { get; } = "calibration.txt";

        public static Services.LowPassFilter PointSmoother { get; set; } = new Services.LowPassFilter(200, 33);
        public static Services.LowPassFilter DepthSmoother { get; set; } = new Services.LowPassFilter(70, 33);

        public static SoundPlayer ForEnterSound { get; }

        static App()
        {
            try
            {
                ForEnterSound = new SoundPlayer("Assets/Sounds/sound.wav");
                ForEnterSound.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load sound file: {ex.Message}");
            }
        }
    }
}

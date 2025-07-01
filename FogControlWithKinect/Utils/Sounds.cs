using System;
using System.Media;

namespace FogControlWithKinect.Utils
{
    internal static class Sounds
    {
        public static SoundPlayer CalibPointDone { get; }
        public static SoundPlayer In { get; }
        public static SoundPlayer Out { get; }

        // Internal

        static Sounds()
        {
            CalibPointDone = Load("done");
            In = Load("in");
            Out = Load("out");
        }

        private static SoundPlayer Load(string name)
        {
            SoundPlayer sound;

            try
            {
                sound = new SoundPlayer($"Assets/Sounds/{name}.wav");
                sound.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to load sound file '{name}.wav': {ex.Message}");
            }
            finally
            {
                sound = new SoundPlayer();
            }

            return sound;
        }
    }
}

using Newtonsoft.Json;

namespace FogScreenControl.Models
{
    public class CalibrationData
    {
        public SpacePoint[] SpacePoints { get; set; }
        public ScreenPoint[] ScreenPoints { get; set; }
        public double TrackerToScreenDistance { get; set; }

        [JsonIgnore]
        public bool IsValid => SpacePoints != null && ScreenPoints != null && SpacePoints.Length >= 4 && ScreenPoints.Length >= 4;
    }
}

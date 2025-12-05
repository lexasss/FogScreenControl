namespace FogScreenControl.Models
{
    /// <summary>
    /// Represents screen coordinates in pixels.
    /// </summary>
    public class ScreenPoint
    {
        public static readonly ScreenPoint Zero = new ScreenPoint(0, 0);

        /// <summary>
        /// Pixels
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Pixels
        /// </summary>
        public double Y { get; set; }

        public ScreenPoint() { }

        public ScreenPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(ScreenPoint point) => X == point.X && Y == point.Y;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();

        public override string ToString() => $"({X}, {Y})";
    }
}

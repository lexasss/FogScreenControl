namespace FogControlWithKinect.Models
{
    /// <summary>
    /// Represents a 3D point in meters.
    /// </summary>
    public class SpacePoint
    {
        public static readonly SpacePoint Zero = new SpacePoint(0, 0, 0);

        /// <summary>
        /// Meters
        /// </summary>
        public double X { get; }
        /// <summary>
        /// Meters
        /// </summary>
        public double Y { get; }
        /// <summary>
        /// Meters
        /// </summary>
        public double Z { get; }

        public SpacePoint()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public SpacePoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static SpacePoint From(Microsoft.Kinect.CameraSpacePoint kinectPoint) =>
            new SpacePoint(kinectPoint.X, kinectPoint.Y, kinectPoint.Z);

        public bool Equals(SpacePoint point) => X == point.X && Y == point.Y && Z == point.Z;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}

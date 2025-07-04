﻿using System;

namespace FogScreenControl.Models
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
        public double X { get; set; }
        /// <summary>
        /// Meters
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Meters
        /// </summary>
        public double Z { get; set; }

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

        public double DistanceToXY(SpacePoint other)
        {
            double x = other.X;
            double y = other.Y;
            double dx = X - x;
            double dy = Y - y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool Equals(SpacePoint point) => X == point.X && Y == point.Y && Z == point.Z;

        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}

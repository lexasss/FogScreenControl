using FogControlWithKinect.Models;
using MathNet.Numerics.LinearAlgebra;
using System;

namespace FogControlWithKinect.Services.Mappers
{
    internal class Projective3D : IMapper
    {
        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            if (spacePoints.Length != 4 || screenPoints.Length != 4)
                throw new ArgumentException("Exactly 4 point correspondences are required.");

            int n = spacePoints.Length;

            // Build matrix A (2n x 8) and vector b (2n x 1)
            var A = Matrix<double>.Build.Dense(2 * n, 8);
            var b = Vector<double>.Build.Dense(2 * n);

            for (int i = 0; i < n; i++)
            {
                double X = spacePoints[i].X, Y = spacePoints[i].Y, Z = spacePoints[i].Z;
                double x = screenPoints[i].X, y = screenPoints[i].Y;

                // Row 2i → x equation
                A.SetRow(2 * i, new[] { X, Y, Z, 1, 0, 0, 0, 0 });
                b[2 * i] = x;

                // Row 2i+1 → y equation
                A.SetRow(2 * i + 1, new[] { 0, 0, 0, 0, X, Y, Z, 1 });
                b[2 * i + 1] = y;
            }

            // Solve A * p = b using least squares
            Vector<double> p = A.Solve(b); // 8 parameters

            // Reshape to 2x4 matrix
            _projectionMatrix = Matrix<double>.Build.DenseOfRowArrays(
                new[] { p[0], p[1], p[2], p[3] },
                new[] { p[4], p[5], p[6], p[7] }
            );
        }

        public ScreenPoint Map(SpacePoint spacePoint)
        {
            if (_projectionMatrix == null)
                throw new InvalidOperationException("Mapper is not configured. Call Configure() first.");

            var vec = Vector<double>.Build.DenseOfArray(new[] { spacePoint.X, spacePoint.Y, spacePoint.Z, 1.0 });
            var result = _projectionMatrix * vec;
            return new ScreenPoint(result[0], result[1]);
        }

        public double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen) =>
            spacePoint.Z - distanceToScreen;

        // Internal

        Matrix<double> _projectionMatrix = null;
    }
}

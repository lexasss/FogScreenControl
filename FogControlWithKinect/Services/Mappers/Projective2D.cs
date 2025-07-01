using FogControlWithKinect.Models;
using MathNet.Numerics.LinearAlgebra;
using System;

namespace FogControlWithKinect.Services.Mappers
{
    internal class Projective2D : IMapper
    {
        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            if (spacePoints.Length != 4 || screenPoints.Length != 4)
                throw new ArgumentException("Exactly 4 point pairs are required.");

            var A = Matrix<double>.Build.Dense(8, 6);
            var b = Vector<double>.Build.Dense(8);

            for (int i = 0; i < 4; i++)
            {
                double x = spacePoints[i].X, y = spacePoints[i].Y;
                double u = screenPoints[i].X, v = screenPoints[i].Y;

                A.SetRow(2 * i, new[] { x, y, 1, 0, 0, 0 });
                A.SetRow(2 * i + 1, new[] { 0, 0, 0, x, y, 1 });

                b[2 * i] = u;
                b[2 * i + 1] = v;
            }

            // Solve the least squares problem A * h = b
            Vector<double> h = A.Solve(b);

            // Reshape h into a 2x3 matrix
            _projectionMatrix = Matrix<double>.Build.DenseOfRowArrays(
                new[] { h[0], h[1], h[2] },
                new[] { h[3], h[4], h[5] }
            );
        }

        public ScreenPoint Map(SpacePoint spacePoint)
        {
            if (_projectionMatrix == null)
                throw new InvalidOperationException("Mapper is not configured. Call Configure() first.");

            double x = _projectionMatrix[0, 0] * spacePoint.X + _projectionMatrix[0, 1] * spacePoint.Y + _projectionMatrix[0, 2];
            double y = _projectionMatrix[1, 0] * spacePoint.X + _projectionMatrix[1, 1] * spacePoint.Y + _projectionMatrix[1, 2];
            return new ScreenPoint(x, y);
        }

        public double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen) =>
            spacePoint.Z - distanceToScreen;

        // Internal

        Matrix<double> _projectionMatrix = null;
    }
}

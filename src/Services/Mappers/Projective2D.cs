using FogScreenControl.Models;
using MathNet.Numerics.LinearAlgebra;
using System;

namespace FogScreenControl.Services.Mappers
{
    internal class Projective2D : IMapper
    {
        public Enums.MappingMethod Method => Enums.MappingMethod.Linear2D;

        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            if (spacePoints.Length < 4 || spacePoints.Length != screenPoints.Length)
                throw new ArgumentException("At least 4 calibration points are required.");

            int n = spacePoints.Length;

            // Build matrix A (2n x 8) and vector b (2n x 1)
            var A = Matrix<double>.Build.Dense(2 * n, 6);
            var b = Vector<double>.Build.Dense(2 * n);

            for (int i = 0; i < n; i++)
            {
                double x = spacePoints[i].X, y = spacePoints[i].Y;
                double u = screenPoints[i].X, v = screenPoints[i].Y;

                // Row 2i → x equation
                A.SetRow(2 * i, new[] { x, y, 1, 0, 0, 0 });
                b[2 * i] = u;

                // Row 2i+1 → y equation
                A.SetRow(2 * i + 1, new[] { 0, 0, 0, x, y, 1 });
                b[2 * i + 1] = v;
            }

            // Solve A * p = b using least squares
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

            var vec = Vector<double>.Build.DenseOfArray(new[] { spacePoint.X, spacePoint.Y, 1.0 });
            var result = _projectionMatrix * vec;

            return new ScreenPoint(result[0], result[1]);
        }

        // Internal

        Matrix<double> _projectionMatrix = null;
    }
}

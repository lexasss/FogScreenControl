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

            var A = Matrix<double>.Build.Dense(8, 12);

            for (int i = 0; i < 4; i++)
            {
                var x = spacePoints[i].X;
                var y = spacePoints[i].Y;
                var z = spacePoints[i].Z;
                var a = screenPoints[i].X;
                var b = screenPoints[i].Y;

                A.SetRow(2 * i, new double[] {
                    x, y, z, 1, 0, 0, 0, 0, -a * x, -a * y, -a * z, -a
                });

                A.SetRow(2 * i + 1, new double[] {
                    0, 0, 0, 0, x, y, z, 1, -b * x, -b * y, -b * z, -b
                });
            }

            // Solve A * p = 0 using SVD: solution is last column of V (right singular vectors)
            var svd = A.Svd(true);

            var p = svd.VT.Row(svd.VT.RowCount - 1); // Smallest singular vector (last column of V == last row of VT)
            _projectionMatrix = Matrix<double>.Build.Dense(3, 4);

            // Reshape p (12-vector) into 3x4 matrix
            for (int i = 0; i < 12; i++)
            {
                _projectionMatrix[i / 4, i % 4] = p[i];
            }
        }

        public ScreenPoint Map(SpacePoint spacePoint)
        {
            if (_projectionMatrix == null)
                throw new InvalidOperationException("Mapper is not configured. Call Configure() first.");

            // As of July 1 2025, it does not work
            var x = Vector<double>.Build.DenseOfArray(new double[] { spacePoint.X, spacePoint.Y, spacePoint.Z, 1 });
            var projected = _projectionMatrix * x;
            double w = projected[2];

            if (Math.Abs(w) < 1e-8)     // Projection resulted in zero homogeneous coordinate.
                return ScreenPoint.Zero;

            return new ScreenPoint(projected[0] / w, projected[1] / w);
        }

        public double GetDistanceFromScreen(SpacePoint spacePoint, double distanceToScreen) =>
            spacePoint.Z - distanceToScreen;

        // Internal

        Matrix<double> _projectionMatrix = null;
    }
}

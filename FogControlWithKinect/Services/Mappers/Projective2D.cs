using FogControlWithKinect.Models;
using System;

namespace FogControlWithKinect.Services.Mappers
{
    internal class Projective2D : IMapper
    {
        public void Configure(ScreenPoint[] screenPoints, SpacePoint[] spacePoints)
        {
            _projectionMatrix = ComputeProjectionMatrix(spacePoints, screenPoints);
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

        double[,] _projectionMatrix = null;

        // Compute the affine transformation matrix [2x3]
        private static double[,] ComputeProjectionMatrix(SpacePoint[] src, ScreenPoint[] dst)
        {
            if (src.Length != 4 || dst.Length != 4)
                throw new ArgumentException("Exactly 4 point pairs are required.");

            // Build matrix A (8x6) and vector b (8x1)
            double[,] A = new double[8, 6];
            double[] b = new double[8];

            for (int i = 0; i < 4; i++)
            {
                int row = 2 * i;
                A[row, 0] = src[i].X;
                A[row, 1] = src[i].Y;
                A[row, 2] = 1;
                A[row, 3] = 0;
                A[row, 4] = 0;
                A[row, 5] = 0;

                A[row + 1, 0] = 0;
                A[row + 1, 1] = 0;
                A[row + 1, 2] = 0;
                A[row + 1, 3] = src[i].X;
                A[row + 1, 4] = src[i].Y;
                A[row + 1, 5] = 1;

                b[row] = dst[i].X;
                b[row + 1] = dst[i].Y;
            }

            // Solve using least squares: h = (A^T * A)^(-1) * A^T * b
            double[,] At = Transpose(A);
            double[,] AtA = Multiply(At, A);
            double[] Atb = Multiply(At, b);
            double[] h = SolveLinearSystem(AtA, Atb); // 6 affine parameters

            // Reshape result into 2x3 affine matrix
            return new double[2, 3] {
                { h[0], h[1], h[2] },
                { h[3], h[4], h[5] }
            };
        }

        // Matrix transpose
        private static double[,] Transpose(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] result = new double[cols, rows];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    result[j, i] = matrix[i, j];

            return result;
        }

        // Matrix multiply (m x n) * (n x p)
        private static double[,] Multiply(double[,] A, double[,] B)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            int p = B.GetLength(1);

            double[,] result = new double[m, p];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < p; j++)
                    for (int k = 0; k < n; k++)
                        result[i, j] += A[i, k] * B[k, j];

            return result;
        }

        // Multiply matrix A (m x n) with vector b (n)
        private static double[] Multiply(double[,] A, double[] b)
        {
            int m = A.GetLength(0);
            int n = A.GetLength(1);
            double[] result = new double[m];

            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    result[i] += A[i, j] * b[j];

            return result;
        }

        // Solve linear system Ax = b using Gaussian elimination
        private static double[] SolveLinearSystem(double[,] A, double[] b)
        {
            int n = b.Length;
            double[,] mat = new double[n, n + 1];

            // Build augmented matrix
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                    mat[i, j] = A[i, j];
                mat[i, n] = b[i];
            }

            // Gaussian elimination
            for (int i = 0; i < n; i++)
            {
                // Pivot
                int maxRow = i;
                for (int k = i + 1; k < n; k++)
                    if (Math.Abs(mat[k, i]) > Math.Abs(mat[maxRow, i]))
                        maxRow = k;

                // Swap rows
                for (int k = i; k <= n; k++)
                {
                    double tmp = mat[maxRow, k];
                    mat[maxRow, k] = mat[i, k];
                    mat[i, k] = tmp;
                }

                // Eliminate
                for (int k = i + 1; k < n; k++)
                {
                    double factor = mat[k, i] / mat[i, i];
                    for (int j = i; j <= n; j++)
                        mat[k, j] -= factor * mat[i, j];
                }
            }

            // Back substitution
            double[] x = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = mat[i, n] / mat[i, i];
                for (int k = 0; k < i; k++)
                    mat[k, n] -= mat[k, i] * x[i];
            }

            return x;
        }
    }
}

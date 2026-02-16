using MathNet.Numerics.LinearAlgebra;

namespace GS.Server.Alignment
{

    public readonly struct LocalAffine2D
    {
        public readonly double A11, A12, A21, A22;
        public readonly double B1, B2;

        public LocalAffine2D(double a11, double a12, double a21, double a22, double b1, double b2)
        {
            A11 = a11; A12 = a12;
            A21 = a21; A22 = a22;
            B1 = b1; B2 = b2;
        }

        public AxisPositionRad Apply(AxisPositionRad ideal)
        {
            double x = ideal.A1;
            double y = ideal.A2;

            double rx = A11 * x + A12 * y + B1;
            double ry = A21 * x + A22 * y + B2;

            return new AxisPositionRad(rx, ry);
        }

        public static LocalAffine2D FromThreePoints(
            AxisPositionRad i1, AxisPositionRad r1,
            AxisPositionRad i2, AxisPositionRad r2,
            AxisPositionRad i3, AxisPositionRad r3)
        {
            var M = Matrix<double>.Build.DenseOfArray(new double[,]
            {
            { i1.A1, i1.A2, 1.0 },
            { i2.A1, i2.A2, 1.0 },
            { i3.A1, i3.A2, 1.0 }
            });

            var v1 = Vector<double>.Build.Dense(new double[] { r1.A1, r2.A1, r3.A1 });
            var v2 = Vector<double>.Build.Dense(new double[] { r1.A2, r2.A2, r3.A2 });

            var s1 = M.Solve(v1);   // [a11, a12, b1]
            var s2 = M.Solve(v2);   // [a21, a22, b2]

            return new LocalAffine2D(
                s1[0], s1[1],
                s2[0], s2[1],
                s1[2], s2[2]);
        }
    }
}

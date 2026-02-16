namespace GS.Server.Alignment
{
    public static class LinearAlgebra
    {
        // Solve X p = r in least squares sense using QR decomposition
        public static double[] SolveLeastSquares(double[][] X, double[] r)
        {
            var m = X.Length;
            var n = X[0].Length;

            // Build Math.NET matrices
            var M = MathNet.Numerics.LinearAlgebra.Double.Matrix.Build;
            var V = MathNet.Numerics.LinearAlgebra.Double.Vector.Build;

            var A = M.DenseOfRowArrays(X);
            var b = V.DenseOfArray(r);

            var qr = A.QR();
            return qr.Solve(b).ToArray();
        }
    }

}

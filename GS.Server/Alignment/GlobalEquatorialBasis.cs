using System;

namespace GS.Server.Alignment
{
    public static class GlobalEquatorialBasis
    {
        public static double[] BasisHa(AxisPositionRad ideal)
        {
            // Typical ranges: HA ~ ±π, Dec ~ ±π/2
            double ha = ideal.A1 / Math.PI;        // ~[-1, 1]
            double dec = ideal.A2 / (Math.PI / 2);  // ~[-1, 1]

            return new double[]
            {
            1.0,
            ha,
            dec,
            Math.Sin(ideal.A1),
            Math.Cos(ideal.A1),
            Math.Sin(ideal.A2),
            Math.Cos(ideal.A2),
            ha * dec,
            ha * ha,
            dec * dec
            };
        }

        public static double[] BasisDec(AxisPositionRad ideal)
        {
            double ha = ideal.A1 / Math.PI;
            double dec = ideal.A2 / (Math.PI / 2);

            return new double[]
            {
            1.0,
            ha,
            dec,
            Math.Sin(ideal.A1),
            Math.Cos(ideal.A1),
            Math.Sin(ideal.A2),
            Math.Cos(ideal.A2),
            ha * dec,
            ha * ha,
            dec * dec
            };
        }
    }
}
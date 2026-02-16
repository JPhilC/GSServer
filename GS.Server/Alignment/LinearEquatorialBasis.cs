using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    public static class LinearEquatorialBasis
    {
        // Basis for ΔH (hour angle correction)
        public static double[] BasisHa(AxisPositionRad m)
        {
            double H = m.A1;
            double d = m.A2;

            return new[]
            {
            1.0,            // constant offset
            Math.Tan(d),    // polar misalignment (alt)
            Math.Sin(H),    // polar misalignment (az)
            Math.Cos(H)     // cone error
        };
        }

        // Basis for ΔDec (declination correction)
        public static double[] BasisDec(AxisPositionRad m)
        {
            double H = m.A1;
            double d = m.A2;

            return new[]
            {
            1.0,        // constant offset
            Math.Cos(H),
            Math.Sin(H)
        };
        }
    }

}

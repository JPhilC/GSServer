using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    public static class AltAzBasis
    {
        // Basis for ΔAz
        public static double[] BasisAz(AxisPositionRad m)
        {
            double az = m.A1;
            double alt = m.A2;

            return new[]
            {
            1.0,            // constant offset
            Math.Tan(alt),  // altitude-dependent error
            Math.Sin(az),   // azimuth-dependent error
            Math.Cos(az)    // azimuth-dependent error
        };
        }

        // Basis for ΔAlt
        public static double[] BasisAlt(AxisPositionRad m)
        {
            double az = m.A1;
            double alt = m.A2;

            return new[]
            {
            1.0,        // constant offset
            Math.Cos(az),
            Math.Sin(az)
        };
        }
    }
}

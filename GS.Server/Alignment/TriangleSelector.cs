using System;
using System.Collections.Generic;

namespace GS.Server.Alignment
{
    public static class TriangleSelector
    {
        public static (bool IsValid,
                       AxisPositionRad I1, AxisPositionRad R1,
                       AxisPositionRad I2, AxisPositionRad R2,
                       AxisPositionRad I3, AxisPositionRad R3)
            SelectThree(IReadOnlyList<AlignmentPoint> points, AxisPositionRad target)
        {
            if (points.Count < 3)
                return (false, default, default, default, default, default, default);

            double bestDist = double.MaxValue;
            AlignmentPoint best1 = null, best2 = null, best3 = null;

            // Try all combinations of 3 points
            for (int a = 0; a < points.Count; a++)
                for (int b = a + 1; b < points.Count; b++)
                    for (int c = b + 1; c < points.Count; c++)
                    {
                        var p1 = points[a];
                        var p2 = points[b];
                        var p3 = points[c];

                        // Reject collinear triples or skinny triangles that are unlikely to be stable. The area is in radians^2, so the thresholds are very small.
                        double area = TriangleArea(p1.IdealRad, p2.IdealRad, p3.IdealRad);
                        if (area < 1e-12) continue;      // collinear
                        if (area < 1e-6) continue;       // too skinny to be stable


                        // Compute centroid
                        var i1 = p1.IdealRad;
                        var i2 = p2.IdealRad;
                        var i3 = p3.IdealRad;

                        var centroid = new AxisPositionRad(
                            (i1.A1 + i2.A1 + i3.A1) / 3.0,
                            (i1.A2 + i2.A2 + i3.A2) / 3.0
                        );


                        double dist = centroid.DistanceTo(target);

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best1 = p1;
                            best2 = p2;
                            best3 = p3;
                        }
                    }

            if (best1 == null)
                return (false, default, default, default, default, default, default);

            return (true,
                best1.IdealRad, best1.RawRad,
                best2.IdealRad, best2.RawRad,
                best3.IdealRad, best3.RawRad);
        }

        private static double TriangleArea(AxisPositionRad a, AxisPositionRad b, AxisPositionRad c)
        {
            return Math.Abs(
                (a.A1 * (b.A2 - c.A2) +
                 b.A1 * (c.A2 - a.A2) +
                 c.A1 * (a.A2 - b.A2)) * 0.5);
        }
    }
}

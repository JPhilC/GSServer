using GS.Principles;
using GS.Server.Alignment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlignmentModelTests
{
    public static class TestAlignmentPoint
    {
        public static AlignmentPoint Create(double idealA1, double idealA2,
                                            double rawA1, double rawA2, double hourAngle)
        {
            return new AlignmentPoint
            {
                Ideal = new AxisPosition(idealA1, idealA2),
                Unsynced = new AxisPosition(rawA1, rawA2),
                HourAngle = hourAngle,
                AlignTime = DateTime.Now
            };
        }

        public static AlignmentPoint Create(double idealA1, double idealA2,
                                    double rawA1, double rawA2)
        {
            return Create(idealA1, idealA2, rawA1, rawA2, hourAngle: 0.0);
        }

        public static AlignmentPoint CreateFromRaDec(
                                                double ra, double dec,
                                                double rawA1, double rawA2,
                                                double lst)
        {
            double ha = Coordinate.Ra2Ha24(ra, lst);
            return Create(ha, dec, rawA1, rawA2, ha);
        }


    }
}

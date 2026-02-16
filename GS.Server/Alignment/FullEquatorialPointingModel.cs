using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    /// <summary>
    /// Full equatorial pointing model based on a linearised mount geometry
    /// (Taki/EQMOD style). Models index errors, polar misalignment, cone error,
    /// and non‑orthogonality using a least‑squares fit in HA/Dec space.
    /// </summary>
    public sealed class FullEquatorialPointingModel : IPointingModel
    {
        // Parameter vectors for RA and Dec corrections
        private double[] _pRa;
        private double[] _pDec;

        public bool IsFitted { get; private set; }

        private const int RequiredPointCount = 8;

        public void Fit(IEnumerable<AlignmentPoint> points)
        {
            var pts = points.ToList();

            // Not enough points → model is not fitted
            if (pts.Count < RequiredPointCount)
            {
                IsFitted = false;
                return;
            }

            var X = new List<double[]>();
            var rRa = new List<double>();
            var rDec = new List<double>();

            foreach (var p in pts)
            {
                double ha = p.HourAngle;   // HA stored in the alignment point
                double dec = p.Ideal.A2;

                double[] row = BuildBasisRow(ha, dec);
                X.Add(row);

                rRa.Add(p.Unsynced.A1 - p.Ideal.A1);
                rDec.Add(p.Unsynced.A2 - p.Ideal.A2);
            }

            try
            {
                (_pRa, _pDec) = SolveParameters(X, rRa, rDec);
                IsFitted = true;
            }
            catch
            {
                // If the solver fails (singular matrix, etc.)
                IsFitted = false;
                throw;
            }
        }

        public AxisPosition Apply(AxisPosition ideal, double hourAngle)
        {
            if (!IsFitted)
                return ideal;

            double ha = hourAngle;
            double dec = ideal.A2;

            ComputeCorrections(ha, dec, out double dRa, out double dDec);

            return new AxisPosition(ideal.A1 + dRa, ideal.A2 + dDec);
        }

        public AxisPosition ApplyReverse(AxisPosition corrected, double hourAngle)
        {
            if (!IsFitted)
                return corrected;

            double ha = hourAngle;
            double dec = corrected.A2;

            ComputeCorrections(ha, dec, out double dRa, out double dDec);

            return new AxisPosition(corrected.A1 - dRa, corrected.A2 - dDec);
        }

        private static double[] BuildBasisRow(double ha, double dec)
        {
            return new[]
            {
            1.0,                     // IH / ID
            Math.Sin(ha),            // MA / CH
            Math.Cos(ha),            // ME / CV
            Math.Tan(dec),           // polar / cone coupling
            Math.Sin(dec),           // flexure-like
            Math.Cos(dec),           // flexure-like
            Math.Sin(ha) * Math.Tan(dec) // NP-like term
        };
        }

        private static (double[] pRa, double[] pDec) SolveParameters(
            List<double[]> X, List<double> rRa, List<double> rDec)
        {
            var Xarr = X.Select(r => r.ToArray()).ToArray();

            var pRa = LinearAlgebra.SolveLeastSquares(Xarr, rRa.ToArray());
            var pDec = LinearAlgebra.SolveLeastSquares(Xarr, rDec.ToArray());

            return (pRa, pDec);
        }

        private void ComputeCorrections(double ha, double dec, out double dRa, out double dDec)
        {
            var basis = BuildBasisRow(ha, dec);

            dRa = 0;
            dDec = 0;

            for (int i = 0; i < basis.Length; i++)
            {
                dRa += _pRa[i] * basis[i];
                dDec += _pDec[i] * basis[i];
            }
        }
    }
}

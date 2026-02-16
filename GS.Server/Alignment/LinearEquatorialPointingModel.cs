using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    /// <summary>
    /// A lightweight, linear equatorial pointing model that estimates and corrects
    /// systematic mount alignment errors using a least‑squares fit.
    ///
    /// This model operates entirely in equatorial axis space (HA/RA and Dec) and
    /// assumes that the mount’s mechanical and geometric imperfections can be
    /// approximated by a linear combination of simple basis functions. Each
    /// alignment point provides a pair of values:
    ///
    ///   • Ideal  – the mount axis angles corresponding to the true sky position  
    ///   • Unsynced – the raw physical axis angles reported by the mount
    ///
    /// The model computes the residuals between these two sets of angles and fits
    /// a linear system that best explains the observed discrepancies. Once fitted,
    /// the model can:
    ///
    ///   • Apply()      – convert ideal axis angles into corrected physical angles  
    ///   • ApplyReverse() – recover ideal angles from corrected physical angles
    ///
    /// The model is intentionally minimal: it does not attempt to represent full
    /// mount geometry, pier‑side behaviour, or higher‑order distortions. Instead,
    /// it provides a stable, fast, and predictable correction suitable for
    /// real‑time slewing and reporting within GS Server. A minimum of four
    /// alignment points is required to solve the linear system.
    /// </summary>
    public sealed class LinearEquatorialPointingModel : IPointingModel
    {
        private double[] _pH;     // parameters for ΔH
        private double[] _pDec;   // parameters for ΔDec

        public bool IsFitted => _pH != null && _pDec != null;

        public void Fit(IEnumerable<AlignmentPoint> points)
        {
            var list = points.ToList();
            int N = list.Count;

            if (N < 3)
                throw new InvalidOperationException("Not enough alignment points to fit model.");

            // Build design matrices
            var XH = new double[N][];
            var XDec = new double[N][];
            var rH = new double[N];
            var rDec = new double[N];

            for (int i = 0; i < N; i++)
            {
                var p = list[i];

                var ideal = p.IdealRad;
                var raw = p.RawRad;

                // Residuals
                rH[i] = raw.A1 - ideal.A1;
                rDec[i] = raw.A2 - ideal.A2;

                // Basis rows
                XH[i] = EquatorialBasis.BasisH(ideal);
                XDec[i] = EquatorialBasis.BasisDec(ideal);
            }

            // Solve least squares
            _pH = LinearAlgebra.SolveLeastSquares(XH, rH);
            _pDec = LinearAlgebra.SolveLeastSquares(XDec, rDec);
        }

        public AxisPosition Apply(AxisPosition idealDeg, double hourAngle)
        {
            if (!IsFitted)
                return idealDeg;

            var ideal = idealDeg.ToRad();

            double dH = EvaluateDeltaH(ideal);
            double dDec = EvaluateDeltaDec(ideal);

            var corrected = new AxisPositionRad(
                ideal.A1 + dH,
                ideal.A2 + dDec
            );

            return corrected.FromRad();
        }

        public AxisPosition ApplyReverse(AxisPosition correctedDeg, double hourAngle)
        {
            if (!IsFitted)
                return correctedDeg;

            // Convert to radians
            var corrected = correctedDeg.ToRad();

            // Evaluate the model at the corrected position
            double dH = EvaluateDeltaH(corrected);
            double dDec = EvaluateDeltaDec(corrected);

            // Reverse the correction
            var ideal = new AxisPositionRad(
                corrected.A1 - dH,
                corrected.A2 - dDec
            );

            return ideal.FromRad();
        }

        private double EvaluateDeltaH(AxisPositionRad m)
        {
            var b = EquatorialBasis.BasisH(m);
            double sum = 0;
            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pH[i];
            return sum;
        }

        private double EvaluateDeltaDec(AxisPositionRad m)
        {
            var b = EquatorialBasis.BasisDec(m);
            double sum = 0;
            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pDec[i];
            return sum;
        }
    }
}

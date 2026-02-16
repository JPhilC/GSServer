using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS.Server.Alignment
{
    /// <summary>
    /// A lightweight, linear Alt‑Az pointing model that estimates and corrects
    /// systematic mount alignment errors using a least‑squares fit.
    ///
    /// This model operates entirely in altitude–azimuth axis space and assumes
    /// that the mount’s mechanical and geometric imperfections can be represented
    /// as a linear combination of simple basis functions. Each alignment point
    /// provides:
    ///
    ///   • Ideal     – the true sky‑aligned Alt/Az axis angles  
    ///   • Unsynced  – the raw physical Alt/Az angles reported by the mount
    ///
    /// The model computes the residuals between these two sets of angles and fits
    /// a linear system that best explains the observed discrepancies. Once fitted,
    /// the model can:
    ///
    ///   • Apply()        – convert ideal Alt/Az angles into corrected physical angles  
    ///   • ApplyReverse() – recover ideal angles from corrected physical angles
    ///
    /// This model is intentionally minimal and does not attempt to model full
    /// Alt‑Az mount geometry, atmospheric refraction, or higher‑order distortions.
    /// Instead, it provides a stable, fast, and predictable correction suitable for
    /// real‑time slewing and position reporting within GS Server. A minimum of four
    /// alignment points is required to solve the linear system.
    /// </summary>
    public sealed class LinearAltAzPointingModel : IPointingModel
    {
        // Parameters for ΔAz and ΔAlt
        private double[] _pAz;
        private double[] _pAlt;

        public bool IsFitted
        {
            get { return _pAz != null && _pAlt != null; }
        }

        public void Fit(IEnumerable<AlignmentPoint> points)
        {
            var list = points.ToList();
            int N = list.Count;

            if (N < 3)
                return; // Not enough points to fit

            // Build design matrices
            var XAz = new double[N][];
            var XAlt = new double[N][];
            var rAz = new double[N];
            var rAlt = new double[N];

            for (int i = 0; i < N; i++)
            {
                var p = list[i];

                AxisPositionRad ideal = p.IdealRad;
                AxisPositionRad raw = p.RawRad;

                // Residuals (raw - ideal)
                rAz[i] = raw.A1 - ideal.A1;
                rAlt[i] = raw.A2 - ideal.A2;

                // Basis functions
                XAz[i] = AltAzBasis.BasisAz(ideal);
                XAlt[i] = AltAzBasis.BasisAlt(ideal);
            }

            // Solve least squares
            _pAz = LinearAlgebra.SolveLeastSquares(XAz, rAz);
            _pAlt = LinearAlgebra.SolveLeastSquares(XAlt, rAlt);
        }

        public AxisPosition Apply(AxisPosition idealDeg, double hourAngle)
        {
            if (!IsFitted)
                return idealDeg;

            AxisPositionRad ideal = idealDeg.ToRad();

            double dAz = EvaluateDeltaAz(ideal);
            double dAlt = EvaluateDeltaAlt(ideal);

            AxisPositionRad corrected = new AxisPositionRad(
                ideal.A1 + dAz,
                ideal.A2 + dAlt
            );

            return corrected.FromRad();
        }

        public AxisPosition ApplyReverse(AxisPosition correctedDeg, double hourAngle)
        {
            if (!IsFitted)
                return correctedDeg;

            // Convert to radians
            AxisPositionRad corrected = correctedDeg.ToRad();

            // Evaluate the model at the corrected position
            double dAz = EvaluateDeltaAz(corrected);
            double dAlt = EvaluateDeltaAlt(corrected);

            // Reverse the correction
            AxisPositionRad ideal = new AxisPositionRad(
                corrected.A1 - dAz,
                corrected.A2 - dAlt
            );

            return ideal.FromRad();
        }


        private double EvaluateDeltaAz(AxisPositionRad m)
        {
            double[] b = AltAzBasis.BasisAz(m);
            double sum = 0;

            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pAz[i];

            return sum;
        }

        private double EvaluateDeltaAlt(AxisPositionRad m)
        {
            double[] b = AltAzBasis.BasisAlt(m);
            double sum = 0;

            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pAlt[i];

            return sum;
        }
    }
}

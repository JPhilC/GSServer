using System;
using System.Collections.Generic;
using System.Linq;

namespace GS.Server.Alignment
{
    public sealed class GlobalEquatorialPointingModel : IPointingModel
    {
        private double[] _pHa;   // parameters for ΔHA
        private double[] _pDec;  // parameters for ΔDec

        public bool IsFitted
        {
            get { return _pHa != null && _pDec != null; }
        }

        public void Fit(IReadOnlyList<AlignmentPoint> points)
        {
            var list = points.ToList();
            int N = list.Count;

            // Need at least as many points as parameters (10) + some margin
            if (N < 10)
                return;

            var XHa = new double[N][];
            var XDec = new double[N][];
            var rHa = new double[N];
            var rDec = new double[N];

            for (int i = 0; i < N; i++)
            {
                var p = list[i];

                // Ideal in HA/Dec radians
                AxisPositionRad ideal = p.IdealRad;   // A1 = HA, A2 = Dec
                AxisPositionRad raw = p.RawRad;     // A1 = raw HA axis, A2 = raw Dec axis

                // Residuals: raw - ideal
                rHa[i] = raw.A1 - ideal.A1;
                rDec[i] = raw.A2 - ideal.A2;

                // Basis functions
                XHa[i] = GlobalEquatorialBasis.BasisHa(ideal);
                XDec[i] = GlobalEquatorialBasis.BasisDec(ideal);
            }

            _pHa = LinearAlgebra.SolveLeastSquares(XHa, rHa);
            _pDec = LinearAlgebra.SolveLeastSquares(XDec, rDec);
        }

        public AxisPosition Apply(AxisPosition idealDeg, double hourAngle)
        {
            if (!IsFitted)
                return idealDeg;

            // idealDeg is in degrees; convert to radians
            AxisPositionRad ideal = idealDeg.ToRad();

            double dHa = EvaluateDeltaHa(ideal);
            double dDec = EvaluateDeltaDec(ideal);

            AxisPositionRad corrected = new AxisPositionRad(
                ideal.A1 + dHa,
                ideal.A2 + dDec
            );

            return corrected.ToDeg();
        }

        public AxisPosition ApplyReverse(AxisPosition correctedDeg, double hourAngle)
        {
            if (!IsFitted)
                return correctedDeg;

            AxisPositionRad corrected = correctedDeg.ToRad();

            double dHa = EvaluateDeltaHa(corrected);
            double dDec = EvaluateDeltaDec(corrected);

            AxisPositionRad ideal = new AxisPositionRad(
                corrected.A1 - dHa,
                corrected.A2 - dDec
            );

            return ideal.ToDeg();
        }

        public void Reset()
        {
            _pHa = null;
            _pDec = null;
        }

        private double EvaluateDeltaHa(AxisPositionRad m)
        {
            double[] b = GlobalEquatorialBasis.BasisHa(m);
            double sum = 0.0;

            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pHa[i];

            return sum;
        }

        private double EvaluateDeltaDec(AxisPositionRad m)
        {
            double[] b = GlobalEquatorialBasis.BasisDec(m);
            double sum = 0.0;

            for (int i = 0; i < b.Length; i++)
                sum += b[i] * _pDec[i];

            return sum;
        }
    }
}
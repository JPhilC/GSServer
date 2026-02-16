using System.Collections.Generic;

namespace GS.Server.Alignment
{
    public sealed class NearestPointDeltaModel : IPointingModel
    {
        private readonly AlignmentPointCollection _points;

        public NearestPointDeltaModel()
        {
            _points = new AlignmentPointCollection();
        }

        public bool IsFitted => _points.Count >= 1;

        public void Fit(IReadOnlyList<AlignmentPoint> points)
        {
            _points.Clear();
            foreach (var p in points)
                _points.Add(p);
        }

        public void Reset()
        {
            _points.Clear();
        }

        public AxisPosition Apply(AxisPosition idealDeg, double hourAngle)
        {
            if (_points.Count == 0)
                return idealDeg;

            var ideal = idealDeg.ToRad();

            // Find nearest alignment point
            AlignmentPoint best = null;
            double bestDist = double.MaxValue;

            foreach (var p in _points)
            {
                double d = p.IdealRad.DistanceTo(ideal);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            // Compute simple offset
            var offset = new AxisPositionRad(
                best.RawRad.A1 - best.IdealRad.A1,
                best.RawRad.A2 - best.IdealRad.A2
            );

            var corrected = new AxisPositionRad(
                ideal.A1 + offset.A1,
                ideal.A2 + offset.A2
            );

            return corrected.ToDeg();
        }

        public AxisPosition ApplyReverse(AxisPosition correctedDeg, double hourAngle)
        {
            // Reverse offset using nearest point
            if (_points.Count == 0)
                return correctedDeg;

            var corrected = correctedDeg.ToRad();

            AlignmentPoint best = null;
            double bestDist = double.MaxValue;

            foreach (var p in _points)
            {
                double d = p.RawRad.DistanceTo(corrected);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }

            var offset = new AxisPositionRad(
                best.RawRad.A1 - best.IdealRad.A1,
                best.RawRad.A2 - best.IdealRad.A2
            );

            var ideal = new AxisPositionRad(
                corrected.A1 - offset.A1,
                corrected.A2 - offset.A2
            );

            return ideal.ToDeg();
        }
    }
}

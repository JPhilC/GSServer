using System;
using System.Collections.Generic;
using System.Linq;

namespace GS.Server.Alignment
{
    public enum LocalPointFilterMode
    {
        None,
        SamePierSide,
        SameQuadrant,
        Custom
    }

    /// <summary>
    /// A hybrid pointing model that applies a *local affine correction* when enough
    /// nearby alignment points are available, and falls back to a simple nearest‑point
    /// delta model when they are not.
    ///
    /// The model works in three stages:
    ///
    /// 1. **Filtering**  
    ///    Before selecting a triangle, the model filters the available alignment
    ///    points according to <see cref="FilterMode"/>.  
    ///    This allows the caller to restrict the search to points on the same pier
    ///    side, the same HA/Dec quadrant, or any user‑defined region.  
    ///    Filtering ensures that only geometrically relevant points are considered
    ///    when building the local affine transform.
    ///
    /// 2. **Triangle Selection**  
    ///    From the filtered set, the model selects the triangle whose centroid is
    ///    closest to the target position.  
    ///    Degenerate or numerically unstable triangles (collinear or extremely
    ///    skinny) are rejected.  
    ///    If a valid triangle is found, a 2D affine transform is constructed that
    ///    maps ideal coordinates to raw coordinates in that local region.
    ///
    /// 3. **Fallback Behaviour**  
    ///    If fewer than three filtered points are available, or if no valid triangle
    ///    can be formed, the model delegates to the fallback pointing model.  
    ///    By default this is a <see cref="NearestPointDeltaModel"/>, which applies a
    ///    simple offset based on the nearest alignment point and always works with
    ///    as few as one point.
    ///
    /// This design provides high accuracy in regions with good alignment coverage,
    /// while remaining robust and predictable across the entire sky.  The model
    /// owns its internal alignment‑point collection and must be populated via
    /// <see cref="Fit"/> before use.
    /// </summary>
    public sealed class LocalEquatorialPointingModel : IPointingModel
    {
        private readonly AlignmentPointCollection _points;
        private readonly IPointingModel _fallback;

        public LocalPointFilterMode FilterMode { get; set; } = LocalPointFilterMode.None;

        // Optional user-supplied filter
        public Func<AlignmentPoint, AxisPositionRad, bool> CustomFilter { get; set; }

        public LocalEquatorialPointingModel(IPointingModel fallback)
        {
            _points = new AlignmentPointCollection();
            _fallback = fallback;
        }

        public bool IsFitted
        {
            get
            {
                // Local model ready when >= 3 points
                // Fallback ready when >= 1 point
                return _points.Count >= 3 || _fallback.IsFitted;
            }
        }

        public void Fit(IReadOnlyList<AlignmentPoint> points)
        {
            _points.Clear();
            foreach (var p in points)
                _points.Add(p);

            _fallback.Fit(points);
        }

        public AxisPosition Apply(AxisPosition ideal, double haRad)
        {
            var idealRad = ideal.ToRad();

            // Apply filtering
            var candidates = FilterPoints(idealRad);

            if (candidates.Count < 3)
                return _fallback.Apply(ideal, haRad);

            var tri = TriangleSelector.SelectThree(candidates, idealRad);

            if (!tri.IsValid)
                return _fallback.Apply(ideal, haRad);

            var local = LocalAffine2D.FromThreePoints(
                tri.I1, tri.R1,
                tri.I2, tri.R2,
                tri.I3, tri.R3);

            var correctedRad = local.Apply(idealRad);

            return correctedRad.ToDeg();
        }

        public AxisPosition ApplyReverse(AxisPosition actual, double haRad)
        {
            // Reverse mapping is delegated to fallback for now
            return _fallback.ApplyReverse(actual, haRad);
        }

        public void Reset()
        {
            _points.Clear();
            _fallback.Reset();
        }

        // -------------------------------
        // Filtering logic
        // -------------------------------
        private List<AlignmentPoint> FilterPoints(AxisPositionRad target)
        {
            switch (FilterMode)
            {
                case LocalPointFilterMode.None:
                    return _points.ToList();

                case LocalPointFilterMode.SamePierSide:
                    return _points
                        .Where(p => Math.Sign(p.HourAngle) == Math.Sign(target.A1))
                        .ToList();

                case LocalPointFilterMode.SameQuadrant:
                    return _points
                        .Where(p =>
                            Math.Sign(p.IdealRad.A1) == Math.Sign(target.A1) &&
                            Math.Sign(p.IdealRad.A2) == Math.Sign(target.A2))
                        .ToList();

                case LocalPointFilterMode.Custom:
                    if (CustomFilter == null)
                        return _points.ToList();
                    return _points
                        .Where(p => CustomFilter(p, target))
                        .ToList();

                default:
                    return _points.ToList();
            }
        }
    }
}

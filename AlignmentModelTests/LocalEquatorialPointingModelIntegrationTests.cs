using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlignmentModelTests
{
    [TestClass]
    public class LocalEquatorialPointingModelIntegrationTests
    {
        [TestMethod]
        public void LocalModel_UsesTriangleAndAffineTransformCorrectly()
        {
            var now = DateTime.UtcNow;

            var i1 = new AxisPosition(10, 20);
            var i2 = new AxisPosition(30, 5);
            var i3 = new AxisPosition(-15, 40);
            var i4 = new AxisPosition(20, 10);

            var r1 = new AxisPosition(10.2, 19.8);
            var r2 = new AxisPosition(30.3, 4.7);
            var r3 = new AxisPosition(-14.7, 39.6);
            var r4 = new AxisPosition(20.1, 9.8);

            var p1 = new AlignmentPoint(i1, r1, 0.0, now);
            var p2 = new AlignmentPoint(i2, r2, 0.0, now);
            var p3 = new AlignmentPoint(i3, r3, 0.0, now);
            var p4 = new AlignmentPoint(i4, r4, 0.0, now);

            var points = new AlignmentPointCollection { p1, p2, p3, p4 };

            var fallback = new NearestPointDeltaModel();
            var model = new LocalEquatorialPointingModel(fallback);
            model.Fit(points);

            var idealTarget = new AxisPosition(5, 25);
            var corrected = model.Apply(idealTarget, 0.0);

            // Use the same triangle selection logic as the model
            var tri = TriangleSelector.SelectThree(points, idealTarget.ToRad());
            Assert.IsTrue(tri.IsValid, "TriangleSelector returned invalid triangle");

            var affine = LocalAffine2D.FromThreePoints(
                tri.I1, tri.R1,
                tri.I2, tri.R2,
                tri.I3, tri.R3);

            var expected = affine.Apply(idealTarget.ToRad()).ToDeg();

            Assert.AreEqual(expected.A1, corrected.A1, 1e-9);
            Assert.AreEqual(expected.A2, corrected.A2, 1e-9);
        }

        [TestMethod]
        public void Filter_SamePierSide_RemovesOppositeSidePoints()
        {
            var now = DateTime.UtcNow;

            // East side (HA > 0)
            var pE1 = new AlignmentPoint(new AxisPosition(10, 20), new AxisPosition(10.1, 20.1), 1.0, now);
            var pE2 = new AlignmentPoint(new AxisPosition(12, 22), new AxisPosition(12.1, 22.1), 1.0, now);
            var pE3 = new AlignmentPoint(new AxisPosition(14, 24), new AxisPosition(14.1, 24.1), 1.0, now);

            // West side (HA < 0)
            var pW1 = new AlignmentPoint(new AxisPosition(-10, 20), new AxisPosition(-9.9, 20.1), -1.0, now);

            var points = new List<AlignmentPoint> { pE1, pE2, pE3, pW1 };

            var fallback = new NearestPointDeltaModel();
            var model = new LocalEquatorialPointingModel(fallback)
            {
                FilterMode = LocalPointFilterMode.SamePierSide
            };

            model.Fit(points);

            var target = new AxisPosition(11, 21); // HA > 0 → east side
            var idealRad = target.ToRad();

            var filtered = model
                .GetType()
                .GetMethod("FilterPoints", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(model, new object[] { idealRad }) as List<AlignmentPoint>;

            Assert.AreEqual(3, filtered.Count);
            Assert.IsFalse(filtered.Contains(pW1));
        }

        [TestMethod]
        public void Filter_SamePierSide_TriggersFallbackWhenTooFewPoints()
        {
            var now = DateTime.UtcNow;

            // Only two east-side points
            var p1 = new AlignmentPoint(new AxisPosition(10, 20), new AxisPosition(10.2, 19.8), 1.0, now);
            var p2 = new AlignmentPoint(new AxisPosition(12, 22), new AxisPosition(12.1, 21.9), 1.0, now);

            // West-side point (filtered out)
            var p3 = new AlignmentPoint(new AxisPosition(-10, 20), new AxisPosition(-9.8, 20.2), -1.0, now);

            var points = new List<AlignmentPoint> { p1, p2, p3 };

            var fallback = new NearestPointDeltaModel();
            var model = new LocalEquatorialPointingModel(fallback)
            {
                FilterMode = LocalPointFilterMode.SamePierSide
            };

            model.Fit(points);

            var target = new AxisPosition(11, 21);

            var corrected = model.Apply(target, 1.0);

            AlignmentPoint nearest = new[] { p1, p2 }
               .OrderBy(p => p.RawRad.DistanceTo(target.ToRad()))
               .First();

            var expected = new AxisPosition(
                target.A1 + (nearest.Raw.A1 - nearest.Ideal.A1),
                target.A2 + (nearest.Raw.A2 - nearest.Ideal.A2));


            Assert.AreEqual(expected.A1, corrected.A1, 1e-9);
            Assert.AreEqual(expected.A2, corrected.A2, 1e-9);
        }

        [TestMethod]
        public void Filter_SameQuadrant_UsesOnlyQuadrantPointsForTriangle()
        {
            var now = DateTime.UtcNow;

            // NE quadrant (A1>0, A2>0)
            var p1 = new AlignmentPoint(new AxisPosition(10, 10), new AxisPosition(10.1, 10.2), 0, now);
            var p2 = new AlignmentPoint(new AxisPosition(12, 14), new AxisPosition(12.1, 14.1), 0, now);
            var p3 = new AlignmentPoint(new AxisPosition(15, 18), new AxisPosition(15.2, 18.1), 0, now);

            // SW quadrant (A1<0, A2<0)
            var p4 = new AlignmentPoint(new AxisPosition(-10, -10), new AxisPosition(-9.9, -9.8), 0, now);

            var points = new List<AlignmentPoint> { p1, p2, p3, p4 };

            var fallback = new NearestPointDeltaModel();
            var model = new LocalEquatorialPointingModel(fallback)
            {
                FilterMode = LocalPointFilterMode.SameQuadrant
            };

            model.Fit(points);

            var target = new AxisPosition(11, 12); // NE quadrant

            var corrected = model.Apply(target, 0.0);

            // Expected affine from p1,p2,p3
            var affine = LocalAffine2D.FromThreePoints(
                p1.IdealRad, p1.RawRad,
                p2.IdealRad, p2.RawRad,
                p3.IdealRad, p3.RawRad);

            var expected = affine.Apply(target.ToRad()).ToDeg();

            Assert.AreEqual(expected.A1, corrected.A1, 1e-9);
            Assert.AreEqual(expected.A2, corrected.A2, 1e-9);
        }

        [TestMethod]
        public void Filter_Custom_OnlyUsesUserDefinedPoints()
        {
            var now = DateTime.UtcNow;

            var p1 = new AlignmentPoint(new AxisPosition(10, 10), new AxisPosition(10.1, 10.2), 0, now);
            var p2 = new AlignmentPoint(new AxisPosition(20, 20), new AxisPosition(20.1, 20.2), 0, now);
            var p3 = new AlignmentPoint(new AxisPosition(30, 30), new AxisPosition(30.1, 30.2), 0, now);
            var p4 = new AlignmentPoint(new AxisPosition(40, 40), new AxisPosition(40.1, 40.2), 0, now);

            var points = new List<AlignmentPoint> { p1, p2, p3, p4 };

            var fallback = new NearestPointDeltaModel();
            var model = new LocalEquatorialPointingModel(fallback)
            {
                FilterMode = LocalPointFilterMode.Custom,
                CustomFilter = (p, t) => p.IdealRad.A1 < 25 // keep only p1,p2
            };

            model.Fit(points);

            var target = new AxisPosition(15, 15);

            var corrected = model.Apply(target, 0.0);

            // Only p1,p2 remain → fallback must be used (only 2 points)
            var nearest = p1; // target is closer to p1

            var expected = new AxisPosition(
                target.A1 + (nearest.Raw.A1 - nearest.Ideal.A1),
                target.A2 + (nearest.Raw.A2 - nearest.Ideal.A2));

            Assert.AreEqual(expected.A1, corrected.A1, 1e-9);
            Assert.AreEqual(expected.A2, corrected.A2, 1e-9);
        }                                                            

    }
}

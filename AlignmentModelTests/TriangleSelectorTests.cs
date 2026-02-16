using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlignmentModelTests
{
    [TestClass]
    public class TriangleSelectorTests
    {

        [TestMethod]
        public void TriangleSelector_PicksTriangleWithCentroidNearestTarget()
        {
            // Arrange: four alignment points in degrees
            // These correspond to 0 rad, 1 rad, and 3 rad positions.
            // 1 rad ≈ 57.2958°, 3 rad ≈ 171.887°
            var now = DateTime.UtcNow;

            var p1 = new AlignmentPoint(
                ideal: new AxisPosition(0.0, 0.0),
                unsynced: new AxisPosition(0.5, -0.5),
                hourAngle: 0.0,
                time: now);

            var p2 = new AlignmentPoint(
                ideal: new AxisPosition(57.2958, 0.0),   // ≈ 1 rad
                unsynced: new AxisPosition(58.0, -0.2),
                hourAngle: 0.0,
                time: now);

            var p3 = new AlignmentPoint(
                ideal: new AxisPosition(0.0, 57.2958),   // ≈ 1 rad
                unsynced: new AxisPosition(0.2, 58.0),
                hourAngle: 0.0,
                time: now);

            var p4 = new AlignmentPoint(
                ideal: new AxisPosition(171.887, 171.887),   // ≈ 3 rad
                unsynced: new AxisPosition(172.0, 172.0),
                hourAngle: 0.0,
                time: now);

            var points = new AlignmentPointCollection { p1, p2, p3, p4 };

            // Target near centroid of triangle (p1, p2, p3)
            // Use radians because TriangleSelector expects AxisPositionRad
            var target = new AxisPositionRad(0.33, 0.33);

            // Act
            var tri = TriangleSelector.SelectThree(points, target);

            // Assert
            Assert.IsTrue(tri.IsValid, "TriangleSelector returned invalid result");

            // Extract chosen ideal points (in radians)
            var chosen = new[]
            {
                tri.I1, tri.I2, tri.I3
            };

            bool containsP1 = chosen.Any(i => NearlyEqual(i, p1.IdealRad));
            bool containsP2 = chosen.Any(i => NearlyEqual(i, p2.IdealRad));
            bool containsP3 = chosen.Any(i => NearlyEqual(i, p3.IdealRad));

            Assert.IsTrue(containsP1, "Triangle does not contain p1");
            Assert.IsTrue(containsP2, "Triangle does not contain p2");
            Assert.IsTrue(containsP3, "Triangle does not contain p3");
        }

        [TestMethod]
        public void TriangleSelector_RejectsCollinearPoints()
        {
            // Arrange: three collinear ideal points (all lie on A2 = 0 line)
            var now = DateTime.UtcNow;

            var p1 = new AlignmentPoint(
                ideal: new AxisPosition(0.0, 0.0),
                unsynced: new AxisPosition(0.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var p2 = new AlignmentPoint(
                ideal: new AxisPosition(1.0, 0.0),
                unsynced: new AxisPosition(1.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var p3 = new AlignmentPoint(
                ideal: new AxisPosition(2.0, 0.0),
                unsynced: new AxisPosition(2.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var points = new AlignmentPointCollection { p1, p2, p3 };

            // Target can be anywhere; selector should still reject the triple
            var target = new AxisPositionRad(0.5, 0.5);

            // Act
            var tri = TriangleSelector.SelectThree(points, target);

            // Assert
            Assert.IsFalse(tri.IsValid, "TriangleSelector should reject collinear triples");

            // Returned values should be default
            Assert.AreEqual(default(AxisPositionRad), tri.I1);
            Assert.AreEqual(default(AxisPositionRad), tri.I2);
            Assert.AreEqual(default(AxisPositionRad), tri.I3);
        }

        [TestMethod]
        public void TriangleSelector_RejectsExtremelySkinnyTriangles()
        {
            // Arrange: three nearly collinear ideal points
            // They differ in A2 by only 1e-8 radians, producing a tiny area.
            var now = DateTime.UtcNow;

            var p1 = new AlignmentPoint(
                ideal: new AxisPosition(0.0, 0.0),
                unsynced: new AxisPosition(0.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var p2 = new AlignmentPoint(
                ideal: new AxisPosition(1.0, 1e-8),   // tiny vertical offset
                unsynced: new AxisPosition(1.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var p3 = new AlignmentPoint(
                ideal: new AxisPosition(2.0, 2e-8),   // tiny vertical offset
                unsynced: new AxisPosition(2.1, -0.1),
                hourAngle: 0.0,
                time: now);

            var points = new AlignmentPointCollection { p1, p2, p3 };

            // Target can be anywhere; selector should still reject the triple
            var target = new AxisPositionRad(0.5, 0.5);

            // Act
            var tri = TriangleSelector.SelectThree(points, target);

            // Assert
            Assert.IsFalse(tri.IsValid, "TriangleSelector should reject extremely skinny triangles");

            // Returned values should be default
            Assert.AreEqual(default(AxisPositionRad), tri.I1);
            Assert.AreEqual(default(AxisPositionRad), tri.I2);
            Assert.AreEqual(default(AxisPositionRad), tri.I3);
        }


        // Helper for comparing AxisPositionRad values
        private bool NearlyEqual(AxisPositionRad a, AxisPositionRad b, double eps = 1e-12)
        {
            return Math.Abs(a.A1 - b.A1) < eps &&
                   Math.Abs(a.A2 - b.A2) < eps;
        }
    }


}

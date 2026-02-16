using GS.Server.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlignmentModelTests
{
    [TestClass]
    public class AxisPositionTests
    {
        [TestMethod]
        public void AxisPosition_RadDeg_RoundTrip()
        {
            // Test a few representative values, including negatives and wrap boundaries
            var samples = new[]
            {
                new AxisPosition(0, 0),
                new AxisPosition(15, -20),
                new AxisPosition(123.456, 78.9),
                new AxisPosition(-45, 270),
                new AxisPosition(359.999, -179.5)
            };

            foreach (var p in samples)
            {
                var rad = p.ToRad();
                var back = rad.ToDeg();

                Assert.AreEqual(p.A1, back.A1, 1e-9, "A1 round-trip mismatch");
                Assert.AreEqual(p.A2, back.A2, 1e-9, "A2 round-trip mismatch");
            }
        }

        [TestMethod]
        public void AxisPositionRad_DistanceTo_WorksCorrectly()
        {
            // 1. Zero distance
            var p0 = new AxisPositionRad(0, 0);
            Assert.AreEqual(0.0, p0.DistanceTo(p0), 1e-12, "Zero distance failed");

            // 2. Pure A1 separation
            var pA1a = new AxisPositionRad(1.0, 0.0);
            var pA1b = new AxisPositionRad(3.0, 0.0);
            Assert.AreEqual(2.0, pA1a.DistanceTo(pA1b), 1e-12, "A1 separation failed");

            // 3. Pure A2 separation
            var pA2a = new AxisPositionRad(0.0, -2.0);
            var pA2b = new AxisPositionRad(0.0, 1.0);
            Assert.AreEqual(3.0, pA2a.DistanceTo(pA2b), 1e-12, "A2 separation failed");

            // 4. Diagonal separation (3-4-5 triangle)
            var pD1 = new AxisPositionRad(0.0, 0.0);
            var pD2 = new AxisPositionRad(3.0, 4.0);
            Assert.AreEqual(5.0, pD1.DistanceTo(pD2), 1e-12, "Diagonal distance failed");

            // 5. Symmetry check
            Assert.AreEqual(
                pD1.DistanceTo(pD2),
                pD2.DistanceTo(pD1),
                1e-12,
                "Distance symmetry failed");
        }

    }
}

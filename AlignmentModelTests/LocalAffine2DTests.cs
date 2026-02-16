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
    public class LocalAffine2DTests
    {
        [TestMethod]
        public void LocalAffine2D_ReproducesAlignmentPointsExactly()
        {
            // Three synthetic alignment points in radians
            var i1 = new AxisPositionRad(0.10, 0.20);
            var r1 = new AxisPositionRad(0.11, 0.19);

            var i2 = new AxisPositionRad(0.30, -0.10);
            var r2 = new AxisPositionRad(0.32, -0.12);

            var i3 = new AxisPositionRad(-0.25, 0.40);
            var r3 = new AxisPositionRad(-0.23, 0.38);

            // Build the local affine transform
            var affine = LocalAffine2D.FromThreePoints(i1, r1, i2, r2, i3, r3);

            // Apply transform to each ideal point
            var t1 = affine.Apply(i1);
            var t2 = affine.Apply(i2);
            var t3 = affine.Apply(i3);

            // Verify exact reproduction (within floating point tolerance)
            Assert.AreEqual(r1.A1, t1.A1, 1e-12, "Point 1 A1 mismatch");
            Assert.AreEqual(r1.A2, t1.A2, 1e-12, "Point 1 A2 mismatch");

            Assert.AreEqual(r2.A1, t2.A1, 1e-12, "Point 2 A1 mismatch");
            Assert.AreEqual(r2.A2, t2.A2, 1e-12, "Point 2 A2 mismatch");

            Assert.AreEqual(r3.A1, t3.A1, 1e-12, "Point 3 A1 mismatch");
            Assert.AreEqual(r3.A2, t3.A2, 1e-12, "Point 3 A2 mismatch");
        }
    }
}

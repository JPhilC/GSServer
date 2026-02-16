using ASCOM.DeviceInterface;
using GS.Server.Alignment;
using GS.Server.SkyTelescope;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AlignmentModelTests
{
    [TestClass]
    public class PointingModelTests
    {
        // ------------------------------
        // 1. Generic tests for ANY model
        // ------------------------------

        [TestMethod]
        public void Model_FitsConstantOffset()
        {
            IPointingModel model = PointingModelFactory.CreateEquatorialModel();
            var points = new AlignmentPointCollection();

            // Hour angle irrelevant for linear model → pass 0
            points.Add(TestAlignmentPoint.Create(10, 20, 11, 20, 0));
            points.Add(TestAlignmentPoint.Create(30, 40, 31, 40, 0));
            points.Add(TestAlignmentPoint.Create(50, 60, 51, 60, 0));
            points.Add(TestAlignmentPoint.Create(70, 80, 71, 80, 0));

            model.Fit(points);

            var corrected = model.Apply(new AxisPosition(12, 34), 0);

            Assert.AreEqual(13, corrected.A1, 1e-6);
            Assert.AreEqual(34, corrected.A2, 1e-6);
        }

        [TestMethod]
        public void Model_IdentityWhenNoError()
        {
            IPointingModel model = PointingModelFactory.CreateEquatorialModel();
            var points = new AlignmentPointCollection();

            points.Add(TestAlignmentPoint.Create(10, 20, 10, 20, 0));
            points.Add(TestAlignmentPoint.Create(30, 40, 30, 40, 0));
            points.Add(TestAlignmentPoint.Create(50, 60, 50, 60, 0));
            points.Add(TestAlignmentPoint.Create(70, 80, 70, 80, 0));

            model.Fit(points);

            var corrected = model.Apply(new AxisPosition(12, 34), 0);

            Assert.AreEqual(12, corrected.A1, 1e-6);
            Assert.AreEqual(34, corrected.A2, 1e-6);
        }

        // ---------------------------------------
        // 2. AlignmentModel orchestration tests
        // ---------------------------------------

        [TestMethod]
        public void AlignmentModel_FitsAfterFourPoints()
        {
            var am = new AlignmentModel(AlignmentModes.algGermanPolar);

            Assert.IsFalse(am.IsFitted);

            am.AddPoint(TestAlignmentPoint.Create(0, 0, 1, 0, 0));
            am.AddPoint(TestAlignmentPoint.Create(10, 10, 11, 10, 0));
            am.AddPoint(TestAlignmentPoint.Create(20, 20, 21, 20, 0));

            Assert.IsFalse(am.IsFitted);

            am.AddPoint(TestAlignmentPoint.Create(30, 30, 31, 30, 0));

            Assert.IsTrue(am.IsFitted);
        }

        [TestMethod]
        public void AlignmentModel_RemovePoint_ResetsWhenLessThanFour()
        {
            var am = new AlignmentModel(AlignmentModes.algGermanPolar);

            var p1 = TestAlignmentPoint.Create(0, 0, 1, 0, 0);
            var p2 = TestAlignmentPoint.Create(10, 10, 11, 10, 0);
            var p3 = TestAlignmentPoint.Create(20, 20, 21, 20, 0);
            var p4 = TestAlignmentPoint.Create(30, 30, 31, 30, 0);

            am.AddPoint(p1);
            am.AddPoint(p2);
            am.AddPoint(p3);
            am.AddPoint(p4);

            Assert.IsTrue(am.IsFitted);

            am.RemovePoint(p4);

            Assert.IsFalse(am.IsFitted);
        }

        // ---------------------------------------
        // 3. Forward + reverse consistency tests
        // ---------------------------------------

        [TestMethod]
        public void ForwardReverse_AreConsistent()
        {
            var am = new AlignmentModel(AlignmentModes.algGermanPolar);

            am.AddPoint(TestAlignmentPoint.Create(0, 0, 1, 0, 0));
            am.AddPoint(TestAlignmentPoint.Create(10, 10, 11, 10, 0));
            am.AddPoint(TestAlignmentPoint.Create(20, 20, 21, 20, 0));
            am.AddPoint(TestAlignmentPoint.Create(30, 30, 31, 30, 0));

            var ideal = new double[] { 15, 30 };

            // Hour angle irrelevant for linear model → pass 0
            var corrected = SkyServer.GetCorrectedAxes(ideal, 0);
            var recovered = SkyServer.GetIdealAxes(corrected, 0);

            Assert.AreEqual(ideal[0], recovered[0], 1e-6);
            Assert.AreEqual(ideal[1], recovered[1], 1e-6);
        }

        // ---------------------------------------
        // 4. AltAz model tests (generic)
        // ---------------------------------------

        [TestMethod]
        public void AltAzModel_FitsConstantOffset()
        {
            IPointingModel model = PointingModelFactory.CreateAltAzModel();
            var points = new AlignmentPointCollection();

            points.Add(TestAlignmentPoint.Create(10, 20, 12, 20, 0));
            points.Add(TestAlignmentPoint.Create(30, 40, 32, 40, 0));
            points.Add(TestAlignmentPoint.Create(50, 60, 52, 60, 0));
            points.Add(TestAlignmentPoint.Create(70, 80, 72, 80, 0));

            model.Fit(points);

            var corrected = model.Apply(new AxisPosition(15, 45), 0);

            Assert.AreEqual(17, corrected.A1, 1e-6);
            Assert.AreEqual(45, corrected.A2, 1e-6);
        }
    }
}
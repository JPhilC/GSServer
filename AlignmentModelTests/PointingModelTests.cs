using ASCOM.DeviceInterface;
using GS.Server.Alignment;
using GS.Server.SkyTelescope;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

        // ---------------------------------------
        // 5.1. Pure HA term (ΔHA proportional to HA)
        // ---------------------------------------

        [TestMethod]
        public void FullEquatorial_PureHaTerm_FitsCorrectly()
        {
            IPointingModel model = PointingModelFactory.Create(AlignmentModes.algGermanPolar, true);
            var points = new AlignmentPointCollection();

            double k = 0.1;

            // Provide enough points and vary both HA and Dec
            for (int haDeg = -60; haDeg <= 60; haDeg += 30)
            {
                for (int decDeg = 10; decDeg <= 50; decDeg += 20)
                {
                    double rawHa = haDeg + k * haDeg;
                    double rawDec = decDeg;

                    double haRad = haDeg * Math.PI / 180.0;

                    points.Add(TestAlignmentPoint.Create(
                        haDeg, decDeg,
                        rawHa, rawDec,
                        haRad));
                }
            }

            model.Fit(points);

            var ideal = new AxisPosition(30.0, 20.0);
            double haRadTest = 30 * Math.PI / 180.0;

            var corrected = model.Apply(ideal, haRadTest);

            Assert.AreEqual(33.0, corrected.A1, 0.05);
            Assert.AreEqual(20.0, corrected.A2, 1e-3);
        }


        // ---------------------------------------
        // 5.2. Pure HA term (ΔHA proportional to HA) constant offsets
        // ---------------------------------------

        [TestMethod]
        public void FullEquatorial_ForwardReverse_AreConsistent()
        {
            var am = PointingModelFactory.Create(AlignmentModes.algGermanPolar, true);

            // Simple synthetic offsets
            var points = new AlignmentPointCollection();
            points.Add(TestAlignmentPoint.Create(0, 0, 1, 0, 0));
            points.Add(TestAlignmentPoint.Create(10, 10, 11, 10, 0));
            points.Add(TestAlignmentPoint.Create(20, 20, 21, 20, 0));
            points.Add(TestAlignmentPoint.Create(30, 30, 31, 30, 0));
            points.Add(TestAlignmentPoint.Create(40, 40, 41, 40, 0));
            points.Add(TestAlignmentPoint.Create(50, 50, 51, 50, 0));

            am.Fit(points);

            var ideal = new double[] { 25, 35 };
            double haRad = 0.0; // linear-ish synthetic case

            var corrected = SkyServer.GetCorrectedAxes(ideal, haRad);
            var recovered = SkyServer.GetIdealAxes(corrected, haRad);

            Assert.AreEqual(ideal[0], recovered[0], 1e-3);
            Assert.AreEqual(ideal[1], recovered[1], 1e-3);
        }

        // ---------------------------------------
        // 5.2. Pure HA term (ΔHA proportional to HA)
        // - HA‑dependent terms
        // - Dec‑dependent terms
        // - Cross terms (ha * dec)
        // - Sin/cos periodic terms
        // ---------------------------------------
        [TestMethod]
        public void FullEquatorial_ForwardReverse_WithHADependentTerms()
        {
            var am = PointingModelFactory.Create(AlignmentModes.algGermanPolar, true);

            var points = new AlignmentPointCollection();

            // Synthetic model: ΔHA = 0.1 * HA, ΔDec = 0.05 * Dec
            for (int haDeg = -60; haDeg <= 60; haDeg += 30)
            {
                double decDeg = 20;
                double haAxis = haDeg;
                double decAxis = decDeg;

                double rawHa = haAxis + 0.1 * haAxis;
                double rawDec = decAxis + 0.05 * decAxis;

                double haRad = haDeg * Math.PI / 180.0;

                points.Add(TestAlignmentPoint.Create(
                    haAxis, decAxis,
                    rawHa, rawDec,
                    haRad));
            }

            am.Fit(points);

            var ideal = new double[] { 30, 20 };
            double haRadTest = 30 * Math.PI / 180.0;

            var corrected = SkyServer.GetCorrectedAxes(ideal, haRadTest);
            var recovered = SkyServer.GetIdealAxes(corrected, haRadTest);

            Assert.AreEqual(ideal[0], recovered[0], 1e-3);
            Assert.AreEqual(ideal[1], recovered[1], 1e-3);
        }

    }
}
using System.Collections.Generic;

namespace GS.Server.Alignment
{
    public interface IPointingModel
    {
        /// <summary>
        /// Fit the pointing model using the supplied alignment points.
        /// </summary>
        void Fit(IEnumerable<AlignmentPoint> points);

        /// <summary>
        /// Apply the pointing model to the ideal mount position (in degrees)
        /// and return the corrected mount position (in degrees).
        /// </summary>
        AxisPosition Apply(AxisPosition ideal, double hourAngle);

        /// <summary>
        /// Apply the pointing model to the correct mount position (in degrees)
        /// and return the ideal mount position (in degrees).
        /// </summary>
        AxisPosition ApplyReverse(AxisPosition corrected, double hourAngle);

        /// <summary>
        /// True if the model has been fitted and is ready to use.
        /// </summary>
        bool IsFitted { get; }
    }

}

using System.Collections.Generic;

namespace GS.Server.Alignment
{
    public interface IPointingModel
    {
        /// <summary>
        /// Fit the pointing model using the supplied alignment points.
        /// </summary>
        void Fit(IReadOnlyList<AlignmentPoint> points);

        /// <summary>
        /// Apply the pointing model to the ideal mount position (in degrees)
        /// and return the corrected mount position (in degrees).
        /// </summary>
        AxisPosition Apply(AxisPosition ideal, double hourAngle);

        /// <summary>
        /// Apply the inverse pointing model to a mount-reported axis position
        /// (in degrees) and return the corresponding ideal axis position.
        /// </summary>
        AxisPosition ApplyReverse(AxisPosition corrected, double hourAngle);

        /// <summary>
        /// True if the model has been fitted and is ready to use.
        /// </summary>
        bool IsFitted { get; }

        /// <summary>
        /// Reset the pointing model to its initial, unfitted state.
        /// After calling this, the model must be fitted again before
        /// Apply or ApplyReverse can be used.
        /// </summary>
        void Reset();
    }

}

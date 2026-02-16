using ASCOM.DeviceInterface;
using GS.Server.Alignment;

namespace AlignmentModelTests
{
    public static class PointingModelFactory
    {
        public static IPointingModel CreateFullEquatorialModel()
        {
            // Swap this out in future without changing tests
            return new FullEquatorialPointingModel();
        }

        public static IPointingModel CreateEquatorialModel()
        {
            // Swap this out in future without changing tests
            return new LinearEquatorialPointingModel();
        }

        public static IPointingModel CreateAltAzModel()
        {
            return new LinearAltAzPointingModel();
        }


        public static IPointingModel Create(AlignmentModes mode, bool full = false)
        {
            if (mode == AlignmentModes.algGermanPolar)
            {
                if (full)
                    return new FullEquatorialPointingModel();

                return new LinearEquatorialPointingModel();
            }

            if (mode == AlignmentModes.algAltAz)
            {
                return new LinearAltAzPointingModel();
            }

            // Default fallback
            return new LinearEquatorialPointingModel();
        }


    }


}

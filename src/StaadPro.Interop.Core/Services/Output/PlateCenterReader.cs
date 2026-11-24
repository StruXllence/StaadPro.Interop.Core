using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateCenterReader
    {
        internal static PlateCenterForces Read(object output, ILoadCase loadCase, Plate plate)
        {
            if (loadCase == null) throw new ArgumentNullException(nameof(loadCase));
            if (plate == null) throw new ArgumentNullException(nameof(plate));
            int loadId = loadCase.Id;
            int plateId = plate.Id;
            if (loadId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCase), "Case ID must be positive.");
            if (plateId <= 0) throw new ArgumentOutOfRangeException(nameof(plate), "Plate ID must be positive.");
            return ReadCaptured(output, loadCase, plate, loadId, plateId);
        }

        internal static PlateCenterForces ReadCaptured(object output, ILoadCase loadCase, Plate plate, int loadId, int plateId)
        {
            object buffer = new[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
            object status = ((dynamic)output).GetAllPlateCenterForces(plateId, loadId, ref buffer);
            if (!NativeValues.ReadBoolean(status, "plate-center stresses success"))
                throw new InvalidOperationException("OpenSTAAD could not retrieve plate-center stresses.");
            if (buffer is not Array values || values.Rank != 1 || values.Length != 5 ||
                values.GetType().GetElementType() != typeof(double))
                throw new InvalidOperationException("Plate-center stresses require exactly five Double values.");
            var copied = new double[5];
            int lower = values.GetLowerBound(0);
            for (int i = 0; i < copied.Length; i++)
            {
                double component = (double)values.GetValue(lower + i);
                if (!double.IsFinite(component)) throw new InvalidOperationException("Plate-center stresses are unwritten or nonfinite.");
                copied[i] = component;
            }
            return new PlateCenterForces(loadCase, plate)
                { Sqx = copied[0], Sqy = copied[1], Sx = copied[2], Sy = copied[3], Sxy = copied[4] };
        }
    }
}

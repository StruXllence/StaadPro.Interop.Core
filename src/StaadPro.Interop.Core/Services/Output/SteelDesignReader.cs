using System;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class SteelDesignReader
    {
        internal static double Read(object output, int beamId)
        {
            if (beamId <= 0) throw new ArgumentOutOfRangeException(nameof(beamId));
            object value = double.NaN;
            object status = ((dynamic)output).GetMemberSteelDesignRatio(beamId, ref value);
            if (!NativeValues.ReadBoolean(status, "steel design ratio status"))
                throw new InvalidOperationException("OpenSTAAD could not read the steel design ratio.");
            if (value is not double ratio || !double.IsFinite(ratio))
                throw new InvalidOperationException("OpenSTAAD returned an unwritten, nonfinite or non-Double steel design ratio.");
            if (ratio < 0 && ratio != -1 && ratio != -999)
                throw new InvalidOperationException("OpenSTAAD returned an unknown negative steel design ratio.");
            return ratio;
        }
    }
}

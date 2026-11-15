using System;
using StaadPro.Interop.Core.Services.Interop;
namespace StaadPro.Interop.Core.Services.Output
{
    internal static class BucklingResultReader
    {
        internal static bool Available(object output) => NativeValues.ReadBoolean(
            (object)((dynamic)output).IsBucklingAnalysisResultsAvailable(), "buckling availability");
        internal static int Count(object output)
        {
            int count = NativeValues.ReadInt32((object)((dynamic)output).GetNoOfBucklingFactors(), "computed buckling factor count");
            if (count < 0) throw new InvalidOperationException("OpenSTAAD returned a negative computed buckling factor count.");
            return count;
        }
    }
}

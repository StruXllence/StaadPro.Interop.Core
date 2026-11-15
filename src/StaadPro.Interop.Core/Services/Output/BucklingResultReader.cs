using System;
using StaadPro.Interop.Core.Services.Interop;
using StaadPro.Interop.Core.Models.Output;
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

        internal static double Factor(object output, int bucklingMode)
        {
            if (bucklingMode <= 0) throw new ArgumentOutOfRangeException(nameof(bucklingMode));
            object value = double.NaN;
            object status = ((dynamic)output).GetBucklingFactor(bucklingMode, ref value);
            RequireSuccess(status, "buckling factor");
            return ReadDouble(value, "buckling factor");
        }

        internal static BucklingModeDisplacementResult Displacement(object output, int bucklingMode, int nodeId)
        {
            if (bucklingMode <= 0) throw new ArgumentOutOfRangeException(nameof(bucklingMode));
            if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(nodeId));
            object buffer = new[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
            object status = ((dynamic)output).GetBucklingModeDisplacementAtNode(bucklingMode, nodeId, ref buffer);
            RequireSuccess(status, "buckling mode displacement");
            if (buffer is not Array array || array.Rank != 1 || array.Length != 6 || array.GetType().GetElementType() != typeof(double))
                throw new InvalidOperationException("Buckling displacement requires six Double components.");
            var values = new double[6]; int lower = array.GetLowerBound(0);
            for (int i = 0; i < values.Length; i++) values[i] = ReadDouble(array.GetValue(lower + i), "buckling displacement component");
            return new BucklingModeDisplacementResult(bucklingMode, nodeId, values);
        }

        private static void RequireSuccess(object status, string name)
        {
            if (!NativeValues.ReadBoolean(status, name + " status"))
                throw new InvalidOperationException("OpenSTAAD could not read " + name + ".");
        }

        private static double ReadDouble(object value, string name)
        {
            if (value is not double number || !double.IsFinite(number))
                throw new InvalidOperationException("OpenSTAAD returned an unwritten, nonfinite or non-Double " + name + ".");
            return number;
        }
    }
}

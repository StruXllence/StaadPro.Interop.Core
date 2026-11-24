using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class SupportResultReader
    {
        internal static LoadCaseForces ReadSupport(object output, int supportId, int loadCaseId, bool invert)
        {
            if (supportId <= 0) throw new ArgumentOutOfRangeException(nameof(supportId));
            if (loadCaseId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCaseId));
            object buffer = NewBuffer();
            object status = ((dynamic)output).GetSupportReactions(supportId, loadCaseId, ref buffer);
            RequireSuccess(status, "support reactions");
            Forces forces = ReadForces(buffer, "support reactions");
            if (invert) forces.Scale(-1);
            return new LoadCaseForces { Id = loadCaseId, Forces = forces };
        }

        internal static StaticCheckResult ReadStaticCheck(object output, int loadCaseId)
        {
            if (loadCaseId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCaseId));
            object forces = NewBuffer();
            object reactions = NewBuffer();
            object status = ((dynamic)output).GetStaticCheckResult(loadCaseId, ref forces, ref reactions);
            RequireSuccess(status, "static check");
            // Publish neither vector until both arrays have been completely qualified and copied.
            Forces qualifiedForces = ReadForces(forces, "static-check loads");
            Forces qualifiedReactions = ReadForces(reactions, "static-check reactions");
            return new StaticCheckResult(loadCaseId) { Forces = qualifiedForces, Reactions = qualifiedReactions };
        }

        private static double[] NewBuffer() =>
            [double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN];

        private static void RequireSuccess(object status, string operation)
        {
            if (!NativeValues.ReadBoolean(status, operation + " success"))
                throw new InvalidOperationException("OpenSTAAD could not retrieve " + operation + ".");
        }

        internal static Forces ReadForces(object buffer, string description)
        {
            if (buffer is not Array array || array.Rank != 1 || array.Length != 6 ||
                array.GetType().GetElementType() != typeof(double))
                throw new InvalidOperationException(description + " require exactly six Double values.");
            var values = new double[6];
            int lower = array.GetLowerBound(0);
            for (int i = 0; i < values.Length; i++)
            {
                double value = (double)array.GetValue(lower + i);
                if (!double.IsFinite(value))
                    throw new InvalidOperationException(description + " contain unwritten or nonfinite components.");
                values[i] = value;
            }
            return Forces.ParseFromArray(values);
        }
    }
}

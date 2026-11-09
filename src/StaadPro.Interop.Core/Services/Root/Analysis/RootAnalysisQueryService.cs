using System;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Interop;
using StaadPro.Interop.Core.Services.Acquisition;
using StaadPro.Interop.Core.Services.Root.Common;

namespace StaadPro.Interop.Core.Services.Root.Analysis
{
    // All dispatch stays on the supplied root and calling thread; output qualification is state-specific.
    internal static class RootAnalysisQueryService
    {
        private const int UnwrittenCount = int.MinValue;

        internal static AnalysisStatusResult GetAnalysisStatus(object root)
        {
            // Freeze the base before a native callback can alter process CWD.
            string directory = Environment.CurrentDirectory;
            string path = OpenStaadModelIdentityReader.ReadPath(root);
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("The bound STAAD session has no named model to query. Supply an explicit model path.");
            return GetAnalysisStatus(root, RootModelPaths.NormalizeRequestedPath(path, null, directory));
        }

        internal static bool IsAnalyzing(object root)
        {
            object returned = ((dynamic)root).IsAnalyzing();
            return NativeValues.ReadBoolean(returned, "IsAnalyzing flag");
        }

        internal static AnalysisStatusResult GetAnalysisStatus(object root, string modelFilePath)
        {
            int warningCount = UnwrittenCount, errorCount = UnwrittenCount;
            double cpuTimeSeconds = double.NaN;
            object returned = ((dynamic)root).GetAnalysisStatus(modelFilePath, ref warningCount, ref errorCount, ref cpuTimeSeconds);
            int status = NativeValues.ReadInt32(returned, "analysis status");
            bool recognizedState = status == -1 || (status >= 1 && status <= 5);
            if (!recognizedState)
                return new AnalysisStatusResult(modelFilePath, status, null, null, null);

            bool completed = status >= 2 && status <= 4;
            return new AnalysisStatusResult(modelFilePath, status,
                ReadCount(warningCount, completed, "warning count"),
                ReadCount(errorCount, completed, "error count"),
                ReadCpuTime(cpuTimeSeconds, completed));
        }

        private static int? ReadCount(int value, bool required, string name)
        {
            if (value == UnwrittenCount && !required) return null;
            if (value < 0)
                throw new InvalidOperationException("OpenSTAAD GetAnalysisStatus returned an unwritten or negative " + name + ".");
            return value;
        }

        private static double? ReadCpuTime(double value, bool required)
        {
            if (double.IsNaN(value) && !required) return null;
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new InvalidOperationException("OpenSTAAD GetAnalysisStatus returned an unwritten or invalid CPU time.");
            return value;
        }
    }
}

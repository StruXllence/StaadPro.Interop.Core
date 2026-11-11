using System;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Enums.Root;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Root.Analysis
{
    // The adapter owns the entire calling-thread/lifetime lease; no implicit save or retry.
    internal static class RootAnalysisRequestService
    {
        internal static void Analyze(object root) => ((dynamic)root).Analyze();

        internal static void AnalyzeModel(object root, AnalysisEngine engine)
        {
            if (engine != AnalysisEngine.StaadPro) throw new ArgumentOutOfRangeException(nameof(engine));
            ((dynamic)root).AnalyzeModel((int)engine);
        }

        internal static AnalysisResult AnalyzeEx(object root, SilentMode silentMode, HiddenMode hiddenMode, WaitAnalysisMode waitMode)
        {
            if (!Enum.IsDefined(silentMode)) throw new ArgumentOutOfRangeException(nameof(silentMode));
            if (!Enum.IsDefined(hiddenMode)) throw new ArgumentOutOfRangeException(nameof(hiddenMode));
            if (!Enum.IsDefined(waitMode)) throw new ArgumentOutOfRangeException(nameof(waitMode));
            object status = ((dynamic)root).AnalyzeEx((int)silentMode, (int)hiddenMode, (int)waitMode);
            return (AnalysisResult)NativeValues.ReadInt32(status, "analysis status");
        }
    }
}

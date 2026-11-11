using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Enums;

using StaadPro.Interop.Core.Services.Root.Capabilities;

namespace StaadPro.Interop.Core.Tests.Adapter
{
    public static class RootOptionalTestDoubles
    {
        public delegate int MessageResponder(string path, ref object buffer);
        public delegate int StatusResponder(string path, ref int warnings, ref int errors, ref double cpu);
        public class NativeRoot : StaadPro.Interop.Core.Tests.Models.OpenStaadWrapperParityTests.NativeRoot
        {
            public readonly List<string> Calls = new List<string>();
            public int ErrorCount = 2, WarningCount = 1, AnalysisCode = 2, ExportCode;
            public int Axis, LoadHandling;
            public Exception Failure;
            public Action OnCall;
            public MessageResponder Errors, Warnings;
            public StatusResponder Status;
            public int GetAnalysisStatus(string path, ref int warnings, ref int errors, ref double cpu)
            {
                Calls.Add("status:" + path); OnCall?.Invoke();
                if (Failure != null) throw Failure;
                if (Status != null) return Status(path, ref warnings, ref errors, ref cpu);
                warnings = WarningCount; errors = ErrorCount; cpu = 0;
                return AnalysisCode;
            }
            public int GetAnalysisErrorMessages(string path, ref object buffer)
            {
                Calls.Add("errors:" + path); OnCall?.Invoke();
                if (Failure != null) throw Failure;
                if (Errors != null) return Errors(path, ref buffer);
                Fill(buffer, "error");
                return 1;
            }
            public int GetAnalysisWarningMessages(string path, ref object buffer)
            {
                Calls.Add("warnings:" + path); OnCall?.Invoke();
                if (Failure != null) throw Failure;
                if (Warnings != null) return Warnings(path, ref buffer);
                Fill(buffer, "warning");
                return 1;
            }
            private static void Fill(object buffer, string prefix)
            {
                var array = (string[])buffer;
                for (int i = 0; i < array.Length; i++) array[i] = prefix + i;
            }
            public int ExportModelToADINA(int axis, int loadHandling)
            {
                Calls.Add("export"); OnCall?.Invoke(); Axis = axis; LoadHandling = loadHandling;
                if (Failure != null) throw Failure;
                return ExportCode;
            }

        }
    }
}

using System;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    // Unchanged 2024-2026 query contracts. The adapter supplies the owning wrapper's operation lease.
    internal static class RootCommonQueryService
    {
        internal static string GetStaadFileFolder(object root)
        {
            string sentinel = "__OpenSTAAD_UnwrittenFolder_" + Guid.NewGuid().ToString("N");
            object output = sentinel; // The native output VARIANT must already be BSTR-shaped.
            ((dynamic)root).GetSTAADFileFolder(ref output);
            string folder = NativeValues.ReadText(output, "model folder");
            if (folder == sentinel)
                throw new InvalidOperationException("OpenSTAAD GetSTAADFileFolder left its output unwritten.");
            return folder;
        }

        internal static bool IsPhysicalModel(object root) =>
            NativeValues.ReadBoolean((object)((dynamic)root).IsPhysicalModel(), "IsPhysicalModel flag");

        internal static string GetErrorMessage(object root) =>
            NativeValues.ReadText((object)((dynamic)root).GetErrorMessage(), "error message");

        internal static uint GetProcessId(object root) =>
            NativeValues.ReadProcessId((object)((dynamic)root).GetProcessId());
    }
}

using System;
namespace StaadPro.Interop.Core.Services.Acquisition
{
    internal static class OpenStaadModelIdentityReader
    {
        internal static string ReadPath(object root)
        {
            string sentinel = "__OpenSTAAD_Unwritten_" + Guid.NewGuid().ToString("N");
            object output = sentinel;
            ((dynamic)root).GetSTAADFile(ref output, true);
            if (!(output is string path) || path == sentinel)
                throw new InvalidOperationException("OpenSTAAD GetSTAADFile returned an unwritten or non-string path.");
            return path;
        }
    }
}

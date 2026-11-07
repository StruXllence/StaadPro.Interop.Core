using System;
using StaadPro.Interop.Core.Enums;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    // The owning wrapper supplies the thread, lifetime and non-reentrancy lease.
    internal static class RootModelOperations
    {
        internal static bool SetSilentMode(object root, SilentMode silentMode)
        {
            Validate(silentMode);
            try
            {
                // Native returns the previous mode, not an operation success flag.
                ((dynamic)root).SetSilentMode((int)silentMode);
                return true;
            }
            catch
            {
                // Preserve the source Boolean contract; do not hide wrapper/validation errors.
                return false;
            }
        }

        internal static void SaveModel(object root, SilentMode silentMode)
        {
            Validate(silentMode);
            ((dynamic)root).SaveModel((int)silentMode);
        }

        internal static void OpenSTAADFile(object root, string filePath)
        {
            string normalized = RootModelPaths.NormalizeRequestedPath(filePath);
            ((dynamic)root).OpenSTAADFile(normalized);
        }

        internal static void CloseSTAADFile(object root) => ((dynamic)root).CloseSTAADFile();
        internal static void ShowApplication(object root) => ((dynamic)root).ShowApplication();

        private static void Validate(SilentMode silentMode)
        {
            if (silentMode != SilentMode.Disable && silentMode != SilentMode.Enable)
                throw new ArgumentOutOfRangeException(nameof(silentMode), "Use SilentMode.Disable (0) or SilentMode.Enable (1).");
        }
    }
}

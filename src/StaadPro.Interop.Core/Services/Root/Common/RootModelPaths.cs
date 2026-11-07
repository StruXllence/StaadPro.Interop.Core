using System;
using System.IO;
using StaadPro.Interop.Core.Services.Acquisition;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    // One captured native filename per operation; all path work is detached managed computation.
    internal static class RootModelPaths
    {
        // Framework rejected quotes/angle brackets too; modern Windows GetInvalidPathChars omits them.
        private static readonly char[] InvalidPathCharacters = ['"', '<', '>', .. Path.GetInvalidPathChars()];

        internal static string GetStaadFileNameOnly(object root)
        {
            string path = OpenStaadModelIdentityReader.ReadPath(root);
            ValidateCharacters(path, null);
            return Path.GetFileName(path);
        }

        internal static string GetAnlFileFullPath(object root)
        {
            string path = OpenStaadModelIdentityReader.ReadPath(root);
            if (path.Length == 0) return string.Empty;
            ValidateCharacters(path, null);
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                Path.GetFileNameWithoutExtension(path) + ".ANL");
        }

        internal static string GetOpenedStaadFilename(object root)
        {
            string path = OpenStaadModelIdentityReader.ReadPath(root);
            if (path.Length == 0) return string.Empty;
            ValidateNamedPath(path, null);
            return Path.GetFullPath(path, Environment.CurrentDirectory);
        }

        internal static bool MatchesOpenedModel(object root, string requestedPath, string parameterName)
        {
            ValidateNamedPath(requestedPath, parameterName);
            string baseDirectory = Environment.CurrentDirectory;
            string normalized = Path.GetFullPath(requestedPath, baseDirectory);
            string opened = OpenStaadModelIdentityReader.ReadPath(root);
            if (opened.Length == 0) return false;
            ValidateNamedPath(opened, null);
            // Resolve both relative paths against the same base even if a native callback changes process CWD.
            return string.Equals(normalized, Path.GetFullPath(opened, baseDirectory), StringComparison.OrdinalIgnoreCase);
        }

        internal static string NormalizeRequestedPath(string filePath)
        {
            ValidateNamedPath(filePath, nameof(filePath));
            return Path.GetFullPath(filePath, Environment.CurrentDirectory);
        }

        private static void ValidateNamedPath(string path, string parameterName)
        {
            if (path == null) throw new ArgumentNullException(parameterName);
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The model path must contain text.", parameterName);
            ValidateCharacters(path, parameterName);
        }

        private static void ValidateCharacters(string path, string parameterName)
        {
            // Modern Path extraction can accept characters rejected by the Framework source's Path methods.
            if (path.IndexOfAny(InvalidPathCharacters) >= 0)
                throw new ArgumentException("The model path contains invalid Windows path characters.", parameterName);
        }
    }
}

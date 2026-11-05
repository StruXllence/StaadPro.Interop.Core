using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
namespace StaadPro.Interop.Core.Services.Acquisition
{
    internal static class OpenStaadInstallationLocator
    {
        internal static string FindExecutable()
        {
            foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var root = RegistryKey.OpenBaseKey(hive, view))
                    using (var key = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\Bentley.Staad.exe", false))
                    {
                        string candidate = (key?.GetValue(null) as string)?.Trim().Trim('"');
                        if (IsExecutable(candidate)) return Path.GetFullPath(candidate);
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is IOException) { }
            }
            return FindConventional(new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) }, File.Exists);
        }
        internal static string FindConventional(IEnumerable<string> programFolders, Func<string, bool> exists)
        {
            foreach (int year in new[] { 2026, 2025, 2024 })
            foreach (string folder in programFolders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                string candidate = Path.Combine(folder, "Bentley", "Engineering", "STAAD.Pro " + year, "STAAD", "Bentley.Staad.exe");
                if (exists(candidate)) return candidate;
            }
            return null;
        }
        private static bool IsExecutable(string value) => !string.IsNullOrEmpty(value) && Path.IsPathFullyQualified(value) && string.Equals(Path.GetFileName(value), "Bentley.Staad.exe", StringComparison.OrdinalIgnoreCase) && File.Exists(value);
    }
}

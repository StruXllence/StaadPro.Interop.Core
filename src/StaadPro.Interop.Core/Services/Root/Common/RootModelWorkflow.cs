using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Services.Acquisition;

namespace StaadPro.Interop.Core.Services.Root.Common
{
    // The adapter holds one wrapper lease for the entire synchronous workflow.
    internal static class RootModelWorkflow
    {
        internal static bool AwaitModelHandle(object root, string fileFullPath, double timeoutDurationInSeconds,
            double sleepIntervalInSeconds, Func<IRootPollingClock> clockFactory)
        {
            string directory = Environment.CurrentDirectory;
            string target = RootModelPaths.NormalizeRequestedPath(fileFullPath, nameof(fileFullPath), directory);
            RootModelPolling.ValidateSeconds(timeoutDurationInSeconds, nameof(timeoutDurationInSeconds));
            RootModelPolling.ValidateSeconds(sleepIntervalInSeconds, nameof(sleepIntervalInSeconds));
            return RootModelPolling.Wait(() => Matches(root, target, directory), timeoutDurationInSeconds,
                sleepIntervalInSeconds, clockFactory());
        }

        internal static bool OpenStaadModel(object root, string staadFileFullPath, int sleepInterval,
            int maxWaitTime, Func<IRootPollingClock> clockFactory)
        {
            string directory = Environment.CurrentDirectory;
            string target = RootModelPaths.NormalizeRequestedPath(staadFileFullPath, nameof(staadFileFullPath), directory);
            if (sleepInterval <= 0) throw new ArgumentOutOfRangeException(nameof(sleepInterval));
            if (maxWaitTime <= 0) throw new ArgumentOutOfRangeException(nameof(maxWaitTime));
            var clock = clockFactory();
            ((dynamic)root).OpenSTAADFile(target);
            return RootModelPolling.Wait(() => Matches(root, target, directory), maxWaitTime,
                sleepInterval / 1000d, clock);
        }

        internal static void CloseCurrentStaadModel(object root)
        {
            if (string.IsNullOrWhiteSpace(OpenStaadModelIdentityReader.ReadPath(root))) return;
            // Deliberately bypass the Boolean facade: a failure must prevent the close.
            ((dynamic)root).SetSilentMode(1);
            ((dynamic)root).CloseSTAADFile();
        }

        internal static bool CloseWriteReopenStaadFile(object root, string filePath, List<string> fileContents,
            int sleepTimeMilliseconds, int maxTimeMilliseconds, Func<IRootPollingClock> clockFactory, IRootModelFiles files)
        {
            string directory = Environment.CurrentDirectory;
            string target = RootModelPaths.NormalizeRequestedPath(filePath, nameof(filePath), directory);
            if (fileContents == null) throw new ArgumentNullException(nameof(fileContents));
            if (sleepTimeMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(sleepTimeMilliseconds));
            if (maxTimeMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(maxTimeMilliseconds));
            string[] lines = fileContents.ToArray();
            if (!Matches(root, target, directory)) return false;
            files.ValidateExistingFile(target);
            ((dynamic)root).SetSilentMode(1);
            ((dynamic)root).CloseSTAADFile();
            var clock = clockFactory();
            double budget = maxTimeMilliseconds / 1000d;
            while (true)
            {
                if (maxTimeMilliseconds > 0 && clock.ElapsedSeconds >= budget) return false;
                string current = RootModelPaths.GetOpenedStaadFilename(root, directory);
                if (current.Length > 0 && !Same(current, target)) return false;
                if (current.Length == 0 && !files.IsLocked(target))
                {
                    if (maxTimeMilliseconds > 0 && clock.ElapsedSeconds > budget) return false;
                    break;
                }
                double remaining = budget - clock.ElapsedSeconds;
                if (remaining <= 0) return false;
                clock.Sleep(Math.Min(remaining, sleepTimeMilliseconds / 1000d));
            }
            if (RootModelPaths.GetOpenedStaadFilename(root, directory).Length > 0) return false;
            files.WriteAtomically(target, lines);
            string afterWrite = RootModelPaths.GetOpenedStaadFilename(root, directory);
            if (afterWrite.Length == 0) ((dynamic)root).OpenSTAADFile(target);
            else if (!Same(afterWrite, target)) return false;
            return true;
        }

        private static bool Matches(object root, string target, string directory) =>
            Same(RootModelPaths.GetOpenedStaadFilename(root, directory), target);

        private static bool Same(string first, string second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
    }
}

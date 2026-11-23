using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class StaticCheckBatchReader
    {
        internal static HashSet<StaticCheckResult> Read(object output, IEnumerable<int> loadCaseIds)
        {
            if (loadCaseIds == null) throw new ArgumentNullException(nameof(loadCaseIds));
            var ids = new List<int>();
            foreach (int id in loadCaseIds)
            {
                if (id <= 0) throw new ArgumentOutOfRangeException(nameof(loadCaseIds), "Every case ID must be positive.");
                if (ids.Count == 100000) throw new ArgumentException("At most 100,000 case occurrences are supported.", nameof(loadCaseIds));
                ids.Add(id);
            }
            var results = new HashSet<StaticCheckResult>();
            foreach (int id in ids) results.Add(SupportResultReader.ReadStaticCheck(output, id));
            return results;
        }

        internal static HashSet<StaticCheckResult> Read(object output, IEnumerable<int> loadCaseIds, int nThreads)
        {
            // Preserve PLINQ's accepted parameter range, while never dispatching COM to workers.
            if (nThreads < 1 || nThreads > 512) throw new ArgumentOutOfRangeException(nameof(nThreads));
            return Read(output, loadCaseIds);
        }
    }
}

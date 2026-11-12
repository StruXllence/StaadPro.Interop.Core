using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models.Root;
using StaadPro.Interop.Core.Services.Interop;
using StaadPro.Interop.Core.Services.Output;
namespace StaadPro.Interop.Core.Services.Root.Analysis
{
    internal static class RootMemberForcesService
    {
        internal const int MaximumQueryCount = 100000;
        internal static MemberForcesResult Retrieve(object output, object load, IEnumerable<int> beams, IEnumerable<int> loadCases)
        {
            int[] members = Snapshot(beams, nameof(beams));
            int[] cases = Snapshot(loadCases, nameof(loadCases));
            long queryCount = 2L * members.Length * cases.Length;
            if (queryCount > MaximumQueryCount)
                throw new ArgumentException("At most 100,000 member-end queries may be requested in one batch.", nameof(beams));
            if (queryCount == 0) return new MemberForcesResult(null, new MemberForceQueryResult[0]);
            if (output == null) throw new InvalidOperationException("The bound root supplies no Output object.");
            if (!MemberForceReader.AreResultsAvailable(output))
                return new MemberForcesResult(false, new MemberForceQueryResult[0]);
            if (load == null) throw new InvalidOperationException("The bound root supplies no Load object.");
            var result = new List<MemberForceQueryResult>((int)queryCount);
            foreach (int loadCaseId in cases)
            {
                string title = MemberForceReader.GetLoadCaseTitle(load, loadCaseId);
                foreach (int beamId in members)
                for (int end = 0; end <= 1; end++)
                {
                    result.Add(MemberForceReader.Read(output, title, beamId, loadCaseId, (MemberEnd)end, LocalOrGlobal.Local));
                }
            }
            return new MemberForcesResult(true, result.ToArray());
        }
        private static int[] Snapshot(IEnumerable<int> values, string name)
        {
            if (values == null) throw new ArgumentNullException(name);
            var result = new List<int>();
            var seen = new HashSet<int>();
            foreach (int value in values)
            {
                if (value <= 0) throw new ArgumentException("Identifiers must be positive.", name);
                if (seen.Add(value))
                {
                    if (result.Count >= MaximumQueryCount)
                        throw new ArgumentException("The input exceeds the managed query budget.", name);
                    result.Add(value);
                }
            }
            return result.ToArray();
        }
    }
}

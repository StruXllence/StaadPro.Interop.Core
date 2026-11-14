using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Load
{
    // All entry points are called under the adapter's existing owner-thread/current-binding lease.
    internal static class LoadCaseMetadataReader
    {
        internal const int MaximumCases = 100000;

        internal static ILoadCase Read(object load, int lcId, bool primary)
        {
            if (lcId <= 0) throw new ArgumentOutOfRangeException(nameof(lcId));
            object title = primary ? ((dynamic)load).GetLoadCaseTitle(lcId) : ((dynamic)load).GetReferenceLoadCaseTitle(lcId);
            string text = NativeValues.ReadText(title, "load-case title");
            object nativeType = primary ? ((dynamic)load).GetLoadType(lcId) : ((dynamic)load).GetReferenceLoadType(lcId);
            int code = NativeValues.ReadInt32(nativeType, "engineering load type");
            if (!Enum.IsDefined((LoadType)code))
                throw new InvalidOperationException("OpenSTAAD returned an undefined engineering load type.");
            return new LoadCase(lcId, text, primary ? LoadCaseType.PrimaryLoad : LoadCaseType.ReferenceLoad, (LoadType)code);
        }

        internal static List<ILoadCase> ReadPrimary(object load, IEnumerable<int> loadCasesIds)
            => ReadRequested(load, loadCasesIds, true);

        internal static List<ILoadCase> ReadReference(object load, IEnumerable<int> loadCasesIds)
            => ReadRequested(load, loadCasesIds, false);

        private static List<ILoadCase> ReadRequested(object load, IEnumerable<int> loadCasesIds, bool primary)
        {
            List<int> ids = CaptureIds(loadCasesIds, nameof(loadCasesIds));
            var result = new List<ILoadCase>(ids.Count);
            foreach (int id in ids) result.Add(Read(load, id, primary));
            return result;
        }

        internal static HashSet<ILoadCase> ReadAll(object load, bool primary)
        {
            int[] ids = ReadIds(load, primary ? LoadCaseType.PrimaryLoad : LoadCaseType.ReferenceLoad);
            var result = new HashSet<ILoadCase>();
            foreach (int id in ids) result.Add(Read(load, id, primary));
            return result;
        }

        internal static ILoadCase ReadCombination(object load, int lcId)
        {
            if (lcId <= 0) throw new ArgumentOutOfRangeException(nameof(lcId));
            int[] available = ReadIds(load, LoadCaseType.LoadCombination);
            if (Array.IndexOf(available, lcId) < 0)
                throw new InvalidOperationException("OpenSTAAD has no load combination with ID " + lcId + ".");
            return ReadCombinationMetadata(load, lcId);
        }

        internal static HashSet<ILoadCase> ReadCombinations(object load, IEnumerable<int> lcIds)
        {
            List<int> ids = CaptureIds(lcIds, nameof(lcIds));
            var result = new HashSet<ILoadCase>();
            if (ids.Count == 0) return result;
            var requested = ids.ToHashSet();
            var available = ReadIds(load, LoadCaseType.LoadCombination).ToHashSet();
            // Qualify all requested membership before reading even the first title.
            foreach (int id in requested)
                if (!available.Contains(id))
                    throw new InvalidOperationException("OpenSTAAD has no load combination with ID " + id + ".");
            foreach (int id in requested) result.Add(ReadCombinationMetadata(load, id));
            return result;
        }

        internal static HashSet<ILoadCase> ReadAllCombinations(object load)
        {
            int[] ids = ReadIds(load, LoadCaseType.LoadCombination);
            var result = new HashSet<ILoadCase>();
            foreach (int id in ids) result.Add(ReadCombinationMetadata(load, id));
            return result;
        }

        internal static HashSet<ILoadCase> ReadAllOfType(object load, LoadCaseType loadCaseType)
            => loadCaseType switch
            {
                LoadCaseType.PrimaryLoad => ReadAll(load, true),
                LoadCaseType.ReferenceLoad => ReadAll(load, false),
                LoadCaseType.LoadCombination => ReadAllCombinations(load),
                _ => throw new NotSupportedException("Case discovery supports only PrimaryLoad, ReferenceLoad and LoadCombination.")
            };

        internal static ILoadCase Find(object load, int lcId, LoadCaseType loadCaseType)
        {
            if (lcId <= 0) throw new ArgumentOutOfRangeException(nameof(lcId), "The load-case ID must be positive.");
            return ReadAllOfType(load, loadCaseType).FirstOrDefault(loadCase => loadCase.Id == lcId);
        }

        internal static List<ILoadCase> ReadRange(object load, int fromId, int toId, LoadCaseType loadCaseType)
        {
            if (fromId <= 0) throw new ArgumentOutOfRangeException(nameof(fromId));
            if (toId <= 0) throw new ArgumentOutOfRangeException(nameof(toId));
            if (toId < fromId) throw new ArgumentException("The upper case ID must not be less than the lower case ID.", nameof(toId));
            return ReadAllOfType(load, loadCaseType)
                .Where(loadCase => loadCase.Id >= fromId && loadCase.Id <= toId).ToList();
        }

        internal static List<ILoadCase> Filter(object load, IEnumerable<int> loadCasesIds, LoadCaseType loadCaseType)
        {
            if (loadCaseType is not (LoadCaseType.PrimaryLoad or LoadCaseType.ReferenceLoad or LoadCaseType.LoadCombination))
                throw new NotSupportedException("Case discovery supports only PrimaryLoad, ReferenceLoad and LoadCombination.");
            List<int> ids = CaptureIds(loadCasesIds, nameof(loadCasesIds));
            if (ids.Count == 0) return new List<ILoadCase>();
            var requested = ids.ToHashSet();
            return ReadAllOfType(load, loadCaseType).Where(loadCase => requested.Contains(loadCase.Id)).ToList();
        }

        internal static HashSet<int> ReadEnvelopes(object load)
        {
            int count = NativeValues.ReadInt32((object)((dynamic)load).GetEnvelopeCount(), "envelope count");
            if (count < 0 || count > MaximumCases)
                throw new InvalidOperationException("Envelope count is negative or exceeds 100,000.");
            if (count == 0) return new HashSet<int>();
            object buffer = Enumerable.Repeat(int.MinValue, count).ToArray();
            object status = ((dynamic)load).GetEnvelopeIDs(ref buffer);
            if (NativeValues.ReadInt32(status, "envelope ID success status") != 1)
                throw new InvalidOperationException("OpenSTAAD GetEnvelopeIDs failed; expected integer success status 1.");
            int[] ids = QualifyIds(buffer, count);
            int observedCount = NativeValues.ReadInt32((object)((dynamic)load).GetEnvelopeCount(), "envelope count recheck");
            if (observedCount != count)
                throw new InvalidOperationException("Envelope inventory changed during ID retrieval.");
            return ids.ToHashSet();
        }

        private static LoadCase ReadCombinationMetadata(object load, int id)
        {
            string title = NativeValues.ReadText((object)((dynamic)load).GetLoadCaseTitle(id), "combination title");
            return new LoadCase(id, title, LoadCaseType.LoadCombination, LoadType.None);
        }

        private static List<int> CaptureIds(IEnumerable<int> source, string parameter)
        {
            if (source == null) throw new ArgumentNullException(parameter);
            var ids = new List<int>();
            foreach (int id in source)
            {
                if (id <= 0) throw new ArgumentOutOfRangeException(parameter, "Every load-case ID must be positive.");
                if (ids.Count == MaximumCases)
                    throw new ArgumentException("At most 100,000 cases may be requested.", parameter);
                ids.Add(id);
            }
            return ids;
        }

        private static int[] ReadIds(object load, LoadCaseType family)
        {
            int count = Count(load, family);
            if (count == 0) return [];
            object buffer = Enumerable.Repeat(int.MinValue, count).ToArray();
            object status = family switch
            {
                LoadCaseType.PrimaryLoad => ((dynamic)load).GetPrimaryLoadCaseNumbers(ref buffer),
                LoadCaseType.ReferenceLoad => ((dynamic)load).GetReferenceLoadCaseNumbers(ref buffer),
                _ => ((dynamic)load).GetLoadCombinationCaseNumbers(ref buffer)
            };
            bool acceptsFlag = family != LoadCaseType.ReferenceLoad;
            if (acceptsFlag && status is bool success)
            {
                if (!success) throw new InvalidOperationException("Load-case number retrieval failed.");
            }
            else if (NativeValues.ReadInt32(status, "load-case number count") != count)
                throw new InvalidOperationException("Case-number count changed or is inconsistent.");
            int[] ids = QualifyIds(buffer, count);
            if (acceptsFlag && status is bool && Count(load, family) != count)
                throw new InvalidOperationException("Load-case inventory changed after Boolean number retrieval.");
            return ids;
        }

        private static int[] QualifyIds(object buffer, int count)
        {
            if (buffer is not Array array || array.Rank != 1 || array.Length != count ||
                array.GetType().GetElementType() != typeof(int))
                throw new InvalidOperationException("Case IDs require a one-dimensional Int32 array matching the count.");
            var ids = new int[count];
            var seen = new HashSet<int>();
            int lower = array.GetLowerBound(0);
            for (int i = 0; i < count; i++)
            {
                int id = (int)array.GetValue(lower + i);
                if (id <= 0 || !seen.Add(id))
                    throw new InvalidOperationException("Case IDs are unwritten, nonpositive or duplicated.");
                ids[i] = id;
            }
            return ids;
        }

        private static int Count(object load, LoadCaseType family)
        {
            object value = family switch
            {
                LoadCaseType.PrimaryLoad => ((dynamic)load).GetPrimaryLoadCaseCount(),
                LoadCaseType.ReferenceLoad => ((dynamic)load).GetReferenceLoadCaseCount(),
                _ => ((dynamic)load).GetLoadCombinationCaseCount()
            };
            int count = NativeValues.ReadInt32(value, "load-case count");
            if (count < 0 || count > MaximumCases)
                throw new InvalidOperationException("Case count is negative or exceeds 100,000.");
            return count;
        }
    }
}

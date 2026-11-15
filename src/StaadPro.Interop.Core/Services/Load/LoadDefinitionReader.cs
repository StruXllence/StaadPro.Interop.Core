using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;
namespace StaadPro.Interop.Core.Services.Load
{
    // The adapter's shared current-binding lease covers every native call and qualification step.
    internal static class LoadDefinitionReader
    {
        internal static LoadEnvelopeDetail ReadEnvelope(object load, int envId,
            int remainingMemberships = LoadCaseMetadataReader.MaximumCases)
        {
            if (envId <= 0) throw new ArgumentOutOfRangeException(nameof(envId));
            int type = int.MinValue, count = int.MinValue;
            object status = ((dynamic)load).GetLoadEnvelopeDetails(envId, ref type, ref count);
            RequireEnvelopeSuccess(status);
            if (!Enum.IsDefined((LoadEnvelopType)type))
                throw new InvalidOperationException("OpenSTAAD returned an undefined or unwritten envelope classification.");
            ValidateCount(count, "envelope membership count");
            if (count > remainingMemberships)
                throw new InvalidOperationException("The envelope query exceeds the total 100,000-membership budget.");
            var ids = new HashSet<int>();
            if (count > 0)
            {
                object buffer = Enumerable.Repeat(int.MinValue, count).ToArray();
                RequireEnvelopeSuccess((object)((dynamic)load).GetLoadListfromLoadEnvelope(envId, ref buffer));
                ids = LoadCaseMetadataReader.QualifyIds(buffer, count).ToHashSet();
                int observedType = int.MinValue, observedCount = int.MinValue;
                RequireEnvelopeSuccess((object)((dynamic)load).GetLoadEnvelopeDetails(envId, ref observedType, ref observedCount));
                if (observedType != type || observedCount != count)
                    throw new InvalidOperationException("Envelope classification or membership count changed during retrieval.");
            }
            return new LoadEnvelopeDetail(envId, (LoadEnvelopType)type, count, ids);
        }

        internal static HashSet<ILoadEnvelopeDetail> ReadAllEnvelopes(object load)
        {
            HashSet<int> ids = LoadCaseMetadataReader.ReadEnvelopes(load);
            var result = new HashSet<ILoadEnvelopeDetail>();
            int remaining = LoadCaseMetadataReader.MaximumCases;
            foreach (int id in ids)
            {
                var detail = ReadEnvelope(load, id, remaining);
                remaining -= detail.LoadCasesCount;
                result.Add(detail);
            }
            return result;
        }

        internal static StaadLoadCombination ReadCombination(object load, int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            int count = ReadPairCount(load, id);
            if (count == 0) return null;
            ILoadCase combination = LoadCaseMetadataReader.ReadCombination(load, id);
            object numbers = Enumerable.Repeat(int.MinValue, count).ToArray();
            object factors = Enumerable.Repeat(double.NaN, count + 1).ToArray();
            object status = ((dynamic)load).GetLoadAndFactorForCombination(id, ref numbers, ref factors);
            if (!NativeValues.ReadBoolean(status, "combination factor success"))
                throw new InvalidOperationException("OpenSTAAD combination factor retrieval failed.");
            int[] ids = LoadCaseMetadataReader.QualifyIds(numbers, count);
            if (factors is not Array array || array.Rank != 1 || array.GetType().GetElementType() != typeof(double) ||
                (array.Length != count && array.Length != count + 1))
                throw new InvalidOperationException("Combination factors require an exact Double array with an optional SRSS slot.");
            int lower = array.GetLowerBound(0);
            if (array.Length == count + 1 && !double.IsNaN((double)array.GetValue(lower + count)))
                throw new NotSupportedException("StaadLoadCombination cannot represent a returned SRSS overall multiplier.");
            var values = new double[count];
            for (int index = 0; index < count; index++)
            {
                double value = (double)array.GetValue(lower + index);
                if (!double.IsFinite(value))
                    throw new InvalidOperationException("Combination factors are unwritten or nonfinite.");
                values[index] = value;
            }
            if (ReadPairCount(load, id) != count)
                throw new InvalidOperationException("Combination factor count changed during retrieval.");
            var primary = LoadCaseMetadataReader.ReadAll(load, true).ToDictionary(item => item.Id);
            var result = new StaadLoadCombination(id, combination.Title);
            for (int index = 0; index < count; index++)
            {
                if (!primary.TryGetValue(ids[index], out ILoadCase source))
                    throw new InvalidOperationException("A combination source ID was not found in the primary family.");
                result.Factors.Add(source, values[index]);
            }
            return result;
        }

        private static int ReadPairCount(object load, int id)
        {
            int count = NativeValues.ReadInt32((object)((dynamic)load).GetNoOfLoadAndFactorPairsForCombination(id), "combination factor count");
            ValidateCount(count, "combination factor count");
            return count;
        }

        private static void ValidateCount(int count, string name)
        {
            if (count < 0 || count > LoadCaseMetadataReader.MaximumCases)
                throw new InvalidOperationException("OpenSTAAD " + name + " is negative or exceeds 100,000.");
        }

        private static void RequireEnvelopeSuccess(object status)
        {
            if (NativeValues.ReadInt32(status, "envelope success status") != 1)
                throw new InvalidOperationException("OpenSTAAD envelope retrieval failed; expected integer success status 1.");
        }
    }
}

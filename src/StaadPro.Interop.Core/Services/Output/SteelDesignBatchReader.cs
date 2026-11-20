using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class SteelDesignBatchReader
    {
        private sealed class Entry
        {
            internal readonly Beam Beam;
            internal readonly int Id;
            internal double Ratio;
            internal Entry(Beam beam, int id) { Beam = beam; Id = id; }
        }

        internal static Dictionary<Beam, double> Read(object output, HashSet<Beam> beams)
        {
            var entries = Capture(beams, nameof(beams));
            Query(output, entries);
            var result = new Dictionary<Beam, double>(entries.Count, ReferenceEqualityComparer.Instance);
            foreach (var entry in entries) result.Add(entry.Beam, entry.Ratio);
            return result;
        }

        internal static void Populate(object output, HashSet<Beam> forBeams)
        {
            var entries = Capture(forBeams, nameof(forBeams));
            Query(output, entries);
            foreach (var entry in entries)
            {
                RequireUnchanged(entry);
                entry.Beam.UtilizationRatio = entry.Ratio;
            }
        }

        internal static double ReadOne(object output, Beam b)
        {
            if (ReferenceEquals(b, null)) throw new ArgumentNullException(nameof(b));
            int id = b.Id;
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(b), "The member ID must be positive.");
            // The scalar observation belongs to the captured ID; it does not populate the Beam.
            return SteelDesignReader.Read(output, id);
        }

        private static List<Entry> Capture(HashSet<Beam> beams, string parameter)
        {
            if (beams == null) throw new ArgumentNullException(parameter);
            var result = new List<Entry>();
            var ids = new HashSet<int>();
            var references = new HashSet<Beam>(ReferenceEqualityComparer.Instance);
            foreach (var beam in beams)
            {
                if (result.Count == 100000) throw new ArgumentException("At most 100,000 members may be queried at once.", parameter);
                if (ReferenceEquals(beam, null)) throw new ArgumentException("A member entry is null.", parameter);
                int id = beam.Id;
                if (id <= 0) throw new ArgumentException("Every member ID must be positive.", parameter);
                if (!references.Add(beam) || !ids.Add(id))
                    throw new ArgumentException("Each selected member must have a distinct reference and ID.", parameter);
                result.Add(new Entry(beam, id));
            }
            return result;
        }

        private static void Query(object output, List<Entry> entries)
        {
            foreach (var entry in entries)
            {
                RequireUnchanged(entry);
                entry.Ratio = SteelDesignReader.Read(output, entry.Id);
            }
            foreach (var entry in entries) RequireUnchanged(entry);
        }

        private static void RequireUnchanged(Entry entry)
        {
            if (entry.Beam.Id != entry.Id)
                throw new InvalidOperationException("A selected member ID changed during the observation.");
        }
    }
}

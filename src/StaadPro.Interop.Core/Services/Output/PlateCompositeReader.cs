using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateCompositeReader
    {
        private const int Limit = 100000;
        private sealed class Entry<T>(T context, int id)
        {
            internal T Context { get; } = context;
            internal int Id { get; } = id;
        }

        internal static PlateCenterResults Read(object output, ILoadCase loadCase, Plate plate)
        {
            ArgumentNullException.ThrowIfNull(loadCase);
            ArgumentNullException.ThrowIfNull(plate);
            int loadId = loadCase.Id, plateId = plate.Id;
            if (loadId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCase), "Case ID must be positive.");
            if (plateId <= 0) throw new ArgumentOutOfRangeException(nameof(plate), "Plate ID must be positive.");
            return ReadCaptured(output, loadCase, plate, loadId, plateId);
        }

        internal static IEnumerable<PlateCenterResults> ReadBatch(object output, IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates)
        {
            ArgumentNullException.ThrowIfNull(loadCases);
            ArgumentNullException.ThrowIfNull(plates);
            var cases = Capture(loadCases, item => item.Id, nameof(loadCases));
            var selection = Capture(plates, item => item.Id, nameof(plates));
            if ((long)cases.Count * selection.Count > Limit)
                throw new ArgumentException("At most 100,000 case/plate pairs may be queried at once.", nameof(plates));
            var results = new List<PlateCenterResults>(cases.Count * selection.Count);
            foreach (var load in cases)
                foreach (var plate in selection)
                    results.Add(ReadCaptured(output, load.Context, plate.Context, load.Id, plate.Id));
            return results.AsReadOnly();
        }

        private static List<Entry<T>> Capture<T>(IEnumerable<T> input, Func<T, int> getId, string parameter) where T : class
        {
            var entries = new List<Entry<T>>();
            foreach (T item in input)
            {
                if (entries.Count == Limit) throw new ArgumentException("At most 100,000 input occurrences are supported.", parameter);
                if (item == null) throw new ArgumentException("A selected entry is null.", parameter);
                int id = getId(item);
                if (id <= 0) throw new ArgumentOutOfRangeException(parameter, "Every selected ID must be positive.");
                entries.Add(new Entry<T>(item, id));
            }
            return entries;
        }

        private static PlateCenterResults ReadCaptured(object output, ILoadCase loadCase, Plate plate, int loadId, int plateId)
        {
            // One captured identity and fixed native order; never reenter the public guarded adapter.
            var forces = PlateCenterReader.ReadCaptured(output, loadCase, plate, loadId, plateId);
            var moments = PlateSurfaceReader.MomentsCaptured(output, loadCase, plate, loadId, plateId);
            var von = PlateSurfaceReader.VonMisesCaptured(output, loadCase, plate, loadId, plateId);
            var principal = PlateSurfaceReader.PrincipalCaptured(output, loadCase, plate, loadId, plateId);
            return PlateCenterResults.Create(forces, moments, von, principal);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Stores corner observations in original order with immutable captured identifier indexes.</summary>
    /// <remarks>Owns collection storage while retaining editable result/context objects. Native factories index the
    /// IDs actually queried; manual construction captures current metadata once. Later edits do not rebuild indexes.
    /// No public reference-output lookup, coordinate transformation, serialization or result aggregation is included.</remarks>
    public sealed class PlateCornerForcesResultSet : IReadOnlyCollection<PlateCornerForces>
    {
        internal sealed class Observation(PlateCornerForcesKey key, PlateCornerForces result)
        {
            internal PlateCornerForcesKey Key { get; } = key;
            internal PlateCornerForces Result { get; } = result;
        }
        private readonly List<Observation> observations;
        private readonly Dictionary<PlateCornerForcesKey, PlateCornerForces> byKey;
        private readonly Dictionary<int, IReadOnlyList<PlateCornerForces>> byLoad, byPlate, byNode;
        /// <summary>Creates an empty independently owned result set.</summary>
        public PlateCornerForcesResultSet() : this(Array.Empty<PlateCornerForces>()) { }
        /// <summary>Materializes and indexes unique results using current metadata captured once per record.</summary>
        /// <param name="results">Non-null sequence of non-null results with positive case, plate and node IDs; at most 100,000 occurrences.</param>
        /// <remarks>Retains result references. Enumeration or validation failure returns no partial set; duplicate keys
        /// are rejected. Null/incomplete metadata is invalid. Original enumeration errors propagate unchanged.</remarks>
        /// <exception cref="ArgumentNullException">results or an entry is null.</exception>
        /// <exception cref="ArgumentException">Metadata is incomplete, a key repeats or the input bound is exceeded.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An identifier is not positive.</exception>
        public PlateCornerForcesResultSet(IEnumerable<PlateCornerForces> results) : this(Capture(results)) { }
        internal PlateCornerForcesResultSet(IEnumerable<Observation> captured)
        {
            observations = new List<Observation>(); byKey = new();
            foreach (var item in captured)
            {
                if (!byKey.TryAdd(item.Key, item.Result)) throw new ArgumentException("A corner key repeats.", "results");
                observations.Add(item);
            }
            Results = observations.Select(o => o.Result).ToList().AsReadOnly();
            byLoad = Index(o => o.Key.LoadCaseId); byPlate = Index(o => o.Key.PlateId); byNode = Index(o => o.Key.CornerNodeId);
            LoadCaseIds = byLoad.Keys.OrderBy(x => x).ToList().AsReadOnly();
            PlateIds = byPlate.Keys.OrderBy(x => x).ToList().AsReadOnly();
            CornerNodeIds = byNode.Keys.OrderBy(x => x).ToList().AsReadOnly();
        }
        private static List<Observation> Capture(IEnumerable<PlateCornerForces> results)
        {
            ArgumentNullException.ThrowIfNull(results); var captured = new List<Observation>();
            foreach (var result in results)
            {
                if (captured.Count == 100000) throw new ArgumentException("At most 100,000 observations are supported.", nameof(results));
                if (result == null) throw new ArgumentNullException(nameof(results), "A corner result is null.");
                if (result.LoadCase == null || result.Plate == null || result.CornerNode == null)
                    throw new ArgumentException("Result metadata is incomplete.", nameof(results));
                captured.Add(new Observation(result.Key, result));
            }
            return captured;
        }
        private Dictionary<int, IReadOnlyList<PlateCornerForces>> Index(Func<Observation, int> id) =>
            observations.GroupBy(id).ToDictionary(g => g.Key, g => (IReadOnlyList<PlateCornerForces>)g.Select(o => o.Result).ToList().AsReadOnly());
        /// <summary>Gets read-only collection storage in original request order.</summary>
        /// <value>Original editable result references; no enumeration performs native work.</value>
        public IReadOnlyList<PlateCornerForces> Results { get; }
        /// <summary>Gets the number of captured observations.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int Count => Results.Count;
        /// <summary>Gets sorted distinct captured case identifiers.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public IReadOnlyCollection<int> LoadCaseIds { get; }
        /// <summary>Gets sorted distinct captured plate identifiers.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public IReadOnlyCollection<int> PlateIds { get; }
        /// <summary>Gets sorted distinct captured incidence-node identifiers.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public IReadOnlyCollection<int> CornerNodeIds { get; }
        /// <summary>Gets an observation by its originally captured key.</summary>
        /// <param name="key">Captured observation key, independent of current editable metadata.</param>
        /// <returns>The original result reference.</returns><exception cref="KeyNotFoundException">No key matches.</exception>
        public PlateCornerForces this[PlateCornerForcesKey key] => byKey[key];
        /// <summary>Gets an observation by three positive captured identifiers.</summary>
        /// <param name="loadCaseId">Case identifier.</param><param name="plateId">Plate identifier.</param><param name="cornerNodeId">Incidence-node identifier.</param>
        /// <returns>The indexed reference.</returns><exception cref="KeyNotFoundException">No key matches.</exception>
        /// <exception cref="ArgumentOutOfRangeException">An ID is not positive.</exception>
        public PlateCornerForces this[int loadCaseId, int plateId, int cornerNodeId] => this[new PlateCornerForcesKey(loadCaseId, plateId, cornerNodeId)];
        /// <summary>Gets original-order observations for a captured case ID.</summary>
        /// <param name="loadCaseId">Positive captured case identifier.</param><returns>Read-only results, or an empty list.</returns>
        /// <exception cref="ArgumentOutOfRangeException">loadCaseId is not positive.</exception>
        public IReadOnlyList<PlateCornerForces> GetForLoadCase(int loadCaseId) => Get(byLoad, loadCaseId, nameof(loadCaseId));
        /// <summary>Gets original-order observations for a captured plate ID.</summary>
        /// <param name="plateId">Positive captured plate identifier.</param><returns>Read-only results, or an empty list.</returns>
        /// <exception cref="ArgumentOutOfRangeException">plateId is not positive.</exception>
        public IReadOnlyList<PlateCornerForces> GetForPlate(int plateId) => Get(byPlate, plateId, nameof(plateId));
        /// <summary>Gets original-order observations for a captured incidence-node ID.</summary>
        /// <param name="cornerNodeId">Positive captured incidence-node identifier.</param><returns>Read-only results, or an empty list.</returns>
        /// <exception cref="ArgumentOutOfRangeException">cornerNodeId is not positive.</exception>
        public IReadOnlyList<PlateCornerForces> GetForCornerNode(int cornerNodeId) => Get(byNode, cornerNodeId, nameof(cornerNodeId));
        /// <summary>Filters captured keys for one plate corner without reading mutable metadata.</summary>
        /// <param name="plateId">Positive captured plate ID.</param><param name="cornerNodeId">Positive captured incidence-node ID.</param>
        /// <returns>A new read-only original-order list, empty when absent.</returns><exception cref="ArgumentOutOfRangeException">An ID is not positive.</exception>
        public IReadOnlyList<PlateCornerForces> GetForPlateCorner(int plateId, int cornerNodeId)
        { Positive(plateId, nameof(plateId)); Positive(cornerNodeId, nameof(cornerNodeId)); return observations.Where(o => o.Key.PlateId == plateId && o.Key.CornerNodeId == cornerNodeId).Select(o => o.Result).ToList().AsReadOnly(); }
        /// <summary>Filters captured keys for one case and plate without requerying native data.</summary>
        /// <param name="loadCaseId">Positive captured case ID.</param><param name="plateId">Positive captured plate ID.</param>
        /// <returns>A new read-only original-order list, empty when absent.</returns><exception cref="ArgumentOutOfRangeException">An ID is not positive.</exception>
        public IReadOnlyList<PlateCornerForces> GetForLoadCaseAndPlate(int loadCaseId, int plateId)
        { Positive(loadCaseId, nameof(loadCaseId)); Positive(plateId, nameof(plateId)); return observations.Where(o => o.Key.LoadCaseId == loadCaseId && o.Key.PlateId == plateId).Select(o => o.Result).ToList().AsReadOnly(); }
        private static void Positive(int id, string parameter) { if (id <= 0) throw new ArgumentOutOfRangeException(parameter); }
        private static IReadOnlyList<PlateCornerForces> Get(Dictionary<int, IReadOnlyList<PlateCornerForces>> index, int id, string parameter)
        { Positive(id, parameter); return index.TryGetValue(id, out var values) ? values : Array.Empty<PlateCornerForces>(); }
        /// <summary>Enumerates retained observations in original request order.</summary>
        /// <returns>A read-only-storage enumerator with no native calls.</returns>
        public IEnumerator<PlateCornerForces> GetEnumerator() => Results.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Adapters.Interfaces;
using StaadPro.Interop.Core.Enums;
using StaadPro.Interop.Core.Services.Load;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateCornerReader
    {
        private const int Limit = 100000;
        private sealed class Corner(Node node, int id)
        { internal Node Node { get; } = node; internal int Id { get; } = id; }
        private sealed class CapturedCase(ILoadCase loadCase, int id)
        { internal ILoadCase LoadCase { get; } = loadCase; internal int Id { get; } = id; }
        private sealed class CapturedPlate(Plate plate, int id, List<Corner> corners)
        { internal Plate Plate { get; } = plate; internal int Id { get; } = id; internal List<Corner> Corners { get; } = corners; }

        // All native calls and caller enumeration run under the single Output operation lease.
        internal static PlateCornerForces ReadByIds(OpenStaadWrapper wrapper, int loadCaseId, int plateId,
            int cornerNodeId, IOSGeometry geometry, IOSLoad load, LoadCaseType? loadCaseType = null)
        {
            Positive(loadCaseId, nameof(loadCaseId)); Positive(plateId, nameof(plateId)); Positive(cornerNodeId, nameof(cornerNodeId));
            if (loadCaseType.HasValue && loadCaseType.Value is not
                (LoadCaseType.PrimaryLoad or LoadCaseType.LoadCombination or LoadCaseType.ReferenceLoad))
                throw new ArgumentOutOfRangeException(nameof(loadCaseType), "Only primary, combination and reference families can be resolved.");
            ValidateBindings(wrapper, geometry, load);
            var plate = Hydrate((object)wrapper.RawGeometry, plateId);
            var corner = HydratedCorner(plate, cornerNodeId);
            var cases = ResolveCases((object)wrapper.RawLoad, [loadCaseId], loadCaseType);
            return ReadCaptured((object)wrapper.RawOutput, cases[0].LoadCase, plate, corner, loadCaseId, plateId);
        }

        internal static PlateCornerForcesResultSet ReadForIds(OpenStaadWrapper wrapper, IEnumerable<int> loadCaseIds,
            int plateId, int cornerNodeId, IOSGeometry geometry, IOSLoad load)
        {
            ArgumentNullException.ThrowIfNull(loadCaseIds);
            Positive(plateId, nameof(plateId)); Positive(cornerNodeId, nameof(cornerNodeId));
            ValidateBindings(wrapper, geometry, load);
            var ids = new List<int>(); var seen = new HashSet<int>(); int occurrences = 0;
            foreach (int id in loadCaseIds)
            {
                if (occurrences == Limit)
                    throw new ArgumentException("At most 100,000 case-ID occurrences are supported.", nameof(loadCaseIds));
                occurrences++;
                Positive(id, nameof(loadCaseIds));
                if (seen.Add(id)) ids.Add(id);
            }
            if (ids.Count == 0) return new PlateCornerForcesResultSet();
            // The batch source contract resolves all cases before the one shared plate hydration.
            var cases = ResolveCases((object)wrapper.RawLoad, ids, null);
            var plate = Hydrate((object)wrapper.RawGeometry, plateId);
            var corner = HydratedCorner(plate, cornerNodeId);
            return ReadSelection((object)wrapper.RawOutput, cases, [new CapturedPlate(plate, plateId, [corner])]);
        }

        private static void ValidateBindings(OpenStaadWrapper wrapper, IOSGeometry geometry, IOSLoad load)
        {
            ArgumentNullException.ThrowIfNull(geometry); ArgumentNullException.ThrowIfNull(load);
            if (!ReferenceEquals(geometry, wrapper.Geometry))
                throw new ArgumentException("Use this wrapper's current Geometry adapter.", nameof(geometry));
            if (!ReferenceEquals(load, wrapper.Load))
                throw new ArgumentException("Use this wrapper's current Load adapter.", nameof(load));
        }

        private static Corner HydratedCorner(Plate plate, int cornerNodeId)
        {
            var corners = Capture(plate.A, plate.B, plate.C, plate.D, "plateId");
            foreach (var corner in corners) if (corner.Id == cornerNodeId) return corner;
            throw new ArgumentException("The requested node is not an incidence node of the hydrated plate.", nameof(cornerNodeId));
        }

        private static List<CapturedCase> ResolveCases(object load, List<int> ids, LoadCaseType? loadCaseType)
        {
            var requested = new HashSet<int>(ids);
            var found = new Dictionary<int, ILoadCase>();
            var families = loadCaseType.HasValue ? new[] { loadCaseType.Value } :
                new[] { LoadCaseType.PrimaryLoad, LoadCaseType.LoadCombination, LoadCaseType.ReferenceLoad };
            foreach (var family in families)
                foreach (var candidate in LoadCaseMetadataReader.ReadAllOfType(load, family))
                    if (requested.Contains(candidate.Id)) found.TryAdd(candidate.Id, candidate);
            var cases = new List<CapturedCase>(ids.Count);
            foreach (int id in ids)
            {
                if (!found.TryGetValue(id, out var loadCase))
                    throw new InvalidOperationException("Load case " + id + " was not found in the requested load families.");
                cases.Add(new CapturedCase(loadCase, id));
            }
            return cases;
        }

        internal static PlateCornerForces Read(object output, object geometry, ILoadCase loadCase, Plate plate, Node cornerNode)
        {
            ArgumentNullException.ThrowIfNull(loadCase); ArgumentNullException.ThrowIfNull(plate); ArgumentNullException.ThrowIfNull(cornerNode);
            int loadId = loadCase.Id, plateId = plate.Id, nodeId = cornerNode.Id;
            Positive(loadId, nameof(loadCase)); Positive(plateId, nameof(plate)); Positive(nodeId, nameof(cornerNode));
            Node a = plate.A, b = plate.B, c = plate.C, d = plate.D;
            if (a == null || b == null || c == null)
            { plate = Hydrate(geometry, plateId); a = plate.A; b = plate.B; c = plate.C; d = plate.D; }
            var corners = Capture(a, b, c, d, nameof(plate));
            var corner = FindCorner(corners, nodeId, "The requested node is not a supplied/resolved plate incidence node.");
            return ReadCaptured(output, loadCase, plate, corner, loadId, plateId);
        }

        internal static PlateCornerForcesResultSet ReadAll(object output, ILoadCase loadCase, Plate plate)
        {
            ArgumentNullException.ThrowIfNull(loadCase); ArgumentNullException.ThrowIfNull(plate);
            int loadId = loadCase.Id, plateId = plate.Id;
            Positive(loadId, nameof(loadCase)); Positive(plateId, nameof(plate));
            var corners = Capture(plate.A, plate.B, plate.C, plate.D, nameof(plate));
            return ReadSelection(output, [new CapturedCase(loadCase, loadId)], [new CapturedPlate(plate, plateId, corners)]);
        }

        internal static PlateCornerForcesResultSet ReadForLoadCases(object output, IEnumerable<ILoadCase> loadCases, Plate plate, Node cornerNode)
        {
            ArgumentNullException.ThrowIfNull(loadCases); ArgumentNullException.ThrowIfNull(plate); ArgumentNullException.ThrowIfNull(cornerNode);
            int plateId = plate.Id, nodeId = cornerNode.Id;
            Positive(plateId, nameof(plate)); Positive(nodeId, nameof(cornerNode));
            var corners = Capture(plate.A, plate.B, plate.C, plate.D, nameof(plate));
            var corner = FindCorner(corners, nodeId, "The requested node is not a supplied plate incidence node.");
            var cases = CaptureLoadCases(loadCases);
            return ReadSelection(output, cases, [new CapturedPlate(plate, plateId, [corner])]);
        }

        internal static PlateCornerForcesResultSet ReadForLoadCases(object output, IEnumerable<ILoadCase> loadCases, Plate plate)
        {
            ArgumentNullException.ThrowIfNull(loadCases); ArgumentNullException.ThrowIfNull(plate);
            int plateId = plate.Id;
            Positive(plateId, nameof(plate));
            var corners = Capture(plate.A, plate.B, plate.C, plate.D, nameof(plate));
            var cases = CaptureLoadCases(loadCases);
            if ((long)cases.Count * corners.Count > Limit)
                throw new ArgumentException("At most 100,000 plate-corner observations may be queried at once.", nameof(loadCases));
            return ReadSelection(output, cases, [new CapturedPlate(plate, plateId, corners)]);
        }

        internal static PlateCornerForcesResultSet ReadForPlates(object output, ILoadCase loadCase, IEnumerable<Plate> plates)
        {
            ArgumentNullException.ThrowIfNull(loadCase); ArgumentNullException.ThrowIfNull(plates);
            int loadId = loadCase.Id;
            Positive(loadId, nameof(loadCase));
            var capturedPlates = CapturePlates(plates);
            return ReadSelection(output, [new CapturedCase(loadCase, loadId)], capturedPlates);
        }

        internal static PlateCornerForcesResultSet ReadForSelections(object output, IEnumerable<ILoadCase> loadCases, IEnumerable<Plate> plates)
        {
            ArgumentNullException.ThrowIfNull(loadCases); ArgumentNullException.ThrowIfNull(plates);
            var cases = CaptureLoadCases(loadCases);
            var capturedPlates = CapturePlates(plates);
            return ReadSelection(output, cases, capturedPlates);
        }

        private sealed class CapturedRequest(PlateCornerForcesKey key, ILoadCase loadCase, Plate plate, Corner corner)
        {
            internal PlateCornerForcesKey Key { get; } = key;
            internal ILoadCase LoadCase { get; } = loadCase;
            internal Plate Plate { get; } = plate;
            internal Corner Corner { get; } = corner;
        }

        internal static PlateCornerForcesResultSet ReadRequests(object output, IEnumerable<PlateCornerForcesRequest> requests)
        {
            ArgumentNullException.ThrowIfNull(requests);
            var captured = new List<CapturedRequest>(); var keys = new HashSet<PlateCornerForcesKey>();
            int occurrences = 0;
            foreach (var request in requests)
            {
                if (occurrences == Limit) throw new ArgumentException("At most 100,000 request occurrences are supported.", nameof(requests));
                occurrences++;
                if (request == null) throw new ArgumentNullException(nameof(requests), "A corner request is null.");
                ILoadCase load = request.LoadCase; Plate plate = request.Plate; Node requested = request.CornerNode;
                if (load == null || plate == null || requested == null)
                    throw new ArgumentException("Request case, plate and corner metadata are required.", nameof(requests));
                int loadId = load.Id, plateId = plate.Id, nodeId = requested.Id;
                Positive(loadId, nameof(requests)); Positive(plateId, nameof(requests)); Positive(nodeId, nameof(requests));
                var corners = Capture(plate.A, plate.B, plate.C, plate.D, nameof(requests));
                Corner selected = null;
                foreach (var corner in corners) if (corner.Id == nodeId) { selected = corner; break; }
                if (selected == null) throw new ArgumentException("A requested node is not a supplied plate incidence node.", nameof(requests));
                var key = new PlateCornerForcesKey(loadId, plateId, nodeId);
                if (keys.Add(key)) captured.Add(new CapturedRequest(key, load, plate, selected));
            }
            var records = new List<PlateCornerForcesResultSet.Observation>(captured.Count);
            foreach (var request in captured)
                records.Add(new(request.Key, ReadCaptured(output, request.LoadCase, request.Plate, request.Corner, request.Key.LoadCaseId, request.Key.PlateId)));
            return new PlateCornerForcesResultSet(records);
        }

        private static PlateCornerForcesResultSet ReadSelection(object output, List<CapturedCase> cases, List<CapturedPlate> plates)
        {
            long perCase = 0; foreach (var p in plates) perCase += p.Corners.Count;
            long total = perCase * cases.Count;
            if (total > Limit) throw new ArgumentException("At most 100,000 unique plate-corner observations may be queried at once.", nameof(plates));
            var records = new List<PlateCornerForcesResultSet.Observation>((int)total);
            foreach (var load in cases)
                foreach (var p in plates)
                    foreach (var corner in p.Corners)
                    {
                        var key = new PlateCornerForcesKey(load.Id, p.Id, corner.Id);
                        records.Add(new(key, ReadCaptured(output, load.LoadCase, p.Plate, corner, load.Id, p.Id)));
                    }
            return new PlateCornerForcesResultSet(records);
        }

        private static List<CapturedCase> CaptureLoadCases(IEnumerable<ILoadCase> loadCases)
        {
            var result = new List<CapturedCase>();
            var seenIds = new HashSet<int>();
            int occurrences = 0;
            foreach (ILoadCase loadCase in loadCases)
            {
                if (occurrences == Limit) throw new ArgumentException("At most 100,000 load-case occurrences are supported.", nameof(loadCases));
                occurrences++;
                if (loadCase == null) throw new ArgumentException("A selected load case is null.", nameof(loadCases));
                int id = loadCase.Id;
                if (id <= 0) throw new ArgumentOutOfRangeException(nameof(loadCases), "Every selected load case ID must be positive.");
                if (seenIds.Add(id)) result.Add(new CapturedCase(loadCase, id));
            }
            return result;
        }

        private static List<CapturedPlate> CapturePlates(IEnumerable<Plate> plates)
        {
            var result = new List<CapturedPlate>();
            var seenIds = new HashSet<int>();
            int occurrences = 0, totalObservations = 0;
            foreach (Plate plate in plates)
            {
                if (occurrences == Limit) throw new ArgumentException("At most 100,000 plate occurrences are supported.", nameof(plates));
                occurrences++;
                if (plate == null) throw new ArgumentException("A selected plate is null.", nameof(plates));
                int id = plate.Id;
                if (id <= 0) throw new ArgumentOutOfRangeException(nameof(plates), "Every selected plate ID must be positive.");
                var corners = Capture(plate.A, plate.B, plate.C, plate.D, nameof(plates));
                if (seenIds.Add(id))
                {
                    totalObservations += corners.Count;
                    if (totalObservations > Limit)
                        throw new ArgumentException("At most 100,000 plate-corner observations may be queried at once.", nameof(plates));
                    result.Add(new CapturedPlate(plate, id, corners));
                }
            }
            return result;
        }

        private static Corner FindCorner(List<Corner> corners, int nodeId, string message)
        {
            foreach (var corner in corners)
                if (corner.Id == nodeId) return corner;
            throw new ArgumentException(message, "cornerNode");
        }

        private static List<Corner> Capture(Node a, Node b, Node c, Node d, string parameter)
        {
            if (a == null || b == null || c == null) throw new ArgumentException("Plate A, B and C incidence references are required.", parameter);
            var corners = new List<Corner>(4); var ids = new HashSet<int>();
            foreach (Node node in new[] { a, b, c, d })
            {
                if (node == null) continue;
                int id = node.Id;
                if (id <= 0) throw new ArgumentException("Every supplied incidence node must have a positive ID.", parameter);
                if (ids.Add(id)) corners.Add(new(node, id));
            }
            return corners;
        }
        private static PlateCornerForces ReadCaptured(object output, ILoadCase load, Plate plate, Corner corner, int loadId, int plateId)
        {
            object buffer = new[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
            object status = ((dynamic)output).GetPlateCornerForces(plateId, corner.Id, loadId, ref buffer);
            if (!NativeValues.ReadBoolean(status, "plate-corner force success")) throw new InvalidOperationException("OpenSTAAD could not retrieve plate-corner forces.");
            var forces = SupportResultReader.ReadForces(buffer, "plate-corner forces");
            return new PlateCornerForces(load, plate, corner.Node, forces);
        }
        private static Plate Hydrate(object geometry, int plateId)
        {
            if (geometry == null) throw new InvalidOperationException("Plate hydration requires the acquired Geometry binding.");
            object a = int.MinValue, b = int.MinValue, c = int.MinValue, d = int.MinValue;
            object status = ((dynamic)geometry).GetPlateIncidence(plateId, ref a, ref b, ref c, ref d);
            // Geometry incidence uses integral zero for success, unlike Output's Boolean/one status.
            if (NativeValues.ReadInt32(status, "plate incidence status") != 0) throw new InvalidOperationException("OpenSTAAD could not resolve plate incidence.");
            int ai = NativeValues.ReadInt32(a, "plate A"), bi = NativeValues.ReadInt32(b, "plate B"), ci = NativeValues.ReadInt32(c, "plate C"), di = NativeValues.ReadInt32(d, "plate D");
            if (ai <= 0 || bi <= 0 || ci <= 0 || di < 0) throw new InvalidOperationException("Native plate incidence is unwritten or invalid.");
            var nodes = new Dictionary<int, Node>();
            Node Read(int id)
            {
                if (id == 0) return null;
                if (nodes.TryGetValue(id, out var known)) return known;
                object x = double.NaN, y = double.NaN, z = double.NaN;
                // GetNodeCoordinates is documented void; qualify every written output, not a fabricated status.
                ((dynamic)geometry).GetNodeCoordinates(id, ref x, ref y, ref z);
                if (x is not double dx || y is not double dy || z is not double dz || !double.IsFinite(dx) || !double.IsFinite(dy) || !double.IsFinite(dz))
                    throw new InvalidOperationException("Native incidence coordinates are unwritten or nonfinite Doubles.");
                var node = new Node(id, dx, dy, dz); nodes.Add(id, node); return node;
            }
            return new Plate(Read(ai), Read(bi), Read(ci), Read(di)) { Id = plateId };
        }
        private static void Positive(int id, string parameter) { if (id <= 0) throw new ArgumentOutOfRangeException(parameter, "The analytical identifier must be positive."); }
    }
}

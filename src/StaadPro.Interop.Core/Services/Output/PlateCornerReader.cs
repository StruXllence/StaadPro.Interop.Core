using System;
using System.Collections.Generic;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Models;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Output
{
    internal static class PlateCornerReader
    {
        private sealed class Corner(Node node, int id)
        { internal Node Node { get; } = node; internal int Id { get; } = id; }
        internal static PlateCornerForces Read(object output, object geometry, ILoadCase loadCase, Plate plate, Node cornerNode)
        {
            ArgumentNullException.ThrowIfNull(loadCase); ArgumentNullException.ThrowIfNull(plate); ArgumentNullException.ThrowIfNull(cornerNode);
            int loadId = loadCase.Id, plateId = plate.Id, nodeId = cornerNode.Id;
            Positive(loadId, nameof(loadCase)); Positive(plateId, nameof(plate)); Positive(nodeId, nameof(cornerNode));
            Node a = plate.A, b = plate.B, c = plate.C, d = plate.D;
            if (a == null || b == null || c == null)
            { plate = Hydrate(geometry, plateId); a = plate.A; b = plate.B; c = plate.C; d = plate.D; }
            var corners = Capture(a, b, c, d);
            foreach (var corner in corners)
                if (corner.Id == nodeId) return ReadCaptured(output, loadCase, plate, corner, loadId, plateId);
            throw new ArgumentException("The requested node is not a supplied/resolved plate incidence node.", nameof(cornerNode));
        }
        internal static PlateCornerForcesResultSet ReadAll(object output, ILoadCase loadCase, Plate plate)
        {
            ArgumentNullException.ThrowIfNull(loadCase); ArgumentNullException.ThrowIfNull(plate);
            int loadId = loadCase.Id, plateId = plate.Id;
            Positive(loadId, nameof(loadCase)); Positive(plateId, nameof(plate));
            var corners = Capture(plate.A, plate.B, plate.C, plate.D);
            var records = new List<PlateCornerForcesResultSet.Observation>(corners.Count);
            foreach (var corner in corners)
            {
                var key = new PlateCornerForcesKey(loadId, plateId, corner.Id);
                records.Add(new(key, ReadCaptured(output, loadCase, plate, corner, loadId, plateId)));
            }
            return new PlateCornerForcesResultSet(records);
        }
        private static List<Corner> Capture(Node a, Node b, Node c, Node d)
        {
            if (a == null || b == null || c == null) throw new ArgumentException("Plate A, B and C incidence references are required.", "plate");
            var corners = new List<Corner>(4); var ids = new HashSet<int>();
            foreach (Node node in new[] { a, b, c, d })
            {
                if (node == null) continue;
                int id = node.Id;
                if (id <= 0) throw new ArgumentException("Every supplied incidence node must have a positive ID.", "plate");
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

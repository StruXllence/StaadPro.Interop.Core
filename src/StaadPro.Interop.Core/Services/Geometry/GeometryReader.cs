using System;
using System.Collections.Generic;
using System.Linq;
using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Geometry
{
    internal static class GeometryReader
    {
        private const int MaximumInventorySize = 1_000_000;

        internal enum Entity { Beams, Nodes, Plates, Solids, SelectedNodes, SelectedBeams, SelectedPlates }

        internal static int[] ReadIds(object geometry, Entity entity)
        {
            int count = ReadCount(geometry, entity);
            if (count == 0) return [];

            // A valid ID is positive, so zero-initialized slots detect unwritten output.
            object buffer = new int[count];
            dynamic native = geometry;
            switch (entity)
            {
                case Entity.Beams: native.GetBeamList(ref buffer); break;
                case Entity.Nodes: native.GetNodeList(ref buffer); break;
                case Entity.Plates: native.GetPlateList(ref buffer); break;
                case Entity.Solids: native.GetSolidList(ref buffer); break;
                case Entity.SelectedNodes: native.GetSelectedNodes(ref buffer, 0); break;
                case Entity.SelectedBeams: native.GetSelectedBeams(ref buffer, 0); break;
                case Entity.SelectedPlates: native.GetSelectedPlates(ref buffer, 0); break;
                default: throw new ArgumentOutOfRangeException(nameof(entity));
            }

            int[] ids = NativeValues.ReadPositiveInt32Ids(buffer, count, $"{entity} IDs");
            if (ReadCount(geometry, entity) != count)
                throw new InvalidOperationException($"OpenSTAAD {entity} inventory changed during ID retrieval.");
            return ids;
        }

        private static int ReadCount(object geometry, Entity entity)
        {
            dynamic native = geometry;
            object value = entity switch
            {
                Entity.Beams => native.GetMemberCount(),
                Entity.Nodes => native.GetNodeCount(),
                Entity.Plates => native.GetPlateCount(),
                Entity.Solids => native.GetSolidCount(),
                Entity.SelectedNodes => native.GetNoOfSelectedNodes(),
                Entity.SelectedBeams => native.GetNoOfSelectedBeams(),
                Entity.SelectedPlates => native.GetNoOfSelectedPlates(),
                _ => throw new ArgumentOutOfRangeException(nameof(entity))
            };
            int count = NativeValues.ReadInt32(value, $"{entity} count");
            if (count < 0 || count > MaximumInventorySize)
                throw new InvalidOperationException($"OpenSTAAD {entity} count must be between zero and {MaximumInventorySize}.");
            return count;
        }

        internal static Node FindNode(int nId, IEnumerable<Node> fromModelNodes)
        {
            ArgumentNullException.ThrowIfNull(fromModelNodes);
            foreach (Node node in fromModelNodes)
            {
                if (node is null)
                    throw new ArgumentException("The supplied sequence contains a null node.", nameof(fromModelNodes));
                if (node.Id == nId)
                    return node;
            }
            return null;
        }

        internal static HashSet<Node> ReadSelectedNodes(object geometry)
        {
            int[] ids = ReadIds(geometry, Entity.SelectedNodes);
            var nodes = new HashSet<Node>();
            foreach (int id in ids)
                nodes.Add(ReadNode(geometry, id));
            return nodes;
        }

        internal static HashSet<T> ReadSuppliedSelection<T>(object geometry, Entity entity, IEnumerable<T> supplied, string parameter)
            where T : class, IEntity
        {
            ArgumentNullException.ThrowIfNull(supplied, parameter);
            var remaining = ReadIds(geometry, entity).ToHashSet();
            var result = new HashSet<T>();
            if (remaining.Count == 0)
                return result;

            foreach (T item in supplied)
            {
                if (item is null)
                    throw new ArgumentException("The supplied sequence contains a null entity.", parameter);
                if (!remaining.Remove(item.Id))
                    continue;

                result.Add(item);
                if (remaining.Count == 0)
                    return result;
            }

            // Preserve one null entry when the supplied entities omit selected IDs.
            result.Add(null);
            return result;
        }

        internal static Node ReadNode(object geometry, int nId)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(nId);
            if (nId == 0) return null;

            object x = double.NaN, y = double.NaN, z = double.NaN;
            ((dynamic)geometry).GetNodeCoordinates(nId, ref x, ref y, ref z);
            if (x is not double dx || y is not double dy || z is not double dz ||
                !double.IsFinite(dx) || !double.IsFinite(dy) || !double.IsFinite(dz))
                throw new InvalidOperationException($"OpenSTAAD coordinates for node {nId} must be three written finite Doubles.");

            return new Node(nId, dx, dy, dz);
        }

        internal static Beam ReadBeam(object geometry, int beamNumber)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(beamNumber);
            if (beamNumber == 0) return null;

            ReadMemberIncidence(geometry, beamNumber, out int startId, out int endId);
            return new Beam(beamNumber, ReadNode(geometry, startId), ReadNode(geometry, endId));
        }

        internal static Beam ReadBeam(object geometry, int bId, IEnumerable<Node> fromModelNodes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(bId);
            if (bId == 0) return null;

            ArgumentNullException.ThrowIfNull(fromModelNodes);
            ReadMemberIncidence(geometry, bId, out int startId, out int endId);
            Node[] nodes = ResolveSuppliedNodes([startId, endId], fromModelNodes);
            return new Beam(bId, nodes[0], nodes[1]);
        }

        internal static void ReadMemberIncidence(object geometry, int beamId, out int startId, out int endId)
        {
            object start = int.MinValue, end = int.MinValue;
            object status = ((dynamic)geometry).GetMemberIncidence(beamId, ref start, ref end);
            int code = NativeValues.ReadInt32(status, "member-incidence status");
            if (code != 0)
                throw new InvalidOperationException($"OpenSTAAD GetMemberIncidence failed with status {code} for member {beamId}.");

            startId = NativeValues.ReadInt32(start, "member start node ID");
            endId = NativeValues.ReadInt32(end, "member end node ID");
            if (startId <= 0 || endId <= 0 || startId == endId)
                throw new InvalidOperationException("Native member incidence must supply two distinct positive node IDs.");
        }

        internal static Plate ReadPlate(object geometry, int pId)
        {
            int[] ids = ReadPlateIds(geometry, pId);
            return MakePlate(pId, ResolveLiveNodes(geometry, ids));
        }

        internal static Plate ReadPlate(object geometry, int pId, IEnumerable<Node> fromModelNodes)
        {
            ArgumentNullException.ThrowIfNull(fromModelNodes);
            int[] ids = ReadPlateIds(geometry, pId);
            return MakePlate(pId, ResolveSuppliedNodes(ids, fromModelNodes));
        }

        internal static IEnumerable<Node> ReadPlateCorners(object geometry, int pId)
        {
            int[] ids = ReadPlateIds(geometry, pId);
            Node[] nodes = ResolveLiveNodes(geometry, ids);
            bool hasDistinctFourth = ids[3] != 0 && Array.IndexOf(ids, ids[3], 0, 3) < 0;
            return hasDistinctFourth ? nodes : nodes[..3];
        }

        private static Plate MakePlate(int pId, Node[] nodes) =>
            new Plate(nodes[0], nodes[1], nodes[2], nodes[3]) { Id = pId };

        private static int[] ReadPlateIds(object geometry, int pId)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pId);
            object a = int.MinValue, b = int.MinValue, c = int.MinValue, d = int.MinValue;
            object status = ((dynamic)geometry).GetPlateIncidence(pId, ref a, ref b, ref c, ref d);
            int code = NativeValues.ReadInt32(status, "plate incidence status");
            if (code != 0)
                throw new InvalidOperationException($"OpenSTAAD GetPlateIncidence failed with status {code} for plate {pId}.");

            int ai = NativeValues.ReadInt32(a, "plate A");
            int bi = NativeValues.ReadInt32(b, "plate B");
            int ci = NativeValues.ReadInt32(c, "plate C");
            int di = NativeValues.ReadInt32(d, "plate D");
            if (ai <= 0 || bi <= 0 || ci <= 0 || ai == bi || ai == ci || bi == ci || di < 0)
                throw new InvalidOperationException("Native plate incidence must supply three distinct positive node IDs and a nonnegative fourth ID.");

            return [ai, bi, ci, di];
        }

        private static Node[] ResolveLiveNodes(object geometry, int[] ids)
        {
            var resolved = new Dictionary<int, Node>();
            foreach (int id in ids)
            {
                if (id > 0 && !resolved.ContainsKey(id))
                    resolved.Add(id, ReadNode(geometry, id));
            }
            return ResolveSlots(ids, resolved);
        }

        private static Node[] ResolveSuppliedNodes(int[] ids, IEnumerable<Node> fromModelNodes)
        {
            var remaining = ids.Where(id => id > 0).ToHashSet();
            var resolved = new Dictionary<int, Node>();
            foreach (Node node in fromModelNodes)
            {
                if (node is null)
                    throw new ArgumentException("The supplied sequence contains a null node.", nameof(fromModelNodes));
                if (!remaining.Remove(node.Id))
                    continue;

                resolved.Add(node.Id, node);
                if (remaining.Count == 0)
                    break;
            }
            return ResolveSlots(ids, resolved);
        }

        private static Node[] ResolveSlots(int[] ids, Dictionary<int, Node> resolved) =>
            ids.Select(id => resolved.GetValueOrDefault(id)).ToArray();
    }
}

using System;
using StaadPro.Interop.Core.Entities;
using StaadPro.Interop.Core.Services.Interop;

namespace StaadPro.Interop.Core.Services.Geometry
{
    internal static class GeometryReader
    {
        private const int MaximumInventorySize = 1_000_000;

        internal enum Entity { Beams, Nodes, Plates, Solids }

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
                _ => throw new ArgumentOutOfRangeException(nameof(entity))
            };
            int count = NativeValues.ReadInt32(value, $"{entity} count");
            if (count < 0 || count > MaximumInventorySize)
                throw new InvalidOperationException($"OpenSTAAD {entity} count must be between zero and {MaximumInventorySize}.");
            return count;
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
    }
}

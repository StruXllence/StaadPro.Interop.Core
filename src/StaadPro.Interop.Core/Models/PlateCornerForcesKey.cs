using System;

namespace StaadPro.Interop.Core.Models
{
    /// <summary>Identifies one corner observation by load-case, plate and incidence-node identifiers.</summary>
    /// <remarks>An immutable value key independent of mutable entity references. Constructed IDs are positive;
    /// default(PlateCornerForcesKey) contains zeros and identifies no qualified query.</remarks>
    public readonly struct PlateCornerForcesKey : IEquatable<PlateCornerForcesKey>
    {
        /// <summary>Creates an immutable key from positive analytical identifiers.</summary>
        /// <param name="loadCaseId">Positive analyzed case or combination number.</param>
        /// <param name="plateId">Positive plate number.</param>
        /// <param name="cornerNodeId">Positive incidence-node number, not a corner ordinal.</param>
        /// <exception cref="ArgumentOutOfRangeException">An identifier is not positive.</exception>
        public PlateCornerForcesKey(int loadCaseId, int plateId, int cornerNodeId)
        {
            if (loadCaseId <= 0) throw new ArgumentOutOfRangeException(nameof(loadCaseId));
            if (plateId <= 0) throw new ArgumentOutOfRangeException(nameof(plateId));
            if (cornerNodeId <= 0) throw new ArgumentOutOfRangeException(nameof(cornerNodeId));
            LoadCaseId = loadCaseId; PlateId = plateId; CornerNodeId = cornerNodeId;
        }
        /// <summary>Gets the captured case or combination identifier.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int LoadCaseId { get; }
        /// <summary>Gets the captured analytical plate identifier.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int PlateId { get; }
        /// <summary>Gets the captured incidence-node identifier.</summary>
        /// <value>The managed identifier snapshot or current metadata projection described above; no native work.</value>
        public int CornerNodeId { get; }
        /// <summary>Compares all three integer identifiers.</summary>
        /// <param name="other">Key to compare.</param>
        /// <returns>True when all three values match.</returns>
        public bool Equals(PlateCornerForcesKey other) => LoadCaseId == other.LoadCaseId && PlateId == other.PlateId && CornerNodeId == other.CornerNodeId;
        /// <summary>Compares another object by this key's value semantics.</summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True only for a key with equal identifiers.</returns>
        public override bool Equals(object obj) => obj is PlateCornerForcesKey other && Equals(other);
        /// <summary>Hashes the three immutable identifier values.</summary>
        /// <returns>A hash consistent with equality.</returns>
        public override int GetHashCode() => HashCode.Combine(LoadCaseId, PlateId, CornerNodeId);
        /// <summary>Compares two keys by identifier values.</summary>
        /// <param name="left">First key.</param><param name="right">Second key.</param>
        /// <returns>True when their identifiers match.</returns>
        public static bool operator ==(PlateCornerForcesKey left, PlateCornerForcesKey right) => left.Equals(right);
        /// <summary>Compares two keys for differing identifier values.</summary>
        /// <param name="left">First key.</param><param name="right">Second key.</param>
        /// <returns>True when an identifier differs.</returns>
        public static bool operator !=(PlateCornerForcesKey left, PlateCornerForcesKey right) => !left.Equals(right);
        /// <summary>Formats the three captured identifiers for diagnostics.</summary>
        /// <returns>A case/plate/corner description without a native query.</returns>
        public override string ToString() => $"Load Case {LoadCaseId}, Plate {PlateId}, Corner Node {CornerNodeId}";
    }
}

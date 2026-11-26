using StaadPro.Interop.Core.Common;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Stores detached analytical-member identity, supplied endpoints, beta angle and steel utilization.</summary>
    /// <remarks>Uses managed property notifications without a native binding or geometry hydration. Construction does
    /// not validate IDs, create a native member, register node connectivity, initialize releases or calculate axes.
    /// Equality and hashing use mutable Id, matching the member-identity contract; remove/reinsert a Beam in ordinary
    /// hash collections before changing its Id. Output batch dictionaries explicitly use reference identity instead.
    /// This type supplies metadata for Output queries, rather than the complete geometry or design model.</remarks>
    public class Beam : ValidatablePropertyStore, IEntity
    {
        /// <summary>Creates an uninitialized detached member with zero ID/ratio and null endpoints.</summary>
        public Beam() { }
        /// <summary>Creates a detached member carrying the supplied endpoint references.</summary>
        /// <param name="startNode">Stored start-node reference; null and incomplete metadata are allowed.</param>
        /// <param name="endNode">Stored end-node reference; no coordinate validation or copying occurs.</param>
        public Beam(Node startNode, Node endNode) : this() { StartNode = startNode; EndNode = endNode; }
        /// <summary>Creates detached member identity and endpoint metadata.</summary>
        /// <param name="number">Stored member ID. Native queries separately require a positive analytical member number.</param>
        /// <param name="startNode">Stored start-node reference without native connectivity registration.</param>
        /// <param name="endNode">Stored end-node reference without geometry calculation.</param>
        public Beam(int number, Node startNode, Node endNode) : this(startNode, endNode) { Id = number; }

        /// <summary>Gets or sets the detached analytical member number.</summary>
        /// <value>The stored ID, initially zero, used by equality and hashing. Edits do not renumber STAAD members.</value>
        public int Id { get => Get<int>(); set => Set(value); }
        /// <summary>Gets the supplied start-node reference.</summary>
        /// <value>The constructor reference or null. Output ratio queries do not inspect its coordinates.</value>
        public Node StartNode { get; private set; }
        /// <summary>Gets the supplied end-node reference.</summary>
        /// <value>The constructor reference or null. No node hydration or axis computation is implied.</value>
        public Node EndNode { get; private set; }
        /// <summary>Gets or sets the supplied member beta angle in degrees.</summary>
        /// <value>The detached angle, initially zero. Positive values rotate the transverse axes by the right-hand rule about the start-to-end axis.</value>
        /// <remarks>The setter stores manual metadata and raises managed notifications. Frame calculations validate the angle and capture its current value.</remarks>
        public double BetaAngle { get => Get<double>(); set => Set(value); }
        /// <summary>Gets or sets the detached steel-utilization observation.</summary>
        /// <value>The stored ratio, initially zero before population. Manual edits are not validated and do not update
        /// STAAD. Qualified Output population may store nonnegative ratios or documented -1/-999 absence sentinels.</value>
        /// <remarks>Notifications run synchronously after storing a changed value. A handler exception propagates after
        /// that write; it does not roll back earlier writes. An unpopulated zero is not proof of design success.</remarks>
        public double UtilizationRatio { get => Get<double>(); set => Set(value); }

        /// <summary>Computes a hash from the current mutable member ID.</summary>
        /// <returns>The current integer ID hash; changing Id invalidates ordinary hash-collection placement.</returns>
        public override int GetHashCode() => Id.GetHashCode();
        /// <summary>Compares detached member identity using the current ID.</summary>
        /// <param name="obj">Another Beam, or null/an object of another type.</param>
        /// <returns>True for another Beam with the same current ID, including draft zero or negative IDs.</returns>
        public override bool Equals(object obj) => obj is Beam beam && Id == beam.Id;
        /// <summary>Compares two member references by current ID, with two nulls equal.</summary>
        /// <param name="a">First member reference.</param>
        /// <param name="b">Second member reference.</param>
        /// <returns>True for the same reference or two non-null members with equal IDs.</returns>
        public static bool operator ==(Beam a, Beam b) => ReferenceEquals(a, b) || !(a is null) && !(b is null) && a.Id == b.Id;
        /// <summary>Determines whether two member references differ by current ID/null state.</summary>
        /// <param name="a">First member reference.</param>
        /// <param name="b">Second member reference.</param>
        /// <returns>The negation of the equality operator.</returns>
        public static bool operator !=(Beam a, Beam b) => !(a == b);
    }
}

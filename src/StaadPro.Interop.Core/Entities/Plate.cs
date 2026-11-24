using StaadPro.Interop.Core.Common;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Stores detached analytical plate identity and optional incidence references for Output queries.</summary>
    /// <remarks>Stores caller metadata only; does not automatically load its corners, create a STAAD plate, compute geometry,
    /// register connectivity or provide the complete original geometry model. Native queries validate Id separately.
    /// Manual edits use managed notifications and never renumber a native plate.</remarks>
    public sealed class Plate : ValidatablePropertyStore, IEntity
    {
        /// <summary>Creates a detached plate record with Id zero and no native binding.</summary>
        public Plate() { }
        /// <summary>Stores three or four caller-owned incidence references without geometry validation.</summary>
        /// <param name="a">First corner reference, including manual null.</param>
        /// <param name="b">Second corner reference, including manual null.</param>
        /// <param name="c">Third corner reference, including manual null.</param>
        /// <param name="d">Optional fourth corner; null or a repeated corner may describe a triangle.</param>
        public Plate(Node a, Node b, Node c, Node d = null) { A = a; B = b; C = c; D = d; }
        /// <summary>Gets or sets the first caller-owned incidence node.</summary>
        /// <value>A detached reference; changing it does not update native incidence.</value>
        public Node A { get => Get<Node>(); set => Set(value); }
        /// <summary>Gets or sets the second caller-owned incidence node.</summary>
        /// <value>A detached reference; corner query methods separately validate identifiers.</value>
        public Node B { get => Get<Node>(); set => Set(value); }
        /// <summary>Gets or sets the third caller-owned incidence node.</summary>
        /// <value>A detached reference required by all-corners queries; no geometry is inferred.</value>
        public Node C { get => Get<Node>(); set => Set(value); }
        /// <summary>Gets or sets the optional fourth caller-owned incidence node.</summary>
        /// <value>Null or a repeated positive identifier can represent a triangle; query results deduplicate IDs.</value>
        public Node D { get => Get<Node>(); set => Set(value); }
        /// <summary>Gets or sets the detached analytical plate number.</summary>
        /// <value>The stored integer, initially zero. Output reads require a positive Id captured at invocation.</value>
        public int Id { get => Get<int>(); set => Set(value); }
    }
}

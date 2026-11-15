using StaadPro.Interop.Core.Common;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Stores detached node identity and coordinate metadata without a native binding.</summary>
    /// <remarks>Property changes raise managed notifications but do not update STAAD. Coordinates retain caller units;
    /// no geometry hydration, coordinate conversion or native existence check occurs. Output queries use Id only.
    /// This data type does not provide geometry operations or override reference equality.</remarks>
    public sealed class Node : ValidatablePropertyStore, IEntity
    {
        /// <summary>Creates an uninitialized node with zero ID and coordinates.</summary>
        public Node() { }

        /// <summary>Creates detached coordinate metadata without assigning a native identifier.</summary>
        /// <param name="x">Stored X coordinate, without unit conversion or finite-value validation.</param>
        /// <param name="y">Stored Y coordinate in the same caller coordinate system.</param>
        /// <param name="z">Stored Z coordinate in the same caller coordinate system.</param>
        public Node(double x, double y, double z) : this() { X = x; Y = y; Z = z; }

        /// <summary>Creates detached node identity and coordinate metadata.</summary>
        /// <param name="id">Stored identifier; native query methods separately require a positive ID.</param>
        /// <param name="x">Stored X coordinate without conversion or validation.</param>
        /// <param name="y">Stored Y coordinate in caller units.</param>
        /// <param name="z">Stored Z coordinate in caller units.</param>
        public Node(int id, double x, double y, double z) : this(x, y, z) { Id = id; }

        /// <summary>Gets or sets the detached node identifier.</summary>
        /// <value>The supplied integer, initially zero. Setting it does not renumber a native node.</value>
        public int Id { get => Get<int>(); set => Set(value); }
        /// <summary>Gets or sets the detached X coordinate.</summary>
        /// <value>The stored caller value in caller length units, initially zero.</value>
        public double X { get => Get<double>(); set => Set(value); }
        /// <summary>Gets or sets the detached Y coordinate.</summary>
        /// <value>The stored caller value in caller length units, initially zero.</value>
        public double Y { get => Get<double>(); set => Set(value); }
        /// <summary>Gets or sets the detached Z coordinate.</summary>
        /// <value>The stored caller value in caller length units, initially zero.</value>
        public double Z { get => Get<double>(); set => Set(value); }
    }
}

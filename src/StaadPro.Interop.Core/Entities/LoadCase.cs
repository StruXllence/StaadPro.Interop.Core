using StaadPro.Interop.Core.Common;
using StaadPro.Interop.Core.Enums;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>
    /// Represents a primary or reference load case in STAAD.Pro.
    /// </summary>
    /// <remarks>Editable detached metadata backed by the existing managed property store. Changes raise
    /// notifications but do not update STAAD. Equality/hash use Id, CaseType and Type; remove from a HashSet
    /// before changing those fields. Title text does not participate in equality. No JSON/UI dependency is added.</remarks>
    public class LoadCase : ValidatablePropertyStore, ILoadCase, IEntity
    {
        /// <summary>Creates an uninitialized detached primary-case record.</summary>
        /// <remarks>Id initially equals zero, Title is null, Type is None and CaseType is PrimaryLoad. Construction performs no validation or native access.</remarks>
        public LoadCase()
        {
            Type = LoadType.None;
            CaseType = LoadCaseType.PrimaryLoad;
        }

        /// <summary>Creates a detached primary-case record carrying the supplied identifier.</summary>
        /// <param name="id">Identifier stored unchanged; native existence/positivity is not checked by this data constructor.</param>
        /// <remarks>Title remains null and Type remains None. Query methods validate IDs independently.</remarks>
        public LoadCase(int id) : this()
        {
            Id = id;
        }

        /// <summary>Creates independent editable load-case metadata from supplied values.</summary>
        /// <param name="id">Case number stored unchanged.</param>
        /// <param name="title">Exact title text, including null/empty/whitespace for manually created records.</param>
        /// <param name="caseType">Classification stored unchanged; defaults to PrimaryLoad.</param>
        /// <param name="loadType">Engineering classification stored unchanged; defaults to None.</param>
        /// <remarks>Does not check native existence or enum membership, activate a case or load assignments. Native query factories separately qualify their inputs and outputs. The record owns property storage but no native object.</remarks>
        public LoadCase(int id, string title, LoadCaseType caseType = LoadCaseType.PrimaryLoad, LoadType loadType = LoadType.None) : this(id)
        {
            Title = title;
            CaseType = caseType;
            Type = loadType;
        }

        /// <summary>Creates a detached primary-case record with a title and engineering type.</summary>
        /// <param name="title">Exact text stored unchanged; null is allowed for manual records.</param>
        /// <param name="loadType">Engineering classification stored unchanged; defaults to Dead.</param>
        /// <remarks>Id remains zero and CaseType is PrimaryLoad. This constructor does not create or modify a STAAD load case.</remarks>
        public LoadCase(string title, LoadType loadType = LoadType.Dead) : this()
        {
            Title = title;
            Type = loadType;
            CaseType = LoadCaseType.PrimaryLoad;
        }

        /// <summary>Gets or sets the detached case identifier.</summary>
        ///       <value>The stored managed value; edits raise notifications but do not submit native changes.</value>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>

        public int Id { get => Get<int>(); set => Set(value); }

        /// <summary>Gets or sets the literal case title without native access.</summary>
        ///       <value>The stored managed value; edits raise notifications but do not submit native changes.</value>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>

        public string Title { get => Get<string>(); set => Set(value); }

        /// <summary>Gets or sets the engineering load classification.</summary>
        ///       <value>The stored managed value; edits raise notifications but do not submit native changes.</value>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>

        public LoadType Type { get => Get<LoadType>(); set => Set(value); }

        /// <summary>Gets or sets the primary/reference/combination/repeat case family.</summary>
        ///       <value>The stored managed value; edits raise notifications but do not submit native changes.</value>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>

        public LoadCaseType CaseType { get => Get<LoadCaseType>(); set => Set(value); }

        /// <summary>Compares this object with another using the type's equality fields.</summary>
        ///       <param name="obj">The object to compare with this instance using the type's documented equality fields.</param>
        ///       <returns>True when the documented condition holds; otherwise false.</returns>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>
        public override bool Equals(object obj)
        {
            return obj is LoadCase other && Id == other.Id && CaseType == other.CaseType && Type == other.Type;
        }

        /// <summary>Calculates a hash from the equality fields of this managed object.</summary>
        ///       <returns>The hash derived from the current equality fields. Do not mutate those fields while the object is a key in a hash collection.</returns>
        ///       <remarks>
        ///         <para>This operation uses detached metadata and does not read or update STAAD. Id, CaseType and Type participate in equality and hashing; Title does not.</para>
        ///       </remarks>
        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ CaseType.GetHashCode() ^ Type.GetHashCode();
        }

        /// <summary>Compares load-case metadata identity, including null operands.</summary>
        /// <param name="left">First case or null.</param>
        /// <param name="right">Second case or null.</param>
        /// <returns>True for the same reference or equal Id, CaseType and Type; title is ignored.</returns>
        public static bool operator ==(LoadCase left, LoadCase right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        /// <summary>Compares whether two case identities differ.</summary>
        /// <param name="left">First case or null.</param>
        /// <param name="right">Second case or null.</param>
        /// <returns>The negation of identity equality.</returns>
        public static bool operator !=(LoadCase left, LoadCase right) => !(left == right);
    }
}

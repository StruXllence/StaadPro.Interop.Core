using StaadPro.Interop.Core.Common;

namespace StaadPro.Interop.Core.Entities
{
    /// <summary>Stores detached analytical plate identity for Output queries.</summary>
    /// <remarks>Supports identity only; does not create/hydrate a STAAD plate, load its corners, compute geometry,
    /// register connectivity or provide the complete original geometry model. Native queries validate Id separately.
    /// Manual edits use managed notifications and never renumber a native plate.</remarks>
    public sealed class Plate : ValidatablePropertyStore, IEntity
    {
        /// <summary>Creates a detached plate record with Id zero and no native binding.</summary>
        public Plate() { }
        /// <summary>Gets or sets the detached analytical plate number.</summary>
        /// <value>The stored integer, initially zero. Output reads require a positive Id captured at invocation.</value>
        public int Id { get => Get<int>(); set => Set(value); }
    }
}

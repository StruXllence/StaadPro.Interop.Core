using StaadPro.Interop.Core.Entities;

namespace StaadPro.Interop.Core.Interfaces
{
    /// <summary>Associates detached plate-result data with supplied load-case and plate metadata.</summary>
    /// <remarks>References are caller-owned and mutable; they are not a native binding, immutable query identity or
    /// proof that model/result data remain current. No units-conversion or coordinate-transform contract is implied.</remarks>
    public interface IPlateStress
    {
        /// <summary>Gets the supplied load-case metadata reference.</summary>
        /// <value>The original reference; manual records may contain null and native queries validate it separately.</value>
        ILoadCase LoadCase { get; }
        /// <summary>Gets the supplied analytical plate metadata reference.</summary>
        /// <value>The original mutable reference, not a cloned plate or a hydrated geometry record.</value>
        Plate Plate { get; }
    }
}

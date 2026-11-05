using StaadPro.Interop.Core.Models;
namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Identifies a domain binding belonging to one OpenSTAAD wrapper.</summary>
    public interface IOSBase
    {
        /// <summary>Gets the wrapper that owns this managed binding.</summary>
        OpenStaadWrapper Wrapper { get; }
    }
}

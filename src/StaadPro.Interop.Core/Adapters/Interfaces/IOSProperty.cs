namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Reserves the Property domain binding; domain operations will be ported separately.</summary>
    /// <remarks>This foundation interface exposes no Property automation functions.</remarks>
    public interface IOSProperty : IOSBase
    {
        /// <summary>Gets the captured native Property object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access is outside managed operation validation. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        dynamic ComObject { get; }
    }
}

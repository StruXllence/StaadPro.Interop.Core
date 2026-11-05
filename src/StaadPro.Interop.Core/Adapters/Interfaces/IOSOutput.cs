namespace StaadPro.Interop.Core.Adapters.Interfaces
{
    /// <summary>Reserves the Output domain binding; domain operations will be ported separately.</summary>
    /// <remarks>This foundation interface exposes no Output automation functions.</remarks>
    public interface IOSOutput : IOSBase
    {
        /// <summary>Gets the captured native Output object for advanced, apartment-aware interoperation.</summary>
        /// <remarks>Raw access is outside managed operation validation. Do not use a captured binding after wrapper refresh/disposal.</remarks>
        dynamic ComObject { get; }
    }
}

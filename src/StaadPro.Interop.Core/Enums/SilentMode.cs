namespace StaadPro.Interop.Core.Enums
{
    /// <summary>Specifies native warning suppression for supported analysis and save dialogs.</summary>
    /// <remarks>Uses the native integer codes 0 and 1. License, filesystem and other error dialogs may still require interaction.</remarks>
    public enum SilentMode
    {
        /// <summary>Disables supported warning suppression using native integer code 0.</summary>
        Disable = 0,

        /// <summary>Enables supported warning suppression using native integer code 1; errors may still display dialogs.</summary>
        Enable = 1
    }
}

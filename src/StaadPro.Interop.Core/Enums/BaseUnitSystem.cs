using System.ComponentModel;
namespace StaadPro.Interop.Core.Enums
{
    /// <summary>Identifies the native base-unit family cached by a wrapper.</summary>
    public enum BaseUnitSystem
    {
        /// <summary>The native base-unit value is unavailable.</summary>
        [Description("Unknown")] Unknown = -1,
        /// <summary>Native code 1: imperial (English) base units.</summary>
        [Description("English")] Imperial = 1,
        /// <summary>Native code 2: metric base units.</summary>
        [Description("Metric")] Metric = 2
    }
}

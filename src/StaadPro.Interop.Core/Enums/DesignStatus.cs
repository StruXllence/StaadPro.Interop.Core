using System.ComponentModel;
namespace StaadPro.Interop.Core.Enums
{
    /// <summary>Represents a qualified native steel-design PASS or FAIL status.</summary>
    public enum DesignStatus
    {
        /// <summary>Native PASS, enum value zero. Does not replace engineering acceptance.</summary>
        [Description("Passed")] Passed = 0,
        /// <summary>Native FAIL, enum value one. Unknown statuses are not mapped to this value.</summary>
        [Description("Failed")] Failed = 1
    }
}

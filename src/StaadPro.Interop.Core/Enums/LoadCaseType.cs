namespace StaadPro.Interop.Core.Enums
{
    /// <summary>
    /// Specifies the classification of a STAAD.Pro load case.
    /// </summary>
    public enum LoadCaseType
    {
        /// <summary>ReferenceLoad; native code 1.</summary>
        ReferenceLoad = 1,
        /// <summary>PrimaryLoad; native code 2.</summary>
        PrimaryLoad = 2,
        /// <summary>LoadCombination; native code 3.</summary>
        LoadCombination = 3,
        /// <summary>RepeatLoad; native code 4.</summary>
        RepeatLoad = 4
    }
}

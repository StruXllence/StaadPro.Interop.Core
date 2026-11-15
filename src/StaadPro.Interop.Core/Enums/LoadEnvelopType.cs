namespace StaadPro.Interop.Core.Enums
{
    /// <summary>Native load-envelope classification codes returned by OpenSTAAD.</summary>
    public enum LoadEnvelopType
    {
        /// <summary>No envelope classification, native code 0.</summary>
        None = 0,
        /// <summary>Stress envelope, native code 1.</summary>
        Stress = 1,
        /// <summary>Serviceability envelope, native code 2.</summary>
        Serviceability = 2,
        /// <summary>Column envelope, native code 3.</summary>
        Column = 3,
        /// <summary>Connection envelope, native code 4.</summary>
        Connection = 4,
        /// <summary>Strength envelope, native code 5.</summary>
        Strength = 5,
        /// <summary>Temporary envelope, native code 6.</summary>
        Temporary = 6
    }
}

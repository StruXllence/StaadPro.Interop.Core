using System.ComponentModel;

namespace StaadPro.Interop.Core.Enums
{
    /// <summary>
    /// Specifies the engineering load type classification for a STAAD.Pro load case.
    /// </summary>
    public enum LoadType
    {
        /// <summary>Dead; native code 0.</summary>
        Dead = 0,
        /// <summary>Live; native code 1.</summary>
        Live = 1,
        /// <summary>RoofLive; native code 2.</summary>
        [Description("Roof Live")]
        RoofLive = 2,
        /// <summary>Wind; native code 3.</summary>
        Wind = 3,
        /// <summary>SeismicH; native code 4.</summary>
        [Description("Seismic Horizontal")]
        SeismicH = 4,
        /// <summary>SeismicV; native code 5.</summary>
        [Description("Seismic Vertical")]
        SeismicV = 5,
        /// <summary>Snow; native code 6.</summary>
        Snow = 6,
        /// <summary>Fluids; native code 7.</summary>
        Fluids = 7,
        /// <summary>Soil; native code 8.</summary>
        Soil = 8,
        /// <summary>Rain; native code 9.</summary>
        Rain = 9,
        /// <summary>Ponding; native code 10.</summary>
        Ponding = 10,
        /// <summary>Dust; native code 11.</summary>
        Dust = 11,
        /// <summary>Traffic; native code 12.</summary>
        Traffic = 12,
        /// <summary>Temp; native code 13.</summary>
        Temp = 13,
        /// <summary>Imperfection; native code 14.</summary>
        Imperfection = 14,
        /// <summary>Accidental; native code 15.</summary>
        Accidental = 15,
        /// <summary>Flood; native code 16.</summary>
        Flood = 16,
        /// <summary>Ice; native code 17.</summary>
        Ice = 17,
        /// <summary>WindIce; native code 18.</summary>
        [Description("Wind on Ice")]
        WindIce = 18,
        /// <summary>CraneHook; native code 19.</summary>
        [Description("Crane Hook")]
        CraneHook = 19,
        /// <summary>Mass; native code 20.</summary>
        Mass = 20,
        /// <summary>Gravity; native code 21.</summary>
        Gravity = 21,
        /// <summary>Push; native code 22.</summary>
        Push = 22,
        /// <summary>None; native code 23.</summary>
        None = 23
    }
}
